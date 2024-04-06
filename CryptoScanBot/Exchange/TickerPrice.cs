using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange;

public class TickerPrice(ExchangeOptions exchangeOptions)
{
    internal ExchangeOptions ExchangeOptions { get; set; } = exchangeOptions;
    internal List<TickerPriceItemBase> TickerList { get; set; } = [];

    //public abstract Task StartAsync();
    //public abstract Task StopAsync();
    //public abstract void Reset();
    //public abstract int Count();


    public virtual async Task StartAsync()
    {
        // TODO: Management van sockets, iedere socket kan een beperkt aantal subscriptions aan (een stuk of 10 oid)

        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} starting price ticker");
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeOptions.ExchangeName, out Model.CryptoExchange exchange))
        {
            int count = 0;
            List<Task> taskList = [];
            foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
            {
                //if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
                {
                    List<CryptoSymbol> symbols = quoteData.SymbolList.ToList();

                    while (symbols.Count > 0)
                    {
                        TickerPriceItemBase ticker = (TickerPriceItemBase)Activator.CreateInstance(ExchangeOptions.PriceTickerItemType, []);
                        TickerList.Add(ticker);

                        // De symbols evenredig verdelen over de tickers
                        while (symbols.Count > 0)
                        {
                            CryptoSymbol symbol = symbols[0];
                            ticker.Symbols.Add(symbol.Name); // Werkt niet voor kucoin..
                            //TODO: ticker.Symbols.Add(Api.ExchangeSymbolName(symbol)); // beter!
                            //ExchangeOptions.ApiType.s
                            symbols.Remove(symbol);
                            count++;

                            if (ticker.Symbols.Count >= ExchangeOptions.SubscriptionLimit)
                                break;
                        }

                        ticker.GroupName = $"{TickerList.Count} ({ticker.Symbols.Count})";
                        Task task = Task.Run(ticker.StartAsync);
                        taskList.Add(task);
                    }
                }
            }

            if (taskList.Count != 0)
            {
                await Task.WhenAll(taskList).ConfigureAwait(false);
                GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} started price ticker stream for {count} symbols");
            }
        }
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} price tickers started");
    }

    public virtual async Task StopAsync()
    {
        if (TickerList.Count != 0)
        {
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} price ticker stopping");
            List<Task> taskList = [];
            foreach (var ticker in TickerList)
            {
                Task task = Task.Run(async () => { await ticker.StopAsync(); });
                taskList.Add(task);
            }
            await Task.WhenAll(taskList).ConfigureAwait(false);
            ScannerLog.Logger.Trace($"{ExchangeOptions.ExchangeName} price tickers stopped");
            TickerList.Clear();
        }
        else
            ScannerLog.Logger.Trace($"{ExchangeOptions.ExchangeName} price tickers already stopped");
    }


    public virtual void Reset()
    {
        foreach (var ticker in TickerList)
            ticker.TickerCount = 0;
    }


    public virtual int Count()
    {
        int count = 0;
        foreach (var ticker in TickerList)
            count += ticker.TickerCount;
        return count;
    }
}
