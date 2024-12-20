using CryptoScanBot.Core.Barometer;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Telegram;
using CryptoScanBot.Core.Trader;
using CryptoScanBot.Core.TradingView;
using CryptoScanBot.Core.Zones;

using Dapper;

namespace CryptoScanBot.Core.Core;

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
    //                    foreach (CryptoSymbolInterval symbolPeriod in symbol.IntervalPeriodList)
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
    //            foreach (CryptoScanBot.CryptoExchange exchange in GlobalData.ExchangeListName.Values)
    //            {
    //                foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
    //                {
    //                    if (quoteData.CreateSignals)
    //                    {
    //                        foreach (CryptoSymbol symbol in quoteData.SymbolList)
    //                        {
    //                            if ((symbol.Status == 1) && !symbol.IsBarometerSymbol() && symbol.IsSpotTradingAllowed)
    //                            {
    //                                //foreach (CryptoSymbolInterval period in symbol.IntervalPeriodList)
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


    //private static async Task<WebCallResult<BinanceOrder>> OrderInfoAsync(CryptoSymbol symbol, long? orderId)
    //{
    //    BinanceWeights.WaitForFairBinanceWeight(1);

    //    // Plaats een sell order op Binance
    //    if (orderId.HasValue)
    //    {
    //        using (var client = new BinanceClient())
    //        {
    //            WebCallResult<BinanceOrder> result = await client.SpotApi.Trading.GetOrderAsync(symbol.Name, orderId);
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

        // Verwijder de quotes die geen symbols bevatten
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values.ToList())
        {
            if (quoteData.SymbolList.Count == 0 && !quoteData.FetchCandles)
                GlobalData.Settings.QuoteCoins.Remove(quoteData.Name);

        }

        // De (nieuwe)muntparen toevoegen aan de userinterface
        GlobalData.SymbolsHaveChanged("");
    }



    public static async Task ExecuteAsync(bool checkPositions)
    {
        try
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Open();
            _ = Task.Run(GlobalData.ThreadSaveObjects!.Execute).ConfigureAwait(false);

            //************************************************************************************
            // Informatie uit de database lezen
            //************************************************************************************

            // TODO: controleren of we de info van de juiste exchange halen (of juist bewust multi exchyange laten zien)

            if (GlobalData.ExchangeListId.TryGetValue(GlobalData.Settings.General.ExchangeId, out Model.CryptoExchange? exchange))
            {
                //************************************************************************************
                // Alle symbols van de exchange halen en mergen met de ingelezen symbols.
                // Via een event worden de muntparen in de userinterface gezet (dat duurt even)
                //************************************************************************************
                if (!exchange.LastTimeFetched.HasValue || exchange.LastTimeFetched?.AddHours(1) < DateTime.UtcNow)
                    await GlobalData.Settings.General.Exchange!.GetApiInstance().Symbol.GetSymbolsAsync();
                IndexQuoteDataSymbols(exchange);

                // Na het inlezen van de symbols de lijsten alsnog goed zetten
                TradingConfig.InitWhiteAndBlackListSettings();

                // Check the (internal) barometer symbols
                BarometerTools.InitBarometerSymbols();

                ZoneTools.LoadAllZones();

                GlobalData.AddTextToLogTab("Reading candle information");
                DataStore.LoadCandles();

                //************************************************************************************
                // Vanaf dit moment worden de candles (en candleperiod) bewaard 
                // (het herberekenen kan definitieve candles produceren)
                //************************************************************************************



                int aantalTotaal = 0;
                foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
                {
                    foreach (CryptoSymbolInterval symbolPeriod in symbol.IntervalPeriodList)
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
                // Diverse informatie tickers
                //************************************************************************************
                await Task.Factory.StartNew(() => new TradingViewSymbolInfo().StartAsync("TVC:DXY", "US Dollar Index", "N2", GlobalData.TradingViewDollarIndex, 1000));
                await Task.Factory.StartNew(() => new TradingViewSymbolInfo().StartAsync("SP:SPX", "S&P 500", "N2", GlobalData.TradingViewSpx500, 1000));
                await Task.Factory.StartNew(() => new TradingViewSymbolInfo().StartAsync("CRYPTOCAP:BTC.D", "BTC Dominance", "N2", GlobalData.TradingViewBitcoinDominance, 1000));
                await Task.Factory.StartNew(() => new TradingViewSymbolInfo().StartAsync("CRYPTOCAP:TOTAL3", "Market Cap total", "N0", GlobalData.TradingViewMarketCapTotal, 1000));
                await Task.Factory.StartNew(() => new FearAndGreatSymbolInfo().StartAsync("https://alternative.me/crypto/fear-and-greed-index/", "Fear and Greed index", "N2", GlobalData.FearAndGreedIndex, 1000));


                //************************************************************************************
                // Vanaf dit moment worden de aangeboden 1m candles in ons systeem verwerkt
                // (Dit moet overlappen met "achterstand bijwerken" want anders ontstaan er gaten)
                // BUG/Probleem! na nieuwe munt of instellingen wordt dit niet opnieuw gedaan (herstart nodig)
                //************************************************************************************
                await ExchangeBase.KLineTicker!.StartAsync();

                //************************************************************************************
                // Om het volume per symbol en laatste prijs te achterhalen
                //************************************************************************************
                await ExchangeBase.PriceTicker!.StartAsync();

                //************************************************************************************
                // De (ontbrekende) candles downloaden (en de achterstand inhalen, blocking!)
                //************************************************************************************
                var api = GlobalData.Settings.General.Exchange!.GetApiInstance();
                await api.Candle.GetCandlesForAllSymbolsAndIntervalsAsync();

                //Ze zijn er wel, deze is eigenlijk overbodig geworden (zit alleen zoveel werk in!)
                //CalculateMissingCandles();

                long currentTime = CandleTools.GetUnixTime(DateTime.UtcNow, 60);
                TradingRules.CheckTradingRules(GlobalData.ActiveAccount!.Data.PauseTrading, currentTime, 60);

                //************************************************************************************
                // Nu we de achterstand ingehaald hebben kunnen/mogen we analyseren (signals maken)
                //************************************************************************************
                _ = Task.Run(GlobalData.ThreadMonitorCandle!.Execute).ConfigureAwait(false);
                _ = Task.Run(GlobalData.ThreadZoneCalculate!.ExecuteAsync).ConfigureAwait(false);


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

                await TradeTools.CheckOpenPositions();



                if (GlobalData.TradingApi.Key != "")
                {

                    //************************************************************************************
                    // Alle data van de exchange monitoren
                    //************************************************************************************
                    _ = ExchangeBase.UserTicker!.StartAsync();


                    //************************************************************************************              
                    // De assets van de exchange halen (overlappend met exchange monitoring om niets te missen)
                    // Via een event worden de assets in de userinterface gezet (dat duurt even)
                    //************************************************************************************
                    await api.Asset.GetAssetsAsync(GlobalData.ActiveAccount!);
                }

                // Toon de ingelezen posities
                //GlobalData.PositionsHaveChanged("");


                ScannerLog.Logger.Trace("");
                ScannerLog.Logger.Trace(GlobalData.AppName + " " + GlobalData.AppVersion + " ready");
                GlobalData.AddTextToLogTab(GlobalData.AppName + " " + GlobalData.AppVersion + " ready");
                GlobalData.AddTextToTelegram(GlobalData.AppName + " " + GlobalData.AppVersion + " ready");
                GlobalData.AddTextToTelegram("");


                // Heb me dag lopen af te vragen waarom er geen signalen kwamen, iets met white&black, right
                //if (GlobalData.Settings.UseWhiteListOversold)
                //    GlobalData.AddTextToLogTab("Oversold whitelist activated!");
                //if (GlobalData.Settings.UseBlackListOversold)
                //    GlobalData.AddTextToLogTab("Oversold blacklist activated!");

                //if (GlobalData.Settings.UseWhiteListOverbought)
                //    GlobalData.AddTextToLogTab("Overbought whitelist activated!");
                //if (GlobalData.Settings.UseBlackListOverbought)
                //    GlobalData.AddTextToLogTab("Overbought blacklist activated!");

                // Dit is een enorme cpu drain, eventjes 3 * 250 * ~3 intervallen bijlangs
                //RecalculateLastXCandles(1);


                if (!checkPositions && GlobalData.ActiveAccount != null)
                {
                    if (GlobalData.Settings.Trading.TradeVia != CryptoAccountType.RealTrading)
                        await PaperTrading.CheckPositionsAfterRestart(GlobalData.ActiveAccount!);
                }

                // Assume we now can run
                GlobalData.ApplicationStatus = CryptoApplicationStatus.Running;
                //GlobalData.DumpSessionInformation();
                ScannerSession.SetTimerDefaults();

                GlobalData.ApplicationHasStarted?.Invoke("");

            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab(error.ToString());
            throw;
        }
    }
}