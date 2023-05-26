﻿using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot;
using CryptoExchange.Net.Objects;

using CryptoSbmScanner.Binance;
using CryptoSbmScanner.Context;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.TradingView;

using Dapper;
using Dapper.Contrib.Extensions;

using System.Reflection;
using System.Text;

namespace CryptoSbmScanner.Intern;

public class ThreadLoadData
{

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
    //                    GlobalData.Logger.Error(error);
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
    //            foreach (CryptoSbmScanner.CryptoExchange exchange in GlobalData.ExchangeListName.Values)
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


    private static async Task<WebCallResult<BinanceOrder>> OrderInfoAsync(CryptoSymbol symbol, long? orderId)
    {
        BinanceWeights.WaitForFairBinanceWeight(1);

        // Plaats een sell order op Binance
        if (orderId.HasValue)
        {
            using (var client = new BinanceClient())
            {
                WebCallResult<BinanceOrder> result = await client.SpotApi.Trading.GetOrderAsync(symbol.Name, orderId);
                if (!result.Success)
                {
                    string text = string.Format("{0} ERROR getorder {1} {2}", symbol.Name, result.ResponseStatusCode, result.Error);
                    GlobalData.AddTextToLogTab(text);
                    GlobalData.AddTextToTelegram(text);
                }
                return result;

            }
        }
        return null;
    }


    public static async Task ExecuteAsync()
    {
        try
        {
            using (CryptoDatabase databaseThread = new())
            {
                databaseThread.Close();
                databaseThread.Open();

                //************************************************************************************
                //Informatie uit de database lezen
                //************************************************************************************

                ////Alle exchanges uit de database lezen (al gedaan in Main.cs)
                //GlobalData.AddTextToLogTab("Reading exchange information from database");
                //foreach (Model.CryptoExchange exchange in databaseThread.Connection.GetAll<Model.CryptoExchange>())
                //{
                //    GlobalData.AddExchange(exchange);
                //}


                // De symbols uit de database lezen 
                GlobalData.AddTextToLogTab("Reading symbol information from database");
                foreach (CryptoSymbol symbol in databaseThread.Connection.GetAll<CryptoSymbol>())
                {
                    // Ga er van uit dat de symbol niet actief is
                    if (symbol.IsBarometerSymbol())
                        symbol.Status = 1;
                    else
                        symbol.Status = 0;
                    GlobalData.AddSymbol(symbol);
                }


#if DATABASE
                // Alle CandleFetched items uit de database lezen
                // En ja, hier is wat duplicaat code, verhuist naar de AddSymbol() 
                GlobalData.AddTextToLogTab("Reading fetched information from database");
                foreach (CryptoCandleFetched candleFetched in databaseThread.Connection.GetAll<CryptoCandleFetched>())
                {
                    Model.CryptoExchange exchange = null;
                    if (GlobalData.ExchangeListId.TryGetValue((int)candleFetched.ExchangeId, out exchange))
                    {
                        //candleFetched.Exchange = exchange;
                        candleFetched.ExchangeId = exchange.Id;

                        CryptoSymbol symbol = null;
                        if (exchange.SymbolListId.TryGetValue((int)candleFetched.SymbolId, out symbol))
                        {
                            //candleFetched.Symbol = symbol;
                            candleFetched.SymbolId = symbol.Id;

                            // De aanwezige CandleFetched in het geheugen OVERSCHRIJVEN (dat is de clue hier)
                            CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval((CryptoIntervalPeriod)candleFetched.IntervalPeriod);
                            symbolPeriod.CandleFetched = candleFetched;

                            candleFetched.Interval = symbolPeriod.Interval;
                            candleFetched.IntervalId = symbolPeriod.Interval.Id;
                        }
                    }
                }


                //Alle symbols uit de database lezen 
                GlobalData.AddTextToLogTab("Reading position information from database");
                //foreach (Position position in databaseThread.MyConnection.GetAll<Position>())
                //var position1 = databaseThread.MyConnection.Query<Position>("SELECT * FROM positions WHERE id=@id", new { id = 3 });
                foreach (CryptoPosition position in databaseThread.Connection.Query<CryptoPosition>("select * from positions where closetime is null and status < 2"))
                {
                    GlobalData.AddPosition(position);

                    //position.Profit = 0;
                    //position.Invested = 0;
                    //position.Percentage = 0;

                    // De steps inlezen en de status van de openstaande orders opvragen
                    string sql = string.Format("select * from positionsteps where PositionId={0} order by Id", position.Id);
                    foreach (CryptoPositionStep step in databaseThread.Connection.Query<CryptoPositionStep>(sql))
                    {
                        // Index op openstaande orders bijwerken
                        // (Is dat zinvol als de status fileld is?)
                        position.Steps.Add(step.OrderId, step);
                        if (step.Order2Id.HasValue)
                            position.Steps.Add((long)step.Order2Id, step);

                        // We gaan de steps bijwerken dmv de trades (of orders?)
                        if (step.Status < OrderStatus.Filled)
                        {
                            GlobalData.AddTextToLogTab(string.Format("{0} Query order {1}", position.Symbol.Name, step.OrderId));
                            WebCallResult<BinanceOrder> orderInfo = await OrderInfoAsync(position.Symbol, step.OrderId);
                            if (orderInfo.Success)
                            {
                                step.Status = orderInfo.Data.Status;
                                step.QuantityFilled = orderInfo.Data.QuantityFilled;
                                step.QuoteQuantityFilled = orderInfo.Data.QuoteQuantityFilled;
                                if (step.Status >= OrderStatus.Filled)
                                    step.CloseTime = orderInfo.Data.UpdateTime;

                                databaseThread.Connection.Update(step);
                            }
                        }

                    }

                    // ====> TODO: Openstaande quantity vergelijken met wat we cash hebben staan, als cash 0 is dan de positie sluiten?
                    // Blijft een lastige kwestie om te achterhalen of een order overgenomen is (alhoewel, dan heb je vreemde orderid's, hmmmm, das nog wel een goed idee denk ik)

                    // Haal de trades van deze positie op vanaf de 1e order
                    await TradeTools.RefreshTrades(databaseThread, position);
                    TradeTools.CheckPosition(databaseThread, position);
                    if (TradeTools.CalculateProfit(position) == 0)
                    {
                        position.Status = CryptoPositionStatus.positionReady;
                        GlobalData.AddTextToLogTab(string.Format("LoadData: {0} status aangepast naar positie.status={1}", position.Symbol.Name, position.Status));
                    }

                    if (position.Status == CryptoPositionStatus.positionReady)
                    {
                        GlobalData.RemovePosition(position);
                        if (!position.CloseTime.HasValue)
                            position.CloseTime = DateTime.UtcNow;
                        databaseThread.Connection.Update(position);
                    }

                }
#endif



                //************************************************************************************
                // Alle symbols van de exchange halen en mergen met de ingelezen symbols.
                // Via een event worden de muntparen in de userinterface gezet (dat duurt even)
                //************************************************************************************
                await Task.Run(async () => { await BinanceFetchSymbols.ExecuteAsync(); });

                // Na het inlezen van de symbols de lijsten goed zetten
                TradingConfig.InitWhiteAndBlackListSettings();
                // De (interne) barometer symbols toevoegen
                GlobalData.InitBarometerSymbols();



                GlobalData.AddTextToLogTab("Reading candle information");

#if DATABASE
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
                    builder.AppendLine(string.Format("select * from candles with(index(IdxCandlesIntervalOpenTime)) where (intervalid={0} and opentime>={1}) -- {2} {3}",
                        //symbolid=12 and (btcusdt)
                        interval.Id, openUnixTime, interval.Name, CandleTools.GetUnixDate(openUnixTime).ToLocalTime()));
                    //builder.AppendLine(string.Format("(interval={0} and date>='{1}')", (int)interval.IntervalPeriod, openUnixDate.ToString("yyyy-MM-dd HH:mm")));
                    // ter debug (controle timing)
                    GlobalData.AddTextToLogTab("Interval " + interval.Name + " " + builder.ToString());

                    // Voeg de candle toe aan de lijst verrijkt met meta data over interval, symbol en exchange
                    foreach (CryptoCandle candle in databaseThread.Connection.Query<CryptoCandle>(builder.ToString()))
                    {
                        if (GlobalData.ExchangeListId.TryGetValue(candle.ExchangeId, out Model.CryptoExchange exchange))
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
                    }
                    GlobalData.AddTextToLogTab("Interval " + interval.Name + " " + aantal.ToString("N0") + " candles read");
                }
                GlobalData.AddTextToLogTab(aantaltotaal.ToString("N0") + " candles read");
#else
                DataStore.LoadCandles();
#endif


                //************************************************************************************
                // Vanaf dit moment worden candles (en candleperiod) bewaard 
                // (want het herberekenen kan definitieve candles produceren)
                //************************************************************************************
#if DATABASE
                GlobalData.AddTextToLogTab("Starting task for saving candles");
                GlobalData.TaskSaveCandles = new ThreadSaveCandles();
                // Geen await, deze mag/MOET parallel
                var whateverX = Task.Run(() => { GlobalData.TaskSaveCandles.Execute(); });
#endif

                //************************************************************************************
                // Alle intervallen herberekenen (het is een bulk hercalculatie voor de laatste in het geheugen gelezen candles)
                // In theorie is dit allemaal reeds in de database opgeslagen, maar baat het niet dan schaad het niet
                //************************************************************************************
                foreach (Model.CryptoExchange exchange in GlobalData.ExchangeListName.Values)
                {
                    //break; //Laat maar even... (HEEL waarschijnlijk een verschil in UTC bij de omzetactie)

                    GlobalData.AddTextToLogTab("Calculating candle intervals for " + exchange.Name + " (" + exchange.SymbolListName.Count.ToString() + " symbols)");
                    foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
                    {
                        // De "barometer" munten overslagen AUB, die hebben slechts 3 intervallen (beetje quick en dirty allemaal)
                        if (symbol.IsBarometerSymbol())
                            continue;

                        if (symbol.CandleList.Any())
                        {
                            try
                            {
                                // Van laag naar hoog zodat de hogere intervallen worden berekend
                                foreach (CryptoSymbolInterval symbolPeriod in symbol.IntervalPeriodList)
                                {
                                    CryptoInterval interval = symbolPeriod.Interval;
                                    if (interval.ConstructFrom != null)
                                    {
                                        // Voeg een candle toe aan een hogere tijd interval (eventueel uit db laden)
                                        var candlesInterval = symbolPeriod.CandleList;
                                        if (candlesInterval.Values.Count > 0)
                                        {
                                            // Periode start
                                            long unixFirst = candlesInterval.Values.First().OpenTime;
                                            unixFirst -= unixFirst % interval.Duration;
                                            DateTime dateFirst = CandleTools.GetUnixDate(unixFirst);

                                            // Periode einde
                                            long unixLast = candlesInterval.Values.Last().OpenTime;
                                            unixLast -= unixLast % interval.Duration;
                                            DateTime dateLast = CandleTools.GetUnixDate(unixLast);


                                            // TODO: Het aantal variabelen verminderen
                                            long unixLoop = unixFirst;
                                            DateTime dateLoop = CandleTools.GetUnixDate(unixLoop);

                                            // Herbereken deze periode opnieuw uit het onderliggende interval
                                            while (unixLoop <= unixLast)
                                            {
                                                CandleTools.CalculateCandleForInterval(interval, interval.ConstructFrom, symbol, unixLoop);

                                                unixLoop += interval.Duration;
                                                dateLoop = CandleTools.GetUnixDate(unixLoop); //ter debug want een unix date is onleesbaar
                                            }
                                        }
                                    }

                                    // De laatste datum bijwerken (zodat we minder candles hoeven op te halen)
                                    CandleTools.UpdateCandleFetched(symbol, interval); // alleen relevant voor 1m
                                }
                            }
                            catch (Exception error)
                            {
                                GlobalData.Logger.Error(error);
                                GlobalData.AddTextToLogTab(error.ToString());
                                throw;
                            }

                        }
                    }
                }


                int aantalTotaal = 0;
                foreach (Model.CryptoExchange exchange in GlobalData.ExchangeListName.Values)
                {
                    foreach (CryptoSymbol symbol in exchange.SymbolListName.Values)
                    {
                        foreach (CryptoSymbolInterval symbolPeriod in symbol.IntervalPeriodList)
                        {
                            aantalTotaal += symbolPeriod.CandleList.Count;
                        }
                    }
                    if (aantalTotaal > 0)
                        GlobalData.AddTextToLogTab("Total amount of candles in memory " + aantalTotaal.ToString("N0") + " candles");
                }
            }


            //************************************************************************************
            // De Telegram bot opstarten
            //************************************************************************************
            GlobalData.AddTextToLogTab("Starting Telegram bot");
            var whateverx = Task.Run(async () => { await ThreadTelegramBot.ExecuteAsync(); }); // Geen await, forever long running


            //************************************************************************************
            // Vanaf dit moment worden de 1m candles bijgewerkt 
            // Vanaf dit moment worden de aangeboden 1m candles in ons systeem verwerkt
            // (Dit moet overlappen met "achterstand bijwerken" want anders ontstaan er gaten)
            // BUG/Probleem! na nieuwe munt of instellingen wordt dit niet opnieuw gedaan (herstart nodig)
            //************************************************************************************
            {
                // Deze methode werkt alleen op Binance
                if (GlobalData.ExchangeListName.TryGetValue("Binance", out Model.CryptoExchange exchange))
                {
                    foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
                    {
                        if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
                        {
                            List<CryptoSymbol> symbols = quoteData.SymbolList.ToList();

                            // We krijgen soms timeouts van Binance (eigenlijk de library) omdat we teveel 
                            // symbols aanbieden, daarom splitsen we het hier de lijst in twee stukken.
                            int splitCount = 200;
                            if (symbols.Count > splitCount)
                                splitCount = symbols.Count / 2;

                            while (symbols.Count > 0)
                            {
                                BinanceStream1mCandles BinanceStream1mCandles = new(quoteData);

                                // Met de volle mep reageert Binance niet snel genoeg (timeout errors enzovoort)
                                // Dit is een quick fix na de update van Binance.Net van 7 -> 8
                                while (symbols.Count > 0)
                                {
                                    CryptoSymbol symbol = symbols[0];
                                    symbols.Remove(symbol);

                                    BinanceStream1mCandles.symbols.Add(symbol.Name);

                                    // Ergens een lijn trekken? 
                                    if (BinanceStream1mCandles.symbols.Count >= splitCount)
                                        break;
                                }

                                // opvullen tot circa 150 coins?
                                quoteData.BinanceStream1mCandles.Add(BinanceStream1mCandles);
                                await BinanceStream1mCandles.StartAsync(); // bewust geen await
                            }
                        }
                    }
                }
            }


            await Task.Factory.StartNew(() => new TradingViewSymbolInfo().StartAsync("TVC:DXY", "US Dollar Index", "N2", GlobalData.TradingViewDollarIndex, 10));
            await Task.Factory.StartNew(() => new TradingViewSymbolInfo().StartAsync("SP:SPX", "S&P 500", "N2", GlobalData.TradingViewSpx500, 10));
            await Task.Factory.StartNew(() => new TradingViewSymbolInfo().StartAsync("CRYPTOCAP:BTC.D", "BTC Dominance", "N2", GlobalData.TradingViewBitcoinDominance, 10));
            await Task.Factory.StartNew(() => new TradingViewSymbolInfo().StartAsync("CRYPTOCAP:TOTAL3", "Market Cap total", "N0", GlobalData.TradingViewMarketCapTotal, 10));
            await Task.Factory.StartNew(() => new FearAndGreatSymbolInfo().StartAsync("https://alternative.me/crypto/fear-and-greed-index/", "Fear and Greed index", "N2", GlobalData.FearAndGreedIndex, 10));


            //************************************************************************************
            // Om het volume per symbol en laatste prijs te achterhalen (weet geen betere manier)
            //************************************************************************************
            // Deze methode werkt alleen op Binance
            GlobalData.TaskBinanceStreamPriceTicker = new BinanceStreamPriceTicker();
            var _ = Task.Run(async () => { await GlobalData.TaskBinanceStreamPriceTicker.ExecuteAsync(); });


            //************************************************************************************
            // De (ontbrekende) candles downloaden (en de achterstand inhalen, blocking!)
            //************************************************************************************
            // Deze methode werkt alleen op Binance
            await Task.Run(async () => { await BinanceFetchCandles.ExecuteAsync(); }); // wachten tot deze klaar is

            //Ze zijn er allemaal wel, deze is overbodig
            //CalculateMissingCandles();



            //************************************************************************************
            // Nu we de achterstand ingehaald hebben kunnen/mogen we analyseren (signals maken)
            //************************************************************************************
            GlobalData.AddTextToLogTab("Starting task for creating signals");
            _ = Task.Run(() => { GlobalData.ThreadMonitorCandle.Execute(); });


#if TRADEBOT
            //************************************************************************************
            // Nu we de achterstand ingehaald hebben kunnen/mogen we analyseren (signals maken)
            //************************************************************************************
            GlobalData.AddTextToLogTab("Starting task for handling orders");
            _ = Task.Run(async () => { await GlobalData.ThreadMonitorOrder.ExecuteAsync(); });


            //************************************************************************************
            // Nu we de achterstand ingehaald hebben kunnen/mogen we monitoren
            //************************************************************************************
            GlobalData.AddTextToLogTab("Starting task for monitor candles");
            _ = Task.Run(async () => { await GlobalData.TaskMonitorSignal.ExecuteAsync(); });



#if BALANCING
            // Nu we de achterstand ingehaald hebben kunnen/mogen we balancen
            //************************************************************************************
            GlobalData.AddTextToLogTab("Starting task for balancing assets");
            _ = Task.Run(async () => { await GlobalData.ThreadBalanceSymbols.ExecuteAsync(); });
#endif


            //************************************************************************************
            // Alle data van Binance monotoren
            // Deze methode werkt alleen op Binance
            //************************************************************************************
            GlobalData.AddTextToLogTab("Starting task for monitoring events");
            _ = Task.Run(async () => { await GlobalData.TaskBinanceStreamUserData.ExecuteAsync(); });


            //************************************************************************************              
            // De assets van de exchange halen (overlappend met Binance monitoring om niets te missen)
            // Via een event worden de assets in de userinterface gezet (dat duurt even)
            //************************************************************************************
            BinanceFetchAssets fetchAssets = new BinanceFetchAssets();
            await Task.Run(async () => { await fetchAssets.Execute(); });
#endif


            var assembly = Assembly.GetExecutingAssembly().GetName();
            string appName = assembly.Name.ToString();
            string appVersion = assembly.Version.ToString();
            while (appVersion.EndsWith(".0"))
                appVersion = appVersion[0..^2];
            GlobalData.AddTextToLogTab(appName + " " + appVersion + " ready", true);
            GlobalData.AddTextToTelegram(appName + " " + appVersion + " ready");

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

            // Assume we now can run
            GlobalData.ApplicationStatus = ApplicationStatus.AppStatusRunning;

        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab(error.ToString());
            throw;
        }
    }


}

