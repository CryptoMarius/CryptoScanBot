using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot.Socket;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.Binance.Perpetual;

public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : Subscription(exchangeOptions)
{
    private async Task ProcessCandleAsync(BinanceStreamKlineData kline)
    {
        if (SymbolByExchangeName.TryGetValue(kline.Symbol, out CryptoSymbol? symbol))
        {
            IncrementTickerCount();
            //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", temp.ScannerSymbol, temp.Data.OpenTime.ToLocalTime()));
            //string json = JsonSerializer.Serialize(kline, ExchangeHelper.JsonSerializerNotIndented);
            //ScannerLog.Logger.Trace($"kline ticker {symbol.ExchangeSymbol} {json}");
            var candle = await CandleTools.Process1mCandleAsync(symbol, kline.Data.OpenTime,
                kline.Data.OpenPrice, kline.Data.HighPrice, kline.Data.LowPrice, kline.Data.ClosePrice,
                kline.Data.QuoteVolume);
            GlobalData.ThreadMonitorCandle!.AddToQueue(symbol, candle);
        }
    }


    public override async Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        SubscriptionBundle!.SocketClient ??= new BinanceSocketClient();
        WebSocketResult<UpdateSubscription> subscriptionResult = await ((BinanceSocketClient)SubscriptionBundle.SocketClient).UsdFuturesApi.ExchangeData.
            SubscribeToKlineUpdatesAsync(
            ExchangeNames, KlineInterval.OneMinute, (data) =>
        {
            if (data.Data.Data.Final)
            {
                Task.Run(async () => { await ProcessCandleAsync((BinanceStreamKlineData)data.Data); });
            }
        }, false, ct: ExchangeBase.CancellationToken).ConfigureAwait(false);

        // Premium: When it's omitted, null or false  it will be the old behavior.
        // Setting it to true will subscribe to the premium index klines
        // for a symbol instead of the price data of a symbol.
        // For reference: https://whaleportal.com/learn/premium-index/



        return subscriptionResult;
    }

}
