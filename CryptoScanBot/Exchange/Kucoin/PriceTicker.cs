using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange.Kucoin;

internal class PriceTicker() : PriceTickerBase
{
    static private List<PriceTickerItem> TickerList { get; set; } = [];

    public override async Task Start()
    {
        // De Kucoin price ticker is een echte CPU killer, uitgezet, dan maar op een andere manier (of niet)

        return;

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

                    //// We krijgen soms timeouts (eigenlijk de library) omdat we teveel 
                    //// symbols aanbieden, daarom splitsen we het hier de lijst in twee stukken.
                    //int splitCount = 200;
                    //if (symbols.Count > splitCount)
                    //    splitCount = 1 + (symbols.Count / 2);

                    //raar..
                    while (symbols.Count > 0)
                    {
                        PriceTickerItem ticker = new();
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
                await Task.WhenAll(taskList);
                GlobalData.AddTextToLogTab($"{Api.ExchangeName} started price ticker stream for {count} symbols");
            }
        }
    }



    public override async Task Stop()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} stopping price ticker");
        List<Task> taskList = [];
        foreach (var ticker in TickerList)
        {
            Task task = Task.Run(async () => { await ticker.StopAsync(); });
            taskList.Add(task);
        }
        if (taskList.Count != 0)
            await Task.WhenAll(taskList);
        TickerList.Clear();
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
