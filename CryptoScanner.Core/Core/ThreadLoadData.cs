using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Const;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Services;
using CryptoScanner.Core.Telegram;
using CryptoScanner.Core.Trader;
using CryptoScanner.Core.Zones;

using Dapper;

namespace CryptoScanner.Core.Core;

public class ThreadLoadData
{

    // AUB laten staan voor het geval we deze later toch nodig hebben (waarschijnlijk voor de emulator)
    //private static void CalculateMissingCandles()
    //{
    //    //************************************************************************************
    //    // Alle intervallen herberekenen (het is een bulk hercalculatie voor de laatste in het geheugen gelezen candles)
    //    // In theorie is dit allemaal reeds in de database opgeslagen, maar baat het niet dan schaad het niet
    //    //************************************************************************************
    //    foreach (CryptoExchange exchange in GlobalData.ExchangeListName.Values)
    //    {
    //        //break; //Laat maar even... (HEEL waarschijnlijk een verschil in UTC bij de omzetactie)

    //        GlobalData.AddTextToLogTab("Calculating candle intervals for " + exchange.Name + " (" + exchange.SymbolListName.Count.ToString() + " symbols)");
    //        foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
    //        {
    //            //De "barometer" munten overslagen AUB, die hebben slechts 3 intervallen (beetje quick en dirty allemaal)
    //            if (symbol.IsBarometerSymbol())
    //                continue;

    //            if (symbol.CandleList.Any())
    //            {
    //                try
    //                {
    //                    // Van laag naar hoog zodat de hogere intervallen worden berekend
    //                    foreach (CryptoSymbolInterval symbolPeriod in symbol.SymbolIntervalList)
    //                    {
    //                        CryptoInterval interval = symbolPeriod.Interval;
    //                        if (interval.ConstructFrom != null)
    //                        {
    //                            // Voeg een candle toe aan een hogere tijd interval (eventueel uit db laden)
    //                            CryptoCandleList candlesInterval = symbolPeriod.CandleList;
    //                            if (candlesInterval.Values.Count > 0)
    //                            {
    //                                // Periode start
    //                                long unixFirst = candlesInterval.Values.First().OpenTime;
    //                                unixFirst -= unixFirst % interval.Duration;
    //                                DateTime dateFirst = CandleTools.GetUnixDate(unixFirst);

    //                                // Periode einde
    //                                long unixLast = candlesInterval.Values.Last().OpenTime;
    //                                unixLast -= unixLast % interval.Duration;
    //                                DateTime dateLast = CandleTools.GetUnixDate(unixLast);


    //                                // TODO: Het aantal variabelen verminderen
    //                                long unixLoop = unixFirst;
    //                                DateTime dateLoop = CandleTools.GetUnixDate(unixLoop);

    //                                // Herbereken deze periode opnieuw uit het onderliggende interval
    //                                while (unixLoop <= unixLast)
    //                                {
    //                                    CandleTools.CalculateCandleForInterval(interval, interval.ConstructFrom, symbol, unixLoop);

    //                                    unixLoop += interval.Duration;
    //                                    dateLoop = CandleTools.GetUnixDate(unixLoop); //ter debug want een unix date is onleesbaar
    //                                }
    //                            }
    //                        }

    //                        // De laatste datum bijwerken (zodat we minder candles hoeven op te halen)
    //                        CandleTools.UpdateCandleFetched(symbol, interval);
    //                    }
    //                }
    //                catch (Exception error)
    //                {
    //                    ScannerLog.Logger.Error(error, "");
    //                    GlobalData.AddTextToLogTab(error.ToString());
    //                    throw;
    //                }

    //            }
    //        }
    //    }
    //}


    //private static void RecalculateLastXCandles(int lookback = 1)
    //{
    //    // PAS OP, die extra candles moeten overeenkomen met de extra 10 in de GetCandleFetchStart()! Dus 260 + X
    //    if (GlobalData.Settings.Signal.SignalsActive)
    //    {
    //        while (lookback > 0)
    //        {
    //            foreach (CryptoScanner.CryptoExchange exchange in GlobalData.ExchangeListName.Values)
    //            {
    //                foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
    //                {
    //                    if (quoteData.CreateSignals)
    //                    {
    //                        foreach (CryptoSymbol symbol in quoteData.SymbolList)
    //                        {
    //                            if ((symbol.Status == 1) && !symbol.IsBarometerSymbol() && symbol.IsSpotTradingAllowed)
    //                            {
    //                                //foreach (CryptoSymbolInterval period in symbol.SymbolIntervalList)
    //                                //{
    //                                //    GlobalData.AddTextToLogTab(string.Format("{0} {1} candles={2}", symbol.Name, period.Interval.Name, period.CandleList.Count));
    //                                //}

    //                                // Aanbieden voor analyse
    //                                if (symbol.CandleList.Any() && (symbol.CandleList.Count - lookback >= 0))
    //                                {
    //                                    CryptoCandle candle = symbol.CandleList.Values[symbol.CandleList.Count - lookback];
    //                                    GlobalData.ThreadCreateSignal.AddToQueue(candle);
    //                                }
    //                            }
    //                        }
    //                    }

    //                }
    //            }

    //            lookback--;
    //        }
    //    }
    //}


    //private static async Task<HttpResult<BinanceOrder>> OrderInfoAsync(CryptoSymbol symbol, long? orderId)
    //{
    //    BinanceWeights.WaitForFairBinanceWeight(1);

    //    // Plaats een sell order op Binance
    //    if (orderId.HasValue)
    //    {
    //        using (var client = new BinanceClient())
    //        {
    //            HttpResult<BinanceOrder> result = await client.SpotApi.Trading.GetOrderAsync(symbol.Name, orderId);
    //            if (!result.Success)
    //            {
    //                string text = string.Format("{0} ERROR getorder {1} {2}", symbol.Name, result.ResponseStatusCode, result.Error);
    //                GlobalData.AddTextToLogTab(text);
    //                GlobalData.AddTextToTelegram(text);
    //            }
    //            return result;

    //        }
    //    }
    //    return null;
    //}


    public static void IndexQuoteDataSymbols(Model.CryptoExchange exchange)
    {
        // De index lijsten opbouwen (een gedeelte van de ~2100 munten)
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            // Lock (zie onder andere de BarometerTools)
            Monitor.Enter(quoteData.SymbolList);
            try
            {
                quoteData.SymbolList.Clear();
                foreach (var symbol in exchange.SymbolListName.Values)
                {
                    if (symbol.Quote.Equals(quoteData.Name) && symbol.Status == 1 && !symbol.IsBarometerSymbol())
                    {
                        quoteData.SymbolList.Add(symbol);
                    }
                }
            }
            finally
            {
                Monitor.Exit(quoteData.SymbolList);
            }
        }

        // Remove the quotes who dont have any symbols
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values.ToList())
        {
            if (quoteData.SymbolList.Count == 0 && !quoteData.FetchCandles)
                GlobalData.Settings.QuoteCoins.Remove(quoteData.Name);

        }

        // Add the symbols to the userinterface
        GlobalData.SendMvvmMessage(new SymbolsHaveChangedMessage());
    }



    public static async Task ExecuteAsync(bool checkPositions)
    {
        // Pin the exchange, its kline ticker and the cancellation token of this session. All three are
        // statics that ApplyConfigurationAsync replaces the moment another exchange is activated, while
        // this method runs for minutes on a cold start. Reading them again halfway through meant the
        // loading of the previous exchange finished its work on the new one: it subscribed the new
        // exchange's ticker to the old exchange's symbols, after which the real start found that ticker
        // "already started" and no data was ever fetched for the exchange the user selected.
        Model.CryptoExchange? activeExchange = GlobalData.ActiveExchange;
        SubscriptionManager? klineTicker = ExchangeBase.KLineTicker;
        CancellationToken cancellationToken = ExchangeBase.CancellationToken;

        // True once this session was stopped, or another exchange became the active one
        bool Interrupted()
        {
            if (!cancellationToken.IsCancellationRequested && ReferenceEquals(GlobalData.ActiveExchange, activeExchange))
                return false;
            GlobalData.AddTextToLogTab($"Loading the data of {activeExchange?.Name} was interrupted");
            return true;
        }

        try
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Open();
            _ = Task.Run(GlobalData.ThreadSaveObjects!.Execute).ConfigureAwait(false);

            //************************************************************************************
            // Informatie uit de database lezen
            //************************************************************************************

            // TODO: controleren of we de info van de juiste exchange halen (of juist bewust multi exchyange laten zien)

            if (GlobalData.ActiveExchange != null)
            {
                //************************************************************************************
                // Alle symbols van de exchange halen en mergen met de ingelezen symbols.
                // Via een event worden de muntparen in de userinterface gezet (dat duurt even)
                //************************************************************************************
                if (!activeExchange.LastTimeFetched.HasValue || activeExchange.LastTimeFetched?.AddHours(1) < GlobalData.Clock.UtcNow)
                    await activeExchange.GetApiInstance().Symbol.GetSymbolsAsync();
                if (Interrupted())
                    return;
                IndexQuoteDataSymbols(activeExchange);

                // Na het inlezen van de symbols de lijsten alsnog goed zetten
                TradingConfig.InitWhiteAndBlackListSettings();

                // Check the (internal) barometer symbols
                BarometerTools.InitBarometerSymbols();

                ZoneDlz.LoadAllZones();

                GlobalData.AddTextToLogTab("Reading candle information");
                await DataStore.LoadCandlesAsync(); // Only for migration to candles.db
                await CandleDatabase.LoadCandlesAsync();
                if (Interrupted())
                    return;

                //************************************************************************************
                // Vanaf dit moment worden de candles (en candleperiod) bewaard
                // (het herberekenen kan definitieve candles produceren)
                //************************************************************************************



                int aantalTotaal = 0;
                foreach (CryptoSymbol symbol in activeExchange.SymbolListName.Values)
                {
                    foreach (CryptoSymbolInterval symbolPeriod in symbol.Data.SymbolIntervalList)
                    {
                        aantalTotaal += symbolPeriod.CandleList.Count;
                    }
                }
                if (aantalTotaal > 0)
                    GlobalData.AddTextToLogTab("Total amount of candles in memory " + aantalTotaal.ToString("N0") + " candles");



                //************************************************************************************
                // Start the Telegram bot
                //************************************************************************************
                if (GlobalData.Telegram.Token != "")
                {
                    var _ = Task.Run(async () => { await ThreadTelegramBot.Start(GlobalData.Telegram.Token, GlobalData.Telegram.ChatId); });
                }


                //************************************************************************************
                // Diverse informatie tickers (moved to TradingViewService)
                //************************************************************************************
                //await Task.Factory.StartNew(() => new TradingViewSymbolInfo().StartAsync("TVC:DXY", "US Dollar Index", "N2", GlobalData.TradingViewDollarIndex, 1000));
                //await Task.Factory.StartNew(() => new TradingViewSymbolInfo().StartAsync("SP:SPX", "S&P 500", "N2", GlobalData.TradingViewSpx500, 1000));
                //await Task.Factory.StartNew(() => new TradingViewSymbolInfo().StartAsync("CRYPTOCAP:BTC.D", "BTC Dominance", "N2", GlobalData.TradingViewBitcoinDominance, 1000));
                //await Task.Factory.StartNew(() => new TradingViewSymbolInfo().StartAsync("CRYPTOCAP:TOTAL3", "Market Cap total", "N2", GlobalData.TradingViewMarketCapTotal, 1000));
                //await Task.Factory.StartNew(() => new FearAndGreedSymbolInfo().StartAsync("https://alternative.me/crypto/fear-and-greed-index/", "Fear and Greed index", "N2", GlobalData.FearAndGreedIndex, 1000));


                //************************************************************************************
                // Vanaf dit moment worden de aangeboden 1m candles in ons systeem verwerkt
                // (Dit moet overlappen met "achterstand bijwerken" want anders ontstaan er gaten)
                // Een munt die er later bij komt of afvalt wordt nu wel opgepakt zonder herstart:
                // SubscriptionManager.SynchronizeSymbolsAsync doet dat elke verversingsronde.
                //************************************************************************************
                if (Interrupted())
                    return;
                await klineTicker!.StartAsync();

                //************************************************************************************
                // Om het volume per symbol en laatste prijs te achterhalen
                //************************************************************************************
                //await ExchangeBase.PriceTicker!.StartAsync();

                //************************************************************************************
                // De (ontbrekende) candles downloaden (en de achterstand inhalen, blocking!)
                //************************************************************************************
                var api = activeExchange.GetApiInstance();
                await api.Candle.GetCandlesForAllSymbolsAndIntervalsAsync();
                if (Interrupted())
                    return;

                //Ze zijn er wel, deze is eigenlijk overbodig geworden (zit alleen zoveel werk in!)
                //CalculateMissingCandles();

                CandleTime currentTime = CandleTime.AlignFromDateTime(GlobalData.Clock.UtcNow, 1);
                TradingRules.CheckTradingRules(activeExchange.Data.PauseTrading, currentTime, 1);

                //************************************************************************************
                // Nu we de achterstand ingehaald hebben kunnen/mogen we analyseren (signals maken)
                //************************************************************************************
                _ = Task.Run(GlobalData.ThreadMonitorCandle!.Execute).ConfigureAwait(false);
                _ = Task.Run(GlobalData.ThreadZoneCalculate!.ExecuteAsync).ConfigureAwait(false);
                _ = Task.Run(async () => { await ZoneBroken.CalculateBrokenZonesForAllSymbols(); }).ConfigureAwait(false);

                // SMC: refresh TouchCount/IsMitigated on the loaded zones and populate any
                // (symbol, interval) that has no SMC rows in the DB yet (e.g. fresh deploy).
                // The diff in Detect suppresses no-op writes so this is cheap when nothing
                // changed since the previous shutdown.
                _ = Task.Run(ZoneSmc.RebuildAllZonesForActiveExchange).ConfigureAwait(false);

                // Queue a full DLZ + FVG recalculation for every symbol at startup.
                // LoadAllZones() above only populates zones that already exist in the DB — new
                // listings and symbols added since the last manual "Calculate zones" run would
                // have no zones otherwise. The worker thread just started, so the queue accepts
                // items immediately; all work runs in the background without blocking startup.
                //ZoneThreadCalculate.CalculateZonesForAllSymbolsAsync();

                //************************************************************************************
                // Nu we de achterstand ingehaald hebben kunnen/mogen we analyseren (signals maken)
                //************************************************************************************
                if (GlobalData.TradingApi.Key != "")
                {
                    //GlobalData.AddTextToLogTab("Starting task for handling orders");
                    _ = Task.Run(async () => { await GlobalData.ThreadMonitorOrder!.ExecuteAsync(); });
                }

                //GlobalData.AddTextToLogTab("Starting task for checking positions");
                _ = Task.Run(async () => { await GlobalData.ThreadCheckPosition!.ExecuteAsync(); });

                // Load open positions into memory so the trading engine can manage them
                // immediately — without waiting for the UI positions tab to be opened.
                PositionTools.LoadOpenPositionsFromDatabase(databaseThread);
                await TradeTools.CheckOpenPositions();



                if (GlobalData.TradingApi.Key != "")
                {

                    //************************************************************************************
                    // Alle data van de exchange monitoren
                    //************************************************************************************
                    //_ = ExchangeBase.UserTicker!.StartAsync();


                    //************************************************************************************
                    // De assets van de exchange halen (overlappend met exchange monitoring om niets te missen)
                    // Via een event worden de assets in de userinterface gezet (dat duurt even)
                    //************************************************************************************
                    //await api.Asset.GetAssets(GlobalData.ActiveExchange!);
                }

                // Toon de ingelezen posities
                //GlobalData.PositionsHaveChanged("");


                ScannerLog.Logger.Trace("");
                ScannerLog.Logger.Trace(Constants.AppName + " " + GlobalData.AppVersion + " ready");
                GlobalData.AddTextToLogTab(Constants.AppName + " " + GlobalData.AppVersion + " ready");
                GlobalData.AddTextToTelegram(Constants.AppName + " " + GlobalData.AppVersion + " ready");
                GlobalData.AddTextToTelegram("");


                // Dit is een enorme cpu drain, eventjes 3 * 250 * ~3 intervallen bijlangs
                //RecalculateLastXCandles(1);


                if (!checkPositions)
                {
                    if (GlobalData.Settings.Trading.TradeVia != CryptoTradeVia.RealTrading)
                        await PaperTrading.CheckPositionsAfterRestart(activeExchange);
                }

                // Assume we now can run
                GlobalData.ApplicationStatus = CryptoApplicationStatus.Running;
                //GlobalData.DumpSessionInformation();

                IScannerSession _scannerSession = GlobalData.GetService<IScannerSession>()
                    ?? throw new InvalidOperationException("IScannerSession not registered in services");
                _scannerSession.SetTimerDefaults();

                // Calculate the barometer once, right now. Its timer runs every 30 seconds but a
                // System.Timers.Timer only fires AFTER its first interval, so without this the
                // scanner spends its first half minute without a barometer at all - which rejects
                // every signal ("Barometer not calculated") and leaves the graph empty. The refresh
                // message used to be sent here immediately, before that first calculation existed,
                // so it told the dashboards to redraw nothing.
                //
                // Off the loading thread: the first calculation has to build the whole graph history
                // and takes a moment. The emulator has no barometer (a handful of symbols is not a
                // market), so it is skipped there.
                if (GlobalData.IsEmulatorMode)
                    GlobalData.SendMvvmMessage(new BarometerRefreshMessage());
                else
                {
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            Barometer.BarometerTools barometerTools = new();
                            barometerTools.ExecuteAsync();
                        }
                        catch (Exception barometerError)
                        {
                            ScannerLog.Logger.Error(barometerError, "Initial barometer calculation");
                        }

                        // Always refresh, even when the calculation failed - the dashboards also use
                        // this message to drop their loading state.
                        GlobalData.SendMvvmMessage(new BarometerRefreshMessage());
                    });
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Stopping the session cancels the calls that were in flight, that is not an error
            GlobalData.AddTextToLogTab($"Loading the data of {activeExchange?.Name} was cancelled");
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab(error.ToString());
            throw;
        }
    }
}