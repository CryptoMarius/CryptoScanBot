using CryptoScanBot.Intern;
using CryptoScanBot.Model;

using Kucoin.Net.Clients;

namespace CryptoScanBot.Exchange.Kucoin;

internal class TickerKLineKucoin(ExchangeOptions exchangeOptions) : TickerKLine(exchangeOptions)
{
    public new List<TickerKLineItem> TickerList { get; set; } = [];

    public override async Task StartAsync()
    {
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} starting kline tickers");

        int symbolCount = 0;
        int tickersCreated = 0;
        List<Task> taskList = [];
        KucoinSocketClient socketClient = new();
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
            {
                List<CryptoSymbol> symbols = quoteData.SymbolList.ToList();

                // Limiteer de munten (dat heeft impact op de barometer..)
                foreach (var s in symbols.ToList())
                {
                    if (s.QuoteData.MinimalVolume > 0 && s.Volume < 0.1m * s.QuoteData.MinimalVolume)
                        symbols.Remove(s);
                }

                while (symbols.Count > 0)
                {
                    TickerKLineItem ticker = new(ExchangeOptions);
                    TickerList.Add(ticker);

                    // Op deze exchange is er een limiet van 1 symbols, dus opknippen in (veel) stukjes
                    // Absurd, interface is niet echt fijn, maar dit werkt ook hoor!
                    while (symbols.Count > 0)
                    {
                        CryptoSymbol symbol = symbols[0];
                        ticker.Symbol = symbol;
                        //ticker.symbols.Add(symbol.Base + "-" + symbol.Quote);
                        //ticker.SymbolName = symbol.Name;
                        symbols.Remove(symbol);
                        tickersCreated++;

                        // Really, 1? Succes qua opstarten
                        //if (ticker.symbols.Count >= 1)
                        symbolCount++;
                        break;
                    }
                    Task task = Task.Run(async () => { await ticker.StartAsync(); }); // TODO had een parameter socketClient!!!
                    taskList.Add(task);
                }
            }
        }

        if (taskList.Count != 0)
        {
            await Task.WhenAll(taskList).ConfigureAwait(false);
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} started kline ticker voor {tickersCreated} van de {symbolCount} symbols");
        }
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} kline tickers started");
    }


    public override async Task StopAsync()
    {
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} stopping kline tickers");
        List<Task> taskList = [];
        foreach (var ticker in TickerList)
        {
            Task task = Task.Run(async () => { await ticker.StopAsync(); });
            taskList.Add(task);
        }
        if (taskList.Count != 0)
            await Task.WhenAll(taskList).ConfigureAwait(false);
        TickerList.Clear();
        ScannerLog.Logger.Trace($"{ExchangeOptions.ExchangeName} kline tickers stopped");
    }


    public override void Reset()
    {
        foreach (var ticker in TickerList)
            ticker.TickerCount = 0;
    }


    public override int Count()
    {
        int TickerCount = 0;
        foreach (var ticker in TickerList)
            TickerCount += ticker.TickerCount;
        return TickerCount;
    }

}
