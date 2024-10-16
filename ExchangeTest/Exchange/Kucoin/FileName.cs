namespace ExchangeTest.Exchange.Kucoin;

internal class FileName
{
    //// De ondersteunde types (alleen Binance heeft OCO)
    //public enum CryptoOrderType
    //{
    //    Market,             // Het "beste" bod van de markt
    //    Limit,              // Een standaard order
    //    StopLimit,          // Een stoplimit order
    //    Oco                 // OCO's alleen op Binance
    //}

    //public enum CryptoOrderSide
    //{
    //    Buy,
    //    Sell
    //}


    //public class TradeParams
    //{
    //    // standaard buy of sell
    //    public CryptoOrderSide Side { get; set; }
    //    public CryptoOrderType OrderType { get; set; }
    //    public long OrderId { get; set; }
    //    public decimal Price { get; set; }
    //    public decimal Quantity { get; set; }
    //    public decimal QuoteQuantity { get; set; }
    //    public DateTime CreateTime { get; set; }

    //    // OCO gerelateerd
    //    public decimal? StopPrice { get; set; }
    //    public decimal? LimitPrice { get; set; }
    //    public long? Order2Id { get; set; }
    //    //public long? OrderListId { get; set; }
    //}

    //private static int first = 1;
    //public object BinanceClient { get; private set; }
    //CryptoDatabase Database;
    //ExchangeBase ExchangeApi;


    /*
private async Task KucoinTest()
{
    //int TickerCount = 0;
    try
    {
        // De symbol naam wordt anders gecodeerd zie ik..? streepjes in de naamgeving van de symbol (zucht)

        // https://api.kucoin.com/api/v1/market/stats?symbol=BTC-USDT
        // https://api.kucoin.com/api/v1/ticker?symbol=BTCUSDT
        // https://api.kucoin.com/api/v1/ticker?symbol=BTC-USDT
        //stream
        // https://api.kucoin.com/market/candles:BTC-USDT_1min
        //kline
        // https://api.kucoin.com/api/v1/market/candles?type=1min&symbol=BTC-USDT&startAt=1566703297&endAt=1566789757
        // https://api.kucoin.com/api/v1/market/candles?symbol=BTC-USDT&type=1hour&startAt=1562460061&endAt=1562467061
        // https://docs.kucoin.com/#get-trade-histories
        /// https://api.kucoin.com/api/v1/market/histories?symbol=BTC-USDT

        // https://docs.kucoin.com/#get-all-tickers
        // Voor het ophalen van onder andere de volumes:
        // https://api.kucoin.com/api/v1/market/allTickers

        // OSMO-USDT is zo'n  flat coin, er komt soms meerdere uren geen kline

        CryptoScanBot.Exchange.Kucoin.Api api = new();

        CryptoSymbol symbol = new()
        {
            ExchangeId = 4,
            Base = "ONE",
            Quote = "USDT",
            Name = "ONEUSDT",
            BaseVolume = 0,
            Volume = 0,
            Status = 1,
            PriceTickSize = 0.000001m,
        };
        GlobalData.AddSymbol(symbol);

        CryptoInterval interval = null;
        if (!GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1m, out interval))
            throw new Exception("Geen intervallen?");


        if (GlobalData.ExchangeListName.TryGetValue("Kucoin", out CryptoScanBot.Model.CryptoExchange exchange))
        {
            // Aanvullend de tickers aanroepen voor het volume...
            KucoinRestClient client = new();

            // tick voor 1 symbol
            // Maar ohjee, daar zit het volume dus niet in!
            //var ktick = await client.SpotApi.ExchangeData.GetTickerAsync(symbol.Base + "-" + symbol.Quote);
            //if (ktick.Success && ktick.Data != null && ktick.Data.qu QuoteVolume.HasValue)
            //    symbol1.Volume = (decimal)x.QuoteVolume;

            // Allemaal
            var iets = await client.SpotApi.ExchangeData.GetTickersAsync();
            foreach (var x in iets.Data.Data)
            {
                string symbolName1 = x.Symbol.Replace("-", "");
                if (exchange.SymbolListName.TryGetValue(symbolName1, out CryptoSymbol? symbol1))
                {
                    if (x.QuoteVolume.HasValue)
                        symbol1.Volume = (decimal)x.QuoteVolume;
                }
            }
        }

        string symbolName = symbol.Base + "-" + symbol.Quote;

        // Historische candles halen
        {
            // Er is blijkbaar geen maximum volgens de docs? toch iets van 1500?
            // En (verrassing) de volgorde van de candles is van nieuw naar oud! 
            KucoinRestClient client = new();
            DateTime dateStart = DateTime.UtcNow.AddMinutes(-300);
            var result = await client.SpotApi.ExchangeData.GetKlinesAsync(symbolName, (Kucoin.Net.Enums.KlineInterval)KlineInterval.OneMinute, dateStart, null);

            string text = JsonSerializer.Serialize(result, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
            string filename = $@"E:\Kucoin\candles.json";
            File.WriteAllText(filename, text);

            if (result.Success)
            {
                foreach (var kline in result.Data)
                {
                    // Quoted = volume * price (expressed in usdt/eth/btc etc), base is coins
                    CryptoCandle candle = CandleTools.CreateCandle(symbol, interval, kline.OpenTime,
                        kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.QuoteVolume, false);
                }

                CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
                foreach (var candle in symbolPeriod.CandleList.Values)
                    GlobalData.AddTextToLogTab(candle.OhlcText(symbol, interval, "N8"));

                GlobalData.AddTextToLogTab($"Succes! {result.Data.Count()} candles");
            }
            else
                GlobalData.AddTextToLogTab("Error: " + result.Error?.Message);
        }


        KucoinSocketClient socketClient = new();
        CryptoScanBot.Exchange.Kucoin.KLineTickerItem ticker = new(symbol.QuoteData);
        ticker.Symbol = symbol;
        Task task = Task.Run(async () => { await ticker.StartAsync(socketClient); });



        //// Implementatie kline ticker (via cache, wordt door de timer verwerkt)
        //SortedList<long, CryptoCandle> klineList = new();
        //    var socketClient = new KucoinSocketClient();
        //    var subscriptionResult = await socketClient.SpotApi.SubscribeToKlineUpdatesAsync(symbolName,
        //        (Kucoin.Net.Enums.KlineInterval)KlineInterval.OneMinute, data =>
        //        {

        //            KucoinKline kline = data.Data.Candles;
        //            string text = JsonSerializer.Serialize(kline, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
        //            GlobalData.AddTextToLogTab(data.Topic + " " + text);

        //            if (GlobalData.ExchangeListName.TryGetValue(CryptoScanBot.Exchange.Kucoin.ExchangeOptions.ExchangeName, out CryptoScanBot.Model.CryptoExchange exchange))
        //            {
        //                string symbolName = data.Topic.Replace("-", "");
        //                if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
        //                {
        //                    TickerCount++;
        //                    //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", topic, kline.Timestamp.ToLocalTime()));

        //                    Monitor.Enter(symbol.CandleList);
        //                    try
        //                    {
        //                        // Toevoegen aan de lokale cache en/of aanvullen
        //                        // (via de cache omdat de candle in opbouw is)
        //                        // (bij veel updates is dit stukje cpu-intensief?)
        //                        long candleOpenUnix = CandleTools.GetUnixTime(kline.OpenTime, 60);
        //                        if (!klineList.TryGetValue(candleOpenUnix, out CryptoCandle candle))
        //                        {
        //                            candle = new();
        //                            klineList.Add(candleOpenUnix, candle);
        //                        }
        //                        candle.IsDuplicated = false;
        //                        candle.OpenTime = candleOpenUnix;
        //                        candle.Open = kline.OpenPrice;
        //                        candle.High = kline.HighPrice;
        //                        candle.Low = kline.LowPrice;
        //                        candle.Close = kline.ClosePrice;
        //                        candle.Volume = kline.QuoteVolume;
        //                        //GlobalData.AddTextToLogTab("Received ticker update " + candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat));

        //                        // Dit is de laatste bekende prijs (de priceticker vult eventueel aan)
        //                        symbol.LastPrice = kline.ClosePrice;
        //                        symbol.AskPrice = kline.ClosePrice;
        //                        symbol.BidPrice = kline.ClosePrice;
        //                    }
        //                    finally
        //                    {
        //                        Monitor.Exit(symbol.CandleList);
        //                    }
        //                }

        //            }
        //        }
        //    );

        //    // Subscribe to network-related stuff
        //    if (subscriptionResult.Success)
        //    {
        //        GlobalData.AddTextToLogTab($"Subscription succes! {subscriptionResult.Data.Id}");
        //        _subscription = subscriptionResult.Data;

        //        // Events
        //        _subscription.Exception += Exception;
        //        _subscription.ConnectionLost += ConnectionLost;
        //        _subscription.ConnectionRestored += ConnectionRestored;
        //    }
        //    else
        //    {
        //        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} {quote} 1m ERROR starting candle stream {subscriptionResult.Error.Message}");
        //        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} {quote} 1m ERROR starting candle stream {symbolNames}");

        //    }




        //// Implementatie kline timer (fix)
        //// Omdat er niet altijd een nieuwe candle aangeboden wordt (zoals "flut" munt TOMOUSDT)
        //// kun je aanvullend een timer kunnen gebruiken die alsnog de vorige candle herhaalt.
        //// De gedachte is om dat iedere minuut 10 seconden na het normale kline event te doen.

        //System.Timers.Timer timerKline = new()
        //{
        //    AutoReset = false,
        //};
        //timerKline.Elapsed += new System.Timers.ElapsedEventHandler((object? sender, System.Timers.ElapsedEventArgs e) =>
        //{
        //    CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        //    long expectedCandlesUpto = CandleTools.GetUnixTime(DateTime.UtcNow, 60) - interval.Duration;

        //    // locking.. nog eens nagaan of dat echt noodzakelijk is hier.
        //    // in principe kun je hier geen "collision" hebben met threads?
        //    Monitor.Enter(symbol.CandleList);
        //    try
        //    {
        //        // De niet aanwezige candles dupliceren
        //        if (symbolPeriod.CandleList.Count > 0)
        //        {
        //            CryptoCandle lastCandle = symbolPeriod.CandleList.Values.Last();
        //            while (lastCandle.OpenTime < expectedCandlesUpto)
        //            {
        //                // Als deze al aanwezig dmv een ticker update niet dupliceren
        //                long nextCandleUnix = lastCandle.OpenTime + interval.Duration;
        //                if (klineList.TryGetValue(nextCandleUnix, out CryptoCandle nextCandle))
        //                    break;

        //                // Dupliceer de laatste candle als deze niet voorkomt (zogenaamde "flat" candle)
        //                // En zet deze in de kline list cache (anders teveel duplicatie van de logica)
        //                if (!symbolPeriod.CandleList.TryGetValue(nextCandleUnix, out nextCandle))
        //                {
        //                    nextCandle = new();
        //                    klineList.Add(nextCandleUnix, nextCandle);
        //                    nextCandle.IsDuplicated = true;
        //                    nextCandle.OpenTime = nextCandleUnix;
        //                    nextCandle.Open = lastCandle.Close;
        //                    nextCandle.High = lastCandle.Close;
        //                    nextCandle.Low = lastCandle.Close;
        //                    nextCandle.Close = lastCandle.Close;
        //                    nextCandle.Volume = 0; // geen volume
        //                    lastCandle = nextCandle;
        //                }
        //                else break;
        //            }
        //        }


        //        // De data van de ticker updates en duplicatie verwerken
        //        foreach (CryptoCandle candle in klineList.Values.ToList())
        //        {
        //            if (candle.OpenTime <= expectedCandlesUpto)
        //            {
        //                klineList.Remove(candle.OpenTime);
        //                CandleTools.CreateCandle(symbol, interval, candle.Date,
        //                    candle.Open, candle.High, candle.Low, candle.Close, candle.Volume);
        //                SaveCandleAndUpdateHigherTimeFrames(symbol, candle);

        //                if (candle.IsDuplicated)
        //                    GlobalData.AddTextToLogTab("Dup candle " + candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat));
        //                else
        //                    GlobalData.AddTextToLogTab("New candle " + candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat));

        //                // Aanbieden voor analyse (dit gebeurd zowel in de ticker als ProcessCandles)
        //                if (candle.OpenTime == expectedCandlesUpto && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
        //                {
        //                    GlobalData.AddTextToLogTab("Aanbieden analyze " + candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat));
        //                    GlobalData.AddTextToLogTab("");
        //                    //GlobalData.ThreadMonitorCandle.AddToQueue(symbol, candle);
        //                }
        //            }
        //            else break;
        //        }

        //    }
        //    finally
        //    {
        //        Monitor.Exit(symbol.CandleList);
        //    }

        //    if (sender is System.Timers.Timer t)
        //    {
        //        t.Interval = GetInterval();
        //        t.Start();
        //    }
        //});
        //timerKline.Interval = GetInterval();
        //timerKline.Start();

    }
    catch (Exception error)
    {
        ScannerLog.Logger.Error(error, "");
        GlobalData.AddTextToLogTab("error back testing " + error.ToString()); // symbol.Text + " " + 
    }
}
*/


    //    void SaveCandleAndUpdateHigherTimeFrames(CryptoSymbol symbol, CryptoCandle candle)
    //    {

    //        // Calculate the higher timeframes
    //        foreach (CryptoInterval interval in GlobalData.IntervalList)
    //        {
    //            // Deze doen een call naar de TaskSaveCandles en doet de UpdateCandleFetched (wellicht overlappend?)
    //            if (interval.ConstructFrom != null)
    //                CandleTools.CalculateCandleForInterval(interval, interval.ConstructFrom, symbol, candle.OpenTime);

    //            // Het risico is dat als de ticker uitvalt dat de candles nooit hersteld worden, willen we dat?
    //            CandleTools.UpdateCandleFetched(symbol, interval);
    //        }
    //    }


    //    static double GetInterval()
    //    {
    //        // bewust 5 seconden en een beetje layer zodat we zeker weten dat de kline er is
    //        // (anders zou deze 60 seconden later alsnog verwerkt worden, maar dat is te laat)
    //        DateTime now = DateTime.Now;
    //        return 5050 + ((60 - now.Second) * 1000 - now.Millisecond);
    //    }

    //void t_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    //{
    //    string time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + "  " + DateTime.Now.ToString("o");
    //    Invoke((MethodInvoker)(() => textBox1.AppendText($"Timer test1 {time} \r\n")));

    //    if (sender is System.Timers.Timer t)
    //    { 
    //        t.Interval = GetInterval();
    //        t.Start();
    //    }
    //}


}
