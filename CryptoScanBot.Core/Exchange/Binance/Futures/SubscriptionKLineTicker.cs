using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.Binance.Futures;

public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    private async Task ProcessCandleAsync(BinanceStreamKlineData kline)
    {
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            if (exchange.SymbolListName.TryGetValue(kline.Symbol, out CryptoSymbol? symbol))
            {
                Interlocked.Increment(ref TickerCount);
                //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", temp.Symbol, temp.Data.OpenTime.ToLocalTime()));
                //string json = JsonSerializer.Serialize(kline, ExchangeHelper.JsonSerializerNotIndented);
                //ScannerLog.Logger.Trace($"kline ticker {symbol.Name} {json}");
                var candle = await CandleTools.Process1mCandleAsync(symbol, kline.Data.OpenTime, kline.Data.OpenPrice, kline.Data.HighPrice, kline.Data.LowPrice, kline.Data.ClosePrice, kline.Data.Volume, kline.Data.QuoteVolume);
                GlobalData.ThreadMonitorCandle!.AddToQueue(symbol, candle);
            }
        }
    }


    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        TickerGroup!.SocketClient ??= new BinanceSocketClient();
        CallResult<UpdateSubscription> subscriptionResult = await ((BinanceSocketClient)TickerGroup.SocketClient).UsdFuturesApi.ExchangeData.SubscribeToKlineUpdatesAsync(
            Symbols, KlineInterval.OneMinute, (data) =>
        {
            if (data.Data.Data.Final)
            {
                Task.Run(async () => { await ProcessCandleAsync((BinanceStreamKlineData)data.Data); });
            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);
        return subscriptionResult;
    }

}
