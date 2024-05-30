namespace ExchangeTest.Exchange.Bybit;

internal class FileName
{
    //private async Task ByBitTestAsync()
    //{
    //try
    //{
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


    //CryptoTradeAccount account = GlobalData.ExchangePaperTradeAccount;

    //CryptoScanBot.Core.Model.CryptoExchange exchange = GlobalData.Settings.General.Exchange;

    //if (!exchange.SymbolListName.TryGetValue("IDUSDT", out CryptoSymbol symbol))
    //return;
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
    //List<Task> taskList = [];
    //while (taskList.Count < 10)
    //{
    //    Task task = Task.Run(async () =>
    //    {
    //        BybitSocketClient socketClient = new();
    //        CallResult<UpdateSubscription> subscriptionResult2 = await socketClient.V5SpotApi.SubscribeToTickerUpdatesAsync(symbols, data =>
    //        {
    //            //if (GlobalData.ExchangeListName.TryGetValue(ExchangeOptions.ExchangeName, out Model.CryptoExchange exchange))
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
    //await Task.WhenAll(taskList).ConfigureAwait(false);

    // Werkt zoals ik het verwacht! een buy order van ongeveer 1.6 dollar
    //CryptoPosition position = null;
    //CryptoPositionPart part = null;
    //var exchangeApi = ExchangeHelper.GetExchangeInstance(GlobalData.Settings.General.ExchangeId);
    //var (result, tradeParams) = await exchangeApi.PlaceOrder(null, position, part, CryptoTradeSide.Long, DateTime.Now, 
    //    CryptoOrderType.Limit, CryptoOrderSide.Buy, 52, 0.2276m, null, null);


    // BTC asset
    //if (!account.AssetList.TryGetValue("BTC", out CryptoAsset assetBtc))
    //{
    //    assetBtc = new CryptoAsset()
    //    {
    //        Name = "BTC",
    //        TradeAccountId = account.Id,
    //    };
    //    account.AssetList.Add(assetBtc.Name, assetBtc);
    //}
    //assetBtc.Total = 10;

    //// USDT asset
    //if (!account.AssetList.TryGetValue("USDT", out CryptoAsset assetUsdt))
    //{
    //    assetUsdt = new CryptoAsset()
    //    {
    //        Name = "USDT",
    //        TradeAccountId = account.Id,
    //    };
    //    account.AssetList.Add(assetUsdt.Name, assetUsdt);
    //}
    //assetUsdt.Total = 100000;


    //        // Initieel
    //        PaperAssets.Change(account, symbol, CryptoOrderSide.Buy, CryptoOrderStatus.New, 1, 10);
    //        // Wordt gevuld
    //        PaperAssets.Change(account, symbol, CryptoOrderSide.Buy, CryptoOrderStatus.Filled, 1, 10);

    //        // Initieel
    //        PaperAssets.Change(account, symbol, CryptoOrderSide.Sell, CryptoOrderStatus.New, 1, 10);
    //        // Wordt gevuld
    //        PaperAssets.Change(account, symbol, CryptoOrderSide.Sell, CryptoOrderStatus.Filled, 1, 10.05m);



    //        GlobalData.AddTextToLogTab($"Balance: {account.Name}");
    //        await ExchangeApi.GetAssetsAsync(account);

    //        foreach (var asset in account.AssetList.Values)
    //        {
    //            GlobalData.AddTextToLogTab($"Quote={asset.Name} Total={asset.Total} Free={asset.Free} Locked={asset.Locked}");
    //        }


    //        GlobalData.AddTextToLogTab("");
    //        GlobalData.AddTextToLogTab($"Trades: {symbol.Name}");
    //        //await ExchangeApi.FetchTradesForSymbolAsync(account, symbol);

    //        //foreach (var trade in symbol.TradeList.Values)
    //        //{
    //        //    GlobalData.AddTextToLogTab($"Quote={trade.Symbol.Name} price={trade.Price} Quantity={trade.Quantity} Value={trade.QuoteQuantity}");
    //        //}
    //    }

    //    catch (Exception error)
    //    {
    //        Invoke((MethodInvoker)(() => textBox1.AppendText(error.Message + "\r\n")));
    //        throw;
    //    }

    //    Invoke((MethodInvoker)(() => textBox1.AppendText("ready" + "\r\n")));
    //}

}
