using BloFin.Net.Clients;
using BloFin.Net.Enums;
using BloFin.Net.Objects.Models;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;


namespace CryptoScanner.Core.Exchange.BloFin.Spot;

/// <summary>
/// Monitoren van 1m candles (die gepushed worden door de exchange)
/// </summary>
public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : Subscription(exchangeOptions)
{
    // BloFin Spot uses api.FormatSymbol() for subscription names, which differ from ExchangeName.
    // The feed then strips underscores before matching — so we key this dict on the stripped name.
    private Dictionary<string, CryptoSymbol> _symbolByStrippedName = [];


    private async Task ProcessCandleAsync(string? symbolName, BloFinKline kline)
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

        if (_symbolByStrippedName.TryGetValue(symbolName, out CryptoSymbol? symbol))
        {
            IncrementTickerCount();
            //ScannerLog.Logger.Trace($"kline ticker {topic} process");
            //GlobalData.AddTextToLogTab($"{topic} Candle {kline.Timestamp.ToLocalTime()} start processing");

            var candle = await CandleTools.Process1mCandleAsync(symbol, kline.OpenTime,
                kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice,
                kline.Volume, kline.Volume * 0.5m * (kline.HighPrice + kline.LowPrice));
            GlobalData.ThreadMonitorCandle!.AddToQueue(symbol, candle);

            //if (GlobalData.Settings.General.DebugKLineReceive && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
            //    GlobalData.AddTextToLogTab($"Debug candle {symbol.Name} 1m {JsonSerializer.Serialize(kline, JsonTools.JsonSerializerNotIndented)}");
        }
    }


    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        SubscriptionBundle!.SocketClient ??= new BloFinSocketClient();
        var client = (BloFinSocketClient)SubscriptionBundle.SocketClient;
        var api = client.SpotApi;

        // BloFin Spot requires formatted symbol names (e.g. "BTC-USDT") for the subscription,
        // while the feed strips underscores before matching against our internal symbol names.
        _symbolByStrippedName = [];
        List<string> formattedNames = [];
        foreach (var symbol in SymbolList)
        {
            string formattedName = api.FormatSymbol(symbol.Base, symbol.Quote, TradingMode.Spot);
            formattedNames.Add(formattedName);
            _symbolByStrippedName.TryAdd(formattedName.Replace("_", ""), symbol);
        }
        string symbolNames = string.Join(",", formattedNames);

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
