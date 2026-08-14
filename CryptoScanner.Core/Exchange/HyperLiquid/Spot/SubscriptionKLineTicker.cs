using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using HyperLiquid.Net.Clients;
using HyperLiquid.Net.Enums;
using HyperLiquid.Net.Objects.Models;

namespace CryptoScanner.Core.Exchange.HyperLiquid.Spot;

public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions)
    : SubscriptionKLineCachedTicker(exchangeOptions)
{
    public override async Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        SubscriptionBundle!.SocketClient ??= new HyperLiquidSocketClient();
        var client = (HyperLiquidSocketClient)SubscriptionBundle.SocketClient;
        //SubscriptionBundle!.SocketClient.ClientOptions.OutputOriginalData = true;
        var api = client.SpotApi.ExchangeData;

        InitializeCache(SymbolList);

        // This stream produces a continuous stream of data (with incomplete candle, so we need a cache and timers)
        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(SymbolNamesAsCommaSeperatedString, KlineInterval.OneMinute, data =>
        {
            HyperLiquidKline kline = data.Data;
            //string json = JsonSerializer.Serialize(data.Data, JsonTools.JsonSerializerNotIndented);
            //GlobalData.AddTextToLogTab($"kline ticker {data.ScannerSymbol} {json}");

            // Handled synchronously (Wait, not WaitAsync/Task.Run), in the exact order the
            // socket delivers messages, so a burst of pushes for the same still-open candle
            // can never have an older message overwrite a newer one's OHLC.
            UpdateCacheFromKline(data.Symbol!, kline.OpenTime,
                open: kline.OpenPrice, high: kline.HighPrice, low: kline.LowPrice,
                close: kline.ClosePrice, volume: kline.Volume);
            //GlobalData.AddTextToLogTab($"kline received {candle.OhlcText(ScannerSymbol, interval, ScannerSymbol.PriceDisplayFormat, true, true)}");
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);


        if (subscriptionResult.Success)
            StartFlushTimer();

        return subscriptionResult;
    }
}
