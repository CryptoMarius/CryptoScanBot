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
                            // Voorlopig alleen traden op Bybit Spot en Perpetual (alleen daar kan ik het testen)
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
                                // reserveForDca: a new position commits the entry and every DCA level
                                // behind it, because those are all placed as soon as the entry fills.
                                var resultAvailableAssets = AssetTools.CheckAvailableAssets(GlobalData.ActiveExchange, Symbol, reserveForDca: true);
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
                                    : Symbol.LastPrice.Value).ClampPrice(signal.Side, Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
                                decimal entryBase = entryQuote / entryPrice;
                                entryBase = entryBase.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);
                                entryBase = TradeTools.CorrectEntryQuantityIfWayLess(Symbol, entryQuote, entryBase, entryPrice);

                                // Its rounded towards zero
                                if (entryBase <= 0)
                                {
                                    GlobalData.AddTextToLogTab(text + $" because of minimum quantity {Symbol.QuantityMinimum} and entry value {entryQuote} the buy is not possible");
                                    Symbol.ClearSignals();
                                    return;
                                }

                                // Below the minimum allowed value. Weighed on the quantity that is
                                // actually going to be ordered and not on the amount that was meant to be
                                // staked: the clamp above puts the quantity on the size grid, so a stake of
                                // 12 on a symbol whose tick is a whole coin at a price of 9 leaves an order
                                // worth 9 - which the exchange refuses while the stake itself was over the
                                // minimum.
                                //
                                // This replaces a comparison against QuantityMinimum that stood here until
                                // 31-08-2026. It caught the same case on most symbols by accident, because a
                                // quantity that lands on the very first step of the grid is nearly always
                                // worth less than the minimum order value anyway. Nearly: on a symbol where
                                // one tick is already worth more than that minimum it refused a perfectly
                                // legitimate order of exactly one tick. On HyperLiquid that is XAUT0, whose
                                // tick of 0.01 is worth 44.62 against a minimum order value of 10, and TSLA
                                // at 15.80.
                                decimal entryValue = entryBase * entryPrice;
                                if (Symbol.QuoteValueMinimum > 0 && entryValue < Symbol.QuoteValueMinimum)
                                {
                                    GlobalData.AddTextToLogTab(text + $" because of minimum value {entryValue} < {Symbol.QuoteValueMinimum} the buy is not possible (too little)");
                                    Symbol.ClearSignals();
                                    return;
                                }

                                // And the same question for every other order this position is going
                                // to place: the DCA levels behind the entry and the exit orders that
                                // have to get us back out again. All of it is decided here, because
                                // the entry is the only moment at which refusing costs nothing.
                                //
                                // Before 31-08-2026 this was half answered afterwards, by the closing
                                // check in TradeTools.CalculatePositionResultsViaOrders: it weighed
                                // the remaining quantity against the minimum order value and closed
                                // the position when it fell short. That reading cannot tell an entry
                                // that was too small to begin with from a genuine unsellable
                                // remainder, and on ZECUSDC.PERP it wrote off a whole position - 0.01
                                // ZEC bought for 8.16 against a minimum order value of 10 - as a
                                // total loss while every coin was still held. Straightening things
                                // out afterwards is not the place to decide whether we should have
                                // entered at all.
                                if (!TradeTools.CheckOrderSetAgainstSymbolLimits(Symbol, signal.Side,
                                    entryPrice, entryBase, signal.SlPercentage, out string limitReason))
                                {
                                    GlobalData.AddTextToLogTab(text + $" because {limitReason} the position is not opened");
                                    Symbol.ClearSignals();
                                    return;
                                }

                                // Enough money for the entry AND all of its DCA levels, weighed on
                                // the entry value that is really going to be ordered. The check up in
                                // CheckAvailableAssets asked this on the amount that was MEANT to be
                                // staked, and putting the quantity onto the size grid has moved that
                                // number since - in both directions.
                                if (!AssetTools.CheckAssetsCoverEntryAndDca(GlobalData.ActiveExchange, Symbol,
                                    entryValue, out string assetReason))
                                {
                                    GlobalData.AddTextToLogTab(text + $" not enough cash available {assetReason}");
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
            // Corrected on the close of the candle. The open is deliberately not used: on a green
            // candle it sits far below the market, which pushes the order much further away than
            // the one tick needed to keep a limit order from filling straight away.
            // Equal counts as too high: the signal price is usually the close of the signal candle,
            // so leaving it alone on equality puts the order right ON the market and it fills at once.
            decimal x = LastCandle1m.Close;
            if (x <= price)
                price = x - position.Symbol.PriceTickSize;

            // Gecorrigeerd op de laatst bekende prijs
            if (position.Symbol.LastPrice.HasValue)
            {
                x = (decimal)position.Symbol.LastPrice;
                if (x <= price)
                    price = x - position.Symbol.PriceTickSize;
            }
        }
        else
        {
            // Corrected on the close of the candle, see the long side above.
            decimal x = LastCandle1m.Close;
            if (x >= price)
                price = x + position.Symbol.PriceTickSize;

            // Gecorrigeerd op de laatst bekende prijs
            if (position.Symbol.LastPrice.HasValue)
            {
                x = (decimal)position.Symbol.LastPrice;
                if (x >= price)
                    price = x + position.Symbol.PriceTickSize;
            }
        }

        return price;
    }



    /// <summary>
    /// Absolute TP price for one level's profit distance (%), anchored on
    /// position.TpGridBreakEvenPrice (Entry+Dca fills only, fee-corrected) - NOT on
    /// position.BreakEvenPrice, which also shifts every time a sibling TP level fills (it banks the
    /// realized profit into Returned and shrinks Quantity), causing every still-open TP level to be
    /// repriced and re-placed. TpGridBreakEvenPrice does shift on a new DCA fill, same as
    /// GetMissingFixedPercentageDcaPrices below already assumes.
    /// multiplier = +1 long / -1 short, so the TP sits above for a long, below for a short.
    /// </summary>
    private decimal CalculateTpPrice(CryptoPosition position, decimal percentage)
    {
        int multiplier = position.Side == CryptoTradeSide.Long ? +1 : -1;

        // Past its deadline the profit target is abandoned and the position leaves at whatever the
        // market offers. Aiming one tick THROUGH the last price rather than at it is what makes
        // this an exit instead of another target: an order sitting exactly at the last close only
        // fills if price comes back to it, which for the positions this rule is meant to catch -
        // the ones that walked away and did not return - is precisely what will not happen.
        if (IsPastMaxDuration(position))
        {
            decimal tick = Symbol.PriceTickSize;
            decimal exit = LastCandle1m.Close - (multiplier * tick);
            return exit.ClampPrice(position.Side, Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
        }

        decimal breakEven = position.TpGridBreakEvenPrice;
        decimal price = breakEven + (multiplier * breakEven * (percentage / 100));
        return price.ClampPrice(position.Side, Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
    }


    /// <summary>
    /// Whether this position has been open longer than Trading.MaxPositionDurationDays allows.
    /// Zero (the default) switches the rule off entirely.
    /// <para>
    /// Static and taking the clock reading as an argument because two callers need the same answer
    /// from different places: the pricing above, and CandleCanMovePosition - the gate that decides
    /// whether a candle is worth looking at. Without the gate agreeing, a position whose deadline
    /// passes on a quiet candle is skipped and never leaves.
    /// </para>
    /// </summary>
    internal static bool IsPastMaxDuration(CryptoPosition position, DateTime now)
    {
        decimal days = GlobalData.Settings.Trading.MaxPositionDurationDays;
        if (days <= 0)
            return false;
        return now >= position.CreateTime.AddDays((double)days);
    }


    private bool IsPastMaxDuration(CryptoPosition position)
        => IsPastMaxDuration(position, LastCandle1mCloseTimeDate);

    /// <summary>
    /// Has the position moved far enough into profit to arm the profit lock? The whole candle has to
    /// be past the trigger, not just the wick that reached it: for a long the lowest point of the
    /// OHLC (the low) has to sit above the trigger price, for a short the highest point (the high)
    /// has to sit below it. A candle that only spikes through the trigger and pulls back leaves the
    /// stop loss where it was.
    /// </summary>
    internal static bool ProfitLockArmed(CryptoTradeSide side, decimal breakEvenPrice,
        decimal triggerPercentage, decimal candleLow, decimal candleHigh, out decimal profitPercentage)
    {
        profitPercentage = 0;
        if (breakEvenPrice <= 0)
            return false;

        int multiplier = side == CryptoTradeSide.Long ? +1 : -1;
        decimal favorable = side == CryptoTradeSide.Long ? candleLow : candleHigh;
        profitPercentage = multiplier * (favorable - breakEvenPrice) / breakEvenPrice * 100m;
        return profitPercentage >= triggerPercentage;
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
        decimal? stop = result.Stop?.ClampPrice(position.Side, position.Symbol.PriceMinimum, position.Symbol.PriceMaximum, position.Symbol.PriceTickSize);
        decimal? limit = result.Limit?.ClampPrice(position.Side, position.Symbol.PriceMinimum, position.Symbol.PriceMaximum, position.Symbol.PriceTickSize);

        // Profit lock: once the position has reached MoveSlToBreakEvenPercentage in profit (the
        // trigger) with a whole candle, move the SL up to protect the profit (sticky — the flag
        // never resets, so a later pullback cannot loosen it). Where the stop lands depends on
        // MoveSlToBreakEvenMethod: Fixed puts it at BE + MoveSlToBreakEvenSlPercentage and leaves
        // it there, TrailingPercentage keeps it MoveSlToBreakEvenTrailPercentage behind the best
        // price the position has seen.
        // Keeping the SL percentage below the trigger leaves room between the price and the stop;
        // equal percentages put the stop right where the trigger fired, which is what the setting
        // did when it was one value.
        // Open DCA orders are cancelled separately in CancelOrdersIfClosedOrTimeoutOrReposition
        // once the flag is set.
        if (GlobalData.Settings.Trading.MoveSlToBreakEven
            && position.BreakEvenPrice > 0)
        {
            int multiplier = position.Side == CryptoTradeSide.Long ? +1 : -1;
            decimal triggerPct = GlobalData.Settings.Trading.MoveSlToBreakEvenPercentage;
            decimal trailPct = GlobalData.Settings.Trading.MoveSlToBreakEvenTrailPercentage;
            bool trailing = GlobalData.Settings.Trading.MoveSlToBreakEvenMethod == CryptoProfitLockMethod.TrailingPercentage;
            // Never place the stop beyond the trigger level: that is at or through the price that
            // just armed the lock, so it would fill on the spot.
            decimal lockPct = Math.Min(GlobalData.Settings.Trading.MoveSlToBreakEvenSlPercentage, triggerPct);

            if (!position.SlMovedToBreakEven
                && ProfitLockArmed(position.Side, position.BreakEvenPrice, triggerPct,
                    LastCandle1m.Low, LastCandle1m.High, out decimal profitPct))
            {
                position.SlMovedToBreakEven = true;
                string where = trailing ? $"trailing {trailPct:N2}% behind the price" : $"BE+{lockPct:N2}%";
                GlobalData.AddTextToLogTab($"{position.Symbol.Name} profit lock: SL moved to {where} (trigger {triggerPct:N2}%, profit reached {profitPct:N2}%)");
            }

            if (position.SlMovedToBreakEven)
            {
                decimal lockLevel;
                if (trailing)
                {
                    // Follow the best price the position has seen - for the trail that IS the high
                    // (the low for a short), even though arming needs the whole candle. The ratchet
                    // sits in the calculator and the level in TrailingStopPrice, so neither a
                    // pullback nor a restart can hand back ground the position already gained.
                    decimal best = position.Side == CryptoTradeSide.Long ? LastCandle1m.High : LastCandle1m.Low;
                    position.TrailingStopPrice = ProfitLockCalculator.TrailingStop(
                        position.Side, best, trailPct, position.TrailingStopPrice);
                    lockLevel = position.TrailingStopPrice;
                }
                else
                    lockLevel = position.BreakEvenPrice + multiplier * position.BreakEvenPrice * lockPct / 100m;

                decimal lockStop = lockLevel
                    .ClampPrice(position.Side, position.Symbol.PriceMinimum, position.Symbol.PriceMaximum, position.Symbol.PriceTickSize);
                decimal lockLimit = ProfitLockCalculator.StopLimit(position.Side, lockStop)
                    .ClampPrice(position.Side, position.Symbol.PriceMinimum, position.Symbol.PriceMaximum, position.Symbol.PriceTickSize);

                // Tighten only: move the stop when there was none, or when the lock level is
                // tighter than the current stop (long: higher is tighter; short: lower is tighter).
                if (ProfitLockCalculator.Tightens(position.Side, lockStop, stop))
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
                    price = price.ClampPrice(position.Side, Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

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

            // The entry order as it is really going to be placed. The whole set was already weighed
            // when the position was created, but two of its inputs are read again here: a market
            // entry takes the price of this moment, and an entry expressed as a percentage takes a
            // balance that has moved since. So the answer can have changed, and no order goes out
            // that the exchange would refuse. The position then stays Waiting and is timed out by
            // the usual Entry Remove Time rule.
            if (part.Purpose == CryptoPartPurpose.Entry && position.Invested == 0 &&
                !TradeTools.CheckOrderSetAgainstSymbolLimits(Symbol, position.Side, price, entryQuantity,
                    position.SlPercentage, out string entryLimitReason))
            {
                GlobalData.AddTextToLogTab($"{position.Symbol.Name} entry order not placed because {entryLimitReason}");
                return;
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
    /// Returns the (fixed % from the original entry price) target price for each missing level, in order,
    /// together with its index in the DCA list. That index is needed to price a level: its cost is the
    /// entry amount times ITS factor, and levels beyond the signal SL are skipped below - so counting
    /// on from existingDcaParts would line the factors up one level off.
    private List<(int levelIndex, decimal price)> GetMissingFixedPercentageDcaPrices(CryptoPosition position)
    {
        List<(int levelIndex, decimal price)> prices = [];

        // Een DCA zonder een voorgaande entry is onmogelijk
        if (!position.EntryPrice.HasValue || position.TpGridBreakEvenPrice == 0 || position.Invested == 0)
            return prices;

        // Afgesloten DCA parts sluiten we uit (omdat we zogenaamde jojo's uitvoeren, zie CanOpenAdditionalDca)
        int existingDcaParts = position.PartList.Values.Count(p => p.Purpose == CryptoPartPurpose.Dca && !p.CloseTime.HasValue);

        decimal entryPrice = position.TpGridBreakEvenPrice;
        for (int i = existingDcaParts; i < GlobalData.Settings.Trading.DcaList.Count; i++)
        {
            var dcaEntry = GlobalData.Settings.Trading.DcaList[i];

            // When the strategy provides a signal SL, skip DCA levels that fall beyond it —
            // those would never fill because the SL triggers first.
            if (position.SlPercentage.HasValue && dcaEntry.Percentage >= position.SlPercentage.Value)
                continue;

            decimal diffPrice = entryPrice * Math.Abs(dcaEntry.Percentage) / 100m;
            prices.Add((i, position.Side == CryptoTradeSide.Long ? entryPrice - diffPrice : entryPrice + diffPrice));
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


    /// <summary>
    /// Which of the missing DCA levels can be paid for right now, nearest level first.
    /// <para>
    /// What used to guard this spot was AssetTools.CheckAvailableAssets, and that asked the wrong
    /// question: it measures one ENTRY amount against the free balance, while what is about to be
    /// placed is a set of DCA orders of an entirely different size (entry x factor per level). So it
    /// refused on a balance that had room for the orders and passed on one that did not - which is
    /// where the "not enough cash available" lines in the log came from.
    /// </para>
    /// <para>
    /// Levels are dropped from the far end: the nearest one is hit first, so that is the one worth
    /// having. Whatever does not fit is simply missing from the position and gets another chance on
    /// the next pass, once a take profit or a stop loss has freed money up.
    /// </para>
    /// </summary>
    private List<(int levelIndex, decimal price)> AffordableDcaLevels(CryptoPosition position,
        List<(int levelIndex, decimal price)> missingLevels, string text)
    {
        // Without asset management nothing is refused for lack of money, and without a known entry
        // amount there is nothing to compute a level's cost from (HandleDcaPart falls back to
        // doubling the invested amount there, which is not ours to predict).
        if (!GlobalData.Settings.Trading.UseAssetManagement || !position.EntryAmount.HasValue)
            return missingLevels;

        var info = AssetTools.GetAsset(GlobalData.ActiveExchange!, Symbol);

        List<(int levelIndex, decimal price)> affordable = [];
        decimal committed = 0;
        foreach (var level in missingLevels)
        {
            // Same sum HandleDcaPart uses: the factor is a percentage of the entry amount
            decimal cost = (decimal)position.EntryAmount * GlobalData.Settings.Trading.DcaList[level.levelIndex].Factor / 100m;
            if (committed + cost > info.QuoteFree)
                break;
            committed += cost;
            affordable.Add(level);
        }

        if (affordable.Count < missingLevels.Count)
            GlobalData.AddTextToLogTab($"{text}: {affordable.Count} of {missingLevels.Count} level(s) fit in the free "
                + $"{Symbol.Quote}={info.QuoteFree} - the rest follows once money is freed up");

        return affordable;
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

            List<(int levelIndex, decimal price)> missingLevels = GetMissingFixedPercentageDcaPrices(position);
            if (missingLevels.Count > 0)
            {
                string text = $"{position.Symbol.Name} + {missingLevels.Count} DCA('s) bijplaatsen";

                // Zo laat mogelijk controleren vanwege extra calls naar de exchange
                var (success, reaction) = AssetTools.FetchAssets(GlobalData.ActiveExchange);
                if (!success)
                {
                    GlobalData.AddTextToLogTab(text + " " + reaction);
                    Symbol.ClearSignals();
                    return;
                }

                // Only the levels there is money for - measured against what a DCA order really costs
                missingLevels = AffordableDcaLevels(position, missingLevels, text);
                if (missingLevels.Count == 0)
                {
                    Symbol.ClearSignals();
                    return;
                }

                foreach ((_, decimal dcaPrice) in missingLevels)
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
                            cancelReason = "cancelling because the position is closing";
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
                                    cancelReason = "cancelling because of a timeout";
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
                            cancelReason = "cancelling because break even moved";
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


    /// <summary>
    /// Whether an exit order has to be cancelled and placed again because its price no longer
    /// matches what the calculation asks for. A difference of one tick or less does not count: that
    /// is the price grid shifting under an unchanged calculation, not a decision to exit somewhere
    /// else.
    /// <para>
    /// HyperLiquid publishes no tick size, so the scanner derives one per symbol and it used to come
    /// out one decimal finer or coarser than the hour before (see PriceTickFromMarkPrice in the
    /// HyperLiquid Perpetual Symbol.cs, fixed on 30-08-2026). Every take profit and stop loss order
    /// of an open position then differed from the recomputed price by a fraction of a tick, was
    /// cancelled and placed again in the same spot, and announced itself over Telegram as "cancel
    /// because of change BE" - while the break-even price had not moved at all. Costing the order
    /// its place in the queue and two exchange calls over a difference the exchange cannot even
    /// represent is not worth it, whichever exchange starts doing this next.
    /// </para>
    /// <para>
    /// A tick size of zero - an exchange that states none - falls back to the exact comparison this
    /// replaced.
    /// </para>
    /// </summary>
    internal static bool PriceMoved(decimal? current, decimal? wanted, decimal tickSize)
    {
        if (current == null && wanted == null)
            return false;
        if (current == null || wanted == null)
            return true;
        return Math.Abs(current.Value - wanted.Value) > tickSize;
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
                decimal tickSize = Symbol.PriceTickSize;
                foreach (var t in targets)
                {
                    CryptoPositionStep? order = PositionTools.FindPositionPartStep(t.Part, takeProfitOrderSide, false);
                    hadExistingOrder[t.Level] = order != null;
                    if (order == null || order.Quantity != t.Quantity
                        || PriceMoved(order.Price, t.Price, tickSize)
                        || PriceMoved(order.StopPrice, sl.stop, tickSize)
                        || PriceMoved(order.StopLimitPrice, sl.limit, tickSize))
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
                        GlobalData.AddErrorToLogTab($"Monitor {Symbol.Name} not all orders could be removed!!!! (partial filled or error?)");
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


    /// <summary>
    /// The price fence above, plus the one reason to look at a position that has nothing to do with
    /// price: its maximum duration has run out.
    /// <para>
    /// There are TWO gates between a candle and a repriced order, and both have to agree.
    /// CandleCanMovePosition decides whether the replay descends into the minute candles at all;
    /// this one decides whether the orders are recomputed once it has. Teaching only the first one
    /// about the deadline is not enough, and it fails in a way that looks like it works: measured on
    /// runs 436-438 (28-08-2026) a "7 day" limit still left positions running 36.8 days and a
    /// "30 day" limit changed nothing whatsoever, because the exit order was only ever repriced on a
    /// candle that happened to reach a trigger - which is exactly what the walked-away positions the
    /// rule exists for do not do.
    /// </para>
    /// </summary>
    internal static bool ShouldRunHandlePosition(CryptoPosition position, decimal candleHigh,
        decimal candleLow, DateTime now)
    {
        if (IsPastMaxDuration(position, now))
            return true;
        return ShouldRunHandlePosition(position, candleHigh, candleLow);
    }


    internal static void UpdateTriggerPrices(CryptoPosition position, decimal nearestTpPrice, decimal? slStop, decimal? nearestDcaPrice = null)
    {
        bool isLong = position.Side == CryptoTradeSide.Long;

        // Favorable side: nearest TP, capped by profit-lock threshold if applicable
        decimal favorablePrice = nearestTpPrice;
        if (GlobalData.Settings.Trading.MoveSlToBreakEven && position.BreakEvenPrice > 0)
        {
            int multiplier = isLong ? +1 : -1;
            decimal boundary = 0;

            if (!position.SlMovedToBreakEven)
            {
                // Not armed yet: wake up on the candle that reaches the trigger.
                decimal lockPct = GlobalData.Settings.Trading.MoveSlToBreakEvenPercentage;
                boundary = position.BreakEvenPrice + multiplier * position.BreakEvenPrice * lockPct / 100m;
            }
            else if (GlobalData.Settings.Trading.MoveSlToBreakEvenMethod == CryptoProfitLockMethod.TrailingPercentage
                     && position.TrailingStopPrice > 0)
            {
                // Armed and trailing: the stop has to move on every new extreme, so the boundary is
                // the price that would move it - anything short of that leaves the stop untouched
                // and HandlePosition can be skipped exactly as before.
                boundary = ProfitLockCalculator.PriceThatMovesTrailingStop(position.Side,
                    position.TrailingStopPrice, GlobalData.Settings.Trading.MoveSlToBreakEvenTrailPercentage);
            }

            if (boundary > 0)
            {
                if (isLong)
                    favorablePrice = Math.Min(favorablePrice, boundary);
                else
                    favorablePrice = Math.Max(favorablePrice, boundary);
            }
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
                if (ShouldRunHandlePosition(position, LastCandle1m.High, LastCandle1m.Low, LastCandle1mCloseTimeDate))
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

        // A position past its maximum duration has to be looked at even on a candle that reaches
        // no trigger price - that is the whole point of the deadline, and those candles are exactly
        // the quiet ones this gate would otherwise skip.
        if (IsPastMaxDuration(position, candleCloseTime.ToDateTime()))
            return true;

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
            GlobalData.AddErrorToLogTab($"{Symbol.Name} error Monitor {error.Message}");
        }
    }

}