using Coinbase.Net.Clients;
using Coinbase.Net.Objects.Models;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.Coinbase.Spot;

public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : Subscription(exchangeOptions)
{
    private async Task ProcessCandleAsync(string? symbolName, CoinbaseStreamKline kline)
    {
        // Interval is prefixed in the "kline.1.SymbolName"? wtf? copied comments probably..
        if (string.IsNullOrEmpty(symbolName))
            return;

        if (SymbolByExchangeName.TryGetValue(symbolName, out CryptoSymbol? symbol))
        {
            IncrementTickerCount();
            //ScannerLog.Logger.Trace($"kline ticker {topic} process");
            //GlobalData.AddTextToLogTab($"{symbolNames} Candle {kline.OpenTime.ToLocalTime()} start processing");

            var candle = await CandleTools.Process1mCandleAsync(symbol, kline.OpenTime,
                kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice,
                kline.Volume * 0.5m * (kline.HighPrice + kline.LowPrice));
            GlobalData.ThreadMonitorCandle!.AddToQueue(symbol, candle);
        }

    }


    public override async Task<WebSocketResult<UpdateSubscription>?> Subscribe()
    {
        SubscriptionBundle!.SocketClient ??= new CoinbaseSocketClient();
        var client = (CoinbaseSocketClient)SubscriptionBundle.SocketClient;
        var api = client.AdvancedTradeApi;

        //------------------------------------------------------------------------------
        // WTF, Subscribe to kline updates.
        // But the Klines are always at a 5 minute interval, that won't work
        //------------------------------------------------------------------------------

        // Coinbase wants its own symbol names ("BTC-USD"), not the scanner names ("BTCUSD"). The names
        // ProcessCandleAsync looks up come from the same dictionary, so both sides stay in step.
        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(SymbolByExchangeName.Keys.ToList(), data =>
        {
            //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} added for processing", data.Data.OpenTime.ToLocalTime(), data.ScannerSymbol));
            foreach (CoinbaseStreamKline kline in data.Data)
            {
                //if (data.Confirm) // Het is een definitieve candle (niet eentje in opbouw)
                Task.Run(async () => { await ProcessCandleAsync(data.Symbol, kline); });
            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}
