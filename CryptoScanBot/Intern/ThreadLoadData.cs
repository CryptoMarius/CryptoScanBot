using CryptoScanBot.Context;
using CryptoScanBot.Enums;
using CryptoScanBot.Exchange;
using CryptoScanBot.Model;
using CryptoScanBot.Trader;
using CryptoScanBot.TradingView;

using Dapper;
using Dapper.Contrib.Extensions;

using System.Reflection;
using System.Text;

namespace CryptoScanBot.Intern;

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
    //                            SortedList<long, CryptoCandle> candlesInterval = symbolPeriod.CandleList;
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
            if (quoteData.SymbolList.Count == 0 && !quoteData.FetchCandles && !quoteData.CreateSignals)
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

            //************************************************************************************
            // Informatie uit de database lezen
            //************************************************************************************

            // TODO: controleren of we de info van de juiste exchange halen (of juist bewust multi exchyange laten zien)

            if (GlobalData.ExchangeListId.TryGetValue(GlobalData.Settings.General.ExchangeId, out Model.CryptoExchange exchange))
            {

#if SQLDATABASE
                // Alle CandleFetched items uit de database lezen
                // En ja, hier is wat duplicaat code, verhuist naar de AddSymbol() 
                GlobalData.AddTextToLogTab("Reading fetched information");
                string sql = string.Format("select * from symbolinterval where exchangeid={0}", exchange.Id);
                foreach (var candleFetched in databaseThread.Connection.Query<CryptoSymbolInterval>(sql))
                {
                    candleFetched.ExchangeId = exchange.Id;
                    if (exchange.SymbolListId.TryGetValue((int)candleFetched.SymbolId, out CryptoSymbol symbol))
                    {
                        candleFetched.SymbolId = symbol.Id;

                        // De aanwezige SymbolInterval in het geheugen OVERSCHRIJVEN (dat is de clue hier)
                        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(candleFetched.IntervalPeriod);

                        // Raar heen en weer gekopieer van data, dit kan optimaler...
                        symbolInterval.Id = candleFetched.Id;
                        symbolInterval.SymbolId = symbol.Id;
                        symbolInterval.ExchangeId = exchange.Id;
                        symbolInterval.TrendInfoDate = candleFetched.TrendInfoDate;
                        symbolInterval.TrendIndicator = candleFetched.TrendIndicator;
                        symbolInterval.LastCandleSynchronized = candleFetched.LastCandleSynchronized;
                        symbolInterval.IsChanged = false;
                    }
                }
#endif



                //************************************************************************************
                // Alle symbols van de exchange halen en mergen met de ingelezen symbols.
                // Via een event worden de muntparen in de userinterface gezet (dat duurt even)
                //************************************************************************************
                if (!exchange.LastTimeFetched.HasValue || exchange.LastTimeFetched?.AddHours(1) < DateTime.UtcNow)
                    await ExchangeHelper.FetchSymbolsAsync();
                IndexQuoteDataSymbols(exchange);

                // Na het inlezen van de symbols de lijsten alsnog goed zetten
                TradingConfig.InitWhiteAndBlackListSettings();

                // De (interne) barometer symbols toevoegen
                GlobalData.InitBarometerSymbols(databaseThread);



                GlobalData.AddTextToLogTab("Reading candle information");

#if SQLDATABASE
                //************************************************************************************
                // De candles uit de database lezen
                // Voor de 1m hebben we de laatste 2 dagen nodig (vanwege de berekening van de barometer)
                // In het algemeen is een minimum van 2 dagen OF 215 candles nodig (indicators)
                //************************************************************************************
                int aantaltotaal = 0;
                int maxCandles = 261;
                long currentTime = CandleTools.GetUnixTime(DateTime.UtcNow, 60);
                // Hmmm, is dit wel UTC?                    
                long openUnixStart = CandleTools.GetUnixTime(DateTime.Today.AddDays(-2), 60);  // ~2 dagen
                DateTime openUnixStartDebug = CandleTools.GetUnixDate(openUnixStart); // ter debug want een unix date is onleesbaar      


                // SQL server kan de hoeveelheid niet aan (het is dan ook veel), daarom per
                // interval opgeknipt (dat lijkt een optimalisatie probleem te zijn? of niet?)
                // Het is ontzettend veel data, dus het is handiger om dit per interval op te vragen
                // (vraag is of we dit altijd zo willen houden, want het is eigenlijk teveel)
                // Met name op de niet SSD HD is het verschil aanzienlijk
                foreach (CryptoInterval interval in GlobalData.IntervalList)
                {
                    int aantal = 0;
                    StringBuilder builder = new StringBuilder();
                    //builder.AppendLine("select * from candles where");
                    //if (interval.IntervalPeriod != IntervalPeriod.interval1m)
                    //  builder.Append("or ");

                    // Tenminste x dagen of 1000 candles terug.

                    // Voor de 1m hebben we twee dagen nodig (2880 candles) vanwege de 24h barometer
                    //if (interval.IntervalPeriod != IntervalPeriod.interval1m)
                    //    openUnixTime = CandleTools.GetUnixTime(DateTime.UtcNow, 60) - (1000 * interval.Duration);  // Ten minste 1000 candles

                    // 1m: 24*60*2=2880
                    // 2m: 1000 candles, 1440 ?
                    // 3m: 24*60*2=2880


                    // Nu dezelfde waarden als in de GetCandlesSub methode
                    //openUnixTime = CandleTools.GetUnixTime(DateTime.Today.AddDays(-2), 60);
                    //if (interval.IntervalPeriod == CryptoIntervalPeriod.interval1m)
                    //    openUnixTime = CandleTools.GetUnixTime(DateTime.Today.AddDays(-2), 60); 
                    //else
                    // 210 candles of 2 dagen (het aantal candles wordt kleiner naar mate het interval hoger wordt)
                    // Vreemd genoeg klopt dat niet helemaal (aldus de read Candles statistiek, waarom?)
                    // Door het gelimiteerd inlezen kunnen de candles niet volledig gecontruct worden uit voorgaande (indien er iets miste)
                    long openUnixTime = currentTime - maxCandles * interval.Duration;
                    DateTime openUnixTimeDebug = CandleTools.GetUnixDate(openUnixTime); // ter debug want een unix date is onleesbaar      
                    if (openUnixTime > openUnixStart)
                        openUnixTime = openUnixStart;
                    openUnixTimeDebug = CandleTools.GetUnixDate(openUnixTime); // ter debug want een unix date is onleesbaar      

                    // Moeten we dan ook nog rekening houden met het voorgaande candleFetched object?
                    // zodat we candles kunnen constructen uit het voorgaande interval?

                    openUnixTime = openUnixTime - openUnixTime % interval.Duration;
                    openUnixTimeDebug = CandleTools.GetUnixDate(openUnixTime); // ter debug want een unix date is onleesbaar      


                    //builder.AppendLine(string.Format("(`opentime`>{0} and `interval`={1})", openUnixTime, (int)interval.IntervalPeriod));
                    builder.AppendLine(string.Format("select * from candle with(index(IdxCandleIntervalOpenTime)) where (exchangeid={0} and intervalid={1} and opentime>={2}) -- {2} {3}",
                        //symbolid=12 and (btcusdt)
                        GlobalData.Settings.General.ExchangeId, interval.Id, openUnixTime, interval.Name, CandleTools.GetUnixDate(openUnixTime).ToLocalTime()));
                    //builder.AppendLine(string.Format("(interval={0} and date>='{1}')", (int)interval.IntervalPeriod, openUnixDate.ToString("yyyy-MM-dd HH:mm")));
                    // ter debug (controle timing)
                    GlobalData.AddTextToLogTab("Interval " + interval.Name + " " + builder.ToString());

                    // Voeg de candle toe aan de lijst verrijkt met meta data over interval, symbol en exchange
                    foreach (CryptoCandle candle in databaseThread.Connection.Query<CryptoCandle>(builder.ToString()))
                    {
                        if (exchange.SymbolListId.TryGetValue(candle.SymbolId, out CryptoSymbol symbol))
                        {
                            CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
                            SortedList<long, CryptoCandle> candles = symbolPeriod.CandleList;

                            candle.IntervalId = symbolPeriod.Interval.Id;

                            if (!candles.ContainsKey(candle.OpenTime))
                            {
                                candles.Add(candle.OpenTime, candle);
                                aantaltotaal++;
                                aantal++;
                            }
                        }
                    }
                    GlobalData.AddTextToLogTab("Interval " + interval.Name + " " + aantal.ToString("N0") + " candles read");
                }
                GlobalData.AddTextToLogTab(aantaltotaal.ToString("N0") + " candles read");
#else
                DataStore.LoadCandles();
#endif


                //************************************************************************************
                // Vanaf dit moment worden de candles (en candleperiod) bewaard 
                // (het herberekenen kan definitieve candles produceren)
                //************************************************************************************
#if SQLDATABASE
                GlobalData.AddTextToLogTab("Starting task for saving candles");
                GlobalData.TaskSaveCandles = new ThreadSaveCandles();
                // Geen await, deze mag/MOET parallel
                var whateverX = Task.Run(() => { GlobalData.TaskSaveCandles.Execute(); });
#endif

                ////************************************************************************************
                //// Alle intervallen herberekenen (het is een bulk hercalculatie voor de laatste in het geheugen gelezen candles)
                //// In theorie is dit allemaal reeds in de database opgeslagen, maar baat het niet dan schaad het niet
                ////************************************************************************************
                //GlobalData.AddTextToLogTab("Calculating candle intervals for " + exchange.Name + " (" + exchange.SymbolListName.Count.ToString() + " symbols)");
                //foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
                //{
                //    // De "barometer" munten overslagen AUB, die hebben slechts 3 intervallen (beetje quick en dirty allemaal)
                //    if (symbol.IsBarometerSymbol())
                //        continue;

                //    if (symbol.CandleList.Any())
                //    {
                //        try
                //        {
                //            // Van laag naar hoog zodat de hogere intervallen worden berekend
                //            foreach (CryptoSymbolInterval symbolInterval in symbol.IntervalPeriodList)
                //            {
                //                CryptoInterval interval = symbolInterval.Interval;
                //                if (interval.ConstructFrom != null)
                //                {
                //                    // Voeg een candle toe aan een hogere tijd interval (eventueel uit db laden)
                //                    var candlesInterval = symbolInterval.CandleList;
                //                    if (candlesInterval.Values.Count > 0)
                //                    {
                //                        // Periode start
                //                        long unixFirst = candlesInterval.Values.First().OpenTime;
                //                        unixFirst -= unixFirst % interval.Duration;
                //                        DateTime dateFirst = CandleTools.GetUnixDate(unixFirst);

                //                        // Periode einde
                //                        long unixLast = candlesInterval.Values.Last().OpenTime;
                //                        unixLast -= unixLast % interval.Duration;
                //                        DateTime dateLast = CandleTools.GetUnixDate(unixLast);


                //                        // TODO: Het aantal variabelen verminderen
                //                        long unixLoop = unixFirst;
                //                        DateTime dateLoop = CandleTools.GetUnixDate(unixLoop);

                //                        //long candle1mOpenTime = candle.OpenTime;
                //                        //long candle1mCloseTime = candle1mOpenTime + 60;
                //                        //foreach (CryptoInterval interval in GlobalData.IntervalList)
                //                        //{
                //                        //    if (interval.ConstructFrom != null && candle1mCloseTime % interval.Duration == 0)

                //                        // Herbereken deze periode opnieuw uit het onderliggende interval
                //                        while (unixLoop <= unixLast)
                //                        {
                //                            if (unixLoop % interval.Duration == 0)
                //                                CandleTools.CalculateCandleForInterval(interval, interval.ConstructFrom, symbol, unixLoop);

                //                            unixLoop += interval.Duration;
                //                            dateLoop = CandleTools.GetUnixDate(unixLoop); //ter debug want een unix date is onleesbaar
                //                        }
                //                    }
                //                }

                //                // De laatste datum bijwerken (zodat we minder candles hoeven op te halen)
                //                CandleTools.UpdateCandleFetched(symbol, interval); // alleen relevant voor 1m
                //            }
                //        }
                //        catch (Exception error)
                //        {
                //            ScannerLog.Logger.Error(error, "");
                //            GlobalData.AddTextToLogTab(error.ToString());
                //            throw;
                //        }

                //    }
                //}



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
                // De Telegram bot opstarten
                //************************************************************************************
                if (GlobalData.Telegram.Token != "")
                {
                    var whateverx = Task.Run(async () => { await ThreadTelegramBot.Start(GlobalData.Telegram.Token, GlobalData.Telegram.ChatId); });
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
                await ExchangeHelper.KLineTicker.StartAsync();

                //************************************************************************************
                // Om het volume per symbol en laatste prijs te achterhalen
                //************************************************************************************
                await ExchangeHelper.PriceTicker.StartAsync();

                //************************************************************************************
                // De (ontbrekende) candles downloaden (en de achterstand inhalen, blocking!)
                //************************************************************************************
                await ExchangeHelper.FetchCandlesAsync();

                //Ze zijn er wel, deze is eigenlijk overbodig geworden (zit alleen zoveel werk in!)
                //CalculateMissingCandles();

                //************************************************************************************
                // Nu we de achterstand ingehaald hebben kunnen/mogen we analyseren (signals maken)
                //************************************************************************************
                _ = Task.Run(GlobalData.ThreadMonitorCandle.Execute).ConfigureAwait(false);

#if TRADEBOT
                //************************************************************************************
                // Nu we de achterstand ingehaald hebben kunnen/mogen we analyseren (signals maken)
                //************************************************************************************
                if (GlobalData.TradingApi.Key != "")
                {
                    GlobalData.AddTextToLogTab("Starting task for handling orders");
                    _ = Task.Run(async () => { await GlobalData.ThreadMonitorOrder.ExecuteAsync(); });
                }

                GlobalData.AddTextToLogTab("Starting task for checking positions");
                _ = Task.Run(async () => { await GlobalData.ThreadDoubleCheckPosition.ExecuteAsync(); });

                await TradeTools.CheckOpenPositions();



                if (GlobalData.TradingApi.Key != "")
                {
#if BALANCING
                    //************************************************************************************
                    // Nu we de achterstand ingehaald hebben kunnen/mogen we balancen
                    //************************************************************************************
                    GlobalData.AddTextToLogTab("Starting task for balancing assets");
                    _ = Task.Run(async () => { await GlobalData.ThreadBalanceSymbols.ExecuteAsync(); });
#endif


                    //************************************************************************************
                    // Alle data van de exchange monitoren
                    //************************************************************************************
                    _ = ExchangeHelper.UserTicker.StartAsync();


                    //************************************************************************************              
                    // De assets van de exchange halen (overlappend met exchange monitoring om niets te missen)
                    // Via een event worden de assets in de userinterface gezet (dat duurt even)
                    //************************************************************************************
                    await ExchangeHelper.GetAssetsForAccountAsync(GlobalData.ExchangeRealTradeAccount);
                }

                // Toon de ingelezen posities
                //GlobalData.PositionsHaveChanged("");
#endif



                var assembly = Assembly.GetExecutingAssembly().GetName();
                string appName = assembly.Name.ToString();
                string appVersion = assembly.Version.ToString();
                while (appVersion.EndsWith(".0"))
                    appVersion = appVersion[0..^2];
                GlobalData.AddTextToLogTab(appName + " " + appVersion + " ready", true);
                GlobalData.AddTextToTelegram(appName + " " + appVersion + " ready");
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


                if (!checkPositions)
                {
#if TRADEBOT
                    if (GlobalData.BackTest)
                        await PaperTrading.CheckPositionsAfterRestart(GlobalData.ExchangeBackTestAccount);
                    if (GlobalData.Settings.Trading.TradeViaPaperTrading)
                        await PaperTrading.CheckPositionsAfterRestart(GlobalData.ExchangePaperTradeAccount);
#endif
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