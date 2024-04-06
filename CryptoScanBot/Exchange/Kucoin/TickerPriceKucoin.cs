using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange.Kucoin;

internal class TickerPriceKucoin(ExchangeOptions exchangeOptions) : TickerPrice(exchangeOptions)
{
    static private List<TickerPriceItem> TickerList { get; set; } = [];

    public override async Task StartAsync()
    {
        // De Kucoin price ticker is een echte CPU killer, uitgezet, dan maar op een andere manier (of niet)
        return;

        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} starting price ticker");
        if (GlobalData.ExchangeListName.TryGetValue(ExchangeOptions.ExchangeName, out Model.CryptoExchange exchange))
        {
            int count = 0;
            List<Task> taskList = [];
            foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
            {
                if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
                {
                    List<CryptoSymbol> symbols = quoteData.SymbolList.ToList();

                    while (symbols.Count > 0)
                    {
                        TickerPriceItem ticker = new();
                        TickerList.Add(ticker);

                        while (symbols.Count > 0)
                        {
                            CryptoSymbol symbol = symbols[0];
                            ticker.Symbols.Add(symbol.Base + '-' + symbol.Quote);
                            symbols.Remove(symbol);
                            count++;

                            if (ticker.Symbols.Count >= 100)
                                break;
                        }

                        // opvullen tot circa 150 coins?
                        //ExchangeStream1mCandles.Add(bybitStream1mCandles);
                        //await bybitStream1mCandles.StartAsync(); // bewust geen await
                        //await TaskBybitStreamPriceTicker.ExecuteAsync(symbolNames);

                        Task task = Task.Run(async () => { await ticker.StartAsync(); });
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



    public override async Task StopAsync()
    {
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} stopping price tickers");
        List<Task> taskList = [];
        foreach (var ticker in TickerList)
        {
            Task task = Task.Run(async () => { await ticker.StopAsync(); });
            taskList.Add(task);
        }
        if (taskList.Count != 0)
            await Task.WhenAll(taskList).ConfigureAwait(false);
        TickerList.Clear();
        ScannerLog.Logger.Trace($"{ExchangeOptions.ExchangeName} price tickers stopped");
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
