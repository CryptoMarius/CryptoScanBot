using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Signal;
using CryptoScanBot.Core.Trader;
using CryptoScanBot.Core.Exchange;
using Dapper.Contrib.Extensions;
using CryptoScanBot.Signal;

namespace CryptoScanBot.Core.Intern;

public class PositionMonitor : IDisposable
{
    // Tellertje die getoond wordt in applicatie (indicatie van aantal meldingen)
    private static int analyseCount;
    public static int AnalyseCount { get { return analyseCount; } }

    public CryptoSymbol Symbol { get; set; }
    public Model.CryptoExchange Exchange { get; set; }
#if TRADEBOT
    private static readonly SemaphoreSlim Semaphore = new(1);
#endif

    // De laatste gesloten 1m candle
    public CryptoCandle LastCandle1m { get; set; }
    // De sluittijd van deze candle (als unixtime) - De CurrentTime bij backtesting
    public long LastCandle1mCloseTime { get; set; }
    // De sluittijd van deze candle (als DateTime) - De CurrentTime bij backtesting
    public DateTime LastCandle1mCloseTimeDate { get; set; }
    public CryptoDatabase Database { get; set; } = new();
    public bool PauseBecauseOfTradingRules { get; set; } = false;


    public PositionMonitor(CryptoSymbol symbol, CryptoCandle lastCandle1m)
    {
        Symbol = symbol;
        Exchange = symbol.Exchange;

        // De laatste 1m candle die definitief is
        LastCandle1m = lastCandle1m;
        LastCandle1mCloseTime = lastCandle1m.OpenTime + 60;
        LastCandle1mCloseTimeDate = CandleTools.GetUnixDate(LastCandle1mCloseTime);

        Database.Open();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Database != null)
            {
                Database.Close();
                Database.Dispose();
                //Database = null;
            }
        }
    }

#if TRADEBOT
    private bool CanOpenAdditionalDca(CryptoPosition position, 
        out CryptoPositionStep? step, out decimal percentage, out decimal dcaPrice, out string reaction)
    {
        dcaPrice = 0;
        percentage = 0;

        // Oud - We willen niet meer dan X keer bijkopen
        //if (position.PartCount > GlobalData.Settings.Trading.DcaCount)
        //{
        //    step = null;
        //    percentage = 0;
        //    reaction = "Geen bijkopen vanwege MAX DCA count (1)";
        //    return false;
        //}
        // TODO: Nieuwe method via de DCA list (moet je in de json instellen)
        // dcalist aantal =  3 (5;2*, 10;4* en 15;8*)
        // Position Entry 0
        // 1e dca actie   1
        // 2e dca actie   2
        // 3e dca actie   3 -- die doet ie nu niet meer?
        // verderop: var dcaEntry = GlobalData.Settings.Trading.DcaList[position.PartCount - 1];

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
            reaction = "Geen 1e entry gevonden (1)";
            return false;
        }

        // long-positie: Retourneer de laagste buy order van een niet afgesloten part.
        // short-positie: Retourneer de hoogste sell order van een niet afgesloten part.
        step = null;
        CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            // Afgesloten DCA parts sluiten we uit (omdat we zogenaamde jojo's uitvoeren)
            if (part.Purpose != CryptoPartPurpose.TakeProfit && !part.CloseTime.HasValue)
            {
                foreach (CryptoPositionStep stepX in part.Steps.Values.ToList())
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

        // Er moet in ieder geval cooldown tijd verstreken zijn ten opzichte van de vorige entry opdracht
        if (step.CloseTime?.AddMinutes(GlobalData.Settings.Trading.GlobalBuyCooldownTime) > LastCandle1mCloseTimeDate)
        {
            reaction = "het is te vroeg voor een bijkoop vanwege de cooldown";
            Symbol.ClearSignals();
            return false;
        }



        // Weird, als we dus even hebben gepauseerd en de prijs eronder zit doen we geen dca meer? echt?
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


        //// Average prijs vanwege gespreide market of stoplimit order
        //decimal lastPrice = step.QuoteQuantityFilled / step.Quantity;
        //if (position.Side == CryptoTradeSide.Long)
        //    percentage = 100m * (lastPrice - signalPrice) / lastPrice;
        //else
        //    // TODO Long/Short - controle of dit wel goed komt? - echt geen idee op dit moment...
        //    percentage = 100m * (signalPrice - lastPrice) / lastPrice;


        // TODO uitzoeken waarom we hier juist geen fixed percentage inzetten? (trailing wellicht? Wat was het oude idee?)
        // Dit was voor trailing bedoeld, activeer trailing pas vanaf een bepaalde percentage (niet direct na 0.0001%)
        //if (stepInMethod != CryptoEntryOrProfitMethod.FixedPercentage) 
        //{
        //    // het percentage geld voor alle mogelijkheden
        //    // Het percentage moet in ieder geval x% onder de vorige entry opdracht zitten
        //    // (en dit heeft voordelen want dan hoef je niet te weten in welke DCA-index je zit!)
        //    // OPMERKING: Besloten om dit altijd te doen na de cooldown tijd
        //    if (percentage < GlobalData.Settings.Trading.DcaPercentage)
        //    {
        //        reaction = $" het is te vroeg voor een bijkoop vanwege het percentage {percentage.ToString0("N2")}";
        //        return false;
        //    }
        //}




        // Is er een openstaande DCA zonder enige entries of openstaande entry?
        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            if (part.Purpose == CryptoPartPurpose.Dca && !part.CloseTime.HasValue)
            {
                //int openOrders = 0;
                foreach (CryptoPositionStep stepX in part.Steps.Values.ToList())
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
                        if (stepX.Trailing == CryptoTrailing.Trailing && stepX.OrderType == CryptoOrderType.StopLimit)
                        {
                            reaction = "de positie heeft al een openstaande trailing DCA";
                            return false;
                        }
                    }
                }

                // Er is al een DCA gemaakt maar het heeft nog geen orders of is gepauseerd vanwege barometer of andere oorzaken..
                if (part.Steps.Count == 0) // || openOrders == 0
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



    public async Task CreateOrExtendPositionViaSignalAsync()
    {
        string? lastPrice = Symbol.LastPrice?.ToString(Symbol.PriceDisplayFormat);
        string text = "Monitor " + Symbol.Name;
        // Anders is het erg lastig traceren
        if (GlobalData.BackTest)
            text += " candle=" + LastCandle1m.DateLocal;
        text += " price=" + lastPrice;


        string reaction;
        // **************************************************
        // Global checks zoals barometer, active bot etc..
        // **************************************************

        // Als de bot niet actief is dan ook geen monitoring (queue leegmaken)
        // Blijkbaar is de bot dan door de gebruiker uitgezet, verwijder de signalen
        if (!GlobalData.Settings.Trading.Active)
        {
            reaction = "trade-bot deactivated";
            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
            Symbol.ClearSignals();
            return;
        }

        // we doen (momenteel) alleen long posities
        if (!Symbol.LastPrice.HasValue)
        {
            reaction = "symbol price null";
            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
            Symbol.ClearSignals();
            return;
        }

        // Om te voorkomen dat we te snel achter elkaar in dezelfde munt stappen
        if (Symbol.LastTradeDate.HasValue && Symbol.LastTradeDate?.AddMinutes(GlobalData.Settings.Trading.GlobalBuyCooldownTime) > LastCandle1m.Date)
        {
            reaction = "is in cooldown";
            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
            Symbol.ClearSignals();
            return;
        }

        // Als BTC snel is gedaald dan stoppen
        if (!TradingRules.CheckTradingRules(LastCandle1m))
        {
            reaction = string.Format(" de bot is gepauseerd omdat {0}", GlobalData.PauseTrading.Text);
            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
            Symbol.ClearSignals();
            return;
        }


        //GlobalData.AddTextToLogTab("Monitor " + symbol.Name); te druk in de log

        // ***************************************************************************
        // Per interval kan een signaal aanwezig zijn, regel de aankoop of de bijkoop
        // ***************************************************************************
        foreach (CryptoSymbolInterval symbolInterval in Symbol.IntervalPeriodList)
        {
            // alleen voor de intervallen waar de candle net gesloten is
            // (0 % 180 = 0, 60 % 180 = 60, 120 % 180 = 120, 180 % 180 = 0)
            if (LastCandle1mCloseTime % symbolInterval.Interval?.Duration == 0)
            {
                CryptoSignal? signal = symbolInterval.Signal;
                if (signal is null)
                    continue;

                text = "Monitor " + signal.DisplayText;
                if (GlobalData.BackTest)
                    text += " candle=" + LastCandle1m.DateLocal;
                text += " price=" + lastPrice;


                // Mogen we traden op dit interval
                if (!TradingConfig.Trading[signal.Side].IntervalPeriod.ContainsKey(signal.Interval.IntervalPeriod))
                {
                    GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " not trading on this interval (removed)");
                    symbolInterval.Signal = null;
                    continue;
                }

                // Mogen we traden met deze strategy
                if (!TradingConfig.Trading[signal.Side].Strategy.ContainsKey(signal.Strategy))
                {
                    GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " not trading on this strategy (removed)");
                    symbolInterval.Signal = null;
                    continue;
                }

                // Er zijn (technisch) niet altijd candles aanwezig
                if (symbolInterval.CandleList.Count == 0)
                {
                    GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " no candles on this interval (removed)");
                    symbolInterval.Signal = null;
                    continue;
                }

                // De candle van het signaal terugzoeken (niet zomaar de laatste candle nemen, dit vanwege backtest!)
                long unix = LastCandle1mCloseTime - symbolInterval.Interval!.Duration;
                //long unix = CandleTools.GetUnixTime(lastCandle1m.OpenTime, symbolInterval.Interval.Duration);
                //long unix = CandleTools.GetUnixTime(candleCloseTime - symbolInterval.Interval.Duration, symbolInterval.Interval.Duration);
                if (!symbolInterval.CandleList.TryGetValue(unix, out CryptoCandle? candleInterval))
                {
                    GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " no candles on this interval (removed)");
                    symbolInterval.Signal = null;
                    continue;
                }

                // Indicators uitrekenen (indien noodzakelijk)
                if (!CandleIndicatorData.PrepareIndicators(Symbol, symbolInterval, candleInterval, out reaction))
                {
                    GlobalData.AddTextToLogTab(signal.DisplayText + " " + reaction + " (removed)");
                    symbolInterval.Signal = null;
                    continue;
                }

                // Bestaan het gekozen strategy wel, klinkt raar, maar is (op dit moment) niet altijd geimplementeerd
                SignalCreateBase algorithm = SignalHelper.GetSignalAlgorithm(signal.Side, signal.Strategy, signal.Symbol, signal.Interval, candleInterval);
                if (algorithm == null)
                {
                    GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " unknown algorithm (removed)");
                    symbolInterval.Signal = null;
                    continue;
                }

                if (algorithm.GiveUp(signal))
                {
                    GlobalData.AddTextToLogTab("Monitor " + signal.DisplayText + " " + algorithm.ExtraText + " giveup (removed)");
                    symbolInterval.Signal = null;
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

                foreach (CryptoTradeAccount tradeAccount in GlobalData.ActiveTradeAccountList.Values.ToList())
                {
                    if (!PositionTools.ValidTradeAccount(tradeAccount))
                        continue;

                    CryptoPosition position = PositionTools.HasPosition(tradeAccount, Symbol);
                    if (position == null)
                    {
                        if (GlobalData.Settings.Trading.DisableNewPositions)
                        {
                            reaction = "openen van nieuwe posities niet toegestaan";
                            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
                            Symbol.ClearSignals();
                            return;
                        }

                        // Controles die noodzakelijk zijn voor een eentry
                        // (inclusief de overhead van controles van de analyser)
                        // Deze code alleen uitvoeren voor de entry (niet een dca bijkoop)

                        // Is de barometer goed genoeg dat we willen traden?
                        if (!TradingRules.CheckBarometerValues(Symbol.QuoteData.PauseBarometer[signal.Side], Symbol.QuoteData, signal.Side, LastCandle1m, out reaction))
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
                        if (!SymbolTools.CheckValidMinimalVolume(Symbol, out reaction))
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

                        //// Is de munt te nieuw? (hebben we vertrouwen in nieuwe munten?)
                        // Duplicaat code: Dit wordt nu gedaan voordat er signalen worden gemaakt (zie PositionMonitor.CreateSignals())
                        //if (!SymbolTools.CheckNewCoin(Symbol, out reaction))
                        //{
                        //    GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
                        //    Symbol.ClearSignals();
                        //    return;
                        //}

                        // Munten waarvan de ticksize percentage nogal groot is (barcode charts)
                        if (!SymbolTools.CheckMinimumTickPercentage(Symbol, out reaction))
                        {
                            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
                            Symbol.ClearSignals();
                            return;
                        }

                        // Controle of bepaalde intervallen in een uptrend of in een downtrend zijn
                        if (!PositionTools.ValidTrendConditions(signal.Symbol, TradingConfig.Trading[signal.Side].Trend, out reaction))
                        {
                            if (TradingConfig.Trading[signal.Side].TrendLog)
                                GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
                            Symbol.ClearSignals();
                            continue;
                        }

                        // Filter op de markettrend waarvan je wil dat die qua percentage bullisch of bearisch zijn
                        if (!PositionTools.ValidMarketTrendConditions(signal.Symbol, TradingConfig.Trading[signal.Side].MarketTrend, out reaction))
                        {
                            GlobalData.AddTextToLogTab(text + " " + reaction + " (removed)");
                            Symbol.ClearSignals();
                            continue;
                        }

                        // Alleen deze 2 ondersteunen we op dit moment (bool CanTrade introduceren ofzo)
                        // Voorlopig alleen traden op Bybit Spot en Futures (alleen daar kan ik het testen)
                        if (!tradeAccount.CanTrade)
                        {
                            GlobalData.AddTextToLogTab(text + $" trader niet ondersteund op {tradeAccount.Name} (removed)");
                            Symbol.ClearSignals();
                            return;
                        }


                        // Locking omdat het aantal posities over de slot limits kunnen gaan
                        // (er zijn x threads tegelijk met deze code aan de gang)
                        await Semaphore.WaitAsync();
                        try
                        {
                            // We willen 1 slot per symbol en x slots voor de long en shorts
                            if (!SymbolTools.CheckAvailableSlots(tradeAccount, Symbol, signal.Side, out reaction))
                            {
                                GlobalData.AddTextToLogTab($"{text} {reaction} (removed)");
                                Symbol.ClearSignals();
                                return;
                            }


                            // Get available assets from the exchange (as late as possible because of webcall)
                            var resultFetchAssets = await AssetTools.FetchAssetsAsync(tradeAccount, true);
                            if (!resultFetchAssets.success)
                            {
                                GlobalData.AddTextToLogTab($"{text} {resultFetchAssets.reaction}");
                                Symbol.ClearSignals();
                                return;
                            }

                            // Enough stuff to take position? + entryAmount
                            var resultAvailableAssets = AssetTools.CheckAvailableAssets(tradeAccount, Symbol);
                            if (!resultAvailableAssets.success)
                            {
                                GlobalData.AddTextToLogTab($"{text} {resultAvailableAssets.reaction}");
                                Symbol.ClearSignals();
                                return;
                            }
                            var info = resultAvailableAssets.info; // short alias
                            decimal entryQuote = resultAvailableAssets.entryQuoteAsset;

                            // Check the assets, the symbol limits..
                            {
                                // Bepaal het entry bedrag 
                                decimal entryPrice = Symbol.LastPrice.Value.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
                                decimal entryBase = entryQuote / entryPrice;
                                entryBase = entryBase.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);
                                entryBase = TradeTools.CorrectEntryQuantityIfWayLess(Symbol, entryQuote, entryBase, entryPrice);

                                // Its rounded towards zero
                                if (entryBase <= 0)
                                {
                                    GlobalData.AddTextToLogTab(text + $" vanwege de minimum quantity {Symbol.QuantityMinimum} en aankoopbedrag {entryQuote} lukt de aankoop niet");
                                    Symbol.ClearSignals();
                                    return;
                                }

                                // Below the minimum allowed quantity
                                if (entryBase == Symbol.QuantityMinimum)
                                {
                                    GlobalData.AddTextToLogTab(text + $" vanwege de minimum quantity {entryBase} < {Symbol.QuantityMinimum} lukt de aankoop niet (te weinig)");
                                    Symbol.ClearSignals();
                                    return;
                                }

                                // Below the minimum allowed value
                                if (Symbol.QuoteValueMinimum > 0 && entryQuote < Symbol.QuoteValueMinimum)
                                {
                                    GlobalData.AddTextToLogTab(text + $" vanwege de minimum value {entryQuote} < {Symbol.QuoteValueMinimum} lukt de aankoop niet (te weinig)");
                                    Symbol.ClearSignals();
                                    return;
                                }

                                if (tradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading && tradeAccount.AccountType == CryptoAccountType.Spot)
                                {
                                    if (info.QuoteFree == 0 || entryBase * entryPrice > info.QuoteTotal)
                                    {
                                        GlobalData.AddTextToLogTab($"{text} niet genoeg assets beschikbaar om in te stappen {entryBase * entryPrice} > {info.QuoteTotal})");
                                        Symbol.ClearSignals();
                                        return;
                                    }
                                }
                            }


                            // De positie + entry aanmaken
                            position = PositionTools.CreatePosition(tradeAccount, Symbol, signal.Strategy, signal.Side, symbolInterval, LastCandle1mCloseTimeDate);
                            Database.Connection.Insert(position);
                            PositionTools.AddPosition(tradeAccount, position);
                            PositionTools.ExtendPosition(Database, position, CryptoPartPurpose.Entry, signal.Interval, signal.Strategy,
                                CryptoEntryOrProfitMethod.AfterNextSignal, signal.Price, LastCandle1mCloseTimeDate);
                        }
                        finally
                        {
                            Semaphore.Release();
                        }

                        // Aanmelden van een nieuwe positie
                        if (!GlobalData.BackTest && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                            GlobalData.PositionsHaveChanged("");

                        return;
                    }
                    else
                    {
                        // long positie: Alleen bijkopen als we ONDER de break-even prijs zitten
                        // short positie: Alleen bijkopen als we BOVEN de break-even prijs zitten
                        if ((position.Side == CryptoTradeSide.Long && signal.Price < position.BreakEvenPrice) ||
                            (position.Side == CryptoTradeSide.Short && signal.Price > position.BreakEvenPrice))
                        {
                            // En een paar aanvullende condities...
                            if (!CanOpenAdditionalDca(position, out CryptoPositionStep? step, out decimal percentage, out decimal dcaPrice, out reaction))
                            {
                                if (reaction != "")
                                    GlobalData.AddTextToLogTab($"{text} {symbolInterval.Interval.Name} {reaction} (removed)");
                                Symbol.ClearSignals();
                                return;
                            }

                            // Zo laat mogelijk controleren vanwege extra calls naar de exchange
                            var (success, reaction2) = await AssetTools.FetchAssetsAsync(tradeAccount);
                            if (!success)
                            {
                                GlobalData.AddTextToLogTab(text + " " + reaction2);
                                Symbol.ClearSignals();
                                return;
                            }

                            var resultCheckAssets = AssetTools.CheckAvailableAssets(tradeAccount, Symbol);
                            if (!resultCheckAssets.success)
                            {
                                GlobalData.AddTextToLogTab(text + " " + resultCheckAssets.reaction);
                                Symbol.ClearSignals();
                                return;
                            }

                            // De positie uitbreiden nalv een nieuw signaal (wordt een aparte DCA)
                            // dcaPrice is gebaseerd op gefixeerde percentage (wellicht niet meer geschikt voor trailing?)
                            PositionTools.ExtendPosition(Database, position, CryptoPartPurpose.Dca, signal.Interval, signal.Strategy,
                                CryptoEntryOrProfitMethod.AfterNextSignal, dcaPrice, LastCandle1mCloseTimeDate);
                            return;
                        }
                    }

                }

            }

        }
    }


    private bool Prepare(CryptoPosition position, CryptoPositionPart part, out CryptoCandle? candleInterval)
    {
        // Stukje migratie, het interval van de part kan null zijn
        CryptoInterval interval = position.Interval;
        if (part.Interval != null)
            interval = part.Interval;
        CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(interval.IntervalPeriod);



        // Maak beslissingen als de candle van het interval afgesloten is (dus NIET die van de 1m candle!)
        // Dus ook niet zomaar een laatste candle nemen in verband met Backtesting (echt even berekenen)
        candleInterval = null;
        if (LastCandle1mCloseTime % interval.Duration != 0)
            return false;
        long candleOpenTimeInterval = LastCandle1mCloseTime - interval.Duration;


        // Die indicator berekening had ik niet verwacht (cooldown?)
        Monitor.Enter(position.Symbol.CandleList);
        try
        {
            // Niet zomaar een laatste candle nemen in verband met Backtesting
            if (!symbolInterval.CandleList.TryGetValue(candleOpenTimeInterval, out candleInterval))
            {
                string t = string.Format("candle 1m interval: {0}", LastCandle1m.DateLocal.ToString()) + ".." + LastCandle1mCloseTimeDate.ToLocalTime() + "\r\n" +
                string.Format("is de candle op het {0} interval echt missing in action?", interval.Name) + "\r\n" +
                    string.Format("position.CreateDate = {0}", position.CreateTime.ToString()) + "\r\n";
                //throw new Exception($"Candle niet aanwezig? {t}");
                GlobalData.AddTextToLogTab($"Analyse {position.Symbol.Name} {t}");
                return false;
            }

            if (candleInterval.CandleData == null)
            {
                List<CryptoCandle>? history = null;
                history = CandleIndicatorData.CalculateCandles(Symbol, interval, candleInterval.OpenTime, out string response);
                if (history == null)
                {
                    GlobalData.AddTextToLogTab("Analyse " + response + $"{position.Symbol.Name} Candle {interval.Name} {candleInterval.DateLocal} niet berekend? {response}");
                    //throw new Exception($"{position.Symbol.Name} Candle {interval.Name} {candleInterval.DateLocal} niet berekend? {response}");
                    return false;
                }

                // Eenmalig de indicators klaarzetten
                CandleIndicatorData.CalculateIndicators(history);
            }
        }
        finally
        {
            Monitor.Exit(position.Symbol.CandleList);
        }


        return true;
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

    private decimal CalculateEntryOrDcaPrice(CryptoPosition position, CryptoPositionPart part, CryptoBuyOrderMethod buyOrderMethod, decimal defaultPrice)
    {
        // Wat wordt de prijs? (hoe graag willen we in de trade?)
        decimal price = defaultPrice;
        switch (buyOrderMethod)
        {
            case CryptoBuyOrderMethod.SignalPrice:
                price = CorrectBuyOrDcaPrice(position, price);
                break;
            case CryptoBuyOrderMethod.BidPrice:
                if (position.Side == CryptoTradeSide.Long && part.Symbol.BidPrice.HasValue)
                    price = part.Symbol.BidPrice ?? 0;
                else if (position.Side == CryptoTradeSide.Short && part.Symbol.AskPrice.HasValue)
                    price = part.Symbol.BidPrice ?? 0;
                price = CorrectBuyOrDcaPrice(position, price);
                break;
            case CryptoBuyOrderMethod.AskPrice:
                if (position.Side == CryptoTradeSide.Long && part.Symbol.AskPrice.HasValue)
                    price = part.Symbol.BidPrice ?? 0;
                else if (position.Side == CryptoTradeSide.Short && part.Symbol.AskPrice.HasValue)
                    price = part.Symbol.BidPrice ?? 0;
                price = CorrectBuyOrDcaPrice(position, price);
                break;
            //case CryptoBuyOrderMethod.BidAndAskPriceAvg:
            //    if (part.Symbol.AskPrice.HasValue)
            //        price = (decimal)(part.Symbol.AskPrice + part.Symbol.BidPrice) / 2;
            //    price = CorrectBuyOrDcaPrice(position, price);
            //    break;
            case CryptoBuyOrderMethod.MarketOrder:
                price = part.Symbol.LastPrice ?? 0;
                break;
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
    ///// Kunnen we de positie afsluiten met de opgegeven winst percentage
    ///// </summary>
    //private async Task HandleCheckProfitablePartClose(CryptoPosition position, CryptoPositionPart part, decimal percentage)
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
    //            decimal x = breakEven + breakEven * (percentage / 100m);
    //            if (position.Symbol.LastPrice < x)
    //                return;

    //            // Als we reeds aan het trailen zijn heeft dat onze voorkeur (geen garanties op dat percentage)
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
    //                    PaperAssets.Change(position.TradeAccount, position.Symbol, result.tradeParams.OrderSide, 
    //                        step.Status, result.tradeParams.Quantity, result.tradeParams.QuoteQuantity);

    //            }
    //            ExchangeBase.Dump(position.Symbol, result.result, result.tradeParams, "placing");
    //        }
    //    }
    //}


    private decimal CalculateTakeProfitPrice(CryptoPosition position)
    {
        decimal price;
        decimal breakEven = position.BreakEvenPrice;

        if (position.Side == CryptoTradeSide.Long)
        {
            // We nemen hiervoor de BreakEvenPrice van de gehele positie en de sell price ligt standaard X% hoger
            if (GlobalData.Settings.Trading.SellMethod == CryptoSellMethod.TrailViaKcPsar)
                price = breakEven + (breakEven * (2.0m / 100)); // In eerste instantie flink hoog!
            else
                price = breakEven + (breakEven * (GlobalData.Settings.Trading.ProfitPercentage / 100));

            if (Symbol.LastPrice.HasValue && Symbol.LastPrice > price)
            {
                decimal oldPrice = price;
                price = (decimal)Symbol.LastPrice + Symbol.PriceTickSize;
                GlobalData.AddTextToLogTab($"{Symbol.Name} SELL correction: {oldPrice:N6} to {price.ToString0()}");
            }
        }
        else
        {
            // We nemen hiervoor de BreakEvenPrice van de gehele positie en de sell price ligt standaard X% lager
            if (GlobalData.Settings.Trading.SellMethod == CryptoSellMethod.TrailViaKcPsar)
                price = breakEven - (breakEven * (2.0m / 100)); // In eerste instantie flink hoog!
            else
                price = breakEven - (breakEven * (GlobalData.Settings.Trading.ProfitPercentage / 100));

            if (Symbol.LastPrice.HasValue && Symbol.LastPrice < price)
            {
                decimal oldPrice = price;
                price = (decimal)Symbol.LastPrice - Symbol.PriceTickSize;
                GlobalData.AddTextToLogTab($"{Symbol.Name} SELL correction: {oldPrice:N6} to {price.ToString0()}");
            }
        }

        price = price.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
        return price;
    }


    private async Task HandleEntryPart(CryptoPosition position, CryptoPositionPart part, CryptoCandle candleInterval,
        CryptoEntryOrProfitMethod stepInMethod, CryptoBuyOrderMethod orderMethod)
    {
        // Controleer de entry
        CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
        CryptoPositionStep step = PositionTools.FindPositionPartStep(part, entryOrderSide, false);


        // defaults
        string logText = "placing";
        decimal? entryPrice = null;
        CryptoOrderType entryOrderType;
        CryptoTrailing trailing = CryptoTrailing.None;

        switch (stepInMethod)
        {
            case CryptoEntryOrProfitMethod.AfterNextSignal:
                entryOrderType = CryptoOrderType.Limit;
                if (orderMethod == CryptoBuyOrderMethod.MarketOrder)
                    entryOrderType = CryptoOrderType.Market;
                if (step == null && part.Quantity == 0) // entry
                    entryPrice = CalculateEntryOrDcaPrice(position, part, orderMethod, part.SignalPrice);
                break;
            case CryptoEntryOrProfitMethod.FixedPercentage:
                // Afspraak= niet bijplaatsen indien de BM te laag is (anders jojo=weghalen+bijplaatsen)
                entryOrderType = CryptoOrderType.Limit;
                if (step == null && part.Quantity == 0) // entry
                    entryPrice = CalculateEntryOrDcaPrice(position, part, orderMethod, part.SignalPrice);
                break;
            case CryptoEntryOrProfitMethod.TrailViaKcPsar:
                trailing = CryptoTrailing.Trailing;
                entryOrderType = CryptoOrderType.StopLimit;
                // Trailing is afwijkend ten opzichte van de sell (zoveel mogelijk gelijk maken)

                // todo: Gaat deze vergelijking goed als er ook dust aanwezig kan zijn?
                // Moet de bestaande verplaatst worden (cq annuleren + opnieuw plaatsen)?
                if (step != null && part.Quantity == 0 && step.Trailing == CryptoTrailing.Trailing)
                {
                    if (position.Side == CryptoTradeSide.Long)
                    {
                        decimal x = (decimal)Math.Max(candleInterval.CandleData?.KeltnerUpperBand ?? 0, candleInterval.CandleData?.PSar ?? 0) + Symbol.PriceTickSize;
                        if (x < step.StopPrice && Symbol.LastPrice < x && candleInterval.High < x)
                        {
                            entryPrice = x;
                            await TradeTools.CancelOrder(Database, position, part, step,
                                LastCandle1mCloseTimeDate, CryptoOrderStatus.TrailingChange, "adjusting trailing");
                        }
                    }
                    else
                    {
                        decimal x = (decimal)Math.Min(candleInterval.CandleData?.KeltnerLowerBand ?? 0, candleInterval.CandleData?.PSar ?? 0) - Symbol.PriceTickSize;
                        if (x > step.StopPrice && Symbol.LastPrice > x && candleInterval.Low > x)
                        {
                            entryPrice = x;
                            await TradeTools.CancelOrder(Database, position, part, step,
                                LastCandle1mCloseTimeDate, CryptoOrderStatus.TrailingChange, "adjusting trailing");
                        }
                    }
                }

                if (step == null && part.Quantity == 0) // entry
                {
                    if (position.Side == CryptoTradeSide.Long)
                    {
                        // Alleen in een neergaande "trend" beginnen we met trailen (niet in een opgaande)
                        // Dit is een fix om te voorkomen dat we direct na het kopen een trailing sell starten (maar of dit okay is?)
                        if (Symbol.LastPrice >= (decimal?)candleInterval.CandleData?.PSar)
                            return;

                        decimal x = (decimal)Math.Max(candleInterval.CandleData?.KeltnerUpperBand ?? 0, candleInterval.CandleData?.PSar ?? 0) + Symbol.PriceTickSize;
                        if (Symbol.LastPrice < x && candleInterval.High < x)
                        {
                            logText = "trailing";
                            entryPrice = x;
                        }
                    }
                    else
                    {
                        // Alleen in een opgaande "trend" beginnen we met trailen (niet in een neergaande)
                        // Dit is een fix om te voorkomen dat we direct na het kopen een trailing buy starten (maar of dit okay is?)
                        if (Symbol.LastPrice <= (decimal?)candleInterval.CandleData?.PSar)
                            return;

                        decimal x = (decimal)Math.Min(candleInterval.CandleData?.KeltnerLowerBand ?? 0, candleInterval.CandleData?.PSar ?? 0) - Symbol.PriceTickSize;
                        if (Symbol.LastPrice > x && candleInterval.Low > x)
                        {
                            logText = "trailing";
                            entryPrice = x;
                        }
                    }
                }
                break;
            default:
                throw new Exception($"{stepInMethod} niet ondersteund");
        }


        if (entryPrice.HasValue)
        {
            decimal? stop = null;
            decimal? limit = null;

            // Amount is het instap bedrag (niet de quantity)
            decimal entryValue;
            if (position.Invested == 0)
            {
                // Bepaal het entry bedrag 
                // Naast een vast bedrag kan het ook een percentage zijn van de totaal beschikbare quote asset
                decimal currentAssetQuantity = 0;
                if (position.TradeAccount.AssetList.TryGetValue(Symbol.Quote, out var asset))
                    currentAssetQuantity = asset.Total;
                entryValue = TradeTools.GetEntryAmount(Symbol, currentAssetQuantity, position.TradeAccount.TradeAccountType);
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

                if (position.PartCount < GlobalData.Settings.Trading.DcaList.Count)
                {
                    var dcaEntry = GlobalData.Settings.Trading.DcaList[position.PartCount];
                    entryValue = position.EntryAmount ?? 0 * dcaEntry.Factor;
                }
                else
                {
                    // DCA, verdubbelen, gebaseerd op Zignally (geeft snel een asset tekort)
                    entryValue = 1 * (position.Invested - position.Returned + position.Commission);
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


            // Plaats de entry order
            var exchangeApi = ExchangeHelper.GetExchangeInstance(GlobalData.Settings.General.ExchangeId);
            (bool result, TradeParams? tradeParams) result = await exchangeApi.PlaceOrder(Database,
                position, part, position.Side, LastCandle1mCloseTimeDate,
                entryOrderType, entryOrderSide, entryQuantity, price, stop, limit);
            if (result.tradeParams is not null)
            {
                if (result.result)
                {
                    part.EntryMethod = stepInMethod;
                    if (part.Purpose == CryptoPartPurpose.Entry) // PartNumber == 0
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

                    // Een eventuele market order direct laten vullen
                    if (position.TradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading && step.OrderType == CryptoOrderType.Market)
                    {
                        await PaperTrading.CreatePaperTradeObject(Database, position, part, step, LastCandle1m.Close, LastCandle1mCloseTimeDate);
                        position.Reposition = false; // anders twee keer achter elkaar indien papertrading of backtesting!
                    }
                }
                else ExchangeBase.Dump(position, result.result, result.tradeParams, logText);
            }
        }
    }



    //private async Task HandleTakeProfitPart(CryptoPosition position, CryptoPositionPart part, CryptoCandle candleInterval)
    //{
    //    CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();

    //    // Is er wel iets om te verkopen in deze "part"? (hetzelfde als part.Quantity !=0 of part.Invested != 0?)
    //    CryptoPositionStep stepEntry = PositionTools.FindPositionPartStep(part, entryOrderSide, true);
    //    if (stepEntry != null && (stepEntry.Status.IsFilled() || stepEntry.Status == CryptoOrderStatus.PartiallyFilled)) // Partially?
    //    {
    //        // TODO, is er genoeg Quantity van de symbol om het te kunnen verkopen? (min-quantity en notation)
    //        // (nog niet opgemerkt in reallive trading, maar dit gaat zeker een keer gebeuren in de toekomst!)

    //        CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();
    //        CryptoPositionStep stepProfit = PositionTools.FindPositionPartStep(part, takeProfitOrderSide, false);
    //        if (stepProfit == null && part.Quantity > 0)
    //        {
    //            decimal takeProfitPrice = CalculateTakeProfitPrice(position);
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
    //        //                GlobalData.AddTextToLogTab($"{Symbol.Name} SELL correction sellStop -> {oldPrice:N6} to {stop.ToString0()}");
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
    //        //            step.Quantity, price, stop, null); // Was een OCO met een sellLimit
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
    //        //                PaperAssets.Change(position.TradeAccount, position.Symbol, tradeParams.OrderSide,
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


    private async Task CheckAddDcaFixedPercentage(CryptoPosition position)
    {
        // Een DCA plaatsen na een bepaalde percentage en de cooldowntijd
        if (position.Status == CryptoPositionStatus.Trading && GlobalData.Settings.Trading.DcaStepInMethod == CryptoEntryOrProfitMethod.FixedPercentage)
        {
            if (CanOpenAdditionalDca(position, out CryptoPositionStep? _, out decimal _, out decimal dcaPrice, out string _))
            {
                //decimal adjust = GlobalData.Settings.Trading.DcaPercentage * step.Price / 100m;

                // Corrigeer de prijs indien de koers ondertussen lager of hoger ligt
                decimal price = dcaPrice;
                if (position.Side == CryptoTradeSide.Long)
                {
                    //price = step.Price - adjust;
                    if (position.Symbol.LastPrice.HasValue && position.Symbol.LastPrice < price)
                        price = (decimal)position.Symbol.LastPrice - position.Symbol.PriceTickSize;
                }
                else
                {
                    //price = step.Price + adjust;
                    if (position.Symbol.LastPrice.HasValue && position.Symbol.LastPrice > price)
                        price = (decimal)position.Symbol.LastPrice + position.Symbol.PriceTickSize;
                }


                string text = $"{position.Symbol.Name} + DCA bijplaatsen op {price.ToString0(position.Symbol.PriceDisplayFormat)}";

                // Zo laat mogelijk controleren vanwege extra calls naar de exchange
                var (success, reaction) = await AssetTools.FetchAssetsAsync(position.TradeAccount);
                if (!success)
                {
                    GlobalData.AddTextToLogTab(text + " " + reaction);
                    Symbol.ClearSignals();
                    return;
                }

                var resultCheckAssets = AssetTools.CheckAvailableAssets(position.TradeAccount, Symbol);
                if (!resultCheckAssets.success)
                {
                    GlobalData.AddTextToLogTab(text + " " + resultCheckAssets.reaction);
                    Symbol.ClearSignals();
                    return;
                }


                // De positie uitbreiden nalv een nieuw signaal (de xe bijkoop wordt altijd een aparte DCA)
                PositionTools.ExtendPosition(Database, position, CryptoPartPurpose.Dca, position.Interval, position.Strategy,
                    CryptoEntryOrProfitMethod.FixedPercentage, price, LastCandle1mCloseTimeDate);
            }
        }
    }


    public async Task CancelOrdersIfClosedOrTimeoutOrReposition(CryptoPosition position)
    {
        // Voor ondersteuning van long/short
        CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
        CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();


        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            if (!part.CloseTime.HasValue)
            {
                foreach (CryptoPositionStep step in part.Steps.Values.ToList())
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
                        // De orders van een gesloten positie allemaal annuleren (dat zijn de fixed percentage buy orders)
                        if (position.CloseTime.HasValue)
                        {
                            newStatus = CryptoOrderStatus.PositionClosed;
                            cancelReason = "annuleren vanwege sluiten positie";
                        }


                        // Een eventuele aan- of bijkoop kan worden geannuleerd indien de instap te lang duurt ("Remove Time")
                        // (een toekomstige gereserveerde DCA buy orders of actieve trailing orders moeten we niet annuleren)
                        // Verwijder openstaande buy orders die niet gevuld worden binnen zoveel X minuten/candles?
                        // En dan mag eventueel de positie gesloten worden (indien het uit 1 deelpositie bestaat)
                        else if (part.EntryMethod != CryptoEntryOrProfitMethod.FixedPercentage && step.Trailing == CryptoTrailing.None)
                        {
                            // Is de order ouder dan X minuten dan deze verwijderen
                            CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(part.Interval.IntervalPeriod);
                            if (step.CreateTime.AddSeconds(GlobalData.Settings.Trading.GlobalBuyRemoveTime * symbolInterval.Interval?.Duration ?? 0) < LastCandle1mCloseTimeDate)
                            {
                                // Trades worden niet altijd op het juiste tijdstip opgemerkt (de user ticker ligt er vaak uit)
                                // Controleer daarom eerst of de order gevallen is, synchroniseer de trades en herberekenen het geheel..

                                // Soms wordt een trade niet gerapporteerd en dan gaat er van alles mis in onze beredeneringen.
                                // (met een partial fill gaat deze code ook gedeeltelijk fout, later nog eens beter nazoeken)
                                // Haal alle trades van deze order op, wellicht gaat dat beter dan via alle trades?
                                // (achteraf gezien, wellicht is dit een betere c.q. betrouwbaardere methode om de trades te halen?)
                                //GlobalData.AddTextToLogTab($"TradeHandler: DETECTIE: ORDER {data.OrderId} NIET GEVONDEN! PANIC MODE ;-)");

                                await TradeTools.CalculatePositionResultsViaOrders(Database, position, forceCalculation: true);

                                // Als er niets mee gedaan is dan de order annuleren
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
                        else if (part.Purpose == CryptoPartPurpose.Entry & part.EntryMethod != GlobalData.Settings.Trading.BuyStepInMethod)
                        {
                            newStatus = CryptoOrderStatus.ChangedSettings;
                            cancelReason = "annuleren vanwege aanpassing entry instellingen";
                        }

                        // Als de instellingen veranderd zijn de lopende order annuleren
                        else if (part.Purpose == CryptoPartPurpose.Dca & part.EntryMethod != GlobalData.Settings.Trading.DcaStepInMethod)
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


                        // De instellingen zijn gewijzigd....?
                        // Oh? Dat klopt niet, we hebben geen StepOutMethod in de instellingen! TODO: Controleren en fixen!
                        //else if (part.StepOutMethod != GlobalData.Settings.Trading.Ehhhh?)
                        //{
                        //    newStatus = CryptoOrderStatus.ChangedSettings;
                        //    cancelText = "annuleren vanwege aanpassing instellingen";
                        //}
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
            foreach (CryptoPositionPart part in position.Parts.Values.ToList())
            {
                if (!part.CloseTime.HasValue)
                {
                    foreach (CryptoPositionStep step in part.Steps.Values.ToList())
                    {
                        if (step.Status == CryptoOrderStatus.New)
                        {
                            hasOpenOrder = true;
                        }
                    }
                }
            }

            // Pas verplaatsen als ALLE DCA orders zijn geannuleerd (een poging daartoe tenminste)
            if (!hasOpenOrder)
            {
                PositionTools.RemovePosition(position.TradeAccount, position, true);
                if (!GlobalData.BackTest && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                    GlobalData.PositionsHaveChanged("");
            }
        }
    }

    internal async Task<bool> CancelAllOrders(CryptoPosition position, CryptoOrderSide takeProfitOrderSide)
    {
        if (position.Quantity > 0)
        {
            foreach (CryptoPositionPart part in position.Parts.Values.ToList())
            {
                if (!part.CloseTime.HasValue)
                {
                    // Has it an open takeprofit order?
                    CryptoPositionStep step = PositionTools.FindPositionPartStep(part, takeProfitOrderSide, false);
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
        CryptoPositionPart? takeProfitPart = null;

        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            // voor de niet afgesloten parts...
            if (!part.CloseTime.HasValue && part.Purpose != CryptoPartPurpose.TakeProfit)
            {
                // De prepare controleert of we een geldige candle in het interval (van de part of positie) hebben!
                if (Prepare(position, part, out CryptoCandle? candleInterval))
                {
                    if (!PauseBecauseOfTradingRules && candleInterval is not null)
                    {
                        // Check entry
                        if (part.Purpose == CryptoPartPurpose.Entry)
                            await HandleEntryPart(position, part, candleInterval, GlobalData.Settings.Trading.BuyStepInMethod, GlobalData.Settings.Trading.BuyOrderMethod);

                        // Check DCA
                        if (part.Purpose == CryptoPartPurpose.Dca)
                            await HandleEntryPart(position, part, candleInterval, GlobalData.Settings.Trading.DcaStepInMethod, GlobalData.Settings.Trading.DcaOrderMethod);
                    }
                }


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
            // remember the tp part
            if (part.Purpose == CryptoPartPurpose.TakeProfit)
                takeProfitPart = part;
        }


        CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();
        if (position.Quantity > 0)
        {
            // Always create a separate take profit part (if it didn't exist)
            takeProfitPart ??= PositionTools.ExtendPosition(Database, position, CryptoPartPurpose.TakeProfit, position.Interval, position.Strategy,
                    CryptoEntryOrProfitMethod.FixedPercentage, 0, DateTime.UtcNow);
            CryptoPositionStep takeProfitOrder = PositionTools.FindPositionPartStep(takeProfitPart, takeProfitOrderSide, false);
            
            decimal takeprofitPrice = CalculateTakeProfitPrice(position);
            if (takeProfitOrder == null || takeProfitOrder.Price != takeprofitPrice)
            {
                string text = $"placing {takeProfitPart.Purpose}";
                if (takeProfitOrder != null && takeProfitOrder.Quantity != position.Quantity)
                    text = $"modyfying {takeProfitPart.Purpose}";

                // Cancel all open take profit orders
                if (await CancelAllOrders(position, takeProfitOrderSide))
                {
                    // Hebben we nog een hercalcualtie nodig? Wellicht???
                    //PositionTools.CalculateProfitAndBreakEvenPrice(position);

                    // And place the (single/combined) take profit order to minimize dust)
                    decimal takeProfitPrice = CalculateTakeProfitPrice(position);
                    await TradeTools.PlaceTakeProfitOrderAtPrice(Database, position, takeProfitPart, takeProfitPrice, LastCandle1mCloseTimeDate, text);
                }
                else
                    GlobalData.AddTextToLogTab($"Monitor {Symbol.Name} Niet alle orders konden verwijderd worden!!!! (partial filled or error?)");
            }
        }

        //// Is er wel een initiele TP order aanwezig? zoniet dan dit alsnog doen!
        //// (buiten de Prepare loop gehaald die intern een controle op het interval doet)
        //// Dus nu wordt de sell order vrijwel direct geplaatst (na een 1m candle)
        //foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        //{
        //    // voor de niet afgesloten parts...
        //    if (!part.CloseTime.HasValue)
        //    {
        //        CryptoPositionStep step = PositionTools.FindPositionPartStep(part, entryOrderSide, true);
        //        if (step != null && step.Status.IsFilled())
        //        //if (step != null && (step.Status == CryptoOrderStatus.Filled /*|| step.Status == CryptoOrderStatus.PartiallyFilled*/)) -- problemen, quick fix voor nu, order laten staan
        //        {
        //            if (position.Quantity > 0) // voldoende saldo om de sell te plaatsen
        //            {
        //                step = PositionTools.FindPositionPartStep(part, takeProfitOrderSide, false);
        //                if (step == null)
        //                {
        //                    decimal takeProfitPrice = CalculateTakeProfitPrice(position);
        //                    await TradeTools.PlaceTakeProfitOrderAtPrice(Database, position, part, takeProfitPrice, LastCandle1mCloseTimeDate, "placing");
        //                }
        //                else
        //                {
        //                    // Als we het verkoop percentages aangepast hebben is het wel prettig dat de order aangepast wordt)
        //                    if (part.ProfitMethod == CryptoEntryOrProfitMethod.FixedPercentage)
        //                    {
        //                        decimal sellPrice = CalculateTakeProfitPrice(position);
        //                        if (step.Price != sellPrice && step.Status == CryptoOrderStatus.New && !part.ManualOrder)
        //                        {
        //                            string cancelReason = $"annuleren vanwege aanpassing verkoop prijs ({step.Price} -> {sellPrice})";
        //                            var (success, _) = await TradeTools.CancelOrder(Database, position, part, step,
        //                                LastCandle1mCloseTimeDate, CryptoOrderStatus.ChangedSettings, cancelReason);
        //                            if (success)
        //                            {
        //                                decimal takeProfitPrice = CalculateTakeProfitPrice(position);
        //                                await TradeTools.PlaceTakeProfitOrderAtPrice(Database, position, part, takeProfitPrice, LastCandle1mCloseTimeDate, "modifying");
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}
        //CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();

        //// Is er wel iets om te verkopen in deze "part"? (hetzelfde als part.Quantity !=0 of part.Invested != 0?)
        //CryptoPositionStep stepEntry = PositionTools.FindPositionPartStep(part, entryOrderSide, true);
        //if (stepEntry != null && (stepEntry.Status.IsFilled() || stepEntry.Status == CryptoOrderStatus.PartiallyFilled)) // Partially?
        //{
        //    // TODO, is er genoeg Quantity van de symbol om het te kunnen verkopen? (min-quantity en notation)
        //    // (nog niet opgemerkt in reallive trading, maar dit gaat zeker een keer gebeuren in de toekomst!)

        //    CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();
        //    CryptoPositionStep stepProfit = PositionTools.FindPositionPartStep(part, takeProfitOrderSide, false);
        //    if (stepProfit == null && part.Quantity > 0)
        //    {
        //        decimal takeProfitPrice = CalculateTakeProfitPrice(position);
        //        await TradeTools.PlaceTakeProfitOrderAtPrice(Database, position, part, takeProfitPrice, LastCandle1mCloseTimeDate, "placing");
        //    }
        //}
    }
#endif


    public bool CreateSignals()
    {
        int createdSignals = 0;
        //GlobalData.Logger.Info($"CreateSignals(start):" + LastCandle1m.OhlcText(Symbol, GlobalData.IntervalList[0], Symbol.PriceDisplayFormat, true, false, true));
        if (GlobalData.Settings.Signal.Active && Symbol.QuoteData.CreateSignals && Symbol.Status == 1)
        {
            // Is de munt te nieuw? (hebben we vertrouwen in nieuwe munten?)
            if (!SymbolTools.CheckNewCoin(Symbol, out string reaction))
            {
                if (GlobalData.Settings.Signal.LogSymbolMustExistsDays)
                    GlobalData.AddTextToLogTab($"Monitor {Symbol.Name} {reaction} (removed)");
                Symbol.ClearSignals();
                return false;
            }

            // Introductie long/short: Pakt wellicht onhandig uit, nu meerdere keren aanroep van de signalcreate zonder dat er hergebruik is van de candles (is dat heel erg?)
            foreach (CryptoTradeSide side in Enum.GetValues(typeof(CryptoTradeSide)))
            {
                // Barometer controle
                if (!BarometerHelper.ValidBarometerConditions(Symbol.QuoteData, TradingConfig.Signals[side].Barometer, out reaction))
                {
                    if (TradingConfig.Signals[side].BarometerLog)
                        GlobalData.AddTextToLogTab($"{Symbol.Name} {side} {reaction}");
                    //Symbol.ClearSignals(); niet doen, dan raken ook de net gemaakte signals van de long weg..
                }
                else
                {
                    foreach (CryptoInterval interval in TradingConfig.Signals[side].Interval.ToList())
                    {
                        // (0 % 180 = 0, 60 % 180 = 60, 120 % 180 = 120, 180 % 180 = 0)
                        if (LastCandle1mCloseTime % interval.Duration == 0)
                        {
                            //GlobalData.Logger.Info($"analyze({interval.Name}):" + LastCandle1m.OhlcText(Symbol, interval, Symbol.PriceDisplayFormat, true, false, true));

                            // We geven als tijd het begin van de "laatste" candle (van dat interval)
                            SignalCreate createSignal = new(Symbol, interval, side, LastCandle1mCloseTime);
                            createSignal.Analyze(LastCandle1mCloseTime - interval.Duration);
                            if (createSignal.CreatedSignal)
                                createdSignals++;

                            // Teller voor op het beeldscherm zodat je ziet dat deze thread iets doet en actief blijft.
                            Interlocked.Increment(ref analyseCount);
                        }
                    }
                }
            }
        }
        //GlobalData.Logger.Info($"CreateSignals(stop):" + LastCandle1m.OhlcText(Symbol, GlobalData.IntervalList[0], Symbol.PriceDisplayFormat, true, false, true));

        return createdSignals > 0;
    }


    private void CleanSymbolData()
    {
        // We nemen aardig wat geheugen in beslag door alles in het geheugen te berekenen, probeer in 
        // ieder geval de CandleData te clearen. Vanaf x candles terug tot de eerste de beste die null is.

        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            if (LastCandle1mCloseTime % interval.Duration == 0)
            {
                Monitor.Enter(Symbol.CandleList);
                try
                {
                    // Remove old indicator data
                    SortedList<long, CryptoCandle> candles = Symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList;
                    for (int i = candles.Count - 62; i > 0; i--)
                    {
                        CryptoCandle c = candles.Values[i];
                        if (c.CandleData != null)
                        {
                            c.CandleData = null;
#if SHOWTIMING
                            GlobalData.Logger.Info($"removed candledata({interval.Name}):" + c.OhlcText(Symbol, GlobalData.IntervalList[0], Symbol.PriceDisplayFormat, true, false, true));
#endif
                        }
                        else break;
                    }


                    // Remove old candles
                    long startFetchUnix = CandleIndicatorData.GetCandleFetchStart(Symbol, interval, DateTime.UtcNow);
                    DateTime startFetchUnixDate = CandleTools.GetUnixDate(startFetchUnix);
                    while (candles.Values.Any())
                    {
                        CryptoCandle c = candles.Values[0];
                        if (c.OpenTime < startFetchUnix)
                        {
                            candles.Remove(c.OpenTime);
#if SHOWTIMING
                            GlobalData.Logger.Info($"removed({interval.Name}):" + c.OhlcText(Symbol, interval, Symbol.PriceDisplayFormat, true, false, true));
#endif
                        }
                        else break;
                    }
                }
                finally
                {
                    Monitor.Exit(Symbol.CandleList);
                }
            }
        }
    }




#if TRADEBOT
    public async Task CheckThePosition(CryptoPosition position)
    {
        // Pauzeren vanwege de trading regels of te lage barometer
        PauseBecauseOfTradingRules = !TradingRules.CheckTradingRules(LastCandle1m);

        //Monitor.Enter(position);
        try
        {
            // Verwijder orders voor verschillende redenenen (timeout, barometer, tradingrules, positie gesloten, reposition enzovoort)
            await CancelOrdersIfClosedOrTimeoutOrReposition(position);

            if (!position.CloseTime.HasValue)
            {
                // Pauzeren vanwege de trading regels of te lage barometer
                if (!PauseBecauseOfTradingRules) // || PauseBecauseOfBarometer
                    await CheckAddDcaFixedPercentage(position);

                // Plaats of modificeer de buy of sell orders + optionele LockProfits
                await HandlePosition(position);
            }
        }
        finally
        {
            //Monitor.Exit(position);
        }
    }
#endif


    /// <summary>
    /// De afhandeling van een nieuwe 1m candle.
    /// (de andere intervallen zijn ook berekend)
    /// </summary>
    public async Task NewCandleArrivedAsync()
    {
        try
        {
            if (!Symbol.IsSpotTradingAllowed || Symbol.Status == 0)
                return;

            //string traceText = LastCandle1m.OhlcText(Symbol, GlobalData.IntervalList[0], Symbol.PriceDisplayFormat, true, false, true);
            //ScannerLog.Logger.Trace($"NewCandleArrivedAsync.Signals " + traceText);

            // Create signals per interval
            //GlobalData.Logger.Info($"analyze.CreateSignals({Symbol.Name})");
            bool hasCreatedAsignal = CreateSignals();


#if TRADEBOT
            //GlobalData.Logger.Trace($"NewCandleArrivedAsync.Positions " + traceText);


            // Simulate Trade indien openstaande orders gevuld zijn
            //GlobalData.Logger.Info($"analyze.PaperTradingCheckOrders({Symbol.Name})");
            if (GlobalData.BackTest || GlobalData.Settings.Trading.TradeVia == CryptoTradeAccountType.BackTest)
                await PaperTrading.PaperTradingCheckOrders(Database, GlobalData.ExchangeBackTestAccount!, Symbol, LastCandle1m);
            if (GlobalData.Settings.Trading.TradeVia == CryptoTradeAccountType.PaperTrade)
                await PaperTrading.PaperTradingCheckOrders(Database, GlobalData.ExchangePaperTradeAccount!, Symbol, LastCandle1m);


            // Pauzeren vanwege de trading regels of te lage barometer
            PauseBecauseOfTradingRules = !TradingRules.CheckTradingRules(LastCandle1m);

            // Open or extend a position (maar willen we wel DCA's als de barometer of trading rules een probleem zijn?)
            if (hasCreatedAsignal)
                await CreateOrExtendPositionViaSignalAsync();

            // Per (actief) trade account de posities controleren
            foreach (CryptoTradeAccount tradeAccount in GlobalData.ActiveTradeAccountList.Values.ToList())
            {
                // Aan de hand van de instellingen accounts uitsluiten
                if (PositionTools.ValidTradeAccount(tradeAccount))
                {
                    // Check the positions
                    if (tradeAccount.PositionList.TryGetValue(Symbol.Name, out var position))
                    {
                        await GlobalData.ThreadDoubleCheckPosition!.AddToQueue(position);
                    }
                }
            }
#endif

            //GlobalData.Logger.Trace($"NewCandleArrivedAsync.Clean " + traceText);

            // Remove old candles or CandleData
            if (!GlobalData.BackTest)
                CleanSymbolData();

            //GlobalData.Logger.Trace($"NewCandleArrivedAsync.Done " + traceText);
        }
        catch (Exception error)
        {
            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"{Symbol.Name} error Monitor {error.Message}");
        }
    }

    public static void ResetAnalyseCount() => analyseCount = 0;
}