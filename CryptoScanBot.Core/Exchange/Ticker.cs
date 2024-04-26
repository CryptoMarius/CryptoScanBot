using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange;

public enum CryptoTickerType
{
    user,
    price,
    kline
}

public class Ticker(ExchangeOptions exchangeOptions, Type userTickerItemType, CryptoTickerType tickerType)
{
    internal ExchangeOptions ExchangeOptions { get; set; } = exchangeOptions;
    internal List<TickerGroup> TickerGroupList { get; set; } = [];
    internal Type UserTickerItemType { get; set; } = userTickerItemType;
    internal CryptoTickerType TickerType { get; set; } = tickerType;
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Voor de user ticker
    /// </summary>
    private List<SubscriptionTicker> CreateUserTickers(ref int symbolCount)
    {
        symbolCount = 0;
        List<SubscriptionTicker> tickerList = [];

        if (Activator.CreateInstance(UserTickerItemType, [ExchangeOptions]) is SubscriptionTicker ticker)
        {
            ticker.GroupName = "*";
            ticker.TickerType = TickerType;
            tickerList.Add(ticker);
        }

        return tickerList;
    }


    /// <summary>
    /// Voor de kline en price ticker
    /// </summary>
    private List<SubscriptionTicker> CreateTheTickers(ref int symbolCount)
    {
        // Splits de symbols
        symbolCount = 0;
        int groupCount = 0;
        List<SubscriptionTicker> tickerList = [];
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values.ToList())
        {
            if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
            {
                List<SubscriptionTicker> tickers = [];
                List<CryptoSymbol> symbols = [.. quoteData.SymbolList];

                // Limiteer de munten (dat heeft dan ook impact op de barometer)
                if (ExchangeOptions.LimitAmountOfSymbols)
                {
                    foreach (var symbol in symbols.ToList())
                    {
                        if (symbol.Name.Equals("KONUSDT"))
                            continue; // debug
                        if (symbol.QuoteData.MinimalVolume > 0 && symbol.Volume < 0.1m * symbol.QuoteData.MinimalVolume)
                            symbols.Remove(symbol);
                    }
                }


                int x = symbols.Count;
                while (x > 0)
                {
                    if (Activator.CreateInstance(UserTickerItemType, [ExchangeOptions]) is SubscriptionTicker ticker)
                    {
                        ticker.GroupName = $"{quoteData.Name}#{groupCount}";
                        ticker.TickerType = TickerType;
                        tickers.Add(ticker);
                        x -= ExchangeOptions.SubscriptionLimitSymbols;
                        groupCount++;
                    }
                }

                // De symbols evenredig verdelen over de tickers
                x = 0;
                foreach (CryptoSymbol symbol in symbols)
                {
                    //ticker.Symbols.Add(symbol.Name); // Werkt niet voor kucoin..
                    //TODO: ticker.Symbols.Add(Api.ExchangeSymbolName(symbol)); // beter!
                    var ticker = tickers[x];
                    ticker.SymbolList.Add(symbol);
                    ticker.Symbols.Add(symbol.Name);

                    //if (ticker.SymbolOverview == "")
                    //    ticker.SymbolOverview += symbol.Name;
                    //else
                    //    ticker.SymbolOverview += "," + symbol.Name;
                    x++;
                    if (x >= tickers.Count)
                        x = 0;
                    symbolCount++;
                }
                //string.Join(',', Symbols)

                // kan gecombineerd worden ^^
                foreach (var ticker in tickers)
                {
                    tickerList.Add(ticker);
                    ticker.GroupName += $" ({ticker.SymbolList.Count})";
                    ticker.SymbolOverview = string.Join(',', ticker.Symbols);
                }
            }
        }
        return tickerList;
    }

    public virtual async Task StartAsync()
    {
        if (!Enabled)
        {
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} {TickerType} ticker is disabled");
            return;
        }

        // Is al gestart
        if (TickerGroupList.Count > 0)
        {
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} {TickerType} is already started");
            return;
        }

        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} starting {TickerType} tickers");


        // Splits de symbols (user ticker is much simpler)
        int symbolCount = 0;
        List<SubscriptionTicker> tickerList;
        if (TickerType == CryptoTickerType.user)
            tickerList = CreateUserTickers(ref symbolCount);
        else
            tickerList = CreateTheTickers(ref symbolCount);


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


        string text = "";
        if (taskList.Count != 0)
        {
            await Task.WhenAll(taskList).ConfigureAwait(false);
            if (TickerType != CryptoTickerType.user)
                text = $" for {symbolCount} symbols";
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} started {TickerType} ticker{text} in {taskList.Count} groups");
        }
        else
        {
            if (TickerType != CryptoTickerType.user)
                text = $" with 0 symbols!";
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} started {TickerType} ticker{text}");
        }




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
                    symbolCount += ticker.SymbolList.Count;
                }
            }
        }
        if (taskList.Count != 0)
        {
            await Task.WhenAll(taskList).ConfigureAwait(false);
            if (TickerType != CryptoTickerType.user)
                text = $" for {symbolCount} symbols";
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} retry - started {TickerType} ticker{text} in {taskList.Count} groups");
        }
    }


    public virtual async Task StopAsync()
    {
        if (!Enabled)
            return;

        if (TickerGroupList.Count != 0)
        {
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} {TickerType} tickers stopping");
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
            ScannerLog.Logger.Trace($"{ExchangeOptions.ExchangeName} {TickerType} tickers stopped");
            TickerGroupList.Clear();
        }
        else
            ScannerLog.Logger.Trace($"{ExchangeOptions.ExchangeName} {TickerType} tickers already stopped");
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

    public virtual async Task CheckTickers()
    {
        int tickerCount = 0;
        List<TickerGroup> tickerGroups = [];
        foreach (var tickerGroup in TickerGroupList)
        {
            foreach (var ticker in tickerGroup.TickerList)
            {
                if (ticker.ConnectionLostCount > 0 || ticker.ErrorDuringStartup)
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
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} herstarten {tickerCount} {TickerType} tickers (stopping)");

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


            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} herstarten {tickerCount} {TickerType} tickers (stopped)");


            // Start de getrande tickers opnieuw
            taskList.Clear();
            foreach (var tickerGroup in TickerGroupList)
            {
                foreach (var ticker in tickerGroup.TickerList)
                {
                    // We hergebruiken de group nu (de indeling is ingewikkelder geworden)
                    //TickerKLineItemBase tickerNew = (TickerKLineItemBase)Activator.CreateInstance(ExchangeOptions.KLineTickerItemType, [ExchangeOptions]);
                    //tickerNew.Symbols = ticker.Symbols;
                    Task task = Task.Run(ticker.StartAsync);
                    taskList.Add(task);
                }
            }
            await Task.WhenAll(taskList).ConfigureAwait(false);
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} herstarten {tickerGroups.Count} {TickerType} tickers (finished)");
        }

        // En de applicatie status herstellen (niet 100% zuiver)
        if (GlobalData.ApplicationStatus == Enums.CryptoApplicationStatus.Initializing)
            GlobalData.ApplicationStatus = Enums.CryptoApplicationStatus.Running;

    }

    public void DumpTickerInfo()
    {
        GlobalData.AddTextToLogTab("");
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} Ticker info {TickerType}");

        foreach (var tickerGroup in TickerGroupList)
        {
            foreach (var ticker in tickerGroup.TickerList)
            {
                GlobalData.AddTextToLogTab($"Ticker {ticker.GroupName} ErrorDuringStartup={ticker.ErrorDuringStartup} ConnectionLostCount={ticker.ConnectionLostCount} {ticker.SymbolOverview}");
            }
        }
    }

}
