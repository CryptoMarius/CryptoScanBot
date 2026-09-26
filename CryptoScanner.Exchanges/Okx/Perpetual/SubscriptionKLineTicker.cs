using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using OKX.Net.Clients;
using OKX.Net.Enums;
using OKX.Net.Objects.Market;

namespace CryptoScanner.Core.Exchange.Okx.Perpetual;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door de exchange)
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : Subscription(exchangeOptions)
{
    private async Task ProcessCandleAsync(string? symbolName, OKXKline kline)
    {
        // Aantekeningen
        // De Base volume is the volume in terms of the first currency pair.
        // De Quote volume is the volume in terms of the second currency pair.
        // For example, for "MFN/USDT": 
        // base volume would be MFN
        // quote volume would be USDT

        //ScannerLog.Logger.Trace($"kline ticker {topic}");

        // De interval wordt geprefixed in de topic "kline.1.SymbolName"
        if (string.IsNullOrEmpty(symbolName))
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

                var candle = await CandleTools.Process1mCandleAsync(symbol, kline.Time,
                    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice,
                    kline.VolumeCurrencyQuote);
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
        var client = SubscriptionBundle!.GetOrCreateSocketClient(() => new OKXSocketClient());
        var api = client.UnifiedApi;

        // OKX expects the hyphenated instrument id (for example "BASED-USDT-SWAP"), not the scanner name ("BASEDUSDT").
        // Use ExchangeName so it matches both the REST candle fetch and the SymbolByExchangeName lookup below.
        // Pass the names as a list: the overload taking a single string treats a comma separated text as one
        // instrument id, which silently breaks as soon as a subscription serves more than one symbol.
        var subscriptionResult = await api.ExchangeData.SubscribeToKlineUpdatesAsync(SymbolList.Select(s => s.ExchangeName).ToList(), KlineInterval.OneMinute, data =>
        {
            OKXKline kline = data.Data;
            {
                if (kline.Confirm) // It is a final candle
                    Task.Run(async () => { await ProcessCandleAsync(data.Symbol, kline); });
            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}
