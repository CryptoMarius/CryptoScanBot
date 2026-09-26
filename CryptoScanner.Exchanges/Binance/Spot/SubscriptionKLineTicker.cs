using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.Binance.Spot;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door Binance)
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : Subscription(exchangeOptions)
{
    private async Task ProcessCandleAsync(IBinanceStreamKlineData kline)
    {
        // Aantekeningen
        // De Base volume is the volume in terms of the first currency pair.
        // De Quote volume is the volume in terms of the second currency pair.
        // For example, for "MFN/USDT": 
        // base volume would be MFN
        // quote volume would be USDT

        // The callback hands this to Task.Run and never looks at the task again, so an exception
        // that escapes here disappears as an unobserved task exception: no log line, the candle is
        // gone and the ticker still looks healthy. Alpaca already caught it, the eight tickers with a definitive kline event did not.
        try
        {
            if (SymbolByExchangeName.TryGetValue(kline.Symbol, out CryptoSymbol? symbol))
            {
                IncrementTickerCount();
                //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", temp.ScannerSymbol, temp.Data.OpenTime.ToLocalTime()));
                var candle = await CandleTools.Process1mCandleAsync(symbol, kline.Data.OpenTime,
                    kline.Data.OpenPrice, kline.Data.HighPrice, kline.Data.LowPrice, kline.Data.ClosePrice,
                    kline.Data.QuoteVolume);
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
        var socketClient = SubscriptionBundle!.GetOrCreateSocketClient(() => new BinanceSocketClient());
        WebSocketResult<UpdateSubscription> subscriptionResult = await socketClient.SpotApi.ExchangeData.SubscribeToKlineUpdatesAsync(
            ExchangeNames, KlineInterval.OneMinute, (data) =>
        {
            if (data.Data.Data.Final)
            {
                Task.Run(async () => { await ProcessCandleAsync(data.Data); });
            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);


        return subscriptionResult;
    }

}
