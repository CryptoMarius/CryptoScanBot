namespace ExchangeTest.Exchange.Binance;

internal class FileName
{
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
    ////    text2 = string.Format("{0} POSITION {1} {2} ORDER {3} PLACED price={4} stop={5} quantity={6} quotequantity={7}",
    ////        symbol, CryptoOrderSide.Buy,
    ////        result.tradeParams?.OrderType.ToString(),
    ////        result.tradeParams?.OrderId,
    ////        result.tradeParams?.Price.ToString(),
    ////        result.tradeParams?.StopPrice?.ToString(),
    ////        result.tradeParams?.Quantity.ToString(),
    ////        result.tradeParams?.QuoteQuantity.ToString());
    ////    Invoke((MethodInvoker)(() => textBox1.AppendText(text2 + "\r\n")));

    ////    result = await BuyOrSell(symbol, CryptoOrderType.Limit, CryptoOrderSide.Sell, quantity, 32000m, null, null, "eerste limit sell");
    ////    text2 = string.Format("{0} POSITION {1} {2} ORDER {3} PLACED price={4} stop={5} quantity={6} quotequantity={7}",
    ////        symbol, CryptoOrderSide.Buy,
    ////        result.tradeParams?.OrderType.ToString(),
    ////        result.tradeParams?.OrderId,
    ////        result.tradeParams?.Price.ToString(),
    ////        result.tradeParams?.StopPrice?.ToString(),
    ////        result.tradeParams?.Quantity.ToString(),
    ////        result.tradeParams?.QuoteQuantity.ToString());
    ////    Invoke((MethodInvoker)(() => textBox1.AppendText(text2 + "\r\n")));


    ////    result = await BuyOrSell(symbol, CryptoOrderType.StopLimit, CryptoOrderSide.Buy, quantity, 32000m, 31500m, null, "eerste stop limit buy");
    ////    text2 = string.Format("{0} POSITION {1} {2} ORDER {3} PLACED price={4} stop={5} quantity={6} quotequantity={7}",
    ////        symbol, CryptoOrderSide.Sell,
    ////        result.tradeParams?.OrderType.ToString(),
    ////        result.tradeParams?.OrderId,
    ////        result.tradeParams?.Price.ToString(),
    ////        result.tradeParams?.StopPrice?.ToString(),
    ////        result.tradeParams?.Quantity.ToString(),
    ////        result.tradeParams?.QuoteQuantity.ToString());
    ////    Invoke((MethodInvoker)(() => textBox1.AppendText(text2 + "\r\n")));

    ////    result = await BuyOrSell(symbol, CryptoOrderType.StopLimit, CryptoOrderSide.Sell, quantity, 28000m, 28500m, null, "eerste stop limit sell");
    ////    text2 = string.Format("{0} POSITION {1} {2} ORDER {3} PLACED price={4} stop={5} quantity={6} quotequantity={7}",
    ////        symbol, CryptoOrderSide.Sell,
    ////        result.tradeParams?.OrderType.ToString(),
    ////        result.tradeParams?.OrderId,
    ////        result.tradeParams?.Price.ToString(),
    ////        result.tradeParams?.StopPrice?.ToString(),
    ////        result.tradeParams?.Quantity.ToString(),
    ////        result.tradeParams?.QuoteQuantity.ToString());
    ////    Invoke((MethodInvoker)(() => textBox1.AppendText(text2+"\r\n")));


    ////    result = await BuyOrSell(symbol, CryptoOrderType.Oco, CryptoOrderSide.Buy, quantity, 28500, 32000m, 32500m, "eerste OCO buy");
    ////    text2 = string.Format("{0} POSITION {1} {2} ORDER {3} PLACED price={4} stop={5} quantity={6} quotequantity={7}",
    ////        symbol, CryptoOrderSide.Sell,
    ////        result.tradeParams?.OrderType.ToString(),
    ////        result.tradeParams?.OrderId,
    ////        result.tradeParams?.Price.ToString(),
    ////        result.tradeParams?.StopPrice?.ToString(),
    ////        result.tradeParams?.Quantity.ToString(),
    ////        result.tradeParams?.QuoteQuantity.ToString());
    ////    Invoke((MethodInvoker)(() => textBox1.AppendText(text2 + "\r\n")));


    ////    result = await BuyOrSell(symbol, CryptoOrderType.Oco, CryptoOrderSide.Sell, quantity, 32500, 28500m, 28000m, "eerste OCO sell");
    ////    text2 = string.Format("{0} POSITION {1} {2} ORDER {3} PLACED price={4} stop={5} quantity={6} quotequantity={7}",
    ////        symbol, CryptoOrderSide.Sell,
    ////        result.tradeParams?.OrderType.ToString(),
    ////        result.tradeParams?.OrderId,
    ////        result.tradeParams?.Price.ToString(),
    ////        result.tradeParams?.StopPrice?.ToString(),
    ////        result.tradeParams?.Quantity.ToString(),
    ////        result.tradeParams?.QuoteQuantity.ToString());
    ////    Invoke((MethodInvoker)(() => textBox1.AppendText(text2 + "\r\n")));
    ////}
}
