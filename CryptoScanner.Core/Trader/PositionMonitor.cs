using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Exchange.Altrady;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;

using Dapper.Contrib.Extensions;

using System.Diagnostics;

namespace CryptoScanner.Core.Trader;

public class PositionMonitor : IDisposable
{
    public CryptoSymbol Symbol { get; set; }
    private static readonly SemaphoreSlim Semaphore = new(1);

    // De laatste gesloten 1m candle
    public CryptoCandle LastCandle1m { get; set; }
    // De sluittijd van deze candle (als unixtime) - De CurrentTime bij backtesting
    public CandleTime LastCandle1mCloseTime { get; set; }
    // De sluittijd van deze candle (als DateTime) - De CurrentTime bij backtesting
    public DateTime LastCandle1mCloseTimeDate { get; set; }
    public CryptoDatabase Database { get; set; } = new();
    public bool PauseBecauseOfTradingRules { get; set; } = false;
    public uint BaseIntervalDuration { get; }


    public PositionMonitor(CryptoSymbol symbol, CryptoCandle lastCandle1m, uint baseIntervalDuration = 1)
    {
        Symbol = symbol;
        LastCandle1m = lastCandle1m;
        BaseIntervalDuration = baseIntervalDuration;

        // The last final 1m candle
        LastCandle1mCloseTime = lastCandle1m.OpenTime + baseIntervalDuration;
        LastCandle1mCloseTimeDate = LastCandle1mCloseTime.ToDateTime();

        Database.Open();
    }

    /// <summary>
    /// A PositionMonitor is created per 1m candle (live) or per tick (emulator) and owns its own
    /// database connection, so it must be disposed deterministically instead of leaving the SQLite
    /// connection to the finalizer (a constant stream of leaked handles with hundreds of symbols).
    /// </summary>
    public void Dispose()
    {
        Database.Dispose();
        GC.SuppressFinalize(this);
    }


    private bool CanOpenAdditionalDca(CryptoPosition position, out CryptoPositionStep? step,
        out decimal percentage, out decimal dcaPrice, out string reaction)
    {
        dcaPrice = 0;
        percentage = 0;

        if (position.SlMovedToBreakEven)
        {
            step = null;
            percentage = 0;
            reaction = "";
            return false;
        }

        if (position.PartCount >= GlobalData.Settings.Trading.DcaList.Count)
        {
            step = null;
            percentage = 0;
            //reaction = $"Geen bijkopen nodig vanwege MAX DCA count (partcount={position.PartCount} dcaCount=GlobalData.Settings.Trading.DcaList.Count)";
            reaction = ""; // ignore
            return false;
        }

        // Een DCA zonder een voorgaande entry is onmogelijk
        if (!position.EntryPrice.HasValue || position.EntryPrice.Value == 0 || position.Invested == 0)
        {
            step = null;
            percentage = 0;
            //reaction = "Geen 1e entry gevonden (1)";
            reaction = ""; // ignore
            return false;
        }

        // long-positie: Retourneer de laagste buy order van een niet afgesloten part.
        // short-positie: Retourneer de hoogste sell order van een niet afgesloten part.
        step = null;
        CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
        foreach (CryptoPositionPart part in position.PartList.Values.ToList())
        {
            // Afgesloten DCA parts sluiten we uit (omdat we zogenaamde jojo's uitvoeren)
            if (part.Purpose != CryptoPartPurpose.TakeProfit && !part.CloseTime.HasValue)
            {
                foreach (CryptoPositionStep stepX in part.StepList.Values.ToList())
                {
                    // Voor de zekerheid de Status=Filled erbij (het kan geen kwaad)
                    if (stepX.Side == entryOrderSide && stepX.CloseTime.HasValue && stepX.Status.IsFilled())
                    {
                        if (position.Side == CryptoTradeSide.Long)
                        {
                            if (step == null || stepX.Price < step.Price)
                                step = stepX;
                        }
                        else
                        {
                            if (step == null || stepX.Price > step.Price)
                                step = stepX;
                        }
                    }
                }
            }
        }
        // Een DCA zonder een voorgaande entry is onmogelijk
        if (step == null)
        {
            percentage = 0;
            reaction = "Geen 1e entry gevonden (2)";
            return false;
        }

        // Puspose: Event driven DCA, but we already have enough tiny problems..
        //// At least one cooldown period must have elapsed since the last filled entry.
        //// step.CloseTime is always set here because the search loop above filters on CloseTime.HasValue.
        //if (step.CloseTime!.Value.AddMinutes(GlobalData.Settings.Trading.GlobalBuyCooldownTime) > LastCandle1mCloseTimeDate)
        //{
        //    reaction = "het is te vroeg voor een bijkoop vanwege de cooldown";
        //    Symbol.ClearSignals();
        //    return false;
        //}



        // The next DCA target is calculated as a fixed % from the original entry price.
        // If that target is above (long) or below (short) the lowest already-filled entry,
        // a new DCA would be placed at a worse price than what we already have — skip it.
        // This guards against triggering an unnecessary DCA after a pause or restart.
        decimal entryPrice = position.EntryPrice.Value;
        var dcaEntry = GlobalData.Settings.Trading.DcaList[position.PartCount];
        decimal diffPrice = entryPrice * Math.Abs(dcaEntry.Percentage) / 100m;
        if (position.Side == CryptoTradeSide.Long)
        {
            dcaPrice = entryPrice - diffPrice;
            if (dcaPrice >= step.Price)
            {
                reaction = $"dca {percentage} is niet nodig (long)";
                return false;
            }
        }
        else
        {
            dcaPrice = entryPrice + diffPrice;
            if (dcaPrice <= step.Price)
            {
                reaction = $"dca {percentage} is niet nodig (short)";
                return false;
            }
        }


        // Is er een openstaande DCA zonder enige entries of openstaande entry?
        foreach (CryptoPositionPart part in position.PartList.Values.ToList())
        {
            if (part.Purpose == CryptoPartPurpose.Dca && !part.CloseTime.HasValue)
            {
                //int openOrders = 0;
                foreach (CryptoPositionStep stepX in part.StepList.Values.ToList())
                {
                    if (stepX.Side == entryOrderSide && stepX.Status == CryptoOrderStatus.New)
                    {
                        //openOrders += 1;

                        // Er staan een buy order klaar, dus openen we geen nieuwe DCA
                        if (stepX.Trailing == CryptoTrailing.None && stepX.OrderType == CryptoOrderType.Limit)
                        {
                            //reaction = "de positie heeft al een openstaande DCA";
                            reaction = "";
                            return false;
                        }

                        // We zijn aan het trailen, dus openen we geen nieuwe DCA
                        // (trailing DCA is niet meer ondersteund, zie CryptoTrailing)
                        //if (stepX.Trailing == CryptoTrailing.Trailing && stepX.OrderType == CryptoOrderType.StopLimit)
                        //{
                        //    reaction = "de positie heeft al een openstaande trailing DCA";
                        //    return false;
                        //}
                    }
                }

                // Er is al een DCA gemaakt maar het heeft nog geen orders of is gepauseerd vanwege barometer of andere oorzaken..
                if (part.StepList.Count == 0) // || openOrders == 0
                {
                    //reaction = "de positie heeft al een openstaande DCA";
                    reaction = "";
                    return false;
                }

            }
        }

        GlobalData.AddTextToLogTab($"{position.Symbol.Name} DCA partcount={position.PartCount} count={GlobalData.Settings.Trading.DcaList.Count} dca.perc={dcaEntry.Percentage}");
        reaction = "";
        return true;
    }



    private async Task CreateOrExtendPositionAsync()
    {
        string? lastPrice = Symbol.LastPrice?.ToString(Symbol.PriceDisplayFormat);
        string text = "Monitor " + Symbol.Name + " price=" + lastPrice;


        string reaction;
        // **************************************************
        // Global checks zoals barometer, active bot etc..
        // **************************************************

        // Als de bot niet actief is dan ook geen monitoring (queue leegmaken)
        // Blijkbaar is de bot dan door de gebruiker uitgezet, verwijder de signalen
        if (!GlobalData.Settings.Trading.Active)
        {
            //reaction = "trade-bot deactivated";
            //GlobalData.AddTextToLogTab($"{text} {reaction} (removed)");
            Symbol.ClearSignals();
            return;
        }

        // we doen (momenteel) alleen long posities
        if (!Symbol.LastPrice.HasValue)
        {
            reaction = "symbol price null";
            GlobalData.AddTextToLogTab($"{text} {reaction} (removed)");
            Symbol.ClearSignals();
            return;
        }

        // Om te voorkomen dat we te snel achter elkaar in dezelfde munt stappen
        // Compare against the candle's CLOSE time: that is the moment this decision is taken, and
        // it is what LastTradeDate itself is written with. Comparing against the OPEN time made the
        // cooldown outlive itself by one base interval - 4 minutes on a 5m run, 14 on a 15m run -
        // so a coarser base interval discarded signals that a 1m run acted on.
        // A losing trade can buy a longer wait than the normal cooldown. It is counted from the
        // close of that losing position, not from the last fill, and it is a separate clock: a DCA
        // or take profit fill afterwards must not push it forward or cut it short.
        bool inLossCooldown = GlobalData.Settings.Trading.LossCooldownTime > 0
            && Symbol.LastLossDate.HasValue
            && Symbol.LastLossDate.Value.AddMinutes(GlobalData.Settings.Trading.LossCooldownTime) > LastCandle1mCloseTimeDate;

        if (inLossCooldown
            || (Symbol.LastTradeDate.HasValue && Symbol.LastTradeDate?.AddMinutes(GlobalData.Settings.Trading.GlobalBuyCooldownTime) > LastCandle1mCloseTimeDate))
        {
            // Bypass cooldown when an unfilled position exists — no actual trade took place yet,
            // so a newer signal should be allowed to replace the waiting entry order.
            if (!GlobalData.ActiveExchange!.Data.PositionList.TryGetValue(Symbol.Name, out var cooldownPos)
                || cooldownPos.Status != CryptoPositionStatus.Waiting)
            {
                reaction = inLossCooldown ? "is in cooldown after a loss" : "is in cooldown";
                GlobalData.AddTextToLogTab($"{text} {reaction} (removed)");
                Symbol.ClearSignals();
                return;
            }
        }

        // Check the trading rules of the user (a quick drop of a symbol causes a pause)
        if (!TradingRules.CheckTradingRules(GlobalData.ActiveExchange!.Data.PauseTrading, LastCandle1m.OpenTime, BaseIntervalDuration))
        {
            reaction = $"paused because of {GlobalData.ActiveExchange!.Data.PauseTrading.Text}";
            GlobalData.AddTextToLogTab($"{text} {reaction} (removed)");
            Symbol.ClearSignals();
            return;
        }


        //GlobalData.AddTextToLogTab("Monitor " + symbol.Name); te druk in de log

        // ***************************************************************************
        // Per interval kan een signaal aanwezig zijn, regel de aankoop of de bijkoop
        // ***************************************************************************
        foreach (CryptoSymbolInterval symbolInterval in Symbol.Data.SymbolIntervalList)
        {
            CryptoInterval interval = symbolInterval.Interval!;
            // alleen voor de intervallen waar de candle net gesloten is
            // (0 % 180 = 0, 60 % 180 = 60, 120 % 180 = 120, 180 % 180 = 0)
            if (LastCandle1mCloseTime % interval.Duration == 0)
            {
                foreach (CryptoSignal signal in symbolInterval.SignalList.ToList())
                {
                    text = "Monitor " + signal.DisplayText + " price=" + lastPrice;


                    // Does the user want to trade on this interval
                    if (!TradingConfig.Trading[signal.Side].IntervalPeriod.ContainsKey(interval.IntervalPeriod))
                    {
                        GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " not trading on this interval (removed)");
                        symbolInterval.SignalList.Remove(signal);
                        continue;
                    }

                    // Does the user want to trade with this strategy
                    if (signal.Strategy == null || !TradingConfig.Trading[signal.Side].Strategy.ContainsKey(signal.Strategy))
                    {
                        GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " not trading on this strategy (removed)");
                        symbolInterval.SignalList.Remove(signal);
                        continue;
                    }

                    // Er zijn (technisch) niet altijd candles aanwezig
                    if (symbolInterval.CandleList.Count == 0)
                    {
                        GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " no candles on this interval (removed)");
                        symbolInterval.SignalList.Remove(signal);
                        continue;
                    }

                    // Get the most recent candle (and check a lot of things)
                    CandleTime openTime = LastCandle1mCloseTime - interval.Duration;
                    var result = IndicatorEngine.CalculateIndicatorsForInterval(Symbol, interval, openTime, symbolInterval.IntervalPeriod);
                    if (!result.success)
                    {
                        GlobalData.AddTextToLogTab($"Monitor {Symbol.Name} unable to prepare indicators for interval {interval.Name} (removed)");
                        symbolInterval.SignalList.Remove(signal);
                        continue;
                    }

                    // Create the strategy and fill all the properties (todo: not a neat solution)
                    SignalCreateBase? algorithm = RegisterAlgorithms.GetAlgorithm(signal.Side, signal.Strategy);
                    if (algorithm == null)
                    {
                        GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " unknown algorithm (removed)");
                        symbolInterval.SignalList.Remove(signal);
                        continue;
                    }
                    // Fill the missing properties
                    algorithm.Symbol = Symbol;
                    algorithm.Interval = signal.Interval;
                    algorithm.SymbolInterval = Symbol.GetSymbolInterval(signal.Interval.IntervalPeriod);
                    algorithm.CandleLast = result.candle!;


                    if (algorithm.GiveUp(signal))
                    {
                        GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " " + algorithm.ExtraText + " giveup (removed)");
                        symbolInterval.SignalList.Remove(signal);
                        continue;
                    }

                    if (!algorithm.AllowStepIn(signal))
                    {
                        GlobalData.AddTextToLogTab(text + " " + algorithm.ExtraText + "  (not allowed yet, waiting)");
                        continue;
                    }


                    //******************************************
                    // GO!GO!GO! kan een aankoop of bijkoop zijn
                    // (kan aangeroepen worden op meerdere TF's)
                    //******************************************

                    {
                        CryptoPosition? position = PositionTools.HasPosition(GlobalData.ActiveExchange!, Symbol);

                        // Replace a Waiting (unfilled) position with the newer signal
                        if (position != null && position.Status == CryptoPositionStatus.Waiting)
                        {
                            GlobalData.AddTextToLogTab($"{text} replacing unfilled {position.Side} position {position.Id} with new signal");

                            bool allCancelled = true;
                            foreach (CryptoPositionPart part in position.PartList.Values.ToList())
                            {
                                foreach (CryptoPositionStep step in part.StepList.Values.ToList())
                                {
                                    if (step.Status == CryptoOrderStatus.New)
                                    {
                                        var (cancelled, _) = await TradeTools.CancelOrder(Database, position, part, step,
                                            LastCandle1mCloseTimeDate, CryptoOrderStatus.PositionClosed, "replaced by new signal");
                                        if (!cancelled)
                                            allCancelled = false;
                                    }
                                }
                            }

                            if (allCancelled)
                            {
                                position.Status = CryptoPositionStatus.Cancelled;
                                position.CloseTime = LastCandle1mCloseTimeDate;
                                Database.Connection.Update(position);

                                // Remove without ClearSignals — signals are needed for the replacement position
                                GlobalData.ActiveExchange!.Data.PositionList.TryRemove(position.Symbol.Name, out _);
                                GlobalData.SendMvvmMessage(new PositionIsClosedMessage(position));
                                GlobalData.PositionClosed?.Invoke(position);
                                position = null;
                            }
                            else
                            {
                                // Cancel failed (order may have been filled in the meantime) — let normal processing handle it
                                position.ForceCheckPosition = true;
                            }
                        }

                        if (position == null)
                        {
                            if (GlobalData.Settings.Trading.DisableNewPositions)
                            {
                                reaction = "openen van nieuwe posities niet toegestaan";
                                GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
                                Symbol.ClearSignals();
                                return;
                            }


                            // Controles die noodzakelijk zijn voor een entry
                            // (inclusief de overhead van controles van de analyser)
                            // Deze code alleen uitvoeren voor de entry (niet een dca bijkoop)

                            // Is de barometer goed genoeg dat we willen traden?
                            if (!TradingRules.CheckBarometerConditions(GlobalData.ActiveExchange!, Symbol.Quote, signal.Side, LastCandle1m.OpenTime, 60, out reaction))
                            {
                                GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
                                Symbol.ClearSignals();
                                return;
                            }

                            // Staat op de whitelist (kan leeg zijn)
                            if (!SymbolTools.CheckSymbolWhiteListOversold(Symbol, signal.Side, out reaction))
                            {
                                GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
                                Symbol.ClearSignals();
                                return;
                            }

                            // Staat niet in de blacklist
                            if (!SymbolTools.CheckSymbolBlackListOversold(Symbol, signal.Side, out reaction))
                            {
                                GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
                                Symbol.ClearSignals();
                                return;
                            }

                            // Heeft de munt genoeg 24h volume
                            if (!SymbolTools.CheckValidMinimalVolume(Symbol, LastCandle1m.OpenTime, BaseIntervalDuration, out reaction))
                            {
                                GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
                                Symbol.ClearSignals();
                                return;
                            }

                            // Heeft de munt een redelijke prijs
                            if (!SymbolTools.CheckValidMinimalPrice(Symbol, out reaction))
                            {
                                GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
                                Symbol.ClearSignals();
                                return;
                            }

                            // Munten waarvan de ticksize perc nogal groot is (barcode charts)
                            if (!SymbolTools.CheckMinimumTickPercentage(Symbol, out reaction))
                            {
                                GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
                                Symbol.ClearSignals();
                                return;
                            }

                            // Controle of bepaalde intervallen in een uptrend of in een downtrend zijn
                            if (!PositionTools.ValidTrendConditions(signal.Symbol, signal.Interval, TrendType.Primary, TradingConfig.Trading[signal.Side].Trend, out reaction))
                            {
                                if (TradingConfig.Trading[signal.Side].TrendLog)
                                    GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
                                symbolInterval.SignalList.Remove(signal);
                                continue;
                            }

                            // Filter op de markettrend waarvan je wil dat die qua perc bullisch of bearisch zijn
                            if (!PositionTools.ValidMarketTrendConditions(signal.Symbol, TrendType.Primary, TradingConfig.Trading[signal.Side].MarketTrend, out reaction))
                            {
                                GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
                                symbolInterval.SignalList.Remove(signal);
                                continue;
                            }

                            // Additional INTERSECT filter on the secondary market trend (lower-timeframe scope).
                            // Allows catching divergences such as Primary +100 / Secondary -63 where the lower
                            // timeframe has already rolled over.
                            if (!PositionTools.ValidMarketTrendConditions(signal.Symbol, TrendType.Secondary, TradingConfig.Trading[signal.Side].MarketTrendSecondary, out reaction))
                            {
                                GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
                                symbolInterval.SignalList.Remove(signal);
                                continue;
                            }

                            // Alleen deze 2 ondersteunen we op dit moment (bool CanTrade introduceren ofzo)
                            // Voorlopig alleen traden op Bybit Spot en Futures (alleen daar kan ik het testen)
                            if (!GlobalData.ActiveExchange!.IsSupported)
                            {
                                GlobalData.AddTextToLogTab(text + $" trader not supported on {GlobalData.ActiveExchange.Name} (removed)");
                                Symbol.ClearSignals();
                                return;
                            }


                            // Locking omdat het aantal posities over de slot limits kunnen gaan
                            // (er zijn x threads tegelijk met deze code aan de gang)
                            await Semaphore.WaitAsync();
                            try
                            {
                                // We willen 1 slot per symbol en x slots voor de long en shorts
                                if (!SymbolTools.CheckAvailableSlots(GlobalData.ActiveExchange, Symbol, signal.Side, out reaction))
                                {
                                    GlobalData.AddTextToLogTab($"{text} {reaction} (removed)");
                                    Symbol.ClearSignals();
                                    return;
                                }


                                // GetSymbolData available assets from the exchange (as late as possible because of webcall)
                                var resultFetchAssets = AssetTools.FetchAssets(GlobalData.ActiveExchange, true);
                                if (!resultFetchAssets.success)
                                {
                                    GlobalData.AddTextToLogTab($"{text} {resultFetchAssets.reaction}");
                                    Symbol.ClearSignals();
                                    return;
                                }

                                // Enough stuff to take position? + entryAmount
                                var resultAvailableAssets = AssetTools.CheckAvailableAssets(GlobalData.ActiveExchange, Symbol);
                                if (!resultAvailableAssets.success)
                                {
                                    GlobalData.AddTextToLogTab($"{text} {resultAvailableAssets.reaction}");
                                    Symbol.ClearSignals();
                                    return;
                                }
                                var info = resultAvailableAssets.info; // short alias
                                decimal entryQuote = resultAvailableAssets.entryQuoteAsset;

                                // Check the assets, the symbol limits..

                                // Bepaal het entry bedrag. Strategies that supplied an explicit entry
                                // price (OverrideSignalPrice → signal.SignalPrice, e.g. vbs's band,
                                // CHoCH/BOS break) enter at that level; otherwise at the current market price.
                                decimal entryPrice = (signal.EntryPriceOverridden && signal.SignalPrice > 0
                                    ? signal.SignalPrice
                                    : Symbol.LastPrice.Value).Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
                                decimal entryBase = entryQuote / entryPrice;
                                entryBase = entryBase.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);
                                entryBase = TradeTools.CorrectEntryQuantityIfWayLess(Symbol, entryQuote, entryBase, entryPrice);

                                // Its rounded towards zero
                                if (entryBase <= 0)
                                {
                                    GlobalData.AddTextToLogTab(text + $" because of minimum quantity {Symbol.QuantityMinimum} en aankoopbedrag {entryQuote} lukt de aankoop niet");
                                    Symbol.ClearSignals();
                                    return;
                                }

                                // Below the minimum allowed quantity
                                if (entryBase == Symbol.QuantityMinimum)
                                {
                                    GlobalData.AddTextToLogTab(text + $" because of minimum quantity {entryBase} < {Symbol.QuantityMinimum} lukt de aankoop niet (te weinig)");
                                    Symbol.ClearSignals();
                                    return;
                                }

                                // Below the minimum allowed value
                                if (Symbol.QuoteValueMinimum > 0 && entryQuote < Symbol.QuoteValueMinimum)
                                {
                                    GlobalData.AddTextToLogTab(text + $" because of minimum value {entryQuote} < {Symbol.QuoteValueMinimum} lukt de aankoop niet (te weinig)");
                                    Symbol.ClearSignals();
                                    return;
                                }

                                if (GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.RealTrading && GlobalData.ActiveExchange.TradingType == CryptoTradingType.Spot)
                                {
                                    if (info.QuoteFree == 0 || entryBase * entryPrice > info.QuoteTotal)
                                    {
                                        GlobalData.AddTextToLogTab($"{text} not enough assets available for trade entry {entryBase * entryPrice} > {info.QuoteTotal})");
                                        Symbol.ClearSignals();
                                        return;
                                    }
                                }


                                // Create position + entry part
                                position = PositionTools.CreatePosition(Symbol, signal.Strategy, signal.Side,
                                    signal.EventText, symbolInterval, LastCandle1mCloseTimeDate);
                                PositionTools.AddSignalProperties(position, signal);
                                Database.Connection.Insert(position);
                                PositionTools.AddPosition(position);
                                PositionTools.ExtendPosition(Database, position, CryptoPartPurpose.Entry,
                                    signal.Interval, signal.Strategy, entryPrice, LastCandle1mCloseTimeDate);

                                // Off-by-one diagnostic: compare the entry candle to the signal's trigger
                                // candle. delayCandles == 1 means we entered at the trigger candle's close
                                // (= next candle open, on time); == 2 means one candle too late.
                                if (TraderTrace.TimingEnabled(Symbol))
                                {
                                    double delayCandles = signal.Interval.Duration > 0
                                        ? (LastCandle1mCloseTimeDate - signal.OpenDate).TotalMinutes / signal.Interval.Duration
                                        : 0;
                                    TraderTrace.Timing(Symbol,
                                        $"entry  {Symbol.Name} {signal.Interval.Name} {signal.Side} {signal.Strategy} " +
                                        $"trigger.open={signal.OpenDate:yyyy-MM-dd HH:mm} trigger.close={signal.CloseDate:HH:mm} " +
                                        $"entry={LastCandle1mCloseTimeDate:HH:mm} clock={GlobalData.Clock.UtcNow:HH:mm} " +
                                        $"entryPrice={entryPrice} delayCandles={delayCandles:0.##}");
                                }
                            }
                            finally
                            {
                                Semaphore.Release();
                            }

                            // Send the created position to the ViewModel
                            GlobalData.SendMvvmMessage(new PositionIsCreatedMessage(position));
                            GlobalData.PositionCreated?.Invoke(position);
                            return;
                        }
                        else
                        {
                            // We have an open position in this symbol (less checks)

                            // long positie: Alleen bijkopen als we ONDER de break-even prijs zitten
                            // short positie: Alleen bijkopen als we BOVEN de break-even prijs zitten
                            if ((position.Side == CryptoTradeSide.Long && signal.SignalPrice < position.BreakEvenPrice) ||
                                (position.Side == CryptoTradeSide.Short && signal.SignalPrice > position.BreakEvenPrice))
                            {
                                // En een paar aanvullende condities...
                                if (!CanOpenAdditionalDca(position, out CryptoPositionStep? step, out decimal percentage, out decimal dcaPrice, out reaction))
                                {
                                    if (reaction != "")
                                    {
                                        GlobalData.AddTextToLogTab($"{text} {symbolInterval.Interval.Name} {reaction} (removed)");
                                    }
                                    Symbol.ClearSignals();
                                    return;
                                }

                                // Zo laat mogelijk controleren vanwege extra calls naar de exchange
                                var (success, reaction2) = AssetTools.FetchAssets(GlobalData.ActiveExchange);
                                if (!success)
                                {
                                    GlobalData.AddTextToLogTab(text + " " + reaction2);
                                    Symbol.ClearSignals();
                                    return;
                                }

                                var resultCheckAssets = AssetTools.CheckAvailableAssets(GlobalData.ActiveExchange!, Symbol);
                                if (!resultCheckAssets.success)
                                {
                                    GlobalData.AddTextToLogTab(text + " " + resultCheckAssets.reaction);
                                    Symbol.ClearSignals();
                                    return;
                                }

                                // Signal the background thread to create the DCA part (all position
                                // mutations are serialized on the background thread under the semaphore).
                                position.PendingDcaSignal = new(signal.Interval, signal.Strategy, dcaPrice, LastCandle1mCloseTimeDate);
                                return;
                            }
                        }
                    }
                }
            }
        }
    }



    private decimal CorrectBuyOrDcaPrice(CryptoPosition position, decimal price)
    {
        if (position.Side == CryptoTradeSide.Long)
        {
            // Gecorrigeerd op de laagste open of close van de candle
            decimal x = Math.Min(LastCandle1m.Close, LastCandle1m.Open);
            if (x < price)
                price = x - position.Symbol.PriceTickSize;

            // Gecorrigeerd op de laatst bekende prijs
            if (position.Symbol.LastPrice.HasValue)
            {
                x = (decimal)position.Symbol.LastPrice;
                if (x < price)
                    price = x - position.Symbol.PriceTickSize;
            }
        }
        else
        {
            // Gecorrigeerd op de hoogste open of close van de candle
            decimal x = Math.Max(LastCandle1m.Close, LastCandle1m.Open);
            if (x > price)
                price = x + position.Symbol.PriceTickSize;

            // Gecorrigeerd op de laatst bekende prijs
            if (position.Symbol.LastPrice.HasValue)
            {
                x = (decimal)position.Symbol.LastPrice;
                if (x > price)
                    price = x + position.Symbol.PriceTickSize;
            }
        }

        return price;
    }



    /// <summary>
    /// Absolute TP price for one level's profit distance (%), anchored on
    /// position.TpGridAnchorPrice (Entry+Dca fills only, fee-corrected) - NOT on
    /// position.BreakEvenPrice, which also shifts every time a sibling TP level fills (it banks the
    /// realized profit into Returned and shrinks Quantity), causing every still-open TP level to be
    /// repriced and re-placed. TpGridAnchorPrice does shift on a new DCA fill, same as
    /// GetMissingFixedPercentageDcaPrices below already assumes.
    /// multiplier = +1 long / -1 short, so the TP sits above for a long, below for a short.
    /// </summary>
    private decimal CalculateTpPrice(CryptoPosition position, decimal percentage)
    {
        int multiplier = position.Side == CryptoTradeSide.Long ? +1 : -1;
        decimal entryAnchor = position.TpGridAnchorPrice;
        decimal price = entryAnchor + (multiplier * entryAnchor * (percentage / 100));
        return price.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
    }

    private (decimal? stop, decimal? limit) CalculateSlPrices(CryptoPosition position)
    {
        // Only paper-trade mode supports SL orders (real trading would need OCO, not yet implemented).
        if (GlobalData.Settings.Trading.TradeVia != CryptoTradeVia.PaperTrade && GlobalData.Settings.Trading.TradeVia != CryptoTradeVia.PaperTradingAndAltrady)
            return (null, null);

        // Find the most extreme DCA step price (lowest buy for long, highest sell for short)
        decimal? extremeDcaPrice = FindExtremeDcaPrice(position);

        var input = new StopLossCalculator.SlInput
        {
            Side = position.Side,
            SlPercentage = position.SlPercentage,
            EntryPrice = position.EntryPrice!.Value,
            ExtremeDcaPrice = extremeDcaPrice,
            GlobalStopLossPercentage = GlobalData.Settings.Trading.StopLossPercentage,
            GlobalStopLossLimitPercentage = GlobalData.Settings.Trading.StopLossLimitPercentage,
        };
        var result = StopLossCalculator.Calculate(input);

        ScannerLog.Logger.Trace(
            $"PositionMonitor.CalculateSlPrices {position.Symbol.Name} {position.Side}: " +
            $"source={result.Source} (SlPct={position.SlPercentage}) " +
            $"stop={result.Stop} limit={result.Limit} " +
            $"(StopPct={GlobalData.Settings.Trading.StopLossPercentage} LimitPct={GlobalData.Settings.Trading.StopLossLimitPercentage})");

        // Clamp to symbol tick/min/max
        decimal? stop = result.Stop?.Clamp(position.Symbol.PriceMinimum, position.Symbol.PriceMaximum, position.Symbol.PriceTickSize);
        decimal? limit = result.Limit?.Clamp(position.Symbol.PriceMinimum, position.Symbol.PriceMaximum, position.Symbol.PriceTickSize);

        // Profit lock: once the position has reached MoveSlToBreakEvenPercentage in profit,
        // move the SL to BE + that percentage to protect the profit (sticky — the flag never
        // resets, so a later pullback cannot loosen it). Open DCA orders are cancelled separately
        // in CancelOrdersIfClosedOrTimeoutOrReposition once the flag is set.
        if (GlobalData.Settings.Trading.MoveSlToBreakEven
            && position.BreakEvenPrice > 0)
        {
            int multiplier = position.Side == CryptoTradeSide.Long ? +1 : -1;
            decimal lockPct = GlobalData.Settings.Trading.MoveSlToBreakEvenPercentage;

            if (!position.SlMovedToBreakEven)
            {
                decimal favorable = position.Side == CryptoTradeSide.Long ? LastCandle1m.High : LastCandle1m.Low;
                decimal profitPct = multiplier * (favorable - position.BreakEvenPrice) / position.BreakEvenPrice * 100m;
                if (profitPct >= lockPct)
                {
                    position.SlMovedToBreakEven = true;
                    GlobalData.AddTextToLogTab($"{position.Symbol.Name} profit lock: SL moved to BE+{lockPct:N2}% (profit reached {profitPct:N2}%)");
                }
            }

            if (position.SlMovedToBreakEven)
            {
                decimal lockStop = (position.BreakEvenPrice + multiplier * position.BreakEvenPrice * lockPct / 100m)
                    .Clamp(position.Symbol.PriceMinimum, position.Symbol.PriceMaximum, position.Symbol.PriceTickSize);
                decimal lockGap = Math.Abs(lockStop * 0.01m);
                decimal lockLimit = (lockStop - multiplier * lockGap)
                    .Clamp(position.Symbol.PriceMinimum, position.Symbol.PriceMaximum, position.Symbol.PriceTickSize);

                // Tighten only: move the stop when there was none, or when the lock level is
                // tighter than the current stop (long: higher is tighter; short: lower is tighter).
                if (stop == null
                    || (multiplier == 1 && lockStop > stop.Value)
                    || (multiplier == -1 && lockStop < stop.Value))
                {
                    stop = lockStop;
                    limit = lockLimit;
                }
            }
        }

        return (stop, limit);
    }


    private static decimal? FindExtremeDcaPrice(CryptoPosition position)
    {
        CryptoOrderSide dcaOrderSide = position.GetEntryOrderSide();

        // problem, if the dca has just been closed we use the global BE and risk being stopped out right away
        //CryptoPositionStep? stepDca = PositionTools.FindOpenStep(position, dcaOrderSide, CryptoPartPurpose.Dca);
        //if (stepDca != null)
        //    breakEven = stepDca.Price;
        CryptoPositionStep? stepDca;
        if (dcaOrderSide == CryptoOrderSide.Buy)
        {
            // Across all DCA parts: entry step with the lowest price (long: lowest dca=buy)
            stepDca = position.PartList.Values
                .Where(p => p.Purpose == CryptoPartPurpose.Dca)
                .SelectMany(p => p.StepList.Values)
                .Where(s => s.Side == dcaOrderSide)
                .MinBy(s => s.Price);
        }
        else
        {
            // Across all DCA parts: entry step with the highest price (short: highest dca sell)
            stepDca = position.PartList.Values
                .Where(p => p.Purpose == CryptoPartPurpose.Dca)
                .SelectMany(p => p.StepList.Values)
                .Where(s => s.Side == dcaOrderSide)
                .MaxBy(s => s.Price);
        }

        return stepDca?.Price;
    }


    private async Task HandleEntryPart(CryptoPosition position, CryptoPositionPart part, CryptoOrderType orderType)
    {
        // Controleer de entry
        CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
        CryptoPositionStep? step = PositionTools.FindPositionPartStep(part, entryOrderSide, false);


        // defaults
        string logText = "placing";
        decimal? entryPrice = null;
        CryptoOrderType entryOrderType = orderType;
        CryptoTrailing trailing = CryptoTrailing.None;

        if (step == null && part.Quantity == 0) // entry
        {
            //entryPrice = CalculateEntryOrDcaPrice(position, part, part.SignalPrice);

            // Wat wordt de prijs? (hoe graag willen we in de trade?)
            switch (orderType)
            {
                case CryptoOrderType.Limit:
                    entryPrice = CorrectBuyOrDcaPrice(position, part.SignalPrice);
                    break;
                case CryptoOrderType.Market:
                    entryPrice = part.Symbol.LastPrice ?? 0;
                    break;
            }
        }




        if (entryPrice.HasValue)
        {
            decimal? stop = null;
            decimal? limit = null;

            // Amount is het instap bedrag (niet de quantity)
            decimal entryValue;
            if (position.Invested == 0)
            {
                // Bepaal het entry bedrag, dat kan een vast bedrag of een perc van de totaal beschikbare quote asset zijn
                decimal currentAssetQuantity = 0;
                if (GlobalData.ActiveExchange!.Data.AssetList.TryGetValue(Symbol.Quote, out var asset) && asset != null)
                    currentAssetQuantity = asset.Total;
                entryValue = TradeTools.GetEntryAmount(Symbol, currentAssetQuantity);
                // No log line here on purpose: ExchangeBase.Dump reports the placed entry order
                // right after this, including the quantity and value AFTER rounding - which is
                // what the user actually needs. Logging the raw amount beforehand only repeated
                // the configured entry amount on every attempt.
            }
            else
            {
                //quoteAmount = position.EntryAmount.Value * part.PartNumber * GlobalData.Settings.Trading.DcaFactor;
                //else
                // Gebaseerd op Zignally, inleg verdubbelen (wat vaak een tekort aan assets geeft)
                //    quoteAmount = (position.Invested - position.Returned + position.Commission) * GlobalData.Settings.Trading.DcaFactor;

                // Als ik nu wist hoe en waar ik dat moest vullen (voor trailing werkt het ook)!
                //if (part.EntryAmount.HasValue)
                //    quoteAmount = part.EntryAmount.Value;
                //else

                // Een gewijzigde dca list is een probleem (qua aantallen en percentages), als we een nieuwe
                // DCA proberen te plaatsen dan moet er uiteindelijk wel een probleem gaan ontstaan (dure vergissing)
                // TODO: Wat is een betere oplossing?
                // Use part.PartNumber (1-based dca level, see TradeTools.CalculateProfitAndBreakEvenPrice) instead of
                // position.PartCount: PartCount only counts already-FILLED dca's, but multiple dca parts can now be
                // open (pending) at the same time when they are all placed at once, so each needs its own factor.
                int dcaLevelIndex = part.PartNumber - 1;
                if (position.EntryAmount.HasValue && dcaLevelIndex >= 0 && dcaLevelIndex < GlobalData.Settings.Trading.DcaList.Count)
                {
                    var dcaEntry = GlobalData.Settings.Trading.DcaList[dcaLevelIndex];
                    // dcaEntry.Factor is a percentage of the entry amount (100 = 1x, 200 = 2x, ...)
                    entryValue = (decimal)position.EntryAmount * dcaEntry.Factor / 100m;
                    GlobalData.AddTextToLogTab($"{position.Symbol.Name} averaging down with DCA {part.PartNumber} of "
                        + $"{GlobalData.Settings.Trading.DcaList.Count} at {dcaEntry.Percentage}% from the entry price: "
                        + $"{entryValue} {Symbol.Quote} extra ({dcaEntry.Factor}% of the {position.EntryAmount} {Symbol.Quote} entry)");
                }
                else
                {
                    // DCA, verdubbelen, gebaseerd op Zignally (geeft snel een asset tekort)
                    entryValue = position.Invested - position.Returned + position.Commission;
                    GlobalData.AddTextToLogTab($"{position.Symbol.Name} WARNING: DCA {part.PartNumber} has no matching level in the "
                        + $"DCA list ({GlobalData.Settings.Trading.DcaList.Count} levels configured) - the list was probably changed "
                        + $"while this position was open. Falling back to doubling the invested amount: {entryValue} {Symbol.Quote} extra");
                }
            }

            if (entryValue <= 0)
            {
                string text = $"{position.Symbol.Name} Er is geen bedrag of percentage ingevuld in de {position.Symbol.Quote} basismunt";
                GlobalData.AddTextToLogTab(text);
                throw new Exception(text);
            }


            decimal price, entryQuantity;
            switch (entryOrderType)
            {
                case CryptoOrderType.Market:
                case CryptoOrderType.Limit:
                    // Voor market en limit nemen we de actionprice (quantiry berekenen)
                    price = (decimal)entryPrice;
                    if (price == 0)
                        price = Symbol.LastPrice ?? 0;
                    price = price.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                    entryQuantity = entryValue / price; // "afgerond"
                    entryQuantity = entryQuantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);
                    if (position.Invested == 0)
                        entryQuantity = TradeTools.CorrectEntryQuantityIfWayLess(Symbol, entryValue, entryQuantity, price);

                    break;
                case CryptoOrderType.StopLimit:
                    //// Voor de stopLimit moet de price en stop berekend worden
                    //price = (decimal)entryPrice + ((decimal)entryPrice * 1.5m / 100); // ergens erboven
                    //price = price.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                    //stop = (decimal)entryPrice;
                    //stop = stop?.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                    //entryQuantity = entryValue / (decimal)stop; // "afgerond"
                    //entryQuantity = entryQuantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);
                    //if (position.Invested == 0)
                    //    entryQuantity = TradeTools.CorrectEntryQuantityIfWayLess(Symbol, entryValue, entryQuantity, price);

                    throw new Exception($"{entryOrderType} niet ondersteund");
                //break;
                default:
                    // Voor de OCO moeten er 3 prijzen berekend worden
                    // De OCO en eventueel andere types worden niet ondersteund
                    // OCO = stoplimit + extra limit die x% onder de stop zit.

                    //price = (decimal)actionPrice + ((decimal)actionPrice * 1.5m / 100); // ergens erboven
                    //price = price.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                    //stop = (decimal)actionPrice;
                    //stop = stop?.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                    //limit = (decimal)actionPrice - ((decimal)actionPrice * 1.5m / 100); // ergens erboven
                    //limit = limit?.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

                    //quantity = quoteAmount / (decimal)stop; // "afgerond"
                    //quantity = quantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);
                    throw new Exception($"{entryOrderType} niet ondersteund");
                    //break;
            }

            if (GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.Altrady)
            {
                part.CloseTime = LastCandle1mCloseTimeDate;
                Database.Connection.Update(part);

                position.Reposition = false;
                position.EntryPrice = price;
                position.EntryAmount = entryQuantity;
                position.UpdateTime = LastCandle1mCloseTimeDate;
                position.CloseTime = LastCandle1mCloseTimeDate;
                position.Status = CryptoPositionStatus.Altrady;
                Database.Connection.Update(position);

                // Only the entry is delegated, see the note in the PaperTradingAndAltrady branch below
                if (part.Purpose == CryptoPartPurpose.Entry)
                {
                    await AltradyWebhook.DelegateControlToAltradyAsync(position);
                    Database.Connection.Update(position);
                }
            }
            else if (GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.PaperTradingAndAltrady)
            {
                // Place the paper-trade order locally
                var exchangeApi = GlobalData.ActiveExchange!.GetApiInstance();
                (bool result, TradeParams? tradeParams) result = await exchangeApi.PlaceOrder(Database,
                    position, part, LastCandle1mCloseTimeDate,
                    entryOrderType, entryOrderSide, entryQuantity, price, stop, limit);
                if (result.tradeParams is not null)
                {
                    if (result.result)
                    {
                        if (part.Purpose == CryptoPartPurpose.Entry)
                        {
                            position.EntryPrice = result.tradeParams.Price;
                            position.EntryAmount = result.tradeParams.QuoteQuantity;
                        }
                        step = PositionTools.CreatePositionStep(position, part, result.tradeParams, trailing);
                        Database.Connection.Insert(step);
                        PositionTools.AddPositionPartStep(part, step);
                        Database.Connection.Update(part);
                        Database.Connection.Update(position);

                        ExchangeBase.Dump(position, result.result, result.tradeParams, logText);

                        PaperAssets.Change(GlobalData.ActiveExchange!, position.Symbol, position.Side, result.tradeParams.OrderSide,
                            step.Status, result.tradeParams.Quantity, result.tradeParams.QuoteQuantity, "HandleEntryPart.PaperAndAltrady");

                        if (step.OrderType == CryptoOrderType.Market)
                        {
                            await PaperTrading.CreatePaperTrade(Database, position, part, step, LastCandle1m.Close, LastCandle1m.OpenTime, BaseIntervalDuration);
                            position.Reposition = false;
                        }

                        // Also delegate control to Altrady, but ONLY for the entry. Opening a position is
                        // the only thing we delegate — a dca, a moved take profit or a close is not
                        // supported. Sending it anyway means Altrady opens a SECOND position with its own
                        // id instead of adding to the first one (2026-08-13: AKEUSDT 12:00:00 id 58624873
                        // and, when the entry filled and the dca was placed, 12:01:11 id 58624883).
                        if (part.Purpose == CryptoPartPurpose.Entry)
                        {
                            await AltradyWebhook.DelegateControlToAltradyAsync(position);
                            Database.Connection.Update(position);
                        }
                    }
                    else
                    {
                        ExchangeBase.Dump(position, result.result, result.tradeParams, logText);
                    }
                }
            }
            else
            {
                // Place the entry order (paper trading, exchange trading or backtest)
                var exchangeApi = GlobalData.ActiveExchange!.GetApiInstance();
                (bool result, TradeParams? tradeParams) result = await exchangeApi.PlaceOrder(Database,
                    position, part, LastCandle1mCloseTimeDate,
                    entryOrderType, entryOrderSide, entryQuantity, price, stop, limit);
                if (result.tradeParams is not null)
                {
                    if (result.result)
                    {
                        if (part.Purpose == CryptoPartPurpose.Entry)
                        {
                            position.EntryPrice = result.tradeParams.Price;
                            position.EntryAmount = result.tradeParams.QuoteQuantity;
                        }
                        step = PositionTools.CreatePositionStep(position, part, result.tradeParams, trailing);
                        Database.Connection.Insert(step);
                        PositionTools.AddPositionPartStep(part, step);
                        Database.Connection.Update(part);
                        Database.Connection.Update(position);

                        ExchangeBase.Dump(position, result.result, result.tradeParams, logText);

                        // Reserve the assets on papertrading/emulator
                        PaperAssets.Change(GlobalData.ActiveExchange!, position.Symbol, position.Side, result.tradeParams.OrderSide,
                            step.Status, result.tradeParams.Quantity, result.tradeParams.QuoteQuantity, "HandleEntryPart.HandleEntryPart");

                        // For paper/backtest: immediately fill a market order.
                        // CreatePaperTrade → HandleTradeAsync → CalculatePositionResultsViaOrders sets Reposition=true.
                        // We reset it here so the outer monitor loop does not try to reposition the TP a second time
                        // in the same candle tick (which would cancel and recreate it unnecessarily).
                        if (GlobalData.Settings.Trading.TradeVia != CryptoTradeVia.RealTrading && step.OrderType == CryptoOrderType.Market)
                        {
                            await PaperTrading.CreatePaperTrade(Database, position, part, step, LastCandle1m.Close, LastCandle1m.OpenTime, BaseIntervalDuration);
                            position.Reposition = false;
                        }
                    }
                    else
                    {
                        ExchangeBase.Dump(position, result.result, result.tradeParams, logText);
                    }
                }
            }
        }
    }



    /// Determine which fixed-percentage DCA levels (GlobalData.Settings.Trading.DcaList) still need to be
    /// created for this position - i.e. levels that have no Dca part yet, filled or still open/pending.
    /// Returns the (fixed % from the original entry price) target price for each missing level, in order.
    private List<decimal> GetMissingFixedPercentageDcaPrices(CryptoPosition position)
    {
        List<decimal> prices = [];

        // Een DCA zonder een voorgaande entry is onmogelijk
        if (!position.EntryPrice.HasValue || position.TpGridAnchorPrice == 0 || position.Invested == 0)
            return prices;

        // Afgesloten DCA parts sluiten we uit (omdat we zogenaamde jojo's uitvoeren, zie CanOpenAdditionalDca)
        int existingDcaParts = position.PartList.Values.Count(p => p.Purpose == CryptoPartPurpose.Dca && !p.CloseTime.HasValue);

        decimal entryPrice = position.TpGridAnchorPrice;
        for (int i = existingDcaParts; i < GlobalData.Settings.Trading.DcaList.Count; i++)
        {
            var dcaEntry = GlobalData.Settings.Trading.DcaList[i];

            // When the strategy provides a signal SL, skip DCA levels that fall beyond it —
            // those would never fill because the SL triggers first.
            if (position.SlPercentage.HasValue && dcaEntry.Percentage >= position.SlPercentage.Value)
                continue;

            decimal diffPrice = entryPrice * Math.Abs(dcaEntry.Percentage) / 100m;
            prices.Add(position.Side == CryptoTradeSide.Long ? entryPrice - diffPrice : entryPrice + diffPrice);
        }
        return prices;
    }


    private void ProcessPendingDcaSignal(CryptoPosition position)
    {
        var request = position.PendingDcaSignal;
        if (request == null)
            return;
        position.PendingDcaSignal = null;

        if (position.Status != CryptoPositionStatus.Trading || position.CloseTime.HasValue)
            return;

        PositionTools.ExtendPosition(Database, position, CryptoPartPurpose.Dca,
            request.Interval, request.Strategy,
            request.DcaPrice, request.CandleCloseTime);

        position.TriggerPriceTop = null;
        position.TriggerPriceBottom = null;
    }


    private async Task CheckAddDcaFixedPercentage(CryptoPosition position)
    {
        // Alle resterende DCA-niveaus in 1x plaatsen zodra de entry gevuld is (in plaats van steeds te
        // wachten tot de vorige DCA gevuld is) - elk niveau krijgt zijn eigen part op zijn vaste %-prijs
        // vanaf de entry, en wordt direct als losse open limit-order neergezet.
        if (position.Status == CryptoPositionStatus.Trading)
        {
            // No new DCA orders once the SL has been moved to break-even
            if (position.SlMovedToBreakEven)
                return;

            List<decimal> missingPrices = GetMissingFixedPercentageDcaPrices(position);
            if (missingPrices.Count > 0)
            {
                string text = $"{position.Symbol.Name} + {missingPrices.Count} DCA('s) bijplaatsen";

                // Zo laat mogelijk controleren vanwege extra calls naar de exchange
                var (success, reaction) = AssetTools.FetchAssets(GlobalData.ActiveExchange);
                if (!success)
                {
                    GlobalData.AddTextToLogTab(text + " " + reaction);
                    Symbol.ClearSignals();
                    return;
                }

                var resultCheckAssets = AssetTools.CheckAvailableAssets(GlobalData.ActiveExchange!, Symbol);
                if (!resultCheckAssets.success)
                {
                    GlobalData.AddTextToLogTab(text + " " + resultCheckAssets.reaction);
                    Symbol.ClearSignals();
                    return;
                }

                foreach (decimal dcaPrice in missingPrices)
                {
                    // Corrigeer de prijs indien de koers ondertussen al lager of hoger ligt dan dit niveau
                    decimal price = dcaPrice;
                    if (position.Side == CryptoTradeSide.Long)
                    {
                        if (position.Symbol.LastPrice.HasValue && position.Symbol.LastPrice < price)
                            price = (decimal)position.Symbol.LastPrice - position.Symbol.PriceTickSize;
                    }
                    else
                    {
                        if (position.Symbol.LastPrice.HasValue && position.Symbol.LastPrice > price)
                            price = (decimal)position.Symbol.LastPrice + position.Symbol.PriceTickSize;
                    }

                    // De positie uitbreiden nalv een nieuw signaal (de xe bijkoop wordt altijd een aparte DCA)
                    PositionTools.ExtendPosition(Database, position, CryptoPartPurpose.Dca, position.Interval!, position.Strategy,
                        price, LastCandle1mCloseTimeDate);
                }

                // De net aangemaakte parts hebben nog geen correct PartNumber (= dca niveau) totdat dit
                // herberekend is, en HandlePosition (dat zo na deze aanroep de orders plaatst) heeft dat
                // PartNumber meteen nodig om per niveau de juiste Factor te bepalen.
                await TradeTools.CalculatePositionResultsViaOrders(Database, position, forceCalculation: true);
            }
        }
    }


    public async Task CancelOrdersIfClosedOrTimeoutOrReposition(CryptoPosition position)
    {
        // Voor ondersteuning van long/short
        CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
        CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();


        foreach (CryptoPositionPart part in position.PartList.Values.ToList())
        {
            if (!part.CloseTime.HasValue)
            {
                foreach (CryptoPositionStep step in part.StepList.Values.ToList())
                {
                    if (step.Status != CryptoOrderStatus.New)
                        continue;

                    bool timeOut = false;
                    bool closePart = true;
                    string cancelReason = "";
                    CryptoOrderStatus newStatus = CryptoOrderStatus.Expired;


                    // Wellicht bij een hele negatieve of positieve baromer alsnog de DCA orders weghalen (maar dat doen we nu bewust niet)
                    //PauseBecauseOfBarometerLong = !TradingRules.CheckBarometerValues(Symbol.QuoteData.PauseTradingLong, Symbol.QuoteData, CryptoTradeSide.Long, LastCandle1m, out string _);
                    //PauseBecauseOfBarometerShort = !TradingRules.CheckBarometerValues(Symbol.QuoteData.PauseTradingShort, Symbol.QuoteData, CryptoTradeSide.Short, LastCandle1m, out string _);

                    if (step.Side == entryOrderSide)
                    {
                        // De orders van een gesloten positie allemaal annuleren (dat zijn de fixed perc buy orders)
                        if (position.CloseTime.HasValue)
                        {
                            newStatus = CryptoOrderStatus.PositionClosed;
                            cancelReason = "annuleren vanwege sluiten positie";
                        }


                        // Een eventuele aan- of bijkoop kan worden geannuleerd indien de instap te lang duurt ("Remove Time")
                        // (een toekomstige gereserveerde DCA buy orders of actieve trailing orders moeten we niet annuleren)
                        // Verwijder openstaande buy orders die niet gevuld worden binnen zoveel X minuten/candles?
                        // En dan mag eventueel de positie gesloten worden (indien het uit 1 deelpositie bestaat)
                        else if (part.Purpose == CryptoPartPurpose.Entry && step.Trailing == CryptoTrailing.None)
                        {
                            // Is de order ouder dan X minuten dan deze verwijderen
                            CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(part.Interval!.IntervalPeriod);
                            if (step.CreateTime.AddMinutes(GlobalData.Settings.Trading.EntryRemoveTime * symbolInterval.Interval.Duration) < LastCandle1mCloseTimeDate)
                            {
                                // Trades worden niet altijd op het juiste tijdstip opgemerkt (de user ticker ligt er vaak uit)
                                // Controleer daarom eerst of de order gevallen is, synchroniseer de trades en herberekenen het geheel..

                                // Soms wordt een trade niet gerapporteerd en dan gaat er van alles mis in onze beredeneringen.
                                // (met een partial fill gaat deze code ook gedeeltelijk fout, later nog eens beter nazoeken)
                                // Haal alle trades van deze order op, wellicht gaat dat beter dan via alle trades?
                                // (achteraf gezien, wellicht is dit een betere c.q. betrouwbaardere methode om de trades te halen?)
                                //GlobalData.AddTextToLogTab($"TradeHandler: DETECTIE: ORDER {data.OrderId} NIET GEVONDEN! PANIC MODE ;-)");

                                await TradeTools.CalculatePositionResultsViaOrders(Database, position, forceCalculation: true);

                                // Check the orders, if its still not filled than timeout
                                if (step.Status == CryptoOrderStatus.New)
                                {
                                    timeOut = true;
                                    newStatus = CryptoOrderStatus.Timeout;
                                    cancelReason = "annuleren vanwege timeout";
                                }
                            }
                        }

                        // Cancel unfilled DCA orders once the SL has been moved to break-even
                        else if (part.Purpose == CryptoPartPurpose.Dca && position.SlMovedToBreakEven)
                        {
                            newStatus = CryptoOrderStatus.ChangedBreakEven;
                            cancelReason = "cancel DCA because SL moved to break-even";
                        }
                    }
                    else if (step.Side == takeProfitOrderSide)
                    {
                        // Verwijder TP orders vanwege een aanpassing in de BE door een buy of sell
                        if (position.Reposition)
                        {
                            newStatus = CryptoOrderStatus.ChangedBreakEven;
                            cancelReason = "annuleren vanwege aanpassing BE";
                        }
                    }


                    if (cancelReason != "")
                    {
                        var (success, _) = await TradeTools.CancelOrder(Database, position, part, step,
                            LastCandle1mCloseTimeDate, newStatus, cancelReason);
                        if (success)
                        {
                            position.TriggerPriceTop = null;
                            position.TriggerPriceBottom = null;

                            // Na een timeout (barometer, tradingrules) even 5 minuten helemaal niets doen
                            if (newStatus == CryptoOrderStatus.TradingRules || newStatus == CryptoOrderStatus.BarameterToLow)
                                Symbol.LastTradeDate = LastCandle1mCloseTimeDate.AddMinutes(-GlobalData.Settings.Trading.GlobalBuyCooldownTime + 5);

                            if (timeOut)
                            {
                                // Door het verwijderen van de laatste buy kan een positie gesloten worden
                                if (closePart)
                                {
                                    part.CloseTime = LastCandle1mCloseTimeDate;
                                    Database.Connection.Update<CryptoPositionPart>(part);

                                    // Als de entry niet lukt dan mag de positie gesloten worden
                                    if (part.Purpose == CryptoPartPurpose.Entry && position.Status == CryptoPositionStatus.Waiting)
                                    {
                                        position.Status = CryptoPositionStatus.Timeout;
                                        position.UpdateTime = LastCandle1mCloseTimeDate;
                                        position.CloseTime = LastCandle1mCloseTimeDate;
                                        Database.Connection.Update<CryptoPosition>(position);
                                    }
                                }


                                await TradeTools.CalculatePositionResultsViaOrders(Database, position, false);
                            }
                        }
                    }

                }
            }
        }


        // Pas op: Het doorrekenen voor de BE kost je 2 tot 5 seconden! (de positie en alle steps worden bewaard, dus niet zomaar uitvoeren!)

        if (position.Reposition)
        {
            position.Reposition = false;
            Database.Connection.Update<CryptoPosition>(position);
        }

        // Een afgesloten posities is niet meer interessant, verplaatsen
        //GlobalData.Logger.Info($"analyze.HandlePosition.CancelOrdersIfClosedOrTimeoutOrReposition.After({Symbol.Name})");
        if (position.CloseTime.HasValue)
        {
            bool hasOpenOrder = false;
            foreach (CryptoPositionPart part in position.PartList.Values.ToList())
            {
                if (!part.CloseTime.HasValue)
                {
                    foreach (CryptoPositionStep step in part.StepList.Values.ToList())
                    {
                        if (step.Status == CryptoOrderStatus.New)
                        {
                            hasOpenOrder = true;
                        }
                    }
                }
            }

            // Move if all the DCA orders are properly cancelled
            if (!hasOpenOrder)
            {
                PositionTools.RemovePosition(GlobalData.ActiveExchange!, position, true);
            }
        }
    }

    internal async Task<bool> CancelAllOrders(CryptoPosition position, CryptoOrderSide takeProfitOrderSide)
    {
        if (position.Quantity > 0)
        {
            foreach (CryptoPositionPart part in position.PartList.Values.ToList())
            {
                if (!part.CloseTime.HasValue)
                {
                    // Has it an open takeprofit order?
                    CryptoPositionStep? step = PositionTools.FindPositionPartStep(part, takeProfitOrderSide, false);
                    if (step != null && step.Status == CryptoOrderStatus.New && step.Side == takeProfitOrderSide)
                    {
                        string cancelReason = $"cancel because of change BE";
                        var (success, _) = await TradeTools.CancelOrder(Database, position, part, step,
                            LastCandle1mCloseTimeDate, CryptoOrderStatus.ChangedBreakEven, cancelReason);
                        if (success)
                        {
                            // niets?
                            //step.RemainingDust = 0; // reset
                            // There are problems closing the position because of dust, added some debugging
                            GlobalData.AddTextToLogTab($"Monitor {Symbol.Name} CancelAllOrders - reset TP RemainingDust?????? {step.RemainingDust}");
                        }
                        else
                            return false;
                    }
                }
            }
        }

        return true;
    }


    /// <summary>
    /// Find the nearest unfilled DCA order price (closest to the current market price).
    /// Long: highest unfilled DCA buy. Short: lowest unfilled DCA sell.
    /// Returns null when no open DCA orders exist.
    /// </summary>
    internal static decimal? FindNearestUnfilledDcaPrice(CryptoPosition position)
    {
        CryptoOrderSide dcaOrderSide = position.GetEntryOrderSide();
        decimal? nearest = null;

        foreach (var part in position.PartList.Values)
        {
            if (part.Purpose != CryptoPartPurpose.Dca || part.CloseTime.HasValue)
                continue;

            CryptoPositionStep? step = PositionTools.FindPositionPartStep(part, dcaOrderSide, false);
            if (step == null)
                continue;

            if (nearest == null)
                nearest = step.Price;
            else if (position.Side == CryptoTradeSide.Long)
                nearest = Math.Max(nearest.Value, step.Price);
            else
                nearest = Math.Min(nearest.Value, step.Price);
        }

        return nearest;
    }


    public async Task HandlePosition(CryptoPosition position)
    {
        //GlobalData.Logger.Info($"position:" + LastCandle1m.OhlcText(Symbol, GlobalData.IntervalList[0], Symbol.PriceDisplayFormat, true, false, true));
        Dictionary<int, CryptoPositionPart> takeProfitPartsByLevel = [];

        foreach (CryptoPositionPart part in position.PartList.Values.ToList())
        {
            // voor de niet afgesloten parts...
            if (!part.CloseTime.HasValue && part.Purpose != CryptoPartPurpose.TakeProfit)
            {
                // Prepare checks if we have a valid candle in the interval (from the part or position)
                if (!PauseBecauseOfTradingRules && part.Purpose == CryptoPartPurpose.Entry)
                    await HandleEntryPart(position, part, GlobalData.Settings.Trading.EntryOrderType);

                // Check DCA - always allowed, even during a TradingRules pause (averaging into an
                // existing position is not gated by the market-wide pause, see CheckThePosition)
                if (part.Purpose == CryptoPartPurpose.Dca)
                    await HandleEntryPart(position, part, GlobalData.Settings.Trading.DcaOrderType);
            }
            // remember the tp parts, one per configured TP level - PartNumber is the 1-based level
            // ("TP 1", "TP 2", ...); convert back to the 0-based index used for TpList/levels lookups.
            if (part.Purpose == CryptoPartPurpose.TakeProfit)
                takeProfitPartsByLevel[part.PartNumber - 1] = part;
        }


        if (position.Quantity > 0)
        {
            CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();
            List<CryptoTpEntry> levels = TradeTools.EffectiveTpList(position);

            // A level stays "open" until its part exists and has been fully filled (CloseTime set).
            List<int> openLevelIndexes = [];
            for (int i = 0; i < levels.Count; i++)
            {
                bool closed = takeProfitPartsByLevel.TryGetValue(i, out CryptoPositionPart? existing)
                    && existing.CloseTime.HasValue;
                if (!closed)
                    openLevelIndexes.Add(i);
            }

            if (openLevelIndexes.Count > 0)
            {
                // Always create a separate take profit part per level (if it didn't exist yet)
                foreach (int i in openLevelIndexes)
                {
                    if (!takeProfitPartsByLevel.ContainsKey(i))
                        takeProfitPartsByLevel[i] = PositionTools.ExtendPosition(Database, position, CryptoPartPurpose.TakeProfit, position.Interval!,
                            position.Strategy, 0, GlobalData.Clock.UtcNow);
                }

                decimal openFractionSum = openLevelIndexes.Sum(i => levels[i].Factor);
                int lastOpenIndex = openLevelIndexes[^1];

                // Splits the CURRENT remaining position quantity across the still-open levels, weighted
                // by their configured share. Re-normalizing the fractions against the live (shrinking
                // when a sibling level fills, or growing on a DCA) quantity keeps each already-open
                // level's absolute target stable - only the last open level absorbs the exact
                // remainder/dust, same role the single combined TP order used to have.
                List<(int Level, CryptoPositionPart Part, decimal Price, decimal Quantity)> ComputeTargets()
                {
                    decimal allocated = 0;
                    List<(int, CryptoPositionPart, decimal, decimal)> result = [];
                    foreach (int i in openLevelIndexes)
                    {
                        CryptoPositionPart part = takeProfitPartsByLevel[i];
                        decimal quantity;
                        if (i == lastOpenIndex)
                            quantity = position.Quantity - allocated; // remainder, absorbs dust/rounding
                        else
                        {
                            decimal fraction = openFractionSum > 0 ? levels[i].Factor / openFractionSum : 0;
                            quantity = (position.Quantity * fraction).Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);
                            allocated += quantity;
                        }
                        decimal price = CalculateTpPrice(position, levels[i].Percentage);
                        result.Add((i, part, price, quantity));
                    }
                    return result;
                }

                List<(int Level, CryptoPositionPart Part, decimal Price, decimal Quantity)> targets = ComputeTargets();
                (decimal? stop, decimal? limit) sl = CalculateSlPrices(position);

                bool anyChange = false;
                Dictionary<int, bool> hadExistingOrder = [];
                foreach (var t in targets)
                {
                    CryptoPositionStep? order = PositionTools.FindPositionPartStep(t.Part, takeProfitOrderSide, false);
                    hadExistingOrder[t.Level] = order != null;
                    if (order == null || order.Price != t.Price || order.Quantity != t.Quantity || order.StopPrice != sl.stop || order.StopLimitPrice != sl.limit)
                    {
                        if (order != null)
                            GlobalData.AddTextToLogTab($"{Symbol.Name} SELL correction TP{t.Level + 1}: {order.Price:N6} to {t.Price.ToString0()}");
                        anyChange = true;
                    }
                }

                bool cancelFailed = false;
                if (anyChange)
                {
                    // Cancel all open take profit orders (across every level)
                    if (await CancelAllOrders(position, takeProfitOrderSide))
                    {
                        // Calculate the BE price (without the previous commission for the TP order)
                        TradeTools.CalculateProfitAndBreakEvenPrice(position);
                        targets = ComputeTargets();
                        sl = CalculateSlPrices(position);

                        foreach (var t in targets)
                        {
                            string text = hadExistingOrder.GetValueOrDefault(t.Level) ? $"modifying TP{t.Level + 1} " : $"placing TP{t.Level + 1} ";

                            // And place the take profit order for this level (last open level minimizes dust)
                            await TradeTools.PlaceTakeProfitOrderAtPrice(Database, position, t.Part,
                                t.Price, sl.stop, sl.limit, LastCandle1mCloseTimeDate, text, t.Quantity, includeDust: t.Level == lastOpenIndex);
                        }
                    }
                    else
                    {
                        GlobalData.AddTextToLogTab($"Monitor {Symbol.Name} Niet alle orders konden verwijderd worden!!!! (partial filled or error?)");
                        cancelFailed = true;
                    }
                }

                if (!cancelFailed)
                {
                    decimal? nearestDca = FindNearestUnfilledDcaPrice(position);
                    UpdateTriggerPrices(position, targets[0].Price, sl.stop, nearestDca);
                }
            }
        }
        else if (position.Status == CryptoPositionStatus.Waiting)
        {
            UpdateTriggerPricesForWaiting(position);
        }

    }


    internal static void UpdateTriggerPricesForWaiting(CryptoPosition position)
    {
        CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
        CryptoPositionStep? entryStep = null;
        foreach (var part in position.PartList.Values)
        {
            if (part.Purpose == CryptoPartPurpose.Entry && !part.CloseTime.HasValue)
            {
                entryStep = PositionTools.FindPositionPartStep(part, entryOrderSide, false);
                break;
            }
        }

        if (entryStep == null || entryStep.Status != CryptoOrderStatus.New)
            return;

        if (entryStep.OrderType == CryptoOrderType.Market)
            return;

        bool isLong = position.Side == CryptoTradeSide.Long;

        if (entryStep.OrderType == CryptoOrderType.Limit)
        {
            if (isLong)
            {
                position.TriggerPriceBottom = entryStep.Price;
                position.TriggerPriceTop = decimal.MaxValue;
            }
            else
            {
                position.TriggerPriceTop = entryStep.Price;
                position.TriggerPriceBottom = 0;
            }
        }
        else if (entryStep.OrderType == CryptoOrderType.StopLimit && entryStep.StopPrice.HasValue)
        {
            if (isLong)
            {
                position.TriggerPriceTop = entryStep.StopPrice.Value;
                position.TriggerPriceBottom = entryStep.Price;
            }
            else
            {
                position.TriggerPriceBottom = entryStep.StopPrice.Value;
                position.TriggerPriceTop = entryStep.Price;
            }
        }
    }


    internal static bool ShouldRunHandlePosition(CryptoPosition position, decimal candleHigh, decimal candleLow)
    {
        if (position.TriggerPriceTop == null && position.TriggerPriceBottom == null)
            return true;
        if (position.TriggerPriceTop != null && candleHigh >= position.TriggerPriceTop.Value)
            return true;
        if (position.TriggerPriceBottom != null && candleLow <= position.TriggerPriceBottom.Value)
            return true;
        return false;
    }


    internal static void UpdateTriggerPrices(CryptoPosition position, decimal nearestTpPrice, decimal? slStop, decimal? nearestDcaPrice = null)
    {
        bool isLong = position.Side == CryptoTradeSide.Long;

        // Favorable side: nearest TP, capped by profit-lock threshold if applicable
        decimal favorablePrice = nearestTpPrice;
        if (GlobalData.Settings.Trading.MoveSlToBreakEven
            && !position.SlMovedToBreakEven
            && position.BreakEvenPrice > 0)
        {
            int multiplier = isLong ? +1 : -1;
            decimal lockPct = GlobalData.Settings.Trading.MoveSlToBreakEvenPercentage;
            decimal lockThreshold = position.BreakEvenPrice + multiplier * position.BreakEvenPrice * lockPct / 100m;

            if (isLong)
                favorablePrice = Math.Min(favorablePrice, lockThreshold);
            else
                favorablePrice = Math.Max(favorablePrice, lockThreshold);
        }

        // Unfavorable side: the nearest of SL and unfilled DCA (closer to current price)
        decimal? unfavorablePrice = slStop;
        if (nearestDcaPrice != null)
        {
            if (unfavorablePrice == null)
                unfavorablePrice = nearestDcaPrice;
            else if (isLong)
                unfavorablePrice = Math.Max(unfavorablePrice.Value, nearestDcaPrice.Value);
            else
                unfavorablePrice = Math.Min(unfavorablePrice.Value, nearestDcaPrice.Value);
        }

        if (isLong)
        {
            position.TriggerPriceTop = favorablePrice;
            position.TriggerPriceBottom = unfavorablePrice;
        }
        else
        {
            position.TriggerPriceBottom = favorablePrice;
            position.TriggerPriceTop = unfavorablePrice;
        }
    }



    public async Task CheckThePosition(CryptoPosition position)
    {
        // Pauzeren vanwege de trading regels of te lage barometer
        PauseBecauseOfTradingRules = !TradingRules.CheckTradingRules(GlobalData.ActiveExchange!.Data.PauseTrading, LastCandle1m.OpenTime, BaseIntervalDuration);

        // Profiling: sub-breakdown of the positionCheck bucket's "other" path (see PipelineProfiler).
        // Runs on every candle that has an open position (not gated behind ForceCheckPosition like
        // CalculatePositionResultsViaOrders), so this is the candidate for the bulk of positionCheck.
        long profCancelStart = Stopwatch.GetTimestamp();
        long profDcaTicks = 0;
        long profHandleTicks = 0;

        //Monitor.Enter(position);
        try
        {
            // Verwijder orders voor verschillende redenenen (timeout, barometer, tradingrules, positie gesloten, reposition enzovoort)
            await CancelOrdersIfClosedOrTimeoutOrReposition(position);
            long profDcaStart = Stopwatch.GetTimestamp();
            long profCancelTicks = profDcaStart - profCancelStart;

            if (!position.CloseTime.HasValue)
            {
                // Process any signal-based DCA that the candle thread queued up
                ProcessPendingDcaSignal(position);

                // Een DCA op een bestaande positie altijd direct toestaan, ook tijdens een
                // marktbrede TradingRules-pauze (bv. snelle BTC-beweging) - alleen nieuwe
                // entries worden door die pauze geblokkeerd, niet het bijkopen op een lopende positie.
                await CheckAddDcaFixedPercentage(position);
                long profHandleStart = Stopwatch.GetTimestamp();
                profDcaTicks = profHandleStart - profDcaStart;

                // Plaats of modificeer de buy of sell orders + optionele LockProfits.
                // Gate: skip when the candle stays inside the trigger boundaries (TP/SL
                // prices only change on order fills, settings changes, or profit-lock —
                // all of which invalidate the triggers via ForceCheckPosition or above).
                if (ShouldRunHandlePosition(position, LastCandle1m.High, LastCandle1m.Low))
                    await HandlePosition(position);
                profHandleTicks = Stopwatch.GetTimestamp() - profHandleStart;
            }
            else
            {
                profDcaTicks = Stopwatch.GetTimestamp() - profDcaStart;
            }

            PipelineProfiler.RecordCheckPositionPhases(
                cancel: profCancelTicks,
                dca: profDcaTicks,
                handle: profHandleTicks);
        }
        finally
        {
            //Monitor.Exit(position);
        }
    }


    /// <summary>
    /// True when this candle can move the position: it reaches one of the trigger prices, the
    /// triggers are unknown, a check was forced, or a waiting entry has run past its timeout.
    /// Only when this is false can the whole order/position handling be skipped.
    ///
    /// Static so the replay can ask the same question about a base candle before deciding whether
    /// to descend into the underlying minute candles.
    /// </summary>
    public static bool CandleCanMovePosition(CryptoPosition position, CryptoCandle candle, CandleTime candleCloseTime)
    {
        if (position.TriggerPriceTop == null || position.TriggerPriceBottom == null)
            return true;
        if (position.ForceCheckPosition)
            return true;
        if (candle.High >= position.TriggerPriceTop.Value || candle.Low <= position.TriggerPriceBottom.Value)
            return true;

        if (position.Status == CryptoPositionStatus.Waiting && position.Interval != null)
        {
            DateTime timeoutAt = position.CreateTime.AddMinutes(
                GlobalData.Settings.Trading.EntryRemoveTime * position.Interval.Duration);
            if (candleCloseTime.ToDateTime() >= timeoutAt)
                return true;
        }
        return false;
    }


    /// <summary>
    /// Order handling for ONE candle: fill what fills, then let the position react — placing the
    /// take profit / DCA / stop loss and recalculating the trigger prices. Deliberately without the
    /// signal side (SignalPrepare/SignalExecute), which stays on the base interval.
    ///
    /// This is what the replay calls per minute candle once a base candle turns out to touch a
    /// trigger. Handling the fill on the coarse candle instead would stamp it at the END of that
    /// candle, so every follow-up order would only start existing minutes later — that is what made
    /// a 5m run miss a take profit a 1m run did take.
    /// </summary>
    public async Task ProcessOrdersAsync()
    {
        var exchange = GlobalData.ActiveExchange!;
        if (!exchange.Data.PositionList.ContainsKey(Symbol.Name))
            return;

        if (GlobalData.Settings.Trading.TradeVia != CryptoTradeVia.RealTrading)
            await PaperTrading.PaperTradingCheckOrders(Database, exchange, Symbol, LastCandle1m, BaseIntervalDuration);

        // Re-lookup: the fill cascade may have closed or replaced the position.
        if (exchange.Data.PositionList.TryGetValue(Symbol.Name, out CryptoPosition? position))
            await GlobalData.ThreadCheckPosition!.AddToQueue(position!);
    }


    /// <summary>
    /// Set by the replay when the orders of this base candle were already handled per minute
    /// candle. The pipeline then skips its own order handling — redoing it on the coarse candle
    /// would fill at coarse-candle prices and stamp it at the end of the candle.
    /// </summary>
    public bool OrdersAlreadyProcessed { get; set; }


    /// <summary>
    /// We have a new candle (for the scanner a 1m interval, emulator can be a higher 
    /// interval candle), calculate the signals
    /// </summary>
    public async Task NewCandleArrivedAsync()
    {
        try
        {
            if (!GlobalData.Settings.Signal.Active ||
                !Symbol.QuoteData!.FetchCandles ||
                Symbol.Status == 0 ||
                !Symbol.LastPrice.HasValue)
                return;

            //GlobalData.Logger.Trace($"SignalCreate.PrepareIndicators.Start {Symbol.Name} {Interval.Name} {Side}");
            //string traceText = LastCandle1m.OhlcText(Symbol, GlobalData.IntervalList[0], Symbol.PriceDisplayFormat, true, false, true);
            //ScannerLog.Logger.Trace($"NewCandleArrivedAsync.Signals " + traceText);

            // Single lookup: reused for the signal skip and the trigger-price check below.
            CryptoPosition? existingPosition = null;
            bool hasPosition = GlobalData.ActiveExchange!.Data.PositionList.TryGetValue(Symbol.Name, out existingPosition);
            if (!hasPosition)
            {
                // Is the Symbol a new one?
                if (!SymbolTools.CheckNewCoin(Symbol, out string response))
                {
                    if (GlobalData.Settings.Signal.LogSymbolMustExistsDays)
                        GlobalData.AddTextToLogTab($"{Symbol.Name} {response}");
                    if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == Symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
                        ScannerLog.Logger.Info($"{Symbol.Name} {response}");
                    return;
                }

                // Is the volume valid within a certain minimal limit
                // candleStart wants the OPEN time of the candle plus its duration; this passed the
                // close time with duration 1, which describes the NEXT candle.
                if (!Symbol.CheckValidMinimalVolume(LastCandle1m.OpenTime, BaseIntervalDuration, out response))
                {
                    if (GlobalData.Settings.Signal.LogMinimalVolume)
                        GlobalData.AddTextToLogTab($"{Symbol.Name} {response}");
                    if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == Symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
                        ScannerLog.Logger.Info($"{Symbol.Name} {response}");
                    return;
                }

                // Is the price valid within a certain minimal limit
                if (!Symbol.CheckValidMinimalPrice(out response))
                {
                    if (GlobalData.Settings.Signal.LogMinimalPrice)
                        GlobalData.AddTextToLogTab($"{Symbol.Name} {response}");
                    if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == Symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
                        ScannerLog.Logger.Info($"{Symbol.Name} {response}");
                    return;
                }
            }

            // Profiling timestamps. GetTimestamp is a cheap QueryPerformanceCounter read; the totals
            // are only accumulated when PipelineProfiler.Enabled (the emulator turns it on), so the
            // live scanner is unaffected. Splits this method into indicators / algorithms / trade
            // handling / position-finished check so we can see where the dominant pipeline cost sits.
            long profPrepareStart = Stopwatch.GetTimestamp();

            // Alway's calculate the indicators, queue the fvg and dlz zones etc
            await SignalPrepare.ExecuteAsync(Symbol, LastCandle1m, LastCandle1mCloseTime);
            long profExecuteStart = Stopwatch.GetTimestamp();

            //GlobalData.Logger.Trace($"NewCandleArrivedAsync.Positions " + traceText);

            // When a position exists and the candle stays inside the known TP/SL/DCA
            // boundaries, order simulation and position processing can be skipped entirely.
            bool canSkipPositionProcessing = false;
            if (hasPosition)
            {
                Interlocked.Increment(ref PipelineProfiler.SkipHasPosition);
                if (existingPosition!.TriggerPriceTop == null || existingPosition.TriggerPriceBottom == null)
                    Interlocked.Increment(ref PipelineProfiler.SkipTriggersNull);
                else if (existingPosition.ForceCheckPosition)
                    Interlocked.Increment(ref PipelineProfiler.SkipForceCheck);
                else if (LastCandle1m.High >= existingPosition.TriggerPriceTop.Value
                      || LastCandle1m.Low <= existingPosition.TriggerPriceBottom.Value)
                    Interlocked.Increment(ref PipelineProfiler.SkipPriceOutside);
                else
                {
                    Interlocked.Increment(ref PipelineProfiler.SkipSuccess);
                    canSkipPositionProcessing = true;
                }

                if (canSkipPositionProcessing && existingPosition!.Status == CryptoPositionStatus.Waiting)
                {
                    var interval = existingPosition.Interval;
                    if (interval != null)
                    {
                        var timeoutAt = existingPosition.CreateTime.AddMinutes(
                            GlobalData.Settings.Trading.EntryRemoveTime * interval.Duration);
                        if (LastCandle1mCloseTimeDate >= timeoutAt)
                            canSkipPositionProcessing = false;
                    }
                }
            }

            // Orders first, signals after. An order this candle fills changes the position (Waiting
            // becomes Trading), and that is what decides whether the signal work is needed at all -
            // so it has to have happened before we ask. No second lookup needed: the fill mutates
            // the very object existingPosition points at, and it does not leave PositionList here.
            if (!canSkipPositionProcessing)
            {
                // Simulate Trade indien openstaande orders gevuld zijn
                //GlobalData.Logger.Info($"analyze.PaperTradingCheckOrders({Symbol.Name})");
                if (GlobalData.Settings.Trading.TradeVia != CryptoTradeVia.RealTrading && !OrdersAlreadyProcessed)
                    await PaperTrading.PaperTradingCheckOrders(Database, GlobalData.ActiveExchange!, this.Symbol, LastCandle1m, BaseIntervalDuration);
            }

            // Only skip signal generation for filled positions (status >= Trading).
            // Waiting (unfilled) positions allow signals so a newer signal can replace them.
            bool skipSignals = hasPosition && existingPosition!.Status >= CryptoPositionStatus.Trading;

            // Also stay quiet for a short while after the last fill on this symbol. Closing a
            // position IS a fill (take profit or stop loss), so this covers the case where the
            // position just disappeared from PositionList - which, depending on the base interval,
            // may or may not already have happened by the time we get here.
            if (!skipSignals && Symbol.LastTradeDate.HasValue
                && Symbol.LastTradeDate.Value.AddMinutes(GlobalData.Settings.Trading.SignalCooldownAfterTradeTime) > LastCandle1mCloseTimeDate)
                skipSignals = true;

            // Calculate signals and touch of the dlz and fvg zones
            if (!skipSignals)
                await SignalExecute.ExecuteAsync(Symbol, LastCandle1mCloseTime);
            long profTradeStart = Stopwatch.GetTimestamp();

            // Pause because of trading rules or low barometer
            PauseBecauseOfTradingRules = !TradingRules.CheckTradingRules(GlobalData.ActiveExchange!.Data.PauseTrading, LastCandle1m.OpenTime, BaseIntervalDuration);

            // Stepping in runs REGARDLESS of the trigger-price skip. That skip guards the ORDER
            // handling - it states that no order can have been touched by this candle - but the
            // step-in asks something else entirely: should a waiting, unfilled position be replaced
            // by a newer signal? That question does not depend on whether an order was touched.
            //
            // Keeping it inside the skip tied the answer to how much of the market the base candle
            // covers. Replacing needs the trigger price to be reached without the order filling,
            // which happens at exactly the entry price (the trigger uses <=, the fill uses <). On a
            // 15m run the candle spans the whole quarter and closes on the quarter boundary, so
            // both conditions meet; on a 1m run the candle at that same boundary only covers the
            // final minute, so they never do. Same market, five times as many replacements.
            //
            //TODO: Reuse the preparedIndicatorDataList in the CreateOrExtendPositionAsync?
            // Open or extend a position
            //if (signalList.Count > 0) // alway's?
            if (!skipSignals)
                await CreateOrExtendPositionAsync();
            long profPositionCheckStart = Stopwatch.GetTimestamp();

            // Check the positions
            // Profiling: dedicated wrap of exactly this statement, as a cross-check against the
            // positionCheck bucket below (which times the same statement via subtraction) — the two
            // totals should match.
            long profAddToQueueStart = Stopwatch.GetTimestamp();

            // Re-lookup: the position may have been created or replaced during
            // CreateOrExtendPositionAsync. When the minute candles already handled the orders, only
            // a position created just now still needs this — an existing one has been reacting per
            // minute all along.
            //
            // Outside the trigger-price skip, for the same reason CreateOrExtendPositionAsync is:
            // this is where a freshly created position gets its entry order PLACED. The skip is
            // about the OLD position's trigger prices, so on a base interval whose candle happened
            // to stay inside them the step above created the position and this line then refused to
            // place its order - which arrived a base candle later, at whatever the price was by
            // then. Two runs, same decision at the same minute, different entry price.
            if (GlobalData.ActiveExchange!.Data.PositionList.TryGetValue(Symbol.Name, out CryptoPosition? currentPosition)
                && (!canSkipPositionProcessing || currentPosition!.Status == CryptoPositionStatus.Waiting)
                && (!OrdersAlreadyProcessed || currentPosition!.Status == CryptoPositionStatus.Waiting))
                await GlobalData.ThreadCheckPosition!.AddToQueue(currentPosition!);
            PipelineProfiler.RecordAddToQueue(Stopwatch.GetTimestamp() - profAddToQueueStart);

            PipelineProfiler.Record(
                prepare: profExecuteStart - profPrepareStart,
                execute: profTradeStart - profExecuteStart,
                trade: profPositionCheckStart - profTradeStart,
                positionCheck: Stopwatch.GetTimestamp() - profPositionCheckStart);

            //GlobalData.Logger.Trace($"NewCandleArrivedAsync.Clean " + traceText);

            // Remove old candles or CandleData
            // Profiling: this tail previously ran AFTER PipelineProfiler.Record above, so it fell
            // outside every bucket.
            //
            // Skipped in emulator mode — the comment here used to claim that was already the case
            // while the guard was missing, and it did real damage. CleanCandleDataAsync trims each
            // CandleList back to GetCandleFetchStart, which for 1m is InitialCandleCountFetch
            // (1450 candles once the barometer has been calculated). SignalCreate's 24-hour change
            // looks back 1441 candles, so the margin is nine candles and in practice the lookup
            // failed; it then silently fell back to the OLDEST candle in the list via
            // TryGetFirstCandle, producing a wrong 24-hour change that differed per run because the
            // trim happened at different moments. A replay owns its own memory management: the
            // TickRunner prunes between chunks using IndicatorWarmup.WarmupDepth, which reserves
            // the full day plus barometer window for 1m.
            long profCleanCandleStart = Stopwatch.GetTimestamp();
            if (!GlobalData.IsEmulatorMode && Symbol.Data.ZoneLock.CurrentCount > 0)
                await CandleTools.CleanCandleDataAsync(Symbol, LastCandle1mCloseTime);
            PipelineProfiler.RecordCleanCandle(Stopwatch.GetTimestamp() - profCleanCandleStart);

            //GlobalData.Logger.Trace($"NewCandleArrivedAsync.Done " + traceText);
        }
        catch (Exception error)
        {
            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"{Symbol.Name} error Monitor {error.Message}");
        }
    }

}