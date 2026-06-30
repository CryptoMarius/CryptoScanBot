using BloFin.Net.Clients;
using BloFin.Net.Enums;
using BloFin.Net.Objects.Models;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.BloFin.Futures;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door de exchange)
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    private async Task ProcessCandleAsync(string? symbolName, BloFinKline kline)
    {
        //ScannerLog.Logger.Trace($"kline ticker {topic}");

        // De interval wordt geprefixed in de topic "kline.1.SymbolName"
        if (!kline.Finished || string.IsNullOrEmpty(symbolName))
            return;

        if (SymbolByExchangeName.TryGetValue(symbolName, out CryptoSymbol? symbol))
        {
            IncrementTickerCount();
            //ScannerLog.Logger.Trace($"kline ticker {topic} process");
            //GlobalData.AddTextToLogTab($"{topic} Candle {kline.Timestamp.ToLocalTime()} start processing");

            var candle = await CandleTools.Process1mCandleAsync(symbol, kline.OpenTime,
                kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice,
                kline.QuoteVolume);
            GlobalData.ThreadMonitorCandle!.AddToQueue(symbol, candle);
        }

    }


    public override async Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        TickerGroup!.SocketClient ??= new BloFinSocketClient();
        var client = (BloFinSocketClient)TickerGroup.SocketClient;
        var api = client.FuturesApi;

        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(Symbols, KlineInterval.OneMinute, data =>
        {
            var kline = data.Data;
            {
                Task.Run(async () => { await ProcessCandleAsync(data.Symbol, kline); });
            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}
