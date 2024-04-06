using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange;

public class Ticker(ExchangeOptions exchangeOptions, Type userTickerItemType, string tickerType)
{
    internal ExchangeOptions ExchangeOptions { get; set; } = exchangeOptions;
    public List<TickerGroup> TickerGroupList { get; set; } = [];
    Type UserTickerItemType { get; set; } = userTickerItemType;
    string tickerType { get; set; } = tickerType;

    public virtual async Task StartAsync()
    {
        // ? Geen controle of het wel gestart is (dan zou het het aantal tickers oplopen) ?
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} starting {tickerType} tickers {TickerGroupList.Count}");


        // Splits de symbols
        int groupCount = 0;
        int symbolCount = 0;
        List<TickerItem> tickerList = [];
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
            {
                List<TickerItem> tickers = [];
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
                    TickerItem ticker = (TickerItem)Activator.CreateInstance(UserTickerItemType, []);
                    ticker.GroupName = $"{quoteData.Name}#{groupCount}";
                    ticker.TickerType = tickerType;
                    tickers.Add(ticker);
                    x -= ExchangeOptions.SubscriptionLimitSymbols;
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
                    tickerList.Add(ticker);
                    ticker.GroupName += $" ({ticker.Symbols.Count})";
                }
            }
        }


        // Vanwege technische limiet reduceren we het aantal subscriptions per client
        while (tickerList.Count > 0)
        {
            TickerGroup tickerGroup = new();
            TickerGroupList.Add(tickerGroup);
            tickerGroup.SocketClient = null; // todo

            // Dit zou ook met een Take() kunnen, maar ach dit werkt ook
            while (tickerList.Count > 0 && tickerGroup.TickerList.Count < ExchangeOptions.SubscriptionLimitClient)
            {
                var ticker = tickerList[0];
                ticker.TickerGroup = tickerGroup;
                tickerGroup.TickerList.Add(ticker);
                tickerList.Remove(ticker);
            }
        }


        // Maak er taken van
        List<Task> taskList = [];
        foreach (var tickerGroup in TickerGroupList)
        {
            foreach (var ticker in tickerGroup.TickerList)
            {
                Task task = Task.Run(ticker.StartAsync);
                taskList.Add(task);
            }
        }

        if (taskList.Count != 0)
        {
            await Task.WhenAll(taskList).ConfigureAwait(false);
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} started {tickerType} ticker for {symbolCount} symbols in {taskList.Count} groups");
        }
        else GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} started {tickerType} ticker with 0 symbols!");




        // Herkansing? (de echte vraag is waarom er fouten ontstaan tijdens het opstarten)
        symbolCount = 0;
        taskList.Clear();
        foreach (var tickerGroup in TickerGroupList)
        {
            foreach (var ticker in tickerGroup.TickerList)
            {
                if (ticker.ErrorDuringStartup)
                {
                    Task task = Task.Run(ticker.StartAsync);
                    taskList.Add(task);
                    symbolCount += ticker.Symbols.Count;
                }
            }
        }
        if (taskList.Count != 0)
        {
            await Task.WhenAll(taskList).ConfigureAwait(false);
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} retry - started {tickerType} ticker for {symbolCount} symbols in {taskList.Count} groups");
        }
    }


    public virtual async Task StopAsync()
    {
        if (TickerGroupList.Count != 0)
        {
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} {tickerType} tickers stopping");
            List<Task> taskList = [];
            foreach (var tickerGroup in TickerGroupList)
            {
                foreach (var ticker in tickerGroup.TickerList)
                {
                    Task task = Task.Run(ticker.StopAsync);
                    taskList.Add(task);
                }
            }
            await Task.WhenAll(taskList).ConfigureAwait(false);
            ScannerLog.Logger.Trace($"{ExchangeOptions.ExchangeName} {tickerType} tickers stopped");
            TickerGroupList.Clear();
        }
        else
            ScannerLog.Logger.Trace($"{ExchangeOptions.ExchangeName} {tickerType} tickers already stopped");
    }


    public virtual void Reset()
    {
        foreach (var tickerGroup in TickerGroupList)
        {
            foreach (var ticker in tickerGroup.TickerList)
            {
                Interlocked.Exchange(ref ticker.TickerCount, 0);
            }
        }
    }


    public virtual int Count()
    {
        int tickerCount = 0;
        foreach (var tickerGroup in TickerGroupList)
        {
            foreach (var ticker in tickerGroup.TickerList)
            {
                tickerCount += ticker.TickerCount;
            }
        }
        return tickerCount;
    }

    public virtual bool NeedsRestart()
    {
        bool restart = false;
        foreach (var tickerGroup in TickerGroupList)
        {
            foreach (var ticker in tickerGroup.TickerList)
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
        }
        return restart;
    }

    public virtual async Task CheckKlineTickers()
    {
        int tickerCount = 0;
        List<TickerGroup> tickerGroups = [];
        foreach (var tickerGroup in TickerGroupList)
        {
            foreach (var ticker in tickerGroup.TickerList)
            {
                if (ticker.ConnectionLostCount > 0)
                {
                    tickerCount += tickerGroup.TickerList.Count;
                    tickerGroups.Add(tickerGroup);
                    break;
                }
            }
        }

        if (tickerGroups.Count != 0)
        {

            // Stop de getrande tickers
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} herstarten {tickerCount} {tickerType} tickers (stopping)");
            
            List<Task> taskList = [];
            foreach (var tickerGroup in TickerGroupList)
            {
                foreach (var ticker in tickerGroup.TickerList)
                {
                    Task task = Task.Run(ticker.StopAsync);
                    taskList.Add(task);
                }
            }
            await Task.WhenAll(taskList).ConfigureAwait(false);


            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} herstarten {tickerCount} {tickerType} tickers (stopped)");


            // Start de getrande tickers opnieuw
            taskList.Clear();
            foreach (var tickerGroup in TickerGroupList)
            {
                foreach (var ticker in tickerGroup.TickerList)
                {
                    // Hergebruiken de groep
                    //TickerKLineItemBase tickerNew = (TickerKLineItemBase)Activator.CreateInstance(ExchangeOptions.KLineTickerItemType, [ExchangeOptions]);
                    //tickerNew.Symbols = ticker.Symbols;

                    Task task = Task.Run(ticker.StartAsync);
                    taskList.Add(task);
                }
            }
            await Task.WhenAll(taskList).ConfigureAwait(false);
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} herstarten {tickerGroups.Count} {tickerType} tickers (finished)");
        }

        // En de applicatie status herstellen (niet 100% zuiver)
        if (GlobalData.ApplicationStatus == Enums.CryptoApplicationStatus.Initializing)
            GlobalData.ApplicationStatus = Enums.CryptoApplicationStatus.Running;

    }
}
