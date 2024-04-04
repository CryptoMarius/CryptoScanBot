using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;

using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;

using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange.BybitSpot;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door Binance)
/// </summary>
public class KLineTickerItem(string apiExchangeName, CryptoQuoteData quoteData) : KLineTickerItemBase(apiExchangeName, quoteData)
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
        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
        {
            if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol symbol))
            {
                Interlocked.Increment(ref TickerCount);
                //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", topic, kline.Timestamp.ToLocalTime()));
                Process1mCandle(symbol, kline.StartTime, kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.Volume);

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
            socketClient = new BybitSocketClient();
            var subscriptionResult = await ((BybitSocketClient)socketClient).V5SpotApi.SubscribeToKlineUpdatesAsync(
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
