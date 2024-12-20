using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

using Kraken.Net.Clients;
using Kraken.Net.Enums;
using Kraken.Net.Objects.Models.Socket;

namespace CryptoScanBot.Core.Exchange.Kraken.Spot;

public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    private async Task ProcessCandleAsync(string topic, KrakenKlineUpdate kline)
    {
        // De interval wordt geprefixed in de topic
        string symbolName = topic.Replace("/", "");
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
            {
                Interlocked.Increment(ref TickerCount);
                //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", topic, kline.Timestamp.ToLocalTime()));
                var candle = await CandleTools.Process1mCandleAsync(symbol, kline.OpenTime, kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, 0, kline.Volume);
                GlobalData.ThreadMonitorCandle!.AddToQueue(symbol, candle);
            }
        }
    }

    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        List<string> symbolList = [];
        foreach (var symbol in SymbolList)
        {
            symbolList.Add(symbol.Base + "/" + symbol.Quote);
        }

        TickerGroup!.SocketClient ??= new KrakenSocketClient();
        var subscriptionResult = await ((KrakenSocketClient)TickerGroup.SocketClient).SpotApi.SubscribeToKlineUpdatesAsync(
            symbolList, KlineInterval.OneMinute, data =>
        {
            foreach (KrakenKlineUpdate kline in data.Data)
            {
                Task.Run(async () => { await ProcessCandleAsync(data.Symbol ?? "", kline); });
            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}
