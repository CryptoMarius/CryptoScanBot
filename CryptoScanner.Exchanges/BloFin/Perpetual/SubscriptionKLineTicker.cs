using BloFin.Net.Clients;
using BloFin.Net.Enums;
using BloFin.Net.Objects.Models;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.BloFin.Perpetual;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door de exchange)
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : Subscription(exchangeOptions)
{
    private async Task ProcessCandleAsync(string? symbolName, BloFinKline kline)
    {
        //ScannerLog.Logger.Trace($"kline ticker {topic}");

        // De interval wordt geprefixed in de topic "kline.1.SymbolName"
        if (!kline.Finished || string.IsNullOrEmpty(symbolName))
            return;

        // The callback hands this to Task.Run and never looks at the task again, so an exception
        // that escapes here disappears as an unobserved task exception: no log line, the candle is
        // gone and the ticker still looks healthy. Alpaca already caught it, the eight tickers with a definitive kline event did not.
        try
        {
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
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddErrorToLogTab($"{ExchangeOptions.ExchangeName} kline ticker group {Name} error {error.Message}");
        }

    }


    public override async Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        // One client per bundle, even when two subscriptions of that bundle start in the same
        // moment; see SubscriptionBundle.GetOrCreateSocketClient.
        var client = SubscriptionBundle!.GetOrCreateSocketClient(() => new BloFinSocketClient());
        var api = client.FuturesApi;

        // Subscribe on the instrument id ("BTC-USDT"), not on the scanner name ("BTCUSDT").
        // BloFin does not know the scanner name, so the old subscription silently received nothing.
        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(SymbolList.Select(s => s.ExchangeName).ToList(),
            KlineInterval.OneMinute, data =>
        {
            var kline = data.Data;
            {
                // Filter before the Task.Run, like Binance, Bybit and Okx do. BloFin pushes an
                // update of the open candle per trade, so without this every trade of every coin
                // costs a threadpool work item that ProcessCandleAsync throws away on its first
                // line. The test in there stays: it is the one that says what this filter means.
                if (!kline.Finished)
                    return;

                Task.Run(async () => { await ProcessCandleAsync(data.Symbol, kline); });
            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}
