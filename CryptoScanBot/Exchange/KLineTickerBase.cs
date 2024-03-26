using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange;

public abstract class KLineTickerBase(string apiExchangeName, int limitOnSymbols, Type kLineTickerItemType)
{
    public int LimitOnSymbols = limitOnSymbols;
    public string ApiExchangeName { get; set; } = apiExchangeName;
    public Type KLineTickerItemType { get; set; } = kLineTickerItemType;
    public List<KLineTickerItemBase> TickerList { get; set; } = [];

    public virtual async Task StartAsync()
    {
        GlobalData.AddTextToLogTab($"{ApiExchangeName} starting kline ticker");

        int groupCount = 0;
        int symbolCount = 0;
        List<Task> taskList = [];
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
            {
                List<KLineTickerItemBase> tickers = [];
                List<CryptoSymbol> symbols = quoteData.SymbolList.ToList();

                // 1 ticker handelt (normaliter) 1..LimitOnSymbols symbols af
                // Kucoin doet in dit geval moeilijk, een afwijkende api..
                int x = symbols.Count;
                while (x > 0)
                {
                    // 1 ticker handelt 1 tot x kline tickers af
                    KLineTickerItemBase ticker = (KLineTickerItemBase)Activator.CreateInstance(KLineTickerItemType, [ApiExchangeName, quoteData]);
                    tickers.Add(ticker);

                    x -= LimitOnSymbols;
                    groupCount++;
                }

                // Symbols evenredig verdelen over de tickers
                x = 0;
                foreach (CryptoSymbol symbol in symbols)
                {
                    tickers[x].Symbols.Add(symbol.Name);
                    x++;
                    if (x >= tickers.Count)
                        x = 0;
                    symbolCount++;
                }

                // kan gecombineerd worden ^^
                foreach (var ticker in tickers)
                {
                    TickerList.Add(ticker);
                    Task task = Task.Run(ticker.StartAsync);
                    taskList.Add(task);
                }
            }
        }

        if (taskList.Count != 0)
        {
            await Task.WhenAll(taskList).ConfigureAwait(false);
            GlobalData.AddTextToLogTab($"{ApiExchangeName} started kline ticker for {symbolCount} symbols in {taskList.Count} groups");
        }
        else GlobalData.AddTextToLogTab($"{ApiExchangeName} started kline ticker with 0 symbols!");



        // Herkansing?
        symbolCount = 0;
        taskList.Clear();
        foreach (var ticker in TickerList)
        {
            if (ticker.ErrorDuringStartup)
            {
                Task task = Task.Run(ticker.StartAsync);
                taskList.Add(task);
                symbolCount += ticker.Symbols.Count;
            }
        }
        if (taskList.Count != 0)
        {
            await Task.WhenAll(taskList).ConfigureAwait(false);
            GlobalData.AddTextToLogTab($"{ApiExchangeName} retry - started kline ticker for {symbolCount} symbols in {taskList.Count} groups");
        }
    }


    public virtual async Task StopAsync()
    {
        GlobalData.AddTextToLogTab($"{ApiExchangeName} stopping kline ticker");
        List<Task> taskList = [];
        foreach (var ticker in TickerList)
        {
            Task task = Task.Run(ticker.StopAsync);
            taskList.Add(task);
        }
        if (taskList.Count != 0)
            await Task.WhenAll(taskList).ConfigureAwait(false);
        else GlobalData.AddTextToLogTab($"{ApiExchangeName} stopped kline ticker with 0 symbols!");
        TickerList.Clear();
        ScannerLog.Logger.Trace($"{ApiExchangeName} kline tickers stopped");
    }


    public virtual void Reset()
    {
        foreach (var ticker in TickerList)
            Interlocked.Exchange(ref ticker.TickerCount, 0); //ticker.TickerCount = 0;
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
        List<KLineTickerItemBase> tickers = [];
        foreach (var ticker in TickerList)
        {
            if (ticker.ConnectionLostCount > 0)
                tickers.Add(ticker);
        }

        if (tickers.Count != 0)
        {
            GlobalData.AddTextToLogTab($"{ApiExchangeName} herstarten {tickers.Count} kline tickers (starting)");

            // de bestaande ticker verwijderen (de combinatie met ticker en timer zou problemen kunnen geven?)

            Task task;
            List<Task> taskList = [];
            foreach (var ticker in tickers)
            {
                task = Task.Run(ticker.StopAsync);
                taskList.Add(task);
                TickerList.Remove(ticker);
            }
            await Task.WhenAll(taskList).ConfigureAwait(false);


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
            await Task.WhenAll(taskList).ConfigureAwait(false);

            GlobalData.AddTextToLogTab($"{ApiExchangeName} herstarten {tickers.Count} kline tickers (finished)");
        }

        // En de applicatie status herstellen (niet 100% zuiver)
        if (GlobalData.ApplicationStatus == Enums.CryptoApplicationStatus.Initializing)
            GlobalData.ApplicationStatus = Enums.CryptoApplicationStatus.Running;

    }
}
