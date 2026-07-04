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


    public PositionMonitor(CryptoSymbol symbol, CryptoCandle lastCandle1m)
    {
        Symbol = symbol;
        LastCandle1m = lastCandle1m;

        // The last final 1m candle
        LastCandle1mCloseTime = lastCandle1m.OpenTime + 1;
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
        if (Symbol.LastTradeDate.HasValue && Symbol.LastTradeDate?.AddMinutes(GlobalData.Settings.Trading.GlobalBuyCooldownTime) > LastCandle1m.Date)
        {
            reaction = "is in cooldown";
            GlobalData.AddTextToLogTab($"{text} {reaction} (removed)");
            Symbol.ClearSignals();
            return;
        }

        // Check the trading rules of the user (a quick drop of a symbol causes a pause)
        if (!TradingRules.CheckTradingRules(GlobalData.ActiveExchange!.Data.PauseTrading, LastCandle1m.OpenTime, 1))
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
                    if (!TradingConfig.Trading[signal.Side].Strategy.ContainsKey(signal.Strategy))
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
                            if (!SymbolTools.CheckValidMinimalVolume(Symbol, LastCandle1m.OpenTime, 1, out reaction))
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
                                // price (OverrideSignalPrice → signal.SignalPrice, e.g. baba's band,
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
                                    signal.Interval, signal.Strategy, GlobalData.Settings.Trading.EntryStrategy,
                                    entryPrice, LastCandle1mCloseTimeDate);

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

                                // Extend the position with a new DCA part using the configured DCA strategy.
                                PositionTools.ExtendPosition(Database, position, CryptoPartPurpose.Dca, signal.Interval, signal.Strategy,
                                    GlobalData.Settings.Trading.DcaStrategy, dcaPrice, LastCandle1mCloseTimeDate);
                                return;
                            }
                        }
                    }
                }
            }
        }
    }


    private async Task<(bool success, CryptoCandle candleInterval)> PrepareAsync(CryptoPosition position, CryptoPositionPart part)
    {
        // Stukje migratie, het interval van de part kan null zijn
        CryptoInterval interval = position.Interval!;
        if (part.Interval != null)
            interval = part.Interval;
        CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(interval.IntervalPeriod);



        // Maak beslissingen als de candle van het interval afgesloten is (dus NIET die van de 1m candle!)
        // Dus ook niet zomaar een laatste candle nemen in verband met Backtesting (echt even berekenen)
        CryptoCandle candleInterval = default;
        if (LastCandle1mCloseTime % interval.Duration != 0)
            return (false, candleInterval);
        CandleTime candleOpenTimeInterval = LastCandle1mCloseTime - interval.Duration;


        // Die indicator berekening had ik niet verwacht (cooldown?)
        await position.Symbol.Data.CandleLock.WaitAsync();
        try
        {
            CryptoCandle lastx = symbolInterval.LastCandle;

            // Niet zomaar een laatste candle nemen in verband met Backtesting
            if (!symbolInterval.CandleList.TryGetValue(candleOpenTimeInterval, out candleInterval))
            {
                string t = string.Format("candle 1m interval: {0}", candleOpenTimeInterval.ToLocalTime()) + " " +
                string.Format("is de candle op het {0} interval echt missing in action?", interval.Name);
                GlobalData.AddTextToLogTab($"Analyse {position.Symbol.Name} position={position.CreateTime} interval={interval.Name} {t}");
                //throw new Exception($"Candle niet aanwezig? {t}");
                return (false, candleInterval);
            }


            // Calculate indicators if needed
            var result = IndicatorEngine.CalculateIndicatorsForInterval(Symbol, interval, candleInterval!.OpenTime, interval.IntervalPeriod);
            if (!result.success)
                return (false, candleInterval);
        }
        finally
        {
            position.Symbol.Data.CandleLock.Release();
        }


        return (true, candleInterval);
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

    private decimal CalculateEntryOrDcaPrice(CryptoPosition position, CryptoPositionPart part,
        CryptoEntryOrDcaPricing buyOrderPricing, decimal defaultPrice)
    {
        // Wat wordt de prijs? (hoe graag willen we in de trade?)
        decimal price = defaultPrice;
        switch (buyOrderPricing)
        {
            case CryptoEntryOrDcaPricing.SignalPrice:
                price = CorrectBuyOrDcaPrice(position, price);
                break;
            //case CryptoEntryOrDcaPricing.BidPrice:
            //    if (position.Side == CryptoTradeSide.Long && part.Symbol.BidPrice.HasValue)
            //        price = part.Symbol.BidPrice ?? 0;
            //    else if (position.Side == CryptoTradeSide.Short && part.Symbol.AskPrice.HasValue)
            //        price = part.Symbol.BidPrice ?? 0;
            //    price = CorrectBuyOrDcaPrice(position, price);
            //    break;
            //case CryptoEntryOrDcaPricing.AskPrice:
            //    if (position.Side == CryptoTradeSide.Long && part.Symbol.AskPrice.HasValue)
            //        price = part.Symbol.BidPrice ?? 0;
            //    else if (position.Side == CryptoTradeSide.Short && part.Symbol.AskPrice.HasValue)
            //        price = part.Symbol.BidPrice ?? 0;
            //    price = CorrectBuyOrDcaPrice(position, price);
            //    break;
            case CryptoEntryOrDcaPricing.MarketPrice:
                price = part.Symbol.LastPrice ?? 0;
                break;
                //case CryptoEntryOrDcaPricing.SignalPriceWithPullback:
                //    // Take SignalPrice and pull it back by the configured percentage toward the
                //    // direction price would need to retrace for a fill — down for long, up for
                //    // short. Lands the entry inside the zone for smc.rejection / dlz.near style
                //    // signals where SignalPrice (= rejection close) is already outside the zone.
                //    {
                //        decimal pullbackPct = part.Purpose == CryptoPartPurpose.Entry
                //            ? GlobalData.Settings.Trading.EntryPullbackPercentage
                //            : GlobalData.Settings.Trading.DcaPullbackPercentage;
                //        price = defaultPrice;
                //        if (position.Side == CryptoTradeSide.Long)
                //            price = price * (100m - pullbackPct) / 100m;
                //        else
                //            price = price * (100m + pullbackPct) / 100m;
                //        price = CorrectBuyOrDcaPrice(position, price);
                //    }
                //    break;
                // De optie is vervallen maar blijft interessant, echter welke BB gebruik je dan (de actuele denk ik?, dus rekening houden met BE enzovoort)
                // voorlopig even afgesterd
                //case BuyPriceMethod.Sma20:
                //    if (price > (decimal)CandleData.Sma20)
                //        price = (decimal)CandleData.Sma20;
                //    break;
                // TODO: maar voorlopig even afgesterd - op zich voor de STOBB en/of SBM is deze okay
                //case BuyPriceMethod.LowerBollingerband:
                //    decimal lowerBand = (decimal)(CandleData.Sma20 - CandleData.BollingerBandsDeviation);
                //    if (price > lowerBand)
                //        price = lowerBand;
                //    break;
        }

        return price;
    }


    ///// <summary>
    ///// Kunnen we de positie afsluiten met de opgegeven winst perc
    ///// </summary>
    //private async Task HandleCheckProfitablePartClose(CryptoPosition position, CryptoPositionPart part, decimal perc)
    //{
    //    // TODO Long/Short

    //    // Is er iets om te verkopen in deze "part"? (part.Quantity > 0?)
    //    CryptoPositionStep step = PositionTools.FindPositionPartStep(part, CryptoOrderSide.Buy, true);
    //    if (step != null && (step.Status == CryptoOrderStatus.Filled || step.Status == CryptoOrderStatus.PartiallyFilled))
    //    {
    //        step = PositionTools.FindPositionPartStep(part, CryptoOrderSide.Sell, false);
    //        if (step != null)
    //        {
    //            // Als de actuele prijs ondertussen substantieel hoger dan winst proberen te nemen (jojo)
    //            // Dit verstoord eigenlijk de trailing sell, maar het is maar even zo...
    //            // Voorlopig even hardcoded (vanwege ontbreken OCO en stop order )
    //            decimal breakEven = part.BreakEvenPrice;
    //            decimal x = breakEven + breakEven * (perc / 100m);
    //            if (position.Symbol.LastPrice < x)
    //                return;

    //            // Als we reeds aan het trailen zijn heeft dat onze voorkeur (geen garanties op dat perc)
    //            if (step.Trailing == CryptoTrailing.Trailing)
    //            {
    //                GlobalData.AddTextToLogTab($"{Symbol.Name} is reeds aan het trailen, take profit exit");
    //                return;
    //            }


    //            // Annuleer de sell order
    //            var (cancelled, tradeParams) = await CancelOrder(position, part, step, CryptoOrderStatus.JoJoSell);
    //            if (GlobalData.Settings.Trading.LogCanceledOrders)
    //                ExchangeBase.Dump(position.Symbol, cancelled, tradeParams, "annuleren vanwege een jojo");


    //            // En zet de nieuwe sell order vlak boven de bekende prijs met (helaas) een limit order (had liever een OCO gehad)
    //            decimal sellPrice = x + Symbol.PriceTickSize;
    //            if (position.Symbol.LastPrice > sellPrice)
    //                sellPrice = (decimal)(position.Symbol.LastPrice + Symbol.PriceTickSize);
    //            decimal sellQuantity = part.Quantity;
    //            sellQuantity = sellQuantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);

    //            (bool result, TradeParams tradeParams) result;
    //            var exchangeApi = ExchangeHelper.GetExchangeInstance(GlobalData.Settings.General.ExchangeId);
    //            result = await exchangeApi.PlaceOrder(Database,
    //                position.TradeAccount, position.Symbol, LastCandle1mCloseTimeDate,
    //                CryptoOrderType.Limit, CryptoOrderSide.Sell, sellQuantity, sellPrice, null, null);

    //            if (result.result)
    //            {
    //                if (part.Purpose == CryptoPartPurpose.Entry)
    //                    position.SellPrice = result.tradeParams.Price;
    //                // Als vervanger van bovenstaande tzt (maar willen we die ook als een afzonderlijke step? Het zou ansich kunnen)
    //                var sellStep = PositionTools.CreatePositionStep(position, part, result.tradeParams);
    //                Database.Connection.Insert<CryptoPositionStep>(step);
    //                PositionTools.AddPositionPartStep(part, sellStep);
    //                part.StepOutMethod = CryptoStepInMethod.FixedPercentage; // niet helemaal waar, hebben we ervan gemaakt
    //                Database.Connection.Update<CryptoPositionPart>(part);
    //                Database.Connection.Update<CryptoPosition>(position);

    //                if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.PaperTrade)
    //                    PaperAssets.Change(GlobalData.ActiveExchange!, position.Symbol, result.tradeParams.OrderSide,
    //                        step.Status, result.tradeParams.Quantity, result.tradeParams.QuoteQuantity);

    //            }
    //            ExchangeBase.Dump(position.Symbol, result.result, result.tradeParams, "placing");
    //        }
    //    }
    //}



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
        if (GlobalData.Settings.Trading.TradeVia != CryptoTradeVia.PaperTrade)
            return (null, null);

        decimal? stop = null;
        decimal? limit = null;
        int multiplier = position.Side == CryptoTradeSide.Long ? +1 : -1;


        // --- Determine which SL source to use ---
        // Priority 1: signal-provided SL% (e.g. AtrRb/Baba strategy), anchored on SignalPrice.
        //   Only valid before any DCA activity: once a DCA is pending (ActiveDca) or filled
        //   (PartCount > 0), the signal SL is anchored too close to the entry and would fire
        //   before the DCA gets a chance to fill.
        bool useSignalSl = position.SlPercentage.HasValue && position.PartCount == 0 && !position.ActiveDca;

        // Priority 2: global user-configured SL%, anchored on the lowest/highest DCA step.
        //   StopLossPercentage must be strictly less than StopLossLimitPercentage so that the
        //   stop triggers before the limit (both measure distance from the anchor in the same direction).
        bool useGlobalSl = !useSignalSl && GlobalData.Settings.Trading.StopLossPercentage > 0;

        ScannerLog.Logger.Trace(
            $"PositionMonitor.CalculateSlPrices {position.Symbol.Name} {position.Side}: " +
            $"useSignalSl={useSignalSl} (SlPct={position.SlPercentage}) " +
            $"useGlobalSl={useGlobalSl} (StopPct={GlobalData.Settings.Trading.StopLossPercentage} LimitPct={GlobalData.Settings.Trading.StopLossLimitPercentage}) " +
            $"PartCount={position.PartCount} ActiveDca={position.ActiveDca}");

        if (useSignalSl)
        {
            // Now calculated from the original signalprice..
            // If price is already below that might be a problem
            decimal perc = position.SlPercentage!.Value / 100m;
            stop = position.SignalPrice - (multiplier * position.SignalPrice * perc);
            stop = stop.Value.Clamp(position.Symbol.PriceMinimum, position.Symbol.PriceMaximum, position.Symbol.PriceTickSize);

            // 1% buffer for the limit beyond the stop
            perc = 1m / 100m;
            limit = stop - (multiplier * stop.Value * perc);
            limit = limit.Value.Clamp(position.Symbol.PriceMinimum, position.Symbol.PriceMaximum, position.Symbol.PriceTickSize);
        }
        else if (useGlobalSl)
        {
            // Anchor on the most extreme DCA step (lowest buy for long, highest sell for short).
            // Falls back to EntryPrice when no DCA part exists yet.
            CryptoOrderSide dcaOrderSide = position.GetEntryOrderSide();

            // problem, if the dca has just been closed we use the global BE and risk being stopped out right away
            //CryptoPositionStep? stepDca = PositionTools.FindOpenStep(position, dcaOrderSide, CryptoPartPurpose.Dca);
            //if (stepDca != null)
            //    breakEven = stepDca.Price;
            CryptoPositionStep? stepDca;
            if (dcaOrderSide == CryptoOrderSide.Buy)
            {
                // Across all DCA parts: step with the lowest price (long: lowest dca=buy)
                stepDca = position.PartList.Values
                    .Where(p => p.Purpose == CryptoPartPurpose.Dca)
                    .SelectMany(p => p.StepList.Values)
                    .MinBy(s => s.Price);
            }
            else
            {
                // Across all DCA parts: step with the highest price (short: highest dca sell)
                stepDca = position.PartList.Values
                    .Where(p => p.Purpose == CryptoPartPurpose.Dca)
                    .SelectMany(p => p.StepList.Values)
                    .MaxBy(s => s.Price);
            }

            decimal lastDcaPrice;
            if (stepDca == null)
                lastDcaPrice = position.EntryPrice!.Value;
            else
                lastDcaPrice = stepDca.Price;

            // We are now using fixed percentages entry=0, dca1=4, dca2=10, stop=12, limit=13
            //breakEven = position.EntryPrice!.Value;

            // Stop price
            decimal perc = GlobalData.Settings.Trading.StopLossPercentage / 100m;
            stop = lastDcaPrice - (multiplier * lastDcaPrice * perc);
            stop = stop.Value.Clamp(position.Symbol.PriceMinimum, position.Symbol.PriceMaximum, position.Symbol.PriceTickSize);

            // Limit prijs x% lager
            // The limit percentage must stay strictly greater than the stop percentage so the stop
            // triggers before the limit. If it is misconfigured (<= stop), fall back to a 1% buffer
            // beyond the stop (same convention as the signal-SL branch) instead of throwing on every candle.
            decimal limitPerc = GlobalData.Settings.Trading.StopLossLimitPercentage;
            if (limitPerc <= GlobalData.Settings.Trading.StopLossPercentage)
                limitPerc = GlobalData.Settings.Trading.StopLossPercentage + 1m;
            perc = limitPerc / 100m;
            limit = lastDcaPrice - (multiplier * lastDcaPrice * perc);
            limit = limit.Value.Clamp(position.Symbol.PriceMinimum, position.Symbol.PriceMaximum, position.Symbol.PriceTickSize);
        }

        //// This does not work, it sets the sl directly after an entry hitting the sl to quick
        //// SL protection (break-even): once the position has reached MoveSlToBreakEvenPercentage in profit,
        //// pull the stop up to break-even and keep it there (sticky — the flag never resets, so a later
        //// pullback cannot loosen it). Paper-trade only, like the stop-loss handling above.
        //if (GlobalData.Settings.Trading.MoveSlToBreakEven
        //    && GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.PaperTrade
        //    && breakEven > 0)
        //{
        //    if (!position.SlMovedToBreakEven)
        //    {
        //        // Favorable extreme of the just-closed 1m candle: high for a long, low for a short.
        //        decimal favorable = position.Side == CryptoTradeSide.Long ? LastCandle1m.High : LastCandle1m.Low;
        //        decimal profitPct = multiplier * (favorable - breakEven) / breakEven * 100m;
        //        if (profitPct >= GlobalData.Settings.Trading.MoveSlToBreakEvenPercentage)
        //            position.SlMovedToBreakEven = true;
        //    }

        //    if (position.SlMovedToBreakEven)
        //    {
        //        decimal beStop = breakEven.Clamp(position.Symbol.PriceMinimum, position.Symbol.PriceMaximum, position.Symbol.PriceTickSize);
        //        decimal beGap = Math.Abs(beStop * 0.01m);
        //        decimal beLimit = (beStop - multiplier * beGap)
        //            .Clamp(position.Symbol.PriceMinimum, position.Symbol.PriceMaximum, position.Symbol.PriceTickSize);

        //        // Tighten only: pull the stop to BE when there was none, or when BE is tighter than the
        //        // current stop (long: a higher stop is tighter; short: a lower stop is tighter).
        //        if (stop == null
        //            || (multiplier == 1 && beStop > stop.Value)
        //            || (multiplier == -1 && beStop < stop.Value))
        //        {
        //            stop = beStop;
        //            limit = beLimit;
        //        }
        //    }
        //}

        return (stop, limit);
    }


    private async Task HandleEntryPart(CryptoPosition position, CryptoPositionPart part,
        CryptoEntryOrDcaStrategy strategy, CryptoEntryOrDcaPricing orderPricing)
    {
        // Controleer de entry
        CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
        CryptoPositionStep? step = PositionTools.FindPositionPartStep(part, entryOrderSide, false);


        // defaults
        string logText = "placing";
        decimal? entryPrice = null;
        CryptoOrderType entryOrderType; // = orderType;
        CryptoTrailing trailing = CryptoTrailing.None;

        //switch (strategy)
        {
            //case CryptoEntryOrDcaStrategy.AfterNextSignal:
            //    //entryOrderType = CryptoOrderType.Limit;
            //    //if (orderMethod == CryptoEntryOrDcaPrice.MarketPrice)
            //    //    entryOrderType = CryptoOrderType.Market;
            //    if (entryOrderType == CryptoOrderType.Market)
            //        orderPricing = CryptoEntryOrDcaPricing.MarketPrice;
            //    if (step == null && part.Quantity == 0) // entry
            //        entryPrice = CalculateEntryOrDcaPrice(position, part, orderPricing, part.SignalPrice);
            //    break;
            //case CryptoEntryOrDcaStrategy.FixedPercentage:
            // Afspraak= niet bijplaatsen indien de BM te laag is (anders jojo=weghalen+bijplaatsen)
            entryOrderType = CryptoOrderType.Limit;
            if (step == null && part.Quantity == 0) // entry
                entryPrice = CalculateEntryOrDcaPrice(position, part, orderPricing, part.SignalPrice);
            //break;
            //case CryptoEntryOrDcaStrategy.TrailViaKcPsar:
            //    trailing = CryptoTrailing.Trailing;
            //    entryOrderType = CryptoOrderType.StopLimit;
            //    // Trailing is afwijkend ten opzichte van de sell (zoveel mogelijk gelijk maken)

            //    // todo: Gaat deze vergelijking goed als er ook dust aanwezig kan zijn?
            //    // Moet de bestaande verplaatst worden (cq annuleren + opnieuw plaatsen)?
            //    //if (step != null && part.Quantity == 0 && step.Trailing == CryptoTrailing.Trailing)
            //    //{
            //    //    if (position.Side == CryptoTradeSide.Long)
            //    //    {
            //    //        decimal x = (decimal)Math.Max(candleInterval.CandleData?.KeltnerUpperBand ?? 0, candleInterval.CandleData?.PSar ?? 0) + Symbol.PriceTickSize;
            //    //        if (x < step.StopPrice && Symbol.LastPrice < x && candleInterval.High < x)
            //    //        {
            //    //            entryPrice = x;
            //    //            await TradeTools.CancelOrder(Database, position, part, step,
            //    //                LastCandle1mCloseTimeDate, CryptoOrderStatus.TrailingChange, "adjusting trailing");
            //    //        }
            //    //    }
            //    //    else
            //    //    {
            //    //        decimal x = (decimal)Math.Min(candleInterval.CandleData?.KeltnerLowerBand ?? 0, candleInterval.CandleData?.PSar ?? 0) - Symbol.PriceTickSize;
            //    //        if (x > step.StopPrice && Symbol.LastPrice > x && candleInterval.Low > x)
            //    //        {
            //    //            entryPrice = x;
            //    //            await TradeTools.CancelOrder(Database, position, part, step,
            //    //                LastCandle1mCloseTimeDate, CryptoOrderStatus.TrailingChange, "adjusting trailing");
            //    //        }
            //    //    }
            //    //}

            //    //if (step == null && part.Quantity == 0) // entry
            //    //{
            //    //    if (position.Side == CryptoTradeSide.Long)
            //    //    {
            //    //        // Alleen in een neergaande "trend" beginnen we met trailen (niet in een opgaande)
            //    //        // Dit is een fix om te voorkomen dat we direct na het kopen een trailing sell starten (maar of dit okay is?)
            //    //        if (Symbol.LastPrice >= (decimal?)candleInterval.CandleData?.PSar)
            //    //            return;

            //    //        decimal x = (decimal)Math.Max(candleInterval.CandleData?.KeltnerUpperBand ?? 0, candleInterval.CandleData?.PSar ?? 0) + Symbol.PriceTickSize;
            //    //        if (Symbol.LastPrice < x && candleInterval.High < x)
            //    //        {
            //    //            logText = "trailing";
            //    //            entryPrice = x;
            //    //        }
            //    //    }
            //    //    else
            //    //    {
            //    //        // Alleen in een opgaande "trend" beginnen we met trailen (niet in een neergaande)
            //    //        // Dit is een fix om te voorkomen dat we direct na het kopen een trailing buy starten (maar of dit okay is?)
            //    //        if (Symbol.LastPrice <= (decimal?)candleInterval.CandleData?.PSar)
            //    //            return;

            //    //        decimal x = (decimal)Math.Min(candleInterval.CandleData?.KeltnerLowerBand ?? 0, candleInterval.CandleData?.PSar ?? 0) - Symbol.PriceTickSize;
            //    //        if (Symbol.LastPrice > x && candleInterval.Low > x)
            //    //        {
            //    //            logText = "trailing";
            //    //            entryPrice = x;
            //    //        }
            //    //    }
            //    //}
            //    break;
            //default:
            //    throw new Exception($"{strategy} niet ondersteund");
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
                if (GlobalData.ActiveExchange!.Data.AssetList.TryGetValue(Symbol.Quote, out var asset))
                    currentAssetQuantity = asset.Total;
                entryValue = TradeTools.GetEntryAmount(Symbol, currentAssetQuantity);
                GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.PartCount} entry {part.PartNumber} value={entryValue}");
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
                    GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.PartCount} dca {part.PartNumber} value={entryValue}");
                }
                else
                {
                    // DCA, verdubbelen, gebaseerd op Zignally (geeft snel een asset tekort)
                    entryValue = position.Invested - position.Returned + position.Commission;
                    GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.PartCount} extra {part.PartNumber} value={entryValue}");
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

                AltradyWebhook.DelegateControlToAltrady(position);
                Database.Connection.Update(position);
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
                        part.EntryMethod = strategy;
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
                            await PaperTrading.CreatePaperTrade(Database, position, part, step, LastCandle1m.Close, LastCandle1m.OpenTime);
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



    //private async Task HandleTakeProfitPart(CryptoPosition position, CryptoPositionPart part, CryptoCandle candleInterval)
    //{
    //    CryptoOrderSide dcaOrderSide = position.GetEntryOrderSide();

    //    // Is er wel iets om te verkopen in deze "part"? (hetzelfde als part.Quantity !=0 of part.Invested != 0?)
    //    CryptoPositionStep stepEntry = PositionTools.FindPositionPartStep(part, dcaOrderSide, true);
    //    if (stepEntry != null && (stepEntry.Status.IsFilled() || stepEntry.Status == CryptoOrderStatus.PartiallyFilled)) // Partially?
    //    {
    //        // TODO, is er genoeg Quantity van de symbol om het te kunnen verkopen? (min-quantity en notation)
    //        // (nog niet opgemerkt in reallive trading, maar dit gaat zeker een keer gebeuren in de toekomst!)

    //        CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();
    //        CryptoPositionStep stepProfit = PositionTools.FindPositionPartStep(part, takeProfitOrderSide, false);
    //        if (stepProfit == null && part.Quantity > 0)
    //        {
    //            decimal takeProfitPrice = CalculateTpPrices(position);
    //            await TradeTools.PlaceTakeProfitOrderAtPrice(Database, position, part, takeProfitPrice, LastCandle1mCloseTimeDate, "placing");
    //        }
    //        //else if (step != null && part.Quantity > 0 && part.BreakEvenPrice > 0 && GlobalData.Settings.Trading.SellMethod == CryptoSellMethod.TrailViaKcPsar)
    //        //{
    //        //    // TODO Long/Short, trailing order...
    //        //    bool doIt = false;

    //        //    // Als de actuele prijs ondertussen substantieel hoger dan winst proberen te nemen (jojo)
    //        //    // Dit verstoord eigenlijk de trailing sell, maar het is maar even zo...
    //        //    // Voorlopig even hardcoded (vanwege ontbreken OCO en stop order )
    //        //    // TODO: Hier nog eens een instelling van maken!
    //        //    // De winst ppercentage is nu eigenlijk de trigger prijs!
    //        //    decimal breakEven = part.BreakEvenPrice;
    //        //    decimal breakEvenExtra = breakEven + breakEven * (GlobalData.Settings.Trading.ProfitPercentage / 100m);

    //        //    //if (position.Symbol.LastPrice > breakEvenExtra) // LastPrice is niet altijd gezet
    //        //    //    doIt = true;

    //        //    // Als de candle in zijn geheel boven de BE + extra zit beginnen met trailen (de zogenaamde trigger)
    //        //    if (candleInterval.Open > breakEvenExtra && candleInterval.Close > breakEvenExtra)
    //        //        doIt = true;


    //        //    // Trailing SELL
    //        //    // Alleen in een opwaarste "trend" beginnen we met trailen (niet in een neergaande)
    //        //    // Dit is een fix om te voorkomen dat we direct na het kopen een trailing sell starten
    //        //    if (step.Trailing == CryptoTrailing.None && candleInterval.Low > (decimal)candleInterval.CandleData.PSar && !doIt)
    //        //        return;


    //        //    decimal x;
    //        //    List<decimal> qqq = [];

    //        //    // Via de psar trailen ipv KC/psar? (dat zou zelfs een instelling kunnen zijn)
    //        //    //x = (decimal)candleInterval.CandleData.PSar - Symbol.PriceTickSize;
    //        //    //qqq.Add(x.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize));
    //        //    x = breakEvenExtra;
    //        //    if (x > breakEvenExtra)
    //        //        qqq.Add(x);

    //        //    x = Math.Min((decimal)candleInterval.CandleData.KeltnerLowerBand, (decimal)candleInterval.CandleData.PSar) - Symbol.PriceTickSize;
    //        //    x = x.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
    //        //    if (x > breakEvenExtra)
    //        //        qqq.Add(x);

    //        //    //x = (((decimal)candleInterval.CandleData.BollingerBandsUpperBand + (decimal)candleInterval.CandleData.BollingerBandsLowerBand) / 2m) - Symbol.PriceTickSize;
    //        //    x = (decimal)candleInterval.CandleData.Sma20 - Symbol.PriceTickSize;
    //        //    x = x.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
    //        //    if (x > breakEvenExtra)
    //        //        qqq.Add(x);

    //        //    x = (decimal)candleInterval.CandleData.KeltnerUpperBand - Symbol.PriceTickSize;
    //        //    x = x.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
    //        //    if (x > breakEvenExtra)
    //        //        qqq.Add(x);

    //        //    x = (decimal)candleInterval.CandleData.BollingerBandsUpperBand - Symbol.PriceTickSize;
    //        //    x = x.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
    //        //    if (x > breakEvenExtra)
    //        //        qqq.Add(x);

    //        //    // De hoogst mogelijke waarde nemen (extra controles op de low anders wordt ie direct gevuld)
    //        //    decimal stop = 0;
    //        //    qqq.Sort((valueA, valueB) => valueB.CompareTo(valueA));
    //        //    foreach (var stopX in qqq)
    //        //    {
    //        //        if (step.Status == CryptoOrderStatus.New && step.Side == CryptoOrderSide.Sell
    //        //            //&& Symbol.LastPrice > stopX
    //        //            && stopX > breakEvenExtra
    //        //            && candleInterval.Low > stopX
    //        //            && (step.StopPrice == null || stopX > step.StopPrice))
    //        //        {
    //        //            decimal oldPrice = stop;
    //        //            stop = stopX;
    //        //            if (oldPrice > 0)
    //        //                GlobalData.AddTextToLogTab($"{Symbol.Name} SELL correction stop -> {oldPrice:N6} to {stop.ToString0()}");
    //        //        }
    //        //        //else break;
    //        //    }

    //        //    if (stop > 0)
    //        //    {
    //        //        var exchangeApi = ExchangeHelper.GetExchangeInstance(GlobalData.Settings.General.ExchangeId);

    //        //        // price moet lager, 1.5% moet genoeg zijn.
    //        //        decimal price = stop - (stop * 1.5m / 100); // ergens eronder
    //        //        price = price.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);


    //        //        var (cancelled, cancelParams) = await TradeTools.CancelOrder(Database, position, part, step, LastCandle1mCloseTimeDate, CryptoOrderStatus.TrailingChange);
    //        //        if (!cancelled || GlobalData.Settings.Trading.LogCanceledOrders)
    //        //            ExchangeBase.Dump(position, cancelled, cancelParams, "annuleren vanwege aanpassing stoploss trailing");

    //        //        // Afhankelijk van de invoer stop of stoplimit een OCO of standaard sell plaatsen.
    //        //        // TODO: Wat als het plaatsen van de order fout gaat? (hoe vangen we de fout op en hoe herstellen we dat? Binance is een bitch af en toe!)
    //        //        //Api exchangeApi = new();
    //        //        var (success, tradeParams) = await exchangeApi.PlaceOrder(Database,
    //        //            position.TradeAccount, position.Symbol, position.Side, LastCandle1mCloseTimeDate,
    //        //            CryptoOrderType.StopLimit, CryptoOrderSide.Sell,
    //        //            step.Quantity, price, stop, null); // Was een OCO met een limit
    //        //        if (success)
    //        //        {
    //        //            // Administratie van de nieuwe sell bewaren (iets met tonen van de posities)
    //        //            if (!position.ProfitPrice.HasValue)
    //        //                position.ProfitPrice = price; // part.SellPrice; // (kan eigenlijk weg, slechts ter debug en tracering, voila)
    //        //            // Als vervanger van bovenstaande tzt (maar willen we die ook als een afzonderlijke step? Het zou ansich kunnen)
    //        //            var sellStep = PositionTools.CreatePositionStep(position, part, tradeParams, CryptoTrailing.Trailing);
    //        //            Database.Connection.Insert<CryptoPositionStep>(sellStep);
    //        //            PositionTools.AddPositionPartStep(part, sellStep);
    //        //            part.ProfitMethod = CryptoEntryOrProfitMethod.TrailViaKcPsar;
    //        //            Database.Connection.Update<CryptoPositionPart>(part);
    //        //            Database.Connection.Update<CryptoPosition>(position);

    //        //            if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.PaperTrade)
    //        //                PaperAssets.Change(GlobalData.ActiveExchange!, position.Symbol, tradeParams.OrderSide,
    //        //                    step.Status, tradeParams.Quantity, tradeParams.QuoteQuantity);
    //        //        }

    //        //        decimal perc = 0;
    //        //        if (part.BreakEvenPrice > 0)
    //        //            perc = (decimal)(100 * ((stop / part.BreakEvenPrice) - 1));
    //        //        ExchangeBase.Dump(position, success, tradeParams, $"locking ({perc:N2}%)");
    //        //    }
    //        //}

    //    }
    //}


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
            decimal diffPrice = entryPrice * Math.Abs(dcaEntry.Percentage) / 100m;
            prices.Add(position.Side == CryptoTradeSide.Long ? entryPrice - diffPrice : entryPrice + diffPrice);
        }
        return prices;
    }


    private async Task CheckAddDcaFixedPercentage(CryptoPosition position)
    {
        // Alle resterende DCA-niveaus in 1x plaatsen zodra de entry gevuld is (in plaats van steeds te
        // wachten tot de vorige DCA gevuld is) - elk niveau krijgt zijn eigen part op zijn vaste %-prijs
        // vanaf de entry, en wordt direct als losse open limit-order neergezet.
        if (position.Status == CryptoPositionStatus.Trading && GlobalData.Settings.Trading.DcaStrategy == CryptoEntryOrDcaStrategy.FixedPercentage)
        {
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
                        CryptoEntryOrDcaStrategy.FixedPercentage, price, LastCandle1mCloseTimeDate);
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
                        //else if (part.EntryMethod != CryptoEntryOrProfitMethod.FixedPercentage && step.Trailing == CryptoTrailing.None)
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

                        // Verwijderen de buy vanwege een te lage barometer, pauseer stand of timeout (behalve trailing of reserved dca)
                        //else if (PauseBecauseOfTradingRules)
                        //{
                        //    timeOut = true;
                        //    closePart = false;
                        //    newStatus = CryptoOrderStatus.TradingRules;
                        //    cancelText = "annuleren vanwege trading regels";
                        //}


                        // Verwijderen de buy vanwege een te lage barometer, pauseer stand of timeout (behalve trailing of reserved dca)
                        // (je wordt gek van het weghalen en opnieuw plaatsen van de orders)
                        // (en je mist zo ook een heleboel goede kansen, dus weg ermee!)
                        //else if (PauseBecauseOfBarometer)
                        //{
                        //    timeOut = true;
                        //    closePart = false;
                        //    newStatus = CryptoOrderStatus.BarameterToLow;
                        //    cancelText = "annuleren vanwege lage barometer";
                        //}

                        // Als de instellingen veranderd zijn de lopende order annuleren
                        else if (part.Purpose == CryptoPartPurpose.Entry && part.EntryMethod != GlobalData.Settings.Trading.EntryStrategy)
                        {
                            newStatus = CryptoOrderStatus.ChangedSettings;
                            cancelReason = "annuleren vanwege aanpassing entry instellingen";
                        }

                        // Als de instellingen veranderd zijn de lopende order annuleren
                        else if (part.Purpose == CryptoPartPurpose.Dca && part.EntryMethod != GlobalData.Settings.Trading.DcaStrategy)
                        {
                            newStatus = CryptoOrderStatus.ChangedSettings;
                            cancelReason = "annuleren vanwege aanpassing dca instellingen";
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


                        // Note: there is no separate TP-strategy setting yet (unlike EntryStrategy / DcaStrategy),
                        // so changed-settings detection for take-profit orders is not implemented here.
                    }


                    if (cancelReason != "")
                    {
                        var (success, _) = await TradeTools.CancelOrder(Database, position, part, step,
                            LastCandle1mCloseTimeDate, newStatus, cancelReason);
                        if (success)
                        {
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
                //var (success, candleInterval) = await PrepareAsync(position, part);
                //if (success)
                //{
                //if (candleInterval.OpenTime != 0)
                //{
                // Check entry - blocked during a market-wide TradingRules pause (no new positions during a fast move)
                if (!PauseBecauseOfTradingRules && part.Purpose == CryptoPartPurpose.Entry)
                    await HandleEntryPart(position, part, GlobalData.Settings.Trading.EntryStrategy,
                        GlobalData.Settings.Trading.EntryOrderPrice);

                // Check DCA - always allowed, even during a TradingRules pause (averaging into an
                // existing position is not gated by the market-wide pause, see CheckThePosition)
                if (part.Purpose == CryptoPartPurpose.Dca)
                    await HandleEntryPart(position, part, GlobalData.Settings.Trading.DcaStrategy,
                        GlobalData.Settings.Trading.DcaOrderPrice);
                //}
                //}


                //if (GlobalData.Settings.Trading.LockProfits)
                //{
                //    // Kunnen we afsluiten met winst?
                //    if (position.Quantity > 0)
                //    {
                //        if (position.CreateTime.AddDays(-20) > LastCandle1mCloseTimeDate)
                //            await HandleCheckProfitablePartClose(position, part, 0.25m);
                //        else if (position.CreateTime.AddDays(-10) > LastCandle1mCloseTimeDate)
                //            await HandleCheckProfitablePartClose(position, part, 0.50m);
                //        else
                //            await HandleCheckProfitablePartClose(position, part, GlobalData.Settings.Trading.ProfitPercentage);
                //    }
                //}

            }
            // remember the tp parts, one per configured TP level - PartNumber is the 1-based level
            // ("TP 1", "TP 2", ...); convert back to the 0-based index used for TpList/levels lookups.
            if (part.Purpose == CryptoPartPurpose.TakeProfit)
                takeProfitPartsByLevel[part.PartNumber - 1] = part;
        }


        if (position.Quantity > 0)
        {
            CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();
            List<CryptoTpEntry> levels = GlobalData.Settings.Trading.TpList;

            // A level stays "open" until its part exists and has been fully filled (CloseTime set).
            List<int> openLevelIndexes = [];
            for (int i = 0; i < levels.Count; i++)
            {
                bool closed = takeProfitPartsByLevel.TryGetValue(i, out CryptoPositionPart? existing) && existing.CloseTime.HasValue;
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
                            position.Strategy, CryptoEntryOrDcaStrategy.FixedPercentage, 0, GlobalData.Clock.UtcNow);
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
                        GlobalData.AddTextToLogTab($"Monitor {Symbol.Name} Niet alle orders konden verwijderd worden!!!! (partial filled or error?)");
                }
            }
        }

        //// Is er wel een initiele TP order aanwezig? zoniet dan dit alsnog doen!
        //// (buiten de PrepareIndicators loop gehaald die intern een controle op het interval doet)
        //// Dus nu wordt de sell order vrijwel direct geplaatst (na een 1m candle)
        //foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        //{
        //    // voor de niet afgesloten parts...
        //    if (!part.CloseTime.HasValue)
        //    {
        //        CryptoPositionStep step = PositionTools.FindPositionPartStep(part, dcaOrderSide, true);
        //        if (step != null && step.Status.IsFilled())
        //        //if (step != null && (step.Status == CryptoOrderStatus.Filled /*|| step.Status == CryptoOrderStatus.PartiallyFilled*/)) -- problemen, quick fix voor nu, order laten staan
        //        {
        //            if (position.Quantity > 0) // voldoende saldo om de sell te plaatsen
        //            {
        //                step = PositionTools.FindPositionPartStep(part, takeProfitOrderSide, false);
        //                if (step == null)
        //                {
        //                    decimal takeProfitPrice = CalculateTpPrices(position);
        //                    await TradeTools.PlaceTakeProfitOrderAtPrice(Database, position, part, takeProfitPrice, LastCandle1mCloseTimeDate, "placing");
        //                }
        //                else
        //                {
        //                    // Als we het verkoop percentages aangepast hebben is het wel prettig dat de order aangepast wordt)
        //                    if (part.ProfitMethod == CryptoEntryOrProfitMethod.FixedPercentage)
        //                    {
        //                        decimal sellPrice = CalculateTpPrices(position);
        //                        if (step.Price != sellPrice && step.Status == CryptoOrderStatus.New && !part.ManualOrder)
        //                        {
        //                            string cancelReason = $"annuleren vanwege aanpassing verkoop prijs ({step.Price} -> {sellPrice})";
        //                            var (success, _) = await TradeTools.CancelOrder(Database, position, part, step,
        //                                LastCandle1mCloseTimeDate, CryptoOrderStatus.ChangedSettings, cancelReason);
        //                            if (success)
        //                            {
        //                                decimal takeProfitPrice = CalculateTpPrices(position);
        //                                await TradeTools.PlaceTakeProfitOrderAtPrice(Database, position, part, takeProfitPrice, LastCandle1mCloseTimeDate, "modifying");
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}
        //CryptoOrderSide dcaOrderSide = position.GetEntryOrderSide();

        //// Is er wel iets om te verkopen in deze "part"? (hetzelfde als part.Quantity !=0 of part.Invested != 0?)
        //CryptoPositionStep stepEntry = PositionTools.FindPositionPartStep(part, dcaOrderSide, true);
        //if (stepEntry != null && (stepEntry.Status.IsFilled() || stepEntry.Status == CryptoOrderStatus.PartiallyFilled)) // Partially?
        //{
        //    // TODO, is er genoeg Quantity van de symbol om het te kunnen verkopen? (min-quantity en notation)
        //    // (nog niet opgemerkt in reallive trading, maar dit gaat zeker een keer gebeuren in de toekomst!)

        //    CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();
        //    CryptoPositionStep stepProfit = PositionTools.FindPositionPartStep(part, takeProfitOrderSide, false);
        //    if (stepProfit == null && part.Quantity > 0)
        //    {
        //        decimal takeProfitPrice = CalculateTpPrices(position);
        //        await TradeTools.PlaceTakeProfitOrderAtPrice(Database, position, part, takeProfitPrice, LastCandle1mCloseTimeDate, "placing");
        //    }
        //}
    }



    //public async Task<List<CryptoSignal>> CreateSignalsAsync()
    //{
    //    List<CryptoSignal> signalList = [];
    //    //GlobalData.Logger.Info($"CreateSignals(start):" + LastCandle1m.OhlcText(Symbol, GlobalData.IntervalList[0], Symbol.PriceDisplayFormat, true, false, true));
    //    if (GlobalData.Settings.Signal.Active && Symbol.QuoteData!.FetchCandles && Symbol.Status == 1 && Symbol.LastPrice != null)
    //    {
    //        // TODO: !!! This is different than te previous version (was not executed for zones) !!!
    //        // not really sure if we want this for zones? The same goes for trend and barometer????
    //        if (Symbol.QuoteData.MinimalVolume == 0 || Symbol.Volume <= Symbol.QuoteData.MinimalVolume)
    //        {
    //            Symbol.ClearSignals();
    //            return [];
    //        }

    //        // Is the symbol a new one?
    //        if (!SymbolTools.CheckNewCoin(Symbol, out string reaction))
    //        {
    //            if (GlobalData.Settings.Signal.LogSymbolMustExistsDays)
    //                GlobalData.AddTextToLogTab($"Monitor {Symbol.Name} {reaction} (removed)");
    //            if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == Symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
    //                GlobalData.AddTextToLogTab($"Monitor {Symbol.Name} {reaction} (removed)");
    //            Symbol.ClearSignals();
    //            return [];
    //        }

    //        // prepare indicators, fvg and dlz zones (dlz will be delayed because of background fetching & zooming)
    //        SignalPrepare.Execute(Symbol, LastCandle1m, LastCandle1mCloseTime);



    //        foreach (CryptoTradeSide side in Enum.GetValues(typeof(CryptoTradeSide)))
    //        {
    //            // Barometer check
    //            if (!BarometerHelper.ValidBarometerConditions(GlobalData.ActiveExchange!, Symbol.Quote, TradingConfig.Signals[side].Barometer, out reaction))
    //            {
    //                if (TradingConfig.Signals[side].BarometerLog)
    //                    GlobalData.AddTextToLogTab($"{Symbol.Name} {side} {reaction}");
    //            }
    //            else
    //            {
    //                // Only for certain strategies and intervals
    //                foreach (CryptoInterval interval in TradingConfig.Signals[side].Interval.ToList())
    //                {
    //                    // (0 % 180 = 0, 60 % 180 = 60, 120 % 180 = 120, 180 % 180 = 0)
    //                    if (LastCandle1mCloseTime % interval.Duration == 0)
    //                    {
    //                        //GlobalData.Logger.Info($"analyze({interval.Name}):" + LastCandle1m.OhlcText(Symbol, interval, Symbol.PriceDisplayFormat, true, false, true));

    //                        // We geven als tijd het begin van de "laatste" candle (van dat interval)
    //                        SignalCreate createSignal = new(Symbol, interval, side, LastCandle1mCloseTime);
    //                        if (await createSignal.AnalyzeAsync(LastCandle1mCloseTime - interval.Duration))
    //                            signalList.AddRange(createSignal.SignalList);

    //                        // Teller voor op het beeldscherm zodat je ziet dat deze thread iets doet en actief blijft.
    //                        Interlocked.Increment(ref analyseCount);
    //                    }
    //                }
    //            }


    //            // FVG - Fair Value Gaps DlzAdmin
    //            if ((side == CryptoTradeSide.Long && GlobalData.Settings.Signal.ZonesFvg.ShowSignalsLong) ||
    //                (side == CryptoTradeSide.Short && GlobalData.Settings.Signal.ZonesFvg.ShowSignalsShort))
    //            {
    //                // Signal if the 1m candles touches a fvg zone
    //                SignalCreate createSignal2 = new(Symbol, GlobalData.IntervalList[0], side, LastCandle1mCloseTime);
    //                if (await createSignal2.AnalyzeFairValueGapAsync(LastCandle1mCloseTime))
    //                    signalList.AddRange(createSignal2.SignalList);
    //            }


    //            // DLZ - Dominant Liquidity DlzAdmin
    //            if ((side == CryptoTradeSide.Long && GlobalData.Settings.Signal.ZonesDlz.ShowSignalsLong) ||
    //                (side == CryptoTradeSide.Short && GlobalData.Settings.Signal.ZonesDlz.ShowSignalsShort))
    //            {
    //                // Signal if the 1m candles approaches or touches a dlz zone
    //                SignalCreate createSignal = new(Symbol, GlobalData.IntervalList[0], side, LastCandle1mCloseTime);
    //                if (await createSignal.AnalyzeZonesAsync(LastCandle1mCloseTime - GlobalData.IntervalList[0].Duration))
    //                    signalList.AddRange(createSignal.SignalList);
    //            }
    //        }
    //    }
    //    //GlobalData.Logger.Info($"CreateSignals(stop):" + LastCandle1m.OhlcText(Symbol, GlobalData.IntervalList[0], Symbol.PriceDisplayFormat, true, false, true));

    //    return signalList;
    //}


    public async Task CheckThePosition(CryptoPosition position)
    {
        // Pauzeren vanwege de trading regels of te lage barometer
        PauseBecauseOfTradingRules = !TradingRules.CheckTradingRules(GlobalData.ActiveExchange!.Data.PauseTrading, LastCandle1m.OpenTime, 1);

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
                // Een DCA op een bestaande positie altijd direct toestaan, ook tijdens een
                // marktbrede TradingRules-pauze (bv. snelle BTC-beweging) - alleen nieuwe
                // entries worden door die pauze geblokkeerd, niet het bijkopen op een lopende positie.
                await CheckAddDcaFixedPercentage(position);
                long profHandleStart = Stopwatch.GetTimestamp();
                profDcaTicks = profHandleStart - profDcaStart;

                // Plaats of modificeer de buy of sell orders + optionele LockProfits
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
    /// We have a new 1m candle, calculate the signals
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

            if (!Symbol.IsTrading())
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
                if (!Symbol.CheckValidMinimalVolume(LastCandle1mCloseTime, 1, out response))
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

            // Calculate all the indicators, queue the fvg and dlz zones etc
            SignalPrepare.Execute(Symbol, LastCandle1m, LastCandle1mCloseTime);
            long profExecuteStart = Stopwatch.GetTimestamp();

            // Calculate signals and touch of the dlz and fvg zones
            await SignalExecute.ExecuteAsync(Symbol, LastCandle1mCloseTime);
            long profTradeStart = Stopwatch.GetTimestamp();

            //GlobalData.Logger.Trace($"NewCandleArrivedAsync.Positions " + traceText);

            // Simulate Trade indien openstaande orders gevuld zijn
            //GlobalData.Logger.Info($"analyze.PaperTradingCheckOrders({Symbol.Name})");
            if (GlobalData.Settings.Trading.TradeVia != CryptoTradeVia.RealTrading)
                await PaperTrading.PaperTradingCheckOrders(Database, GlobalData.ActiveExchange!, this.Symbol, LastCandle1m);

            // Pause because of trading rules or low barometer
            PauseBecauseOfTradingRules = !TradingRules.CheckTradingRules(GlobalData.ActiveExchange!.Data.PauseTrading, LastCandle1m.OpenTime, 1);

            //TODO: Reuse the preparedIndicatorDataList in the CreateOrExtendPositionAsync?
            // Open or extend a position
            //if (signalList.Count > 0) // alway's?
            await CreateOrExtendPositionAsync();
            long profPositionCheckStart = Stopwatch.GetTimestamp();

            // Check the positions
            // Profiling: dedicated wrap of exactly this statement, as a cross-check against the
            // positionCheck bucket below (which times the same statement via subtraction) — the two
            // totals should match.
            long profAddToQueueStart = Stopwatch.GetTimestamp();
            if (GlobalData.ActiveExchange!.Data.PositionList.TryGetValue(Symbol.Name, out CryptoPosition? position))
                await GlobalData.ThreadCheckPosition!.AddToQueue(position!);
            PipelineProfiler.RecordAddToQueue(Stopwatch.GetTimestamp() - profAddToQueueStart);

            PipelineProfiler.Record(
                prepare: profExecuteStart - profPrepareStart,
                execute: profTradeStart - profExecuteStart,
                trade: profPositionCheckStart - profTradeStart,
                positionCheck: Stopwatch.GetTimestamp() - profPositionCheckStart);

            //GlobalData.Logger.Trace($"NewCandleArrivedAsync.Clean " + traceText);

            // Remove old candles or CandleData
            // Profiling: this tail previously ran AFTER PipelineProfiler.Record above, so it fell
            // outside every bucket. Gated behind !IsEmulatorMode, so it stays ~0 in emulator runs.
            long profCleanCandleStart = Stopwatch.GetTimestamp();
            if (Symbol.Data.ZoneLock.CurrentCount > 0)
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