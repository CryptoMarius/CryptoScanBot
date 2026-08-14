using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.BybitEu.Futures;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door Binance)
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

        if (SymbolByExchangeName.TryGetValue(symbolName, out CryptoSymbol? symbol))
        {
            IncrementTickerCount();
            //ScannerLog.Logger.Trace($"kline ticker {topic} process");
            //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", topic, kline.Timestamp.ToLocalTime()));
            var candle = await CandleTools.Process1mCandleAsync(symbol, kline.StartTime,
                kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice,
                kline.Turnover);
            GlobalData.ThreadMonitorCandle!.AddToQueue(symbol, candle);
        }

    }


    public override async Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        SubscriptionBundle!.SocketClient ??= new BybitSocketClient();
        var subscriptionResult = await ((BybitSocketClient)SubscriptionBundle.SocketClient).V5LinearApi.SubscribeToKlineUpdatesAsync(
            Symbols, KlineInterval.OneMinute, data =>
        {
            // Er zit tot ongeveer 8 a 10 seconden vertraging is van de exchange tot hier, dat moet ansich genoeg zijn
            //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} added for processing", data.Data.OpenTime.ToLocalTime(), data.ScannerSymbol));
            foreach (BybitKlineUpdate kline in data.Data)
            {
                if (kline.Confirm) // Het is een definitieve candle (niet eentje in opbouw)
                    _ = Task.Run(() => { _ = ProcessCandleAsync(data.Symbol, kline); });
            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}
