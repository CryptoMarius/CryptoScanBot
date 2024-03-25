using CryptoScanBot.Intern;
using CryptoScanBot.Model;

using Kucoin.Net.Clients;

namespace CryptoScanBot.Exchange.Kucoin;

// Deze class afwijkend tov de base

internal class KLineTicker() : KLineTickerBase(Api.ExchangeName, 1, typeof(KLineTickerItem))
{
    public static new List<KLineTickerItem> TickerList { get; set; } = [];

    public override async Task StartAsync()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} starting kline tickers (slow)");

        int totalSymbols = 0;
        int tickersCreated = 0;
        List<Task> taskList = [];
        KucoinSocketClient socketClient = new();
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
            {
                List<CryptoSymbol> symbols = quoteData.SymbolList.ToList();
                totalSymbols += symbols.Count;

                // Limiteer de munten enigzins (heeft wellicht impact op de barometer)
                foreach (var s in symbols.ToList())
                {
                    if (s.QuoteData.MinimalVolume > 0 && s.Volume < 0.1m * s.QuoteData.MinimalVolume)
                        symbols.Remove(s);
                }

                // TODO: Wellicht versnellen door een lijst van taken voor te bereiden (zie Task.WhenAll)

                //// We krijgen soms timeouts (eigenlijk de library) omdat we teveel 
                //// symbols aanbieden, daarom splitsen we het hier de lijst in twee stukken.
                //int splitCount = 200;
                //if (symbols.Count > splitCount)
                //    splitCount = 1 + (symbols.Count / 2);

                while (symbols.Count > 0)
                {
                    KLineTickerItem ticker = new("Kucoin", quoteData);
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
                        break;
                    }
                    Task task = Task.Run(async () => { await ticker.StartAsync(socketClient); });
                    taskList.Add(task);

                    //if (taskList.Count > 25)
                    //    break;
                }
            }
        }

        if (taskList.Count != 0)
        {
            await Task.WhenAll(taskList);
            GlobalData.AddTextToLogTab($"{Api.ExchangeName} started kline ticker voor {tickersCreated} van de {totalSymbols} symbols");
        }
    }


    public override async Task StopAsync()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} stopping kline ticker");
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
        int TickerCount = 0;
        foreach (var ticker in TickerList)
            TickerCount += ticker.TickerCount;
        return TickerCount;
    }

}
