using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;

namespace CryptoScanBot.Core.Exchange.BybitSpot;

public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    private void ProcessCandle(string topic, BybitKlineUpdate kline)
    {
        // Aantekeningen
        // De Base volume is the volume in terms of the first currency pair.
        // De Quote volume is the volume in terms of the second currency pair.
        // For example, for "MFN/USDT": 
        // base volume would be MFN
        // quote volume would be USDT

        // De interval wordt geprefixed in de topic "kline.1.SymbolName"
        string symbolName = topic[8..];
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange exchange))
        {
            if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol symbol))
            {
                Interlocked.Increment(ref TickerCount);
                //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", topic, kline.Timestamp.ToLocalTime()));
                CandleTools.Process1mCandle(symbol, kline.StartTime, kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.Volume);
            }
        }

    }


    public override async Task<CallResult<UpdateSubscription>> Subscribe()
    {
        TickerGroup.SocketClient ??= new BybitSocketClient();
        var subscriptionResult = await ((BybitSocketClient)TickerGroup.SocketClient).V5SpotApi.SubscribeToKlineUpdatesAsync(
            Symbols, KlineInterval.OneMinute, data =>
        {
            //Er zit tot ongeveer 8 a 10 seconden vertraging is van de exchange tot hier, dat moet ansich genoeg zijn
            //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} added for processing", data.Data.OpenTime.ToLocalTime(), data.Symbol));
            foreach (BybitKlineUpdate kline in data.Data)
            {
                if (kline.Confirm) // Het is een definitieve candle (niet eentje in opbouw)
                    Task.Run(() => { ProcessCandle(data.Topic, kline); });
            }
        }, ExchangeHelper.CancellationToken).ConfigureAwait(false);


        return subscriptionResult;
    }

}
