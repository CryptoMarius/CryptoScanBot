using CryptoScanBot.Intern;
using CryptoScanBot.Model;



namespace CryptoScanBot.Exchange;

public class TickerKLine(ExchangeOptions exchangeOptions)
{
    internal ExchangeOptions ExchangeOptions { get; set; } = exchangeOptions;
    public List<TickerKLineItemBase> TickerList { get; set; } = [];

    public virtual async Task StartAsync()
    {
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} starting kline tickers");

        int groupCount = 0;
        int symbolCount = 0;
        List<Task> taskList = [];
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
            {
                List<TickerKLineItemBase> tickers = [];
                List<CryptoSymbol> symbols = quoteData.SymbolList.ToList();

                // Limiteer de munten (dat heeft dan ook impact op de barometer)
                if (ExchangeOptions.LimitAmountOfSymbols)
                {
                    foreach (var symbol in symbols.ToList())
                    {
                        if (symbol.QuoteData.MinimalVolume > 0 && symbol.Volume < 0.1m * symbol.QuoteData.MinimalVolume)
                            symbols.Remove(symbol);
                    }
                }


                int x = symbols.Count;
                while (x > 0)
                {
                    TickerKLineItemBase ticker = (TickerKLineItemBase)Activator.CreateInstance(ExchangeOptions.KLineTickerItemType, []);
                    ticker.GroupName = $"{quoteData.Name}#{groupCount}";
                    tickers.Add(ticker);
                    x -= ExchangeOptions.SubscriptionLimit;
                    groupCount++;
                }

                // De symbols evenredig verdelen over de tickers
                x = 0;
                foreach (CryptoSymbol symbol in symbols)
                {
                    //ticker.Symbols.Add(symbol.Name); // Werkt niet voor kucoin..
                    //TODO: ticker.Symbols.Add(Api.ExchangeSymbolName(symbol)); // beter!
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
                    ticker.GroupName += $" ({ticker.Symbols.Count})";
                    Task task = Task.Run(ticker.StartAsync);
                    taskList.Add(task);
                }
            }
        }

        if (taskList.Count != 0)
        {
            await Task.WhenAll(taskList).ConfigureAwait(false);
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} started kline ticker for {symbolCount} symbols in {taskList.Count} groups");
        }
        else GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} started kline ticker with 0 symbols!");



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
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} retry - started kline ticker for {symbolCount} symbols in {taskList.Count} groups");
        }
    }


    public virtual async Task StopAsync()
    {
        if (TickerList.Count != 0)
        {
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} kline tickers stopping");
            List<Task> taskList = [];
            foreach (var ticker in TickerList)
            {
                Task task = Task.Run(ticker.StopAsync);
                taskList.Add(task);
            }
            await Task.WhenAll(taskList).ConfigureAwait(false);
            ScannerLog.Logger.Trace($"{ExchangeOptions.ExchangeName} kline tickers stopped");
            TickerList.Clear();
        }
        else
            ScannerLog.Logger.Trace($"{ExchangeOptions.ExchangeName} kline tickers already stopped");
    }


    public virtual void Reset()
    {
        foreach (var ticker in TickerList)
            Interlocked.Exchange(ref ticker.TickerCount, 0);
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
        List<TickerKLineItemBase> tickers = [];
        foreach (var ticker in TickerList)
        {
            if (ticker.ConnectionLostCount > 0)
                tickers.Add(ticker);
        }

        if (tickers.Count != 0)
        {
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} herstarten {tickers.Count} kline tickers (starting)");

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


            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} herstarten {tickers.Count} kline tickers (stopped)");


            taskList.Clear();
            foreach (var ticker in tickers)
            {
                TickerKLineItemBase tickerNew = (TickerKLineItemBase)Activator.CreateInstance(ExchangeOptions.KLineTickerItemType, [ExchangeOptions]);
                tickerNew.Symbols = ticker.Symbols;
                TickerList.Add(tickerNew);

                task = Task.Run(tickerNew.StartAsync);
                taskList.Add(task);
            }
            await Task.WhenAll(taskList).ConfigureAwait(false);

            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} herstarten {tickers.Count} kline tickers (finished)");
        }

        // En de applicatie status herstellen (niet 100% zuiver)
        if (GlobalData.ApplicationStatus == Enums.CryptoApplicationStatus.Initializing)
            GlobalData.ApplicationStatus = Enums.CryptoApplicationStatus.Running;

    }
}
