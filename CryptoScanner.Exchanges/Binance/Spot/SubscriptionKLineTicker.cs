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


    public override async Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        SubscriptionBundle!.SocketClient ??= new BinanceSocketClient();
        WebSocketResult<UpdateSubscription> subscriptionResult = await ((BinanceSocketClient)SubscriptionBundle.SocketClient).SpotApi.ExchangeData.SubscribeToKlineUpdatesAsync(
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
