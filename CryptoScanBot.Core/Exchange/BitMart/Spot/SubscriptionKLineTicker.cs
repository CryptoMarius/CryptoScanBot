using BitMart.Net.Clients;
using BitMart.Net.Enums;
using BitMart.Net.Objects.Models;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.SharedApis;

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;


namespace CryptoScanBot.Core.Exchange.BitMart.Spot;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door de exchange)
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    private async Task ProcessCandleAsync(string? symbolName, BitMartKline kline)
    {
        // Aantekeningen
        // De Base volume is the volume in terms of the first currency pair.
        // De Quote volume is the volume in terms of the second currency pair.
        // For example, for "MFN/USDT": 
        // base volume would be MFN
        // quote volume would be USDT

        //ScannerLog.Logger.Trace($"kline ticker {topic}");

        // De interval wordt geprefixed in de topic "kline.1.SymbolName"
        if (string.IsNullOrEmpty(symbolName))
            return;
        symbolName = symbolName.Replace("_", "");

        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
            {
                Interlocked.Increment(ref TickerCount);
                //ScannerLog.Logger.Trace($"kline ticker {topic} process");
                //GlobalData.AddTextToLogTab($"{topic} Candle {kline.Timestamp.ToLocalTime()} start processing");

                var candle = await CandleTools.Process1mCandleAsync(symbol, kline.OpenTime, 
                    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice,
                    kline.Volume, kline.Volume * 0.5m * (kline.HighPrice + kline.LowPrice));
                GlobalData.ThreadMonitorCandle!.AddToQueue(symbol, candle);
            }
        }

    }


    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        SemaphoreSlim symbolListSemaphore = new(1, 1);
        TickerGroup!.SocketClient ??= new BitMartSocketClient();
        var client = (BitMartSocketClient)TickerGroup.SocketClient;
        var api = client.SpotApi;

        // TODO: quick en dirty
        List<string> symbols = [];
        foreach (var symbol in SymbolList)
        {
            string symbolName = api.FormatSymbol(symbol.Base, symbol.Quote, TradingMode.Spot);
            symbols.Add(symbolName);
        }
        string symbolNames = string.Join(",", symbols);

        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(symbolNames, KlineStreamInterval.OneMinute, data =>
        {
            foreach (var kline in data.Data)
            {
                Task.Run(async () => { await ProcessCandleAsync(kline.Symbol, kline.Kline); });
            }

                
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}
