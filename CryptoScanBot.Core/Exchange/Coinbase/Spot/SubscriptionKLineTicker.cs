using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

using Coinbase.Net.Clients;
using Coinbase.Net.Objects.Models;
using CryptoExchange.Net.SharedApis;

namespace CryptoScanBot.Core.Exchange.Coinbase.Spot;

public class SubscriptionKLineTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    private async Task ProcessCandleAsync(string? symbolName, CoinbaseStreamKline kline)
    {
        // Interval is prefixed in the "kline.1.SymbolName"? wtf? copied comments probably..
        if (string.IsNullOrEmpty(symbolName))
            return;

        if (GlobalData.ExchangeListName.TryGetValue(ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
        {
            if (exchange.SymbolListExchangeName.TryGetValue(symbolName, out CryptoSymbol? symbol))
            {
                Interlocked.Increment(ref TickerCount);
                //ScannerLog.Logger.Trace($"kline ticker {topic} process");
                //GlobalData.AddTextToLogTab($"{symbolNames} Candle {kline.OpenTime.ToLocalTime()} start processing");

                var candle = await CandleTools.Process1mCandleAsync(symbol, kline.OpenTime, 
                    kline.OpenPrice, kline.HighPrice, kline.LowPrice, kline.ClosePrice, 
                    kline.Volume, kline.Volume * 0.5m * (kline.HighPrice + kline.LowPrice));
                GlobalData.ThreadMonitorCandle!.AddToQueue(symbol, candle);
            }
        }

    }


    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        TickerGroup!.SocketClient ??= new CoinbaseSocketClient();
        var client = (CoinbaseSocketClient)TickerGroup.SocketClient;
        var api = client.AdvancedTradeApi;

        // TODO: quick en dirty code hier, nog eens verbeteren
        // We verwachten (helaas) slechts 1 symbol per ticker
        List<string> symbols = [];
        
        foreach (var symbol in SymbolList)
        {
            symbols.Add(symbol.ExchangeName);
        }
        string symbolNames = string.Join(",", symbols);


        //------------------------------------------------------------------------------
        // WTF, Subscribe to kline updates.
        // But the Klines are always at a 5 minute interval, that won't work
        //------------------------------------------------------------------------------

        var subscriptionResult = await api.SubscribeToKlineUpdatesAsync(symbols, data =>
        {
            //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} added for processing", data.Data.OpenTime.ToLocalTime(), data.ScannerSymbol));
            foreach (CoinbaseStreamKline kline in data.Data)
            {
                //if (data.Confirm) // Het is een definitieve candle (niet eentje in opbouw)
                Task.Run(async () => { await ProcessCandleAsync(data.Symbol, kline); });
            }
        }, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }

}
