using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Kraken.Net.Clients;
using Kraken.Net.Objects.Models.Socket;

namespace CryptoScanner.Core.Exchange.Kraken.Futures;

public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    private async Task ProcessCandleAsync(string topic, KrakenKlineUpdate kline)
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            if (exchange.SymbolListExchangeName.TryGetValue(topic, out CryptoSymbol? symbol))
            {
                Interlocked.Increment(ref TickerCount);
                //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", topic, kline.Timestamp.ToLocalTime()));
                var candle = await CandleTools.Process1mCandleAsync(symbol, 
                    kline.OpenTime, kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, 
                    kline.Volume);
                GlobalData.ThreadMonitorCandle!.AddToQueue(symbol, candle);
            }
        }
    }

    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        TickerGroup!.SocketClient ??= new KrakenSocketClient();
        var client = (KrakenSocketClient)TickerGroup.SocketClient;
        var api = client.FuturesApi;

        List<string> symbolList = [];
        foreach (var symbol in SymbolList)
        {
            symbolList.Add(symbol.ExchangeName);
        }

        // TODO: SubscribeToKlineUpdatesAsync does not exist?

        //var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(symbolList, KlineInterval.OneMinute, data =>
        //{
        //    foreach (KrakenKlineUpdate kline in data.Data)
        //    {
        //        Task.Run(async () => { await ProcessCandleAsync(data.ScannerSymbol ?? "", kline); });
        //    }
        //}, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return null;
        //return subscriptionResult;
    }

}
