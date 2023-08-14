using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Exchange.BybitFutures;

internal class KLineTicker : KLineTickerBase
{
    public static List<KLineTickerStream> TickerList { get; set; } = new();

    public override async Task Start()
    {
        //if (GlobalData.ExchangeListName.TryGetValue(ExchangeName, out Model.CryptoExchange _))
        {
            int count = 0;
            List<Task> taskList = new();
            foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
            {
                if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
                {
                    List<CryptoSymbol> symbols = quoteData.SymbolList.ToList();

                    // TODO: Wellicht versnellen door een lijst van taken voor te bereiden (zie Task.WhenAll)

                    // We krijgen soms timeouts (eigenlijk de library) omdat we teveel 
                    // symbols aanbieden, daarom splitsen we het hier de lijst in twee stukken.
                    //int splitCount = 200;
                    //if (symbols.Count > splitCount)
                    //    splitCount = 1 + (symbols.Count / 2);

                    while (symbols.Count > 0)
                    {
                        KLineTickerStream ticker = new(quoteData);
                        TickerList.Add(ticker);

                        // Op deze exchange is er een limiet van 10 symbols, dus opknippen in (veel) stukjes
                        while (symbols.Count > 0)
                        {
                            CryptoSymbol symbol = symbols[0];
                            ticker.symbols.Add(symbol.Name);
                            symbols.Remove(symbol);
                            count++;

                            if (ticker.symbols.Count >= 10)
                                break;
                        }
                        //await bybitStream1mCandles.StartAsync(); // bewust geen await

                        Task task = Task.Run(async () => { await ticker.StartAsync(); });
                        taskList.Add(task);
                    }
                }
            }

            if (taskList.Any())
            {
                await Task.WhenAll(taskList);
                GlobalData.AddTextToLogTab($"{Api.ExchangeName} started kline ticker for {count} symbols");
            }
        }
    }


    public override async Task Stop()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} stopping kline ticker");
        List<Task> taskList = new();
        foreach (var ticker in TickerList)
        {
            Task task = Task.Run(async () => { await ticker.StopAsync(); });
            taskList.Add(task);
        }
        if (taskList.Any())
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

    public override async Task CheckKlineTickers()
    {
        List<KLineTickerStream> tickers = new();
        foreach (var ticker in TickerList)
        {
            if (ticker.ConnectionLostCount > 0)
                tickers.Add(ticker);
        }

        if (tickers.Any())
        {
            GlobalData.AddTextToLogTab($"{Api.ExchangeName} restart {tickers.Count} tickers");

            Task task;
            List<Task> stopTickers = new();
            foreach (var ticker in tickers)
            {
                task = Task.Run(async () => { await ticker.StopAsync(); });
                stopTickers.Add(task);
            }
            await Task.WhenAll(stopTickers);

            List<Task> startTickers = new();
            foreach (var ticker in tickers)
            {
                task = Task.Run(async () => { await ticker.StartAsync(); });
                startTickers.Add(task);
            }
            await Task.WhenAll(startTickers);
        }
    }

}
