using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange.BybitSpot;

internal class PriceTicker() : PriceTickerBase
{
    static private List<PriceTickerItem> TickerList { get; set; } = [];

    public override async Task StartAsync()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} starting price ticker");
        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
        {
            int count = 0;
            List<Task> taskList = [];
            foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
            {
                if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
                {
                    List<CryptoSymbol> symbols = quoteData.SymbolList.ToList();

                    // We krijgen soms timeouts (eigenlijk de library) omdat we teveel 
                    // symbols aanbieden, daarom splitsen we het hier de lijst in twee stukken.
                    //int splitCount = 200;
                    //if (symbols.Count > splitCount)
                    //    splitCount = 1 + (symbols.Count / 2);

                    //raar..
                    while (symbols.Count > 0)
                    {
                        PriceTickerItem ticker = new();
                        TickerList.Add(ticker);

                        // Op deze exchange is er een limiet van 10 symbols, dus opknippen in (veel) stukjes
                        while (symbols.Count > 0)
                        {
                            CryptoSymbol symbol = symbols[0];
                            ticker.Symbols.Add(symbol.Name);
                            symbols.Remove(symbol);
                            count++;

                            if (ticker.Symbols.Count >= 10)
                                break;
                        }

                        ticker.GroupName = $"{TickerList.Count} ({ticker.Symbols.Count})";
                        Task task = Task.Run(async () => { await ticker.StartAsync(); });
                        taskList.Add(task);
                    }
                }
            }

            if (taskList.Count != 0)
            {
                await Task.WhenAll(taskList).ConfigureAwait(false);
                GlobalData.AddTextToLogTab($"{Api.ExchangeName} started price ticker stream for {count} symbols");
            }
        }
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} price tickers started");
    }



    public override async Task StopAsync()
    {
        if (TickerList.Count != 0)
        {
            GlobalData.AddTextToLogTab($"{Api.ExchangeName} price ticker stopping");
            List<Task> taskList = [];
            foreach (var ticker in TickerList)
            {
                Task task = Task.Run(async () => { await ticker.StopAsync(); });
                taskList.Add(task);
            }
            await Task.WhenAll(taskList).ConfigureAwait(false);
            ScannerLog.Logger.Trace($"{Api.ExchangeName} price tickers stopped");
            TickerList.Clear();
        }
        else
            ScannerLog.Logger.Trace($"{Api.ExchangeName} price tickers already stopped");
    }


    public override void Reset()
    {
        foreach (var ticker in TickerList)
            ticker.TickerCount = 0;
    }


    public override int Count()
    {
        int count = 0;
        foreach (var ticker in TickerList)
            count += ticker.TickerCount;
        return count;
    }
}
