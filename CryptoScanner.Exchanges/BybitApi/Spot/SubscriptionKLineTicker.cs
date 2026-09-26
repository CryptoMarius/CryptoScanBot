using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.BybitApi.Spot;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door de exchange)
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : Subscription(exchangeOptions)
{
    private async Task ProcessCandleAsync(string? symbolName, BybitKlineUpdate kline)
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

                var candle = await CandleTools.Process1mCandleAsync(symbol, kline.StartTime,
                    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice,
                    kline.Turnover);
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
        var socketClient = SubscriptionBundle!.GetOrCreateSocketClient(() => new BybitSocketClient());
        var subscriptionResult = await socketClient.V5SpotApi.SubscribeToKlineUpdatesAsync(
            ExchangeNames, KlineInterval.OneMinute, data =>
        {
            // Er zit tot ongeveer 8 a 10 seconden vertraging is van de exchange tot hier, dat moet ansich genoeg zijn
            //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} added for processing", data.Data.OpenTime.ToLocalTime(), data.ScannerSymbol));
            foreach (BybitKlineUpdate kline in data.Data)
            {
                if (kline.Confirm) // Het is een definitieve candle (niet eentje in opbouw)
                    Task.Run(async () => { await ProcessCandleAsync(data.Symbol, kline); });
            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}
