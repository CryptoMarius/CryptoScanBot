using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

using Coinbase.Net.Clients;
using Coinbase.Net.Objects.Models;
using CryptoExchange.Net.SharedApis;

namespace CryptoScanBot.Core.Exchange.Coinbase.Spot;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door de exchange)
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    private async Task ProcessCandleAsync(string? symbolName, CoinbaseStreamKline kline)
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
        symbolName = symbolName.Replace("-", "");

        if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
            {
                Interlocked.Increment(ref TickerCount);
                //ScannerLog.Logger.Trace($"kline ticker {topic} process");
                //GlobalData.AddTextToLogTab($"{symbolNames} Candle {kline.OpenTime.ToLocalTime()} start processing");

                var candle = await CandleTools.Process1mCandleAsync(symbol, kline.OpenTime, kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, kline.Volume, kline.Volume);
                GlobalData.ThreadMonitorCandle!.AddToQueue(symbol, candle);

                //if (GlobalData.Settings.General.DebugKLineReceive && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
                //    GlobalData.AddTextToLogTab($"Debug candle {symbol.Name} 1m {JsonSerializer.Serialize(kline, JsonTools.JsonSerializerNotIndented)}");
            }
        }

    }


    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        SemaphoreSlim symbolListSemaphore = new(1, 1);
        TickerGroup!.SocketClient ??= new CoinbaseSocketClient();
        var client = (CoinbaseSocketClient)TickerGroup.SocketClient;
        var api = client.AdvancedTradeApi;

        // TODO: quick en dirty code hier, nog eens verbeteren
        // We verwachten (helaas) slechts 1 symbol per ticker
        List<string> symbols = [];
        string symbolNames = "";
        foreach (var symbol in SymbolList)
        {
            string symbolName = api.FormatSymbol(symbol.Base, symbol.Quote, TradingMode.Spot);
            if (symbolNames == "")
                symbolNames = symbolName;
            else
                symbolNames += "," + symbolName;
            symbols.Add(symbolNames);
        }

        // WTF, dit is een kline die alleen de 5m ondersteund, wat moet je hier nu weer mee?


        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(symbolNames, data =>
        {
            //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} added for processing", data.Data.OpenTime.ToLocalTime(), data.Symbol));
            foreach (CoinbaseStreamKline kline in data.Data)
            {
                //if (data.Confirm) // Het is een definitieve candle (niet eentje in opbouw)
                Task.Run(async () => { await ProcessCandleAsync(data.Symbol, kline); });
            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}
