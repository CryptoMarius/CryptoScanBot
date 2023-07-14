using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Exchange.Binance;

internal class KLineTicker: KLineTickerBase
{
    public static List<KLineTickerStream> TickerList { get; set; } = new();

    public override async Task Start()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} starting kline ticker");
        // Deze methode werkt alleen op Binance
        //if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
        {
            int count = 0;
            List<Task> taskList = new();
            foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
            {
                if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
                {
                    List<CryptoSymbol> symbols = quoteData.SymbolList.ToList();

                    // We krijgen soms timeouts (eigenlijk de library) omdat we 
                    // teveel symbols aanbieden, daarom splitsen we het de lijst in stukken.
                    int splitCount = 200;
                    if (symbols.Count > splitCount)
                        splitCount = 1 + (symbols.Count / 2);

                    while (symbols.Count > 0)
                    {
                        KLineTickerStream ticker = new(quoteData);
                        TickerList.Add(ticker);

                        // Met de volle mep reageert de exchange niet snel genoeg (timeout errors enzovoort)
                        // Dit is een quick fix na de update van Binance.Net van 7 -> 8
                        while (symbols.Count > 0)
                        {
                            CryptoSymbol symbol = symbols[0];
                            ticker.symbols.Add(symbol.Name);
                            symbols.Remove(symbol);
                            count++;

                            // Ergens een lijn trekken? 
                            if (ticker.symbols.Count >= splitCount)
                                break;
                        }

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
        foreach (KLineTickerStream ticker in TickerList)
            await ticker?.StopAsync();
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
