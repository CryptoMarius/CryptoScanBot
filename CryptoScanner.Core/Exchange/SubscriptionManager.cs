using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange;

public enum CryptoTickerType
{
    user,
    price,
    kline
}

/// <summary>
/// Owns all subscriptions of one kind (kline, price or user) for one exchange, and keeps them alive.
///
/// The layout is three levels deep:
///
///   SubscriptionManager              one per kind, per exchange
///   └── SubscriptionBundle           one socket client, holds up to SubscriptionsPerBundle
///       └── Subscription             one subscription at the exchange
///           └── SymbolList           up to SymbolLimitPerSubscription symbols
///
/// So 500 symbols with SymbolLimitPerSubscription 50 and SubscriptionsPerBundle 10 become
/// 10 subscriptions of 50 symbols, all riding on the socket client of a single bundle. The user
/// subscription has no symbols at all: it listens to the account, so there is one of everything.
///
/// A bundle is not the websocket connection itself - the exchange library owns those and decides
/// how many subscriptions share one. In practice they share a single connection per bundle, which
/// is why losing a connection marks every subscription in that bundle at the same time.
/// </summary>
public class SubscriptionManager(ExchangeOptions exchangeOptions, Type subscriptionType, CryptoTickerType tickerType)
{
    internal ExchangeOptions ExchangeOptions { get; set; } = exchangeOptions;
    internal List<SubscriptionBundle> SubscriptionBundleList { get; set; } = [];
    internal Type SubscriptionType { get; set; } = subscriptionType;
    internal CryptoTickerType TickerType { get; set; } = tickerType;
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Voor de user subscription
    /// </summary>
    private List<Subscription> CreateUserSubscription(ref int symbolCount)
    {
        symbolCount = 0;
        List<Subscription> subscriptionList = [];

        if (Activator.CreateInstance(SubscriptionType, [ExchangeOptions]) is Subscription subscription)
        {
            subscription.Name = "*";
            subscription.TickerType = TickerType;
            subscriptionList.Add(subscription);
        }

        return subscriptionList;
    }


    /// <summary>
    /// Prepare the kline subscription groups
    /// </summary>
    private List<Subscription> CreateTheSubscriptions(ref int symbolCount)
    {
        // Splits de symbols
        symbolCount = 0;
        int groupCount = 0;
        List<Subscription> subscriptionList = [];
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values.ToList())
        {
            if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
            {
                List<Subscription> subscriptions = [];
                List<CryptoSymbol> symbols = [.. quoteData.SymbolList];

                // Limit the amount of symbols (this has impact on the barometer)
                if (ExchangeOptions.LimitAmountOfSymbols)
                {
                    foreach (var symbol in symbols.ToList())
                    {
                        if (!symbol.EnoughVolume() && !symbol.IsTrading())
                            symbols.Remove(symbol);
                    }
                }


                int x = symbols.Count;
                while (x > 0)
                {
                    if (Activator.CreateInstance(SubscriptionType, [ExchangeOptions]) is Subscription subscription)
                    {
                        subscription.Name = $"{quoteData.Name}#{groupCount}";
                        subscription.TickerType = TickerType;
                        subscriptions.Add(subscription);
                        x -= ExchangeOptions.SymbolLimitPerSubscription;
                        groupCount++;
                    }
                }

                // Divide the symbols evenly
                x = 0;
                foreach (CryptoSymbol symbol in symbols)
                {
                    var subscription = subscriptions[x];
                    subscription.SymbolList.Add(symbol);
                    subscription.Symbols.Add(symbol.Name);
                    subscription.SymbolByExchangeName[symbol.ExchangeName] = symbol;

                    x++;
                    if (x >= subscriptions.Count)
                        x = 0;
                    symbolCount++;
                }

                // kan gecombineerd worden ^^
                foreach (var subscription in subscriptions)
                {
                    subscriptionList.Add(subscription);
                    subscription.Name += $" ({subscription.SymbolList.Count})";
                    subscription.SymbolOverview = string.Join(',', subscription.Symbols);
                }
            }
        }
        return subscriptionList;
    }

    public virtual async Task StartAsync()
    {
        if (!Enabled)
        {
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} {TickerType} subscriptions are disabled");
            return;
        }

        // Is al gestart
        if (SubscriptionBundleList.Count > 0)
        {
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} {TickerType} is already started");
            return;
        }

        List<string> list = [];
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values.ToList())
        {
            if (quoteData.FetchCandles && quoteData.SymbolList.Count > 0)
            {
                list.Add(quoteData.Name);
            }
        }
        string textQuotes = String.Join(",", list);
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} starting {TickerType} subscriptions for ({textQuotes})");


        // Splits de symbols (user subscription is much simpler)
        int symbolCount = 0;
        List<Subscription> subscriptionList;
        if (TickerType == CryptoTickerType.user)
            subscriptionList = CreateUserSubscription(ref symbolCount);
        else
            subscriptionList = CreateTheSubscriptions(ref symbolCount);


        // Vanwege technische limiet reduceren we het aantal subscriptions per client
        while (subscriptionList.Count > 0)
        {
            SubscriptionBundle bundle = new();
            SubscriptionBundleList.Add(bundle);
            bundle.SocketClient = null; // todo

            // Dit zou ook met een Take() kunnen, maar ach dit werkt ook
            while (subscriptionList.Count > 0 && bundle.SubscriptionList.Count < ExchangeOptions.SubscriptionsPerBundle)
            {
                var subscription = subscriptionList[0];
                subscription.SubscriptionBundle = bundle;
                bundle.SubscriptionList.Add(subscription);
                subscriptionList.Remove(subscription);
            }
        }


        // Maak er taken van
        List<Task> taskList = [];
        foreach (var bundle in SubscriptionBundleList)
        {
            foreach (var subscription in bundle.SubscriptionList)
            {
                Task task = Task.Run(subscription.StartAsync);
                taskList.Add(task);
            }
        }


        string text = "";
        if (taskList.Count != 0)
        {
            await Task.WhenAll(taskList).ConfigureAwait(false);
            if (TickerType != CryptoTickerType.user)
                text = $" for {symbolCount} symbols";
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} started {TickerType} subscriptions{text} over {SubscriptionBundleList.Count} bundles");
        }
        else
        {
            if (TickerType != CryptoTickerType.user)
                text = $" with 0 symbols!";
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} started {TickerType} subscriptions{text}");
        }




        // Herkansing? (de echte vraag is waarom er fouten ontstaan tijdens het opstarten)
        symbolCount = 0;
        taskList.Clear();
        foreach (var bundle in SubscriptionBundleList)
        {
            foreach (var subscription in bundle.SubscriptionList)
            {
                if (subscription.ErrorDuringStartup || subscription.NeedsRestart)
                {
                    Task task = Task.Run(subscription.StartAsync);
                    taskList.Add(task);
                    symbolCount += subscription.SymbolList.Count;
                }
            }
        }
        if (taskList.Count != 0)
        {
            await Task.WhenAll(taskList).ConfigureAwait(false);
            if (TickerType != CryptoTickerType.user)
                text = $" for {symbolCount} symbols";
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} retry - started {TickerType} subscriptions{text} over {SubscriptionBundleList.Count} bundles");
        }
    }


    public virtual async Task StopAsync()
    {
        if (!Enabled)
            return;

        if (SubscriptionBundleList.Count != 0)
        {
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} {TickerType} subscriptions stopping");
            List<Task> taskList = [];
            foreach (var bundle in SubscriptionBundleList)
            {
                foreach (var subscription in bundle.SubscriptionList)
                {
                    Task task = Task.Run(subscription.StopAsync);
                    taskList.Add(task);
                }
            }
            await Task.WhenAll(taskList).ConfigureAwait(false);
            ScannerLog.Logger.Trace($"{ExchangeOptions.ExchangeName} {TickerType} subscriptions stopped");
            SubscriptionBundleList.Clear();
        }
        else
            ScannerLog.Logger.Trace($"{ExchangeOptions.ExchangeName} {TickerType} subscriptions already stopped");
    }


    public virtual void Reset()
    {
        foreach (var bundle in SubscriptionBundleList)
        {
            foreach (var subscription in bundle.SubscriptionList)
            {
                Interlocked.Exchange(ref subscription.TickerCount, 0);
                // Also reset TickerCountLast so NeedsRestart() doesn't false-trigger
                // when the new count reaches the same value as before the reset.
                subscription.TickerCountLast = 0;
            }
        }
    }


    public virtual int Count()
    {
        int tickerCount = 0;
        foreach (var bundle in SubscriptionBundleList)
        {
            foreach (var subscription in bundle.SubscriptionList)
            {
                tickerCount += subscription.TickerCount;
            }
        }
        return tickerCount;
    }

    // A 1m subscription that has not delivered anything for this long is considered dead. The check runs
    // in UTC so a daylight saving switch cannot make a healthy subscription look silent for an hour.
    public static readonly TimeSpan MaximumTickerSilence = TimeSpan.FromMinutes(4);

    public virtual bool NeedsRestart()
    {
        // this get called every 4 or 5 candles, if there was no activity in that period we will schedule a restart

        int count = 0;
        bool restart = false;
        DateTime deadline = GlobalData.Clock.UtcNow - MaximumTickerSilence;

        foreach (var bundle in SubscriptionBundleList)
        {
            foreach (var subscription in bundle.SubscriptionList)
            {
                count++;

                // Also restart subscriptions that lost their connection (flag set by TickerConnectionLost/TickerException)
                if (subscription.NeedsRestart)
                {
                    restart = true;
                    continue;
                }

                // Nothing received for a while? Then restart it. This also covers a subscription that never
                // delivered a single candle, which the TickerCount comparison below cannot detect because it
                // only looks at subscriptions that were already running. Only for the kline subscription: a user subscription
                // can legitimately stay quiet for hours when there is no order activity.
                if (TickerType == CryptoTickerType.kline && subscription.LastActivity < deadline)
                {
                    restart = true;
                    subscription.NeedsRestart = true;
                    ScannerLog.Logger.Trace($"{TickerType} subscription {subscription.Name} silent since {subscription.LastActivity} (utc) {subscription.SymbolOverview}");
                    continue;
                }

                // Is this subscription already receiving data?
                if (subscription.TickerCount != 0)
                {
                    // Is there still activity (otherwise restart it)
                    if (subscription.TickerCount == subscription.TickerCountLast)
                    {
                        restart = true;
                        subscription.NeedsRestart = true;
                    }
                    subscription.TickerCountLast = subscription.TickerCount;
                }
            }
        }

        //if (restart)
        //    GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeSymbol} check for restart {count} {TickerType} subscriptions {restart}");
        return restart;
    }


    public virtual async Task CheckTickers()
    {
        // Only the subscriptions that reported a problem are restarted. They share a socket client per
        // group, but stopping and starting a single subscription does not disturb its neighbours, so
        // there is no reason to interrupt the healthy ones as well.
        List<Subscription> subscriptions = [];
        foreach (var bundle in SubscriptionBundleList)
        {
            foreach (var subscription in bundle.SubscriptionList)
            {
                if (subscription.ConnectionLostCount > 0 || subscription.ErrorDuringStartup || subscription.NeedsRestart)
                    subscriptions.Add(subscription);
            }
        }

        if (subscriptions.Count != 0)
        {

            // Stop de getrande subscriptions
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} herstarten {subscriptions.Count} {TickerType} subscriptions (stopping)");

            List<Task> taskList = [];
            foreach (var subscription in subscriptions)
            {
                Task task = Task.Run(subscription.StopAsync);
                taskList.Add(task);
            }
            await Task.WhenAll(taskList).ConfigureAwait(false);


            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} herstarten {subscriptions.Count} {TickerType} subscriptions (stopped)");


            // Start de getrande subscriptions opnieuw
            taskList.Clear();
            foreach (var subscription in subscriptions)
            {
                // We hergebruiken de group nu (de indeling is ingewikkelder geworden)
                //TickerKLineItemBase tickerNew = (TickerKLineItemBase)Activator.CreateInstance(ExchangeOptions.KLineTickerItemType, [ExchangeOptions]);
                //tickerNew.Symbols = subscription.Symbols;
                Task task = Task.Run(subscription.StartAsync);
                taskList.Add(task);
            }
            await Task.WhenAll(taskList).ConfigureAwait(false);
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} herstarten {subscriptions.Count} {TickerType} subscriptions (finished)");
        }

        // En de applicatie status herstellen (niet 100% zuiver)
        if (GlobalData.ApplicationStatus == Enums.CryptoApplicationStatus.Initializing)
            GlobalData.ApplicationStatus = Enums.CryptoApplicationStatus.Running;

    }

    public void DumpSubscriptionInfo()
    {
        GlobalData.AddTextToLogTab("");
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} Subscription info {TickerType}");

        foreach (var bundle in SubscriptionBundleList)
        {
            foreach (var subscription in bundle.SubscriptionList)
            {
                GlobalData.AddTextToLogTab($"{TickerType} subscription {subscription.Name} " +
                    $"ErrorDuringStartup={subscription.ErrorDuringStartup} " +
                    $"ConnectionLostCount={subscription.ConnectionLostCount} " +
                    $"TickerCount={subscription.TickerCount} " +
                    $"TickerCountLast={subscription.TickerCountLast} " +
                    $"NeedsRestart={subscription.NeedsRestart} " +
                    $"{subscription.SymbolOverview}");
            }
        }
    }

}
