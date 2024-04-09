using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;

using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange.BinanceSpot;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door Binance)
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    private void ProcessCandle(BinanceStreamKlineData kline)
    {
        // Aantekeningen
        // De Base volume is the volume in terms of the first currency pair.
        // De Quote volume is the volume in terms of the second currency pair.
        // For example, for "MFN/USDT": 
        // base volume would be MFN
        // quote volume would be USDT

        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeOptions.ExchangeName, out Model.CryptoExchange exchange))
        {
            if (exchange.SymbolListName.TryGetValue(kline.Symbol, out CryptoSymbol symbol))
            {
                Interlocked.Increment(ref TickerCount);
                //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", temp.Symbol, temp.Data.OpenTime.ToLocalTime()));
                CandleTools.Process1mCandle(symbol, kline.Data.OpenTime, kline.Data.OpenPrice, kline.Data.HighPrice, kline.Data.LowPrice, kline.Data.ClosePrice, kline.Data.Volume);

            }
        }

    }


    public override async Task<CallResult<UpdateSubscription>> Subscribe()
    {
        TickerGroup.SocketClient ??= new BinanceSocketClient();
        CallResult<UpdateSubscription> subscriptionResult = await ((BinanceSocketClient)TickerGroup.SocketClient).SpotApi.ExchangeData.SubscribeToKlineUpdatesAsync(
            Symbols, KlineInterval.OneMinute, (data) =>
        {
            if (data.Data.Data.Final)
            {
                Task.Run(() => { ProcessCandle(data.Data as BinanceStreamKlineData); });
            }
        }, ExchangeHelper.CancellationToken).ConfigureAwait(false);


        return subscriptionResult;
    }

}
