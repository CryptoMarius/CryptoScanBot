//using System.Text.Encodings.Web;
//using System.Text.Json;

//using Bybit.Net.Clients;
//using Bybit.Net.Enums;
//using Bybit.Net.Objects;
//using Bybit.Net.Objects.Models.V5;
//using Bybit.Net.Objects.Models.Socket;

//using Binance.Net.Clients;
//using Binance.Net.Enums;
//using Binance.Net.Interfaces.Clients;
//using Binance.Net.Objects;
//using Binance.Net.Objects.Models;
//using Binance.Net.Objects.Models.Spot;
//using Binance.Net.Objects.Models.Spot.Socket;

//using CryptoExchange.Net.Objects;
//using CryptoExchange.Net.Sockets;
//using CryptoExchange.Net.Authentication;

//using Kucoin.Net.Clients;
//using Kucoin.Net.Enums;
//using Kucoin.Net.Objects.Models.Spot;

using System.Text.Encodings.Web;
using System.Text.Json;

using Bybit.Net.Clients;
using Bybit.Net.Enums;

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;

using CryptoScanBot;
using CryptoScanBot.Context;
using CryptoScanBot.Enums;
using CryptoScanBot.Exchange;
using CryptoScanBot.Exchange.BybitSpot;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;
using CryptoScanBot.Trader;

using Microsoft.Extensions.Logging;

namespace ExchangeTest;

// Een test dingetje

public partial class Form1 : Form
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
    ExchangeBase ExchangeApi;

    public Form1()
    {
        InitializeComponent();

       
        GlobalData.LogToLogTabEvent += new AddTextEvent(AddTextToLogTab);

        CryptoDatabase.SetDatabaseDefaults();
        GlobalData.LoadExchanges();
        GlobalData.LoadIntervals();

        // Is er via de command line aangegeven dat we default een andere exchange willen?
        {
            ApplicationParams.InitApplicationOptions();

            string exchangeName = ApplicationParams.Options.ExchangeName;
            if (exchangeName != null)
            {
                // De default exchange is Binance (geen goede keuze in NL op dit moment)
                if (exchangeName == "")
                    exchangeName = "Binance";
                if (GlobalData.ExchangeListName.TryGetValue(exchangeName, out var exchange))
                {
                    GlobalData.Settings.General.Exchange = exchange;
                    GlobalData.Settings.General.ExchangeId = exchange.Id;
                    GlobalData.Settings.General.ExchangeName = exchange.Name;
                }
                else throw new Exception(string.Format("Exchange {0} bestaat niet", exchangeName));
            }
        }


        GlobalData.LoadAccounts();
        GlobalData.LoadSymbols();
        ExchangeHelper.ExchangeDefaults();
        GlobalData.ApplicationStatus = CryptoApplicationStatus.Running;

        //Database = new();
        ExchangeApi = ExchangeHelper.GetExchangeInstance(GlobalData.Settings.General.ExchangeId);

        GlobalData.TradingApi.Key = "";
        GlobalData.TradingApi.Secret = "";

        //GlobalData.Settings.General.Exchange = exchange;
        //GlobalData.Settings.General.ExchangeId = exchange.Id;
        //GlobalData.Settings.General.ExchangeName = exchange.Name;


        //BinanceTestAsync();
        ByBitSpotTestAsync();
        //KucoinTest();
        //MexcTest();
    }

    private void AddTextToLogTab(string text, bool extraLineFeed = false)
    {
        if (IsHandleCreated)
        {
            text = text.TrimEnd();
            ScannerLog.Logger.Info(text);

            if (text != "")
                text = DateTime.Now.ToLocalTime() + " " + text;
            if (extraLineFeed)
                text += "\r\n\r\n";
            else
                text += "\r\n";

            if (InvokeRequired)
                Invoke((MethodInvoker)(() => textBox1.AppendText(text)));
            else
                textBox1.AppendText(text);

            //File.AppendAllText(@"D:\Shares\Projects\.Net\CryptoScanBot\Testjes\bin\Debug\data\backtest.txt", text);
        }
    }

    //private static string baseUrl = "https://api.mexc.com";

    //private async Task<string> SendAsync(HttpClient httpClient, string requestUri, HttpMethod httpMethod, object content = null)
    //{
    //    Console.WriteLine(requestUri);
    //    using (var request = new HttpRequestMessage(httpMethod, baseUrl + requestUri))
    //    {
    //        request.Headers.Add("X-MEXC-APIKEY", apiKey);

    //        if (content is not null)
    //        {
    //            //request.Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");
    //            string text = System.Text.Json.JsonSerializer.Serialize(content, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
    //            //File.WriteAllText(filename, text);
    //            request.Content = new StringContent(text);
    //            //request.Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");
    //        }

    //        HttpResponseMessage response = await httpClient.SendAsync(request);

    //        using (HttpContent responseContent = response.Content)
    //        {
    //            string jsonString = await responseContent.ReadAsStringAsync();

    //            return jsonString;
    //        }
    //    }
    //}

    private async Task MexcTest()
    {
        try
        {
            //string text;
            //string apiKey = "your apikey";
            //string apiSecret = "your secret";
            //string BaseUrl = "https://api.mexc.com";

            HttpClient httpClient = new HttpClient();
            //MexcService service = new(apiKey, apiSecret, BaseUrl, httpClient);


            //text = await SendAsync(httpClient, "/api/v3/exchangeInfo?symbol=BTCUSDT", HttpMethod.Get);
            //GlobalData.AddTextToLogTab(text);

            //text = await SendAsync(httpClient, "/api/v3/exchangeInfo?symbols=BTCUSDT,ETHUSDT", HttpMethod.Get);
            //GlobalData.AddTextToLogTab(text);

            //text = await SendAsync(httpClient, "/api/v3/exchangeInfo", HttpMethod.Get);
            //GlobalData.AddTextToLogTab(text);


            ///// Exchange Information
            //using (var response = service.SendPublicAsync("/api/v3/exchangeInfo", HttpMethod.Get, new Dictionary<string, object> {{"symbol", "BTCUSDT"}}))
            //{
            //    string text = await response;
            //    GlobalData.AddTextToLogTab(text);
            //};

            //using (var response = service.SendPublicAsync("/api/v3/exchangeInfo", HttpMethod.Get, new Dictionary<string, object> { }))
            //{
            //    string text = await response;
            //    //GlobalData.AddTextToLogTab(text);
            //};
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("error back testing " + error.ToString()); // symbol.Text + " " + 
        }
    }


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
                    if (exchange.SymbolListName.TryGetValue(symbolName1, out CryptoSymbol symbol1))
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
                        CryptoCandle candle = CandleTools.HandleFinalCandleData(symbol, interval, kline.OpenTime,
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

            //            if (GlobalData.ExchangeListName.TryGetValue(CryptoScanBot.Exchange.Kucoin.Api.ExchangeName, out CryptoScanBot.Model.CryptoExchange exchange))
            //            {
            //                string symbolName = data.Topic.Replace("-", "");
            //                if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol symbol))
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
            //        GlobalData.AddTextToLogTab($"{Api.ExchangeName} {quote} 1m ERROR starting candle stream {subscriptionResult.Error.Message}");
            //        GlobalData.AddTextToLogTab($"{Api.ExchangeName} {quote} 1m ERROR starting candle stream {symbolNames}");

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
            //                CandleTools.HandleFinalCandleData(symbol, interval, candle.Date,
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
    //#if SQLDATABASE
    //        GlobalData.TaskSaveCandles.AddToQueue(lastCandle);
    //#endif

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

    private async Task ByBitTestAsync()
    {
        try
        {
            // Problemen met de opties, met name de AutoTimestamp en Reconnect wil ik hebben
            //string text;

            //IServiceCollection serviceCollection = new ServiceCollection();
            //serviceCollection.AddBinance()
            //    .AddLogging(options =>
            //    {
            //        options.SetMinimumLevel(LogLevel.Trace);
            //        options.AddProvider(new TraceLoggerProvider());
            //    });

            //BybitRestOptions x = new();
            //x.ApiCredentials = new ApiCredentials("", "");
            //x.AutoTimestamp = true;
            //x.SpotOptions.AutoTimestamp = true;

            //BybitRestClient bybitClient = new();

            //var client = new BybitRestClient(options =>
            //{
            //    //options.ApiCredentials = new ApiCredentials(apiKey, apiSecret);
            //    //LogLevel = LogLevel.Trace;
            //    //RequestTimeout = TimeSpan.FromSeconds(60),
            //    //InverseFuturesApiOptions = new RestApiClientOptions
            //    //{
            //    //    //ApiCredentials = new ApiCredentials("", ""),
            //    //    AutoTimestamp = false
            //    //}
            //});

            ////client.RateLimitingBehaviour = RateLimitingBehaviour.Wait;

            //new BybitRestClient(options =>
            //{
            //    options.AutoTimestamp = true;
            //    //options.RateLimitingBehaviour = RateLimitingBehaviour.Wait;
            //    //options.ApiCredentials = new ApiCredentials(apiKey, apiSecret);

            //    options.SpotOptions.AutoTimestamp = true;
            //    //options.SpotOptions.RateLimiters = new();
            //    // Alleen voor Binance zie ik iets met limits
            //    //options.SpotOptions.RateLimiters. AddTotalRateLimit(50, TimeSpan.FromSeconds(10));
            //    // override options just for the InverseFuturesOptions api
            //    //options.SpotOptions.ApiCredentials = new ApiCredentials("API-KEY", "API-SECRET");
            //    //options.SpotOptions.RequestTimeout = TimeSpan.FromSeconds(60);
            //});

            ////new RateLimiter().AddTotalRateLimit(50, TimeSpan.FromSeconds(10));

            ////await bybitClient.SpotApiV3.CommonSpotClient.


            ////Experiment (geen volume):
            //var spotApiV3SymbolData = await client.SpotApiV3.ExchangeData.GetSymbolsAsync();
            //text = JsonSerializer.Serialize(spotApiV3SymbolData.Data, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
            //File.WriteAllText("E:\\ByBit\\spotApiV3SymbolData.json", text);

            ////Experiment (geen volume):
            //var usdPerpetualApiSymbolData = await client.UsdPerpetualApi.ExchangeData.GetSymbolsAsync();
            //text = JsonSerializer.Serialize(usdPerpetualApiSymbolData.Data, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
            //File.WriteAllText("E:\\ByBit\\usdPerpetualApiSymbolData.json", text);

            ////Experiment (geen volume):
            //var inversePerpetualApi = await client.InversePerpetualApi.ExchangeData.GetSymbolsAsync();
            //text = JsonSerializer.Serialize(inversePerpetualApi.Data, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
            //File.WriteAllText("E:\\ByBit\\inversePerpetualApi.json", text);

            ////Experiment (geen volume):
            //var inverseFuturesApi = await client.InverseFuturesApi.ExchangeData.GetSymbolsAsync();
            //text = JsonSerializer.Serialize(inverseFuturesApi.Data, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
            //File.WriteAllText("E:\\ByBit\\inverseFuturesApi.json", text);


            //GlobalData.Settings.General.ExchangeId = 4;
            //GlobalData.Settings.General.ExchangeId = 4;

            //CryptoTradeAccount account = new();
            //account.Name = "Fictief";
            //GlobalData.TradeAccountList.TryAdd(account.Id, account);

            /*
              {
                "Name": "BTCUSDT",
                "BaseAsset": "BTC",
                "QuoteAsset": "USDT",
                "Status": 1,
                "MarginTrading": 1,
                "Innovation": false,
                "LotSizeFilter": {
                  "BasePrecision": 0.000001,
                  "QuotePrecision": 0.00000001,
                  "MinOrderQuantity": 0.000048,
                  "MaxOrderQuantity": 71.73956243,
                  "MinOrderValue": 1,
                  "MaxOrderValue": 2000000
                },
                "PriceFilter": {
                  "TickSize": 0.01
                }
              },             
            */


            CryptoTradeAccount account = GlobalData.ExchangePaperTradeAccount;

            CryptoScanBot.Model.CryptoExchange exchange = GlobalData.Settings.General.Exchange;

            if (!exchange.SymbolListName.TryGetValue("IDUSDT", out CryptoSymbol symbol))
                return;
            //CryptoSymbol symbol = new()
            //{
            //    Id = -1,
            //    Status = 1,
            //    Name = "BTCUSDT",
            //    Base = "BTC",
            //    Quote = "USDT",
            //    Exchange = exchange,
            //    ExchangeId = exchange.Id,
            //    PriceTickSize = 0.01000000m,
            //    PriceMinimum = 0.01000000m,
            //    PriceMaximum = 1000000m,

            //    QuantityTickSize = 0.00001000m,
            //    QuantityMinimum = 0.00001000m,
            //    QuantityMaximum = 9000.00000000m
            //};
            //GlobalData.AddSymbol(symbol);



            // Werkt zoals ik het verwacht! een buy order van ongeveer 1.6 dollar
            //var exchangeApi = ExchangeHelper.GetExchangeInstance(GlobalData.Settings.General.ExchangeId);
            //var (result, tradeParams) = await exchangeApi.BuyOrSell(null, GlobalData.ExchangeRealTradeAccount, symbol, DateTime.Now, 
            //    CryptoOrderType.Limit, CryptoOrderSide.Buy, 52, 0.2276m, null, null);

            //var (result, tradeParams) = await api.BuyOrSell(Database, GlobalData.ExchangeRealTradeAccount, symbol, DateTime.Now,
            //    CryptoOrderType.Limit, CryptoOrderSide.Buy, 0.000048m, 25000m, null, null);

            //GlobalData.AddTextToLogTab($"{symbol.Name} {result} {tradeParams.Error}");

            //2023-09-06 16:48:22 BTCUSDT False 170140: Order value exceeded lower limit. (2*0.000048m)
            //2023-09-06 16:49:00 BTCUSDT False 170140: Order value exceeded lower limit. (1*0.000048m)



            ////todo: GetLinearInverseSymbolsAsync, category.sport
            //var symbolData = await client.V5Api.ExchangeData.GetSpotSymbolsAsync();
            //text = JsonSerializer.Serialize(symbolData.Data, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
            //File.WriteAllText("E:\\ByBit\\SymbolV5Spot.json", text);

            //// 'Invalid category; should be Linear or Inverse'
            ////var v5spotSymbols = await client.V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Spot);
            ////text = JsonSerializer.Serialize(v5spotSymbols, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
            ////File.WriteAllText("E:\\ByBit\\symbolV5Spot.json", text);

            //var v5InverseSymbols = await client.V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Inverse);
            //text = JsonSerializer.Serialize(v5InverseSymbols.Data, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
            //File.WriteAllText("E:\\ByBit\\symbolV5Inverse.json", text);

            ////'Invalid category; should be Linear or Inverse'
            ////var v5OptionSymbols = await client.V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Option);
            ////text = JsonSerializer.Serialize(v5OptionSymbols, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
            ////File.WriteAllText("E:\\ByBit\\symbolV5Option.json", text);

            //var v5LinearSymbols = await client.V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear);
            //text = JsonSerializer.Serialize(v5LinearSymbols.Data, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
            //File.WriteAllText("E:\\ByBit\\symbolV5Linear.json", text);

            //// Crap, dat is 200 candles per keer, dat duurt EINDELOOS!
            //DateTime dateStart = DateTime.Now.AddDays(-3);
            //var kLineData = await client.V5Api.ExchangeData.GetKlinesAsync(Category.Spot, "BTCUSDT", Bybit.Net.Enums.KlineInterval.FiveMinutes, dateStart, null, 1000);
            //text = JsonSerializer.Serialize(kLineData, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
            //File.WriteAllText("E:\\ByBit\\kLineData.json", text);


            //// Experiment (hier komt de volume wel mee, maar het is wel een extra call tov Binance):
            ////https://api-testnet.bybit.com/v5/market/tickers?category=spot&symbol=BTCUSDT
            //var v5SpotTickersAsync = await client.V5Api.ExchangeData.GetSpotTickersAsync();
            //text = JsonSerializer.Serialize(v5SpotTickersAsync.Data, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
            //File.WriteAllText("E:\\ByBit\\v5SpotTickersAsync.json", text);


            //var v5LinearInverseTickers = await client.V5Api.ExchangeData.GetLinearInverseTickersAsync(Category.Inverse);
            //text = JsonSerializer.Serialize(v5LinearInverseTickers.Data, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
            //File.WriteAllText("E:\\ByBit\\v5LinearInverseTickers.json", text);


            ////var v5OptionTickers = await client.V5Api.ExchangeData.GetOptionTickersAsync();
            ////text = JsonSerializer.Serialize(v5OptionTickers.Data, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
            ////File.WriteAllText("E:\\ByBit\\v5OptionTickers.json", text);


            //List<string> symbols = new();
            //symbols.Add("ETHBTC");
            //symbols.Add("BTCUSDT");
            //symbols.Add("ETHUSDT");
            //symbols.Add("ADAUSDT");
            //symbols.Add("XRPUSDT");
            //symbols.Add("PENDLEUSDT");
            //symbols.Add("XRPUSDT");
            //symbols.Add("EOSUSDT");
            //symbols.Add("XRPBTC");
            //symbols.Add("DOTUSDT");

            //symbols.Add("XLMUSDT");
            //symbols.Add("LTCUSDT");
            //symbols.Add("DATABTC");
            //symbols.Add("KNCBTC");



            //// En dan door x tasks de queue leeg laten trekken
            //List<Task> taskList = new();
            //while (taskList.Count < 10)
            //{
            //    Task task = Task.Run(async () =>
            //    {
            //        BybitSocketClient socketClient = new();
            //        CallResult<UpdateSubscription> subscriptionResult2 = await socketClient.V5SpotApi.SubscribeToTickerUpdatesAsync(symbols, data =>
            //        {
            //            //if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
            //            //{
            //            var tick = data.Data;
            //            //foreach (var tick in data.Data)
            //            //{
            //            //tickerCount++;

            //            //if (exchange.SymbolListName.TryGetValue(data.Topic, out CryptoSymbol symbol))
            //            //{
            //            // Waarschijnlijk ALLEMAAL gebaseerd op de 24h prijs
            //            //symbol.OpenPrice = tick.OpenPrice;
            //            //symbol.HighPrice = tick.HighPrice24h;
            //            //symbol.LowPrice = tick.LowPrice24h;
            //            //symbol.LastPrice = tick.LastPrice;
            //            //symbol.BidPrice = tick.BestBidPrice;
            //            //symbol.AskPrice = tick.BestAskPrice;
            //            //symbol.Volume = tick.BaseVolume; //?
            //            //symbol.Volume = tick.Volume24h; //= Quoted = het volume * de prijs                                

            //            //Invoke((MethodInvoker)(() => textBox1.AppendText(data.Topic +
            //            //    " lastprice=" + tick.LastPrice.ToString() + ", " +
            //            //    //                    " nidprice=" + tick.bes.ToString() + ", " +
            //            //    " volume24h=" + tick.Volume24h.ToString()

            //            //    + "\r\n")));
            //            //}
            //            //}


            //            //}
            //        });
            //        if (subscriptionResult2.Success)
            //        {
            //            //Invoke((MethodInvoker)(() => textBox1.AppendText("Succes! \r\n")));
            //        }
            //        else
            //            Invoke((MethodInvoker)(() => textBox1.AppendText(subscriptionResult2.Error?.Message + "\r\n")));

            //    });
            //    taskList.Add(task);
            //}
            //Task t = Task.WhenAll(taskList);
            //t.Wait();

            // Werkt zoals ik het verwacht! een buy order van ongeveer 1.6 dollar
            var exchangeApi = ExchangeHelper.GetExchangeInstance(GlobalData.Settings.General.ExchangeId);
            var (result, tradeParams) = await exchangeApi.PlaceOrder(null, GlobalData.ExchangeRealTradeAccount, symbol, CryptoTradeSide.Long, DateTime.Now, 
                CryptoOrderType.Limit, CryptoOrderSide.Buy, 52, 0.2276m, null, null);


            // BTC asset
            if (!account.AssetList.TryGetValue("BTC", out CryptoAsset assetBtc))
            {
                assetBtc = new CryptoAsset()
                {
                    Name = "BTC",
                    TradeAccountId = account.Id,
                };
                account.AssetList.Add(assetBtc.Name, assetBtc);
            }
            assetBtc.Total = 10;

            // USDT asset
            if (!account.AssetList.TryGetValue("USDT", out CryptoAsset assetUsdt))
            {
                assetUsdt = new CryptoAsset()
                {
                    Name = "USDT",
                    TradeAccountId = account.Id,
                };
                account.AssetList.Add(assetUsdt.Name, assetUsdt);
            }
            assetUsdt.Total = 100000;


            // Initieel
            PaperAssets.Change(account, symbol, CryptoOrderSide.Buy, CryptoOrderStatus.New, 1, 10);
            // Wordt gevuld
            PaperAssets.Change(account, symbol, CryptoOrderSide.Buy, CryptoOrderStatus.Filled, 1, 10);

            // Initieel
            PaperAssets.Change(account, symbol, CryptoOrderSide.Sell, CryptoOrderStatus.New, 1, 10);
            // Wordt gevuld
            PaperAssets.Change(account, symbol, CryptoOrderSide.Sell, CryptoOrderStatus.Filled, 1, 10.05m);



            GlobalData.AddTextToLogTab($"Balance: {account.Name}");
            await ExchangeApi.GetAssetsForAccountAsync(account);

            foreach (var asset in account.AssetList.Values)
            {
                GlobalData.AddTextToLogTab($"Quote={asset.Name} Total={asset.Total} Free={asset.Free} Locked={asset.Locked}");
            }


            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab($"Trades: {symbol.Name}");
            //await ExchangeApi.FetchTradesForSymbolAsync(account, symbol);

            //foreach (var trade in symbol.TradeList.Values)
            //{
            //    GlobalData.AddTextToLogTab($"Quote={trade.Symbol.Name} price={trade.Price} Quantity={trade.Quantity} Value={trade.QuoteQuantity}");
            //}
        }

        catch (Exception error)
        {
            Invoke((MethodInvoker)(() => textBox1.AppendText(error.Message + "\r\n")));
            throw;
        }

        Invoke((MethodInvoker)(() => textBox1.AppendText("ready" + "\r\n")));
    }




    ////private async Task BinanceTestAsync()
    ////{
    ////    // TM account, IP protected
    ////    string api = "nlKFRX8wmxRsu8qNST5oTfrW3tg9JSOKsY0O14VwqPDnhDVAuu7ix5VgFM5ROgF0";
    ////    string key = "C4NPbofOp4x7xJFa4UDLCQrGkEAfIFOv3psOlX4xE3vponxmaWjcQ5Jj0KHkxn9Z";

    ////    var binanceClient = new BinanceClient(new BinanceClientOptions
    ////    {
    ////        ApiCredentials = new BinanceApiCredentials(api, key),
    ////        SpotApiOptions = new BinanceApiClientOptions
    ////        {
    ////            BaseAddress = BinanceApiAddresses.Default.RestClientAddress,
    ////            //AutoReconnect = true,
    ////            AutoTimestamp = true
    ////        }
    ////    });

    ////    //BinanceClient.Api SetDefaultOptions(new BinanceClientOptions() { });

    ////    BinanceSocketClientOptions options = new();
    ////    options.ApiCredentials = new BinanceApiCredentials(api, key);
    ////    options.SpotStreamsOptions.AutoReconnect = true;
    ////    options.SpotStreamsOptions.ReconnectInterval = TimeSpan.FromSeconds(15);
    ////    BinanceSocketClient.SetDefaultOptions(options);

    ////    string text2;
    ////    string symbol = "BTCUSDT";
    ////    decimal quantity = 0.00041m;
    ////    (bool result, TradeParams? tradeParams) result;

    ////    result = await BuyOrSell(symbol, CryptoOrderType.Limit, CryptoOrderSide.Buy, quantity, 29500m, null, null, "eerste limit buy");
    ////    text2 = string.Format("{0} POSITION {1} {2} ORDER #{3} PLACED price={4} stop={5} quantity={6} quotequantity={7}",
    ////        symbol, CryptoOrderSide.Buy,
    ////        result.tradeParams?.OrderType.ToString(),
    ////        result.tradeParams?.OrderId,
    ////        result.tradeParams?.Price.ToString(),
    ////        result.tradeParams?.StopPrice?.ToString(),
    ////        result.tradeParams?.Quantity.ToString(),
    ////        result.tradeParams?.QuoteQuantity.ToString());
    ////    Invoke((MethodInvoker)(() => textBox1.AppendText(text2 + "\r\n")));

    ////    result = await BuyOrSell(symbol, CryptoOrderType.Limit, CryptoOrderSide.Sell, quantity, 32000m, null, null, "eerste limit sell");
    ////    text2 = string.Format("{0} POSITION {1} {2} ORDER #{3} PLACED price={4} stop={5} quantity={6} quotequantity={7}",
    ////        symbol, CryptoOrderSide.Buy,
    ////        result.tradeParams?.OrderType.ToString(),
    ////        result.tradeParams?.OrderId,
    ////        result.tradeParams?.Price.ToString(),
    ////        result.tradeParams?.StopPrice?.ToString(),
    ////        result.tradeParams?.Quantity.ToString(),
    ////        result.tradeParams?.QuoteQuantity.ToString());
    ////    Invoke((MethodInvoker)(() => textBox1.AppendText(text2 + "\r\n")));


    ////    result = await BuyOrSell(symbol, CryptoOrderType.StopLimit, CryptoOrderSide.Buy, quantity, 32000m, 31500m, null, "eerste stop limit buy");
    ////    text2 = string.Format("{0} POSITION {1} {2} ORDER #{3} PLACED price={4} stop={5} quantity={6} quotequantity={7}",
    ////        symbol, CryptoOrderSide.Sell,
    ////        result.tradeParams?.OrderType.ToString(),
    ////        result.tradeParams?.OrderId,
    ////        result.tradeParams?.Price.ToString(),
    ////        result.tradeParams?.StopPrice?.ToString(),
    ////        result.tradeParams?.Quantity.ToString(),
    ////        result.tradeParams?.QuoteQuantity.ToString());
    ////    Invoke((MethodInvoker)(() => textBox1.AppendText(text2 + "\r\n")));

    ////    result = await BuyOrSell(symbol, CryptoOrderType.StopLimit, CryptoOrderSide.Sell, quantity, 28000m, 28500m, null, "eerste stop limit sell");
    ////    text2 = string.Format("{0} POSITION {1} {2} ORDER #{3} PLACED price={4} stop={5} quantity={6} quotequantity={7}",
    ////        symbol, CryptoOrderSide.Sell,
    ////        result.tradeParams?.OrderType.ToString(),
    ////        result.tradeParams?.OrderId,
    ////        result.tradeParams?.Price.ToString(),
    ////        result.tradeParams?.StopPrice?.ToString(),
    ////        result.tradeParams?.Quantity.ToString(),
    ////        result.tradeParams?.QuoteQuantity.ToString());
    ////    Invoke((MethodInvoker)(() => textBox1.AppendText(text2+"\r\n")));


    ////    result = await BuyOrSell(symbol, CryptoOrderType.Oco, CryptoOrderSide.Buy, quantity, 28500, 32000m, 32500m, "eerste OCO buy");
    ////    text2 = string.Format("{0} POSITION {1} {2} ORDER #{3} PLACED price={4} stop={5} quantity={6} quotequantity={7}",
    ////        symbol, CryptoOrderSide.Sell,
    ////        result.tradeParams?.OrderType.ToString(),
    ////        result.tradeParams?.OrderId,
    ////        result.tradeParams?.Price.ToString(),
    ////        result.tradeParams?.StopPrice?.ToString(),
    ////        result.tradeParams?.Quantity.ToString(),
    ////        result.tradeParams?.QuoteQuantity.ToString());
    ////    Invoke((MethodInvoker)(() => textBox1.AppendText(text2 + "\r\n")));


    ////    result = await BuyOrSell(symbol, CryptoOrderType.Oco, CryptoOrderSide.Sell, quantity, 32500, 28500m, 28000m, "eerste OCO sell");
    ////    text2 = string.Format("{0} POSITION {1} {2} ORDER #{3} PLACED price={4} stop={5} quantity={6} quotequantity={7}",
    ////        symbol, CryptoOrderSide.Sell,
    ////        result.tradeParams?.OrderType.ToString(),
    ////        result.tradeParams?.OrderId,
    ////        result.tradeParams?.Price.ToString(),
    ////        result.tradeParams?.StopPrice?.ToString(),
    ////        result.tradeParams?.Quantity.ToString(),
    ////        result.tradeParams?.QuoteQuantity.ToString());
    ////    Invoke((MethodInvoker)(() => textBox1.AppendText(text2 + "\r\n")));
    ////}


    // testje
    internal static TraceLoggerProvider TraceProvider;
    internal static LoggerFactory LogFactory;
    //internal static ILoggerFactory factory = null;

    internal static BybitRestClient CreateRestClient()
    {
        // Ik snap er helemaal niets van.. Heb een paar classes verkeerd begrepen log en logger

        //NLog.Extensions.Logging.NLogLoggerFactory loggerFactory = new();
        //MyClass myClass = new(loggerFactory);
        //loggerFactory.
        //LoadNLogConfigurationOnFactory(loggerFactory);
        //using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddNLog());
        //Microsoft.Extensions.Logging.ILogger logger = factory.CreateLogger("Program");
        //logger.LogInformation("Hello World! Logging is {Description}.", "fun");

        //var loggerFactory = new NLog.Extensions.Logging.NLogLoggerFactory();

        //Logger moduleLogger = NLog.LogManager.GetLogger("Modules.MyModuleName");
        //NLog.LogFactory Factory1 = new LogFactory();
        //LoadNLogConfigurationOnFactory(Factory1);


        BybitRestClient client = new(null, LogFactory, options =>
        {
            //options.Environment = _environment;
            options.OutputOriginalData = true;
            options.ReceiveWindow = TimeSpan.FromSeconds(15);
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });
        return client;
    }


    private async void ByBitSpotTestAsync()
    {
        GlobalData.LoadSettings();

        BybitRestClient.SetDefaultOptions(options =>
        {
            options.OutputOriginalData = true;
            options.SpotOptions.AutoTimestamp = true;
            options.ReceiveWindow = TimeSpan.FromSeconds(15);
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        BybitSocketClient.SetDefaultOptions(options =>
        {
            options.AutoReconnect = true;
            options.OutputOriginalData = true;
            options.ReconnectInterval = TimeSpan.FromSeconds(15);
            if (GlobalData.TradingApi.Key != "")
                options.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
        });

        //ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddNLog());
        string text;
        TraceProvider = new();
        LogFactory = new(new[] { TraceProvider });


        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out CryptoScanBot.Model.CryptoExchange exchange))
        {
            if (exchange.SymbolListName.TryGetValue("FETUSDT", out CryptoSymbol symbol))
            {
                BybitRestClient client = CreateRestClient();
                //client.ClientOptions.OutputOriginalData = true;
                //client.ClientOptions.ApiCredentials = new ApiCredentials(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret);
                try
                {
                    //    DateTime startDate = DateTime.UtcNow.AddDays(-1);

                    //    // Experiment bybit spot, even quick en dirty om te zien of dit echt werkt...
                    //    // Controleer de order, als het de status "PartiallyFilledCanceled" heeft dan bijstellen
                    //    // Idee achter de boekhouding, de status van de order bepaald of het gesloten is
                    //    // de trades gebruiken we dan tzt enkel om de fee te bepalen (.... idee he......)
                    //    // Het zou ook geheel zonder de trades kunnen lijkt me, maar dan weet ik niet hoe we met de commissie omgaan
                    //    var orderInfo = await client.V5Api.Trading.GetOrderHistoryAsync(
                    //        Category.Spot, 
                    //        symbol: symbol.Name,
                    //        //startTime: startDate
                    //        orderId: "1636499753797799680"
                    //    );

                    //    if (orderInfo.Success && orderInfo.Data != null)
                    //    {
                    //        foreach (var order in orderInfo.Data.List)
                    //        {
                    //            text = JsonSerializer.Serialize(order, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
                    //            System.Diagnostics.Debug.WriteLine(text);
                    //            GlobalData.AddTextToLogTab(text);
                    //        }
                    //    }

                    //text = "Output client.V5Api.Trading.GetUserTradesAsync";
                    //System.Diagnostics.Debug.WriteLine(text);
                    //GlobalData.AddTextToLogTab(text);
                    //var resultV5 = await client.V5Api.Trading.GetUserTradesAsync(Category.Spot, symbol.Name, orderId: "1636499753797799680");
                    //text = JsonSerializer.Serialize(resultV5, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
                    //System.Diagnostics.Debug.WriteLine(text);
                    //GlobalData.AddTextToLogTab(text);


                    // , fromId: 1636499753797799679, toId: 1636499753797799681
                    text = "client.SpotApiV3.Trading.GetUserTradesAsync";
                    System.Diagnostics.Debug.WriteLine(text);
                    var resultV3 = await client.SpotApiV3.Trading.GetUserTradesAsync(symbol.Name, fromId: 1636499753898383104);
                    text = JsonSerializer.Serialize(resultV3, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true });
                    System.Diagnostics.Debug.WriteLine(text);
                    GlobalData.AddTextToLogTab(text);

                    if (!resultV3.Success)
                    {
                        GlobalData.AddTextToLogTab("error getting mytrades " + resultV3.Error);
                    }

                }
                catch (Exception error)
                {
                    ScannerLog.Logger.Error(error, "");
                    GlobalData.AddTextToLogTab("error get prices " + error.ToString()); // symbol.Text + " " + 
                }


            }
        }
    }
}