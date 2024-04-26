using Binance.Net.Clients;
using Binance.Net.Enums;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using Binance.Net.Interfaces;

namespace CryptoScanBot.Core.Exchange.BinanceSpot;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door Binance)
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    private void ProcessCandle(IBinanceStreamKlineData kline)
    {
        // Aantekeningen
        // De Base volume is the volume in terms of the first currency pair.
        // De Quote volume is the volume in terms of the second currency pair.
        // For example, for "MFN/USDT": 
        // base volume would be MFN
        // quote volume would be USDT

        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            if (exchange.SymbolListName.TryGetValue(kline.Symbol, out CryptoSymbol? symbol))
            {
                Interlocked.Increment(ref TickerCount);
                //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", temp.Symbol, temp.Data.OpenTime.ToLocalTime()));
                CandleTools.Process1mCandle(symbol, kline.Data.OpenTime, kline.Data.OpenPrice, kline.Data.HighPrice, kline.Data.LowPrice, kline.Data.ClosePrice, kline.Data.Volume);

            }
        }

    }

    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        TickerGroup!.SocketClient ??= new BinanceSocketClient();
        CallResult<UpdateSubscription> subscriptionResult = await ((BinanceSocketClient)TickerGroup.SocketClient).SpotApi.ExchangeData.SubscribeToKlineUpdatesAsync(
            Symbols, KlineInterval.OneMinute, (data) =>
        {
            if (data.Data.Data.Final)
            {
                Task.Run(() => { ProcessCandle(data.Data); });
            }
        }, ExchangeHelper.CancellationToken).ConfigureAwait(false);


        return subscriptionResult;
    }
}
