using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;

using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange.Binance;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door Binance)
/// </summary>
public class KLineTickerItem(string apiExchangeName, CryptoQuoteData quoteData) : KLineTickerItemBase(apiExchangeName, quoteData)
{
    private void ProcessCandle(BinanceStreamKlineData kline)
    {
        // Aantekeningen
        // De Base volume is the volume in terms of the first currency pair.
        // De Quote volume is the volume in terms of the second currency pair.
        // For example, for "MFN/USDT": 
        // base volume would be MFN
        // quote volume would be USDT

        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
        {
            if (exchange.SymbolListName.TryGetValue(kline.Symbol, out CryptoSymbol symbol))
            {
                Interlocked.Increment(ref TickerCount);
                //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", temp.Symbol, temp.Data.OpenTime.ToLocalTime()));
                Process1mCandle(symbol, kline.Data.OpenTime, kline.Data.OpenPrice, kline.Data.HighPrice, kline.Data.LowPrice, kline.Data.ClosePrice, kline.Data.Volume);

            }
        }

    }


    public override async Task StartAsync()
    {
        if (_subscription != null)
        {
            ScannerLog.Logger.Trace($"kline ticker for group {GroupName} already started");
            return;
        }


        ConnectionLostCount = 0;
        ErrorDuringStartup = false;
        ScannerLog.Logger.Trace($"kline ticker for group {GroupName} starting");

        if (Symbols.Count > 0)
        {
            socketClient = new BinanceSocketClient();
            CallResult<UpdateSubscription> subscriptionResult = await ((BinanceSocketClient)socketClient).SpotApi.ExchangeData.SubscribeToKlineUpdatesAsync(
                Symbols, KlineInterval.OneMinute, (data) =>
            {
                if (data.Data.Data.Final)
                {
                    Task.Run(() => { ProcessCandle(data.Data as BinanceStreamKlineData); });
                }
            }, ExchangeHelper.CancellationToken).ConfigureAwait(false);


            if (subscriptionResult.Success)
            {
                _subscription = subscriptionResult.Data;
                _subscription.Exception += Exception;
                _subscription.ConnectionLost += ConnectionLost;
                _subscription.ConnectionRestored += ConnectionRestored;
                ScannerLog.Logger.Trace($"kline ticker for group {GroupName} started");
            }
            else
            {
                _subscription.Exception -= Exception;
                _subscription.ConnectionLost -= ConnectionLost;
                _subscription.ConnectionRestored -= ConnectionRestored;
                _subscription = null;

                socketClient.Dispose();
                socketClient = null;

                ConnectionLostCount++;
                ErrorDuringStartup = true;

                ScannerLog.Logger.Trace($"kline ticker for group {GroupName} error {subscriptionResult.Error.Message} {string.Join(',', Symbols)}");
                GlobalData.AddTextToLogTab($"kline ticker for group {GroupName} error {subscriptionResult.Error.Message} {string.Join(',', Symbols)}");
            }
        }
    }

}
