using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using Mexc.Net.Clients;
using Mexc.Net.Enums;
using Mexc.Net.Objects.Models.Futures;

namespace CryptoScanner.Core.Exchange.Mexc.Futures;

public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions)
    : SubscriptionKLineCachedTicker(exchangeOptions)
{
    public override async Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        SubscriptionBundle!.SocketClient ??= new MexcSocketClient();
        var client = (MexcSocketClient)SubscriptionBundle!.SocketClient;
        //SubscriptionBundle!.SocketClient.ClientOptions.OutputOriginalData = true;
        var api = client.FuturesApi;

        // Only one symbol per subscription: the library has no overload that takes a list for the
        // futures kline stream (the spot side does), hence SymbolLimitPerSubscription=1
        InitializeCache(SymbolList);

        // This stream produces a continuous stream of data (with incomplete candle, so we need a cache and timers)
        // Mexc wants its own contract names ("BTC_USDT"), not the scanner names ("BTCUSDT").
        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(SymbolNamesAsCommaSeperatedString, FuturesKlineInterval.OneMinute, data =>
        {
            MexcFuturesStreamKline kline = data.Data;
            //string json = JsonSerializer.Serialize(data.Data, JsonTools.JsonSerializerNotIndented);
            //GlobalData.AddTextToLogTab($"kline ticker {data.Symbol} {json}");

            // QuoteVolume ("q") is the turnover of the open candle and grows within the minute, which is
            // what the cache expects. Volume ("a") counts contracts, the same distinction the fetched
            // candles make - so history and live candles stay in the same unit.

            // Handled synchronously (Wait, not WaitAsync/Task.Run), in the exact order the
            // socket delivers messages, so a burst of pushes for the same still-open candle
            // can never have an older message overwrite a newer one's OHLC.
            UpdateCacheFromKline(data.Symbol!, kline.OpenTime,
                open: kline.OpenPrice, high: kline.HighPrice, low: kline.LowPrice,
                close: kline.ClosePrice, volume: kline.QuoteVolume);
            //GlobalData.AddTextToLogTab($"kline received {candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true)}");
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        // Kline timer implementation (fix)
        // Because a new candle is not always offered (like the "junk" coin TOMOUSDT) an additional
        // timer can be used that repeats the previous candle after all.
        // The idea is to do that every minute, 10 seconds after the normal kline event.

        if (subscriptionResult.Success)
            StartFlushTimer();

        return subscriptionResult;
    }
}
