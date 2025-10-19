using CryptoExchange.Net.Clients;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.SharedApis;

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

using BloFin.Net.Clients;
using BloFin.Net.Enums;
using BloFin.Net.Objects.Models;

namespace CryptoScanBot.Core.Exchange.BloFin.Futures;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door de exchange)
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    private async Task ProcessCandleAsync(string? symbolName, BloFinKline kline)
    {
        //ScannerLog.Logger.Trace($"kline ticker {topic}");

        // De interval wordt geprefixed in de topic "kline.1.SymbolName"
        if (!kline.Finished || string.IsNullOrEmpty(symbolName))
            return;

        if (GlobalData.ExchangeListName.TryGetValue(ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            if (exchange.SymbolListExchangeName.TryGetValue(symbolName, out CryptoSymbol? symbol))
            {
                Interlocked.Increment(ref TickerCount);
                //ScannerLog.Logger.Trace($"kline ticker {topic} process");
                //GlobalData.AddTextToLogTab($"{topic} Candle {kline.Timestamp.ToLocalTime()} start processing");

                var candle = await CandleTools.Process1mCandleAsync(symbol, kline.OpenTime, 
                    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, 
                    kline.Volume, kline.QuoteVolume);
                GlobalData.ThreadMonitorCandle!.AddToQueue(symbol, candle);
            }
        }

    }


    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        TickerGroup!.SocketClient ??= new BloFinSocketClient();
        var client = (BloFinSocketClient)TickerGroup.SocketClient;
        var api = client.FuturesApi;

        // TODO: quick en dirty code hier, nog eens verbeteren
        // We verwachten (helaas) slechts 1 symbol per ticker
        List<string> symbols = [];
        foreach (var symbol in SymbolList)
        {
            symbols.Add(symbol.ExchangeName);
        }
        //string symbolNames = string.Join(",", symbols);

        TickerGroup!.SocketClient ??= new BloFinSocketClient();
        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(symbols, KlineInterval.OneMinute, data =>
        {
            var kline = data.Data;
            {
                Task.Run(async () => { await ProcessCandleAsync(data.Symbol, kline); });
            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}
