using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Exchange;

public abstract class KLineTickerBase
{
    public int LimitOnSymbols = 100;
    //public ExchangeBase Api { get; set; } hmm, de exchangeName is niet static..
    public string ApiExchangeName { get; set; } // beetje overbodig, hoe los je dat netjes op?
    public Type KLineTickerItemType { get; set; }
    public List<KLineTickerItemBase> TickerList { get; set; } = new();

    public KLineTickerBase(string apiExchangeName, int limitOnSymbols, Type kLineTickerItemType)
    {
        LimitOnSymbols = limitOnSymbols;
        ApiExchangeName = apiExchangeName;
        KLineTickerItemType = kLineTickerItemType;
    }

    public virtual async Task StartAsync()
    {
        GlobalData.AddTextToLogTab($"{ApiExchangeName} starting kline ticker");

        int groupCount = 0;
        int symbolCount = 0;
        List<Task> taskList = new();
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
            {
                //var activeSymbols = quoteData.SymbolList.Where(s => s.Status == 1);

                List<KLineTickerItemBase> tickers = new();

                // 1 ticker handelt (normaliter) 1..LimitOnSymbols symbols af
                // Kucoin dot in dit geval weer wat moeilijker, een rare api..
                int x = quoteData.SymbolList.Count;
                while (x > 0)
                {
                    // 1 ticker handelt 1 tot x kline tickers af
                    KLineTickerItemBase ticker = (KLineTickerItemBase)Activator.CreateInstance(KLineTickerItemType, new object[] { ApiExchangeName, quoteData });
                    tickers.Add(ticker);

                    x -= LimitOnSymbols;
                    groupCount++;
                }

                // Symbols evenredig verdelen over de tickers
                x = 0;
                foreach (CryptoSymbol symbol in quoteData.SymbolList.ToList())
                {
                    if (symbol.Status == 1)
                    {
                        tickers[x].Symbols.Add(symbol.Name);
                        x++;
                        if (x >= tickers.Count)
                            x = 0;
                        symbolCount++;
                    }
                }


                foreach (var ticker in tickers)
                {
                    TickerList.Add(ticker);

                    Task task = Task.Run(ticker.StartAsync);
                    taskList.Add(task);
                }
            }
        }

        if (taskList.Any())
        {
            await Task.WhenAll(taskList);
            GlobalData.AddTextToLogTab($"{ApiExchangeName} started kline ticker for {symbolCount} symbols in {groupCount} groepen");
        }
        else GlobalData.AddTextToLogTab($"{ApiExchangeName} started kline ticker met 0 symbols!");
    }


    public virtual async Task StopAsync()
    {
        GlobalData.AddTextToLogTab($"{ApiExchangeName} stopping kline ticker");
        List<Task> taskList = new();
        foreach (var ticker in TickerList)
        {
            Task task = Task.Run(ticker.StopAsync);
            taskList.Add(task);
        }
        if (taskList.Any())
            await Task.WhenAll(taskList);
        else GlobalData.AddTextToLogTab($"{ApiExchangeName} stopped kline ticker met 0 symbols!");
        TickerList.Clear();
    }


    public virtual void Reset()
    {
        foreach (var ticker in TickerList)
            ticker.TickerCount = 0;
    }


    public virtual int Count()
    {
        int tickerCount = 0;
        foreach (var ticker in TickerList)
            tickerCount += ticker.TickerCount;
        return tickerCount;
    }

    public virtual bool NeedsRestart()
    {
        bool restart = false;
        foreach (var ticker in TickerList)
        {
            // Is deze ooit gestart?
            if (ticker.TickerCount != 0)
            {
                // Is ie blijven staan? Netwerk storing enzovoort?
                if (ticker.TickerCount == ticker.TickerCountLast)
                    restart = true;
                else
                    ticker.TickerCountLast = ticker.TickerCount;
            }
        }
        return restart;
    }

    public virtual async Task CheckKlineTickers()
    {
        List<KLineTickerItemBase> tickers = new();
        foreach (var ticker in TickerList)
        {
            if (ticker.ConnectionLostCount > 0)
                tickers.Add(ticker);
        }

        if (tickers.Any())
        {
            GlobalData.AddTextToLogTab($"{ApiExchangeName} herstarten {tickers.Count} kline tickers (starting)");

            // de bestaande ticker verwijderen (de combinatie met ticker en timer zou problemen kunnen geven?)

            Task task;
            List<Task> taskList = new();
            foreach (var ticker in tickers)
            {
                task = Task.Run(ticker.StopAsync);
                taskList.Add(task);
                TickerList.Remove(ticker);
            }
            await Task.WhenAll(taskList);


            GlobalData.AddTextToLogTab($"{ApiExchangeName} herstarten {tickers.Count} kline tickers (stopped)");


            taskList.Clear();
            foreach (var ticker in tickers)
            {
                KLineTickerItemBase tickerNew = (KLineTickerItemBase)Activator.CreateInstance(KLineTickerItemType, new object[] { ApiExchangeName, ticker.QuoteData });
                tickerNew.Symbols = ticker.Symbols;
                TickerList.Add(tickerNew);

                task = Task.Run(tickerNew.StartAsync);
                taskList.Add(task);
            }
            await Task.WhenAll(taskList);

            GlobalData.AddTextToLogTab($"{ApiExchangeName} herstarten {tickers.Count} kline tickers (finished)");
        }

        // En de applicatie status herstellen (niet 100% zuiver)
        if (GlobalData.ApplicationStatus == Enums.CryptoApplicationStatus.Initializing)
            GlobalData.ApplicationStatus = Enums.CryptoApplicationStatus.Running;

    }
}
