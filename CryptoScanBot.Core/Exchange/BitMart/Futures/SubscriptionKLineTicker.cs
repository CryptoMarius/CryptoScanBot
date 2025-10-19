using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

using BitMart.Net.Clients;
using BitMart.Net.Enums;
using BitMart.Net.Objects.Models;

namespace CryptoScanBot.Core.Exchange.BitMart.Futures;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door de exchange)
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    private async Task ProcessCandleAsync(string? symbolName, BitMartFuturesKlineItem kline)
    {
        //ScannerLog.Logger.Trace($"kline ticker {topic}");

        // De interval wordt geprefixed in de topic "kline.1.SymbolName"
        if (string.IsNullOrEmpty(symbolName))
            return;

        if (GlobalData.ExchangeListName.TryGetValue(ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            if (exchange.SymbolListExchangeName.TryGetValue(symbolName, out CryptoSymbol? symbol))
            {
                Interlocked.Increment(ref TickerCount);
                //ScannerLog.Logger.Trace($"kline ticker {topic} process");
                //GlobalData.AddTextToLogTab($"{topic} Candle {kline.Timestamp.ToLocalTime()} start processing");

                var candle = await CandleTools.Process1mCandleAsync(symbol, kline.Timestamp!.Value, 
                    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, 
                    kline.Volume, kline.Volume * 0.5m * (kline.HighPrice + kline.LowPrice));
                GlobalData.ThreadMonitorCandle!.AddToQueue(symbol, candle);
            }
        }

    }


    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        TickerGroup!.SocketClient ??= new BitMartSocketClient();
        var client = (BitMartSocketClient)TickerGroup.SocketClient;
        var api = client.UsdFuturesApi;

        // TODO: quick en dirty code hier, nog eens verbeteren
        // We verwachten (helaas) slechts 1 symbol per ticker
        List<string> symbols = [];
        foreach (var symbol in SymbolList)
        {
            symbols.Add(symbol.ExchangeName);
        }
        string symbolNames = string.Join(",", symbols);

        TickerGroup!.SocketClient ??= new BitMartSocketClient();
        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(symbolNames, FuturesStreamKlineInterval.OneMinute, data =>
        {
            //var klines = data.Data;
            {
                foreach (var kline in data.Data.Klines)
                Task.Run(async () => { await ProcessCandleAsync(data.Symbol, kline); });
            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}
