using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanBot.Intern;
using CryptoScanBot.Model;

using Kraken.Net.Clients;
using Kraken.Net.Enums;
using Kraken.Net.Objects.Models;

namespace CryptoScanBot.Exchange.Kraken;

public class TickerKLineItem() : TickerItem(Api.ExchangeOptions)
{
    private void ProcessCandle(string topic, KrakenStreamKline kline)
    {
        // Aantekeningen
        // De Base volume is the volume in terms of the first currency pair.
        // De Quote volume is the volume in terms of the second currency pair.
        // For example, for "MFN/USDT": 
        // base volume would be MFN
        // quote volume would be USDT

        // De interval wordt geprefixed in de topic
        string symbolName = topic[2..];
        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeOptions.ExchangeName, out Model.CryptoExchange exchange))
        {
            if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol symbol))
            {
                Interlocked.Increment(ref TickerCount);
                //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", topic, kline.Timestamp.ToLocalTime()));
                CandleTools.Process1mCandle(symbol, kline.OpenTime, kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.Volume);
            }
        }

    }

    public override async Task<CallResult<UpdateSubscription>> Subscribe()
    {
        if (TickerGroup.SocketClient == null)
            TickerGroup.SocketClient = new KrakenSocketClient();
        var subscriptionResult = await ((KrakenSocketClient)TickerGroup.SocketClient).SpotApi.SubscribeToKlineUpdatesAsync(
            Symbols, KlineInterval.OneMinute, data =>
        {
            //foreach (KrakenStreamKline kline in data.Data)
            //{
            //    Task.Run(() => { ProcessCandle(data.Topic, kline); });
            //}
        }, ExchangeHelper.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}
