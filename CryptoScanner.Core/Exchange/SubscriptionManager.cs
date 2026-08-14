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
            subscription.BaseName = "*";
            subscription.TickerType = TickerType;
            subscriptionList.Add(subscription);
        }

        return subscriptionList;
    }


    // Running number for the subscription names, so a subscription added later never reuses the name
    // of one that was removed earlier - that would make the log very hard to follow.
    private int _nameCounter = 0;

    private Subscription CreateSubscription(string quoteName)
    {
        if (Activator.CreateInstance(SubscriptionType, [ExchangeOptions]) is not Subscription subscription)
            throw new InvalidOperationException($"Could not create a {SubscriptionType.Name}");

        subscription.BaseName = $"{quoteName}#{_nameCounter++}";
        subscription.TickerType = TickerType;
        return subscription;
    }


    /// <summary>
    /// The symbols that should be subscribed to right now, per quote. Both the initial layout and the
    /// later synchronisation read from here, so they can never disagree about who belongs.
    /// </summary>
    private List<(CryptoQuoteData QuoteData, List<CryptoSymbol> Symbols)> GetWantedSymbolsPerQuote()
    {
        List<(CryptoQuoteData, List<CryptoSymbol>)> result = [];
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values.ToList())
        {
            if (!quoteData.FetchCandles || quoteData.SymbolList.Count == 0)
                continue;

            List<CryptoSymbol> symbols = [];
            foreach (CryptoSymbol symbol in quoteData.SymbolList.ToList())
            {
                // Limit the amount of symbols (this has impact on the barometer)
                if (ExchangeOptions.LimitAmountOfSymbols && !symbol.EnoughVolume() && !symbol.IsTrading())
                    continue;
                symbols.Add(symbol);
            }

            if (symbols.Count > 0)
                result.Add((quoteData, symbols));
        }
        return result;
    }


    /// <summary>
    /// Prepare the kline subscription groups
    /// </summary>
    private List<Subscription> CreateTheSubscriptions(ref int symbolCount)
    {
        // Splits de symbols
        symbolCount = 0;
        List<Subscription> subscriptionList = [];
        foreach (var (quoteData, symbols) in GetWantedSymbolsPerQuote())
        {
            List<Subscription> subscriptions = [];

            int x = symbols.Count;
            while (x > 0)
            {
                subscriptions.Add(CreateSubscription(quoteData.Name));
                x -= ExchangeOptions.SymbolLimitPerSubscription;
            }

            // Divide the symbols evenly
            List<List<CryptoSymbol>> buckets = [];
            foreach (var _ in subscriptions)
                buckets.Add([]);

            x = 0;
            foreach (CryptoSymbol symbol in symbols)
            {
                buckets[x].Add(symbol);

                x++;
                if (x >= subscriptions.Count)
                    x = 0;
                symbolCount++;
            }

            // kan gecombineerd worden ^^
            for (int i = 0; i < subscriptions.Count; i++)
            {
                subscriptions[i].SetSymbols(buckets[i]);
                subscriptionList.Add(subscriptions[i]);
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


    public virtual async Task CheckSubscriptions()
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

    /// <summary>
    /// Bring the subscribed symbols back in line with the ones that qualify right now. The layout is
    /// decided once in StartAsync, but the symbol list moves: a coin can pass the volume threshold hours
    /// later, or drop below it. Without this, such a symbol never got a subscription and had to lean on
    /// the hourly REST catch-up for its 1m candles - which hit exactly the coins that just became
    /// interesting because their volume spiked.
    ///
    /// Call it after the volumes have been refreshed and the candles fetched, so a symbol that is added
    /// already has its history. Does nothing at all when nothing changed.
    /// </summary>
    public virtual async Task SynchronizeSymbolsAsync()
    {
        if (!Enabled || TickerType == CryptoTickerType.user || SubscriptionBundleList.Count == 0)
            return;

        // What we serve now, and what we should be serving
        Dictionary<string, Subscription> currentBySymbol = [];
        foreach (var bundle in SubscriptionBundleList)
        {
            foreach (var subscription in bundle.SubscriptionList)
            {
                foreach (var symbol in subscription.SymbolList)
                    currentBySymbol[symbol.Name] = subscription;
            }
        }

        List<CryptoSymbol> added = [];
        List<CryptoSymbol> removed = [];
        HashSet<string> wantedNames = [];
        // Subscriptions that need to be resubscribed because their symbol set changed
        Dictionary<Subscription, List<CryptoSymbol>> newContent = [];

        foreach (var (quoteData, symbols) in GetWantedSymbolsPerQuote())
        {
            foreach (CryptoSymbol symbol in symbols)
            {
                wantedNames.Add(symbol.Name);
                if (!currentBySymbol.ContainsKey(symbol.Name))
                    added.Add(symbol);
            }
        }

        foreach (var entry in currentBySymbol)
        {
            if (!wantedNames.Contains(entry.Key))
            {
                CryptoSymbol? symbol = entry.Value.SymbolList.Find(x => x.Name == entry.Key);
                if (symbol != null)
                    removed.Add(symbol);
            }
        }

        if (added.Count == 0 && removed.Count == 0)
            return;

        // Take the symbols out first, that frees room for the new ones
        foreach (CryptoSymbol symbol in removed)
        {
            Subscription subscription = currentBySymbol[symbol.Name];
            List<CryptoSymbol> content = ContentOf(newContent, subscription);
            content.RemoveAll(x => x.Name == symbol.Name);
        }

        foreach (CryptoSymbol symbol in added)
        {
            Subscription? target = FindSubscriptionWithRoom(symbol, newContent);
            if (target == null)
            {
                // Everything is full, add a subscription and place it in a bundle with room
                target = CreateSubscription(symbol.QuoteData.Name);
                SubscriptionBundle bundle = SubscriptionBundleList.Find(x => x.SubscriptionList.Count < ExchangeOptions.SubscriptionsPerBundle)
                    ?? AddBundle();
                target.SubscriptionBundle = bundle;
                bundle.SubscriptionList.Add(target);
            }
            ContentOf(newContent, target).Add(symbol);
        }

        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} {TickerType} symbols changed: " +
            $"{added.Count} added, {removed.Count} removed, {newContent.Count} subscriptions affected");
        if (added.Count > 0)
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} added: {string.Join(',', added.Select(x => x.Name))}");
        if (removed.Count > 0)
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} removed: {string.Join(',', removed.Select(x => x.Name))}");

        // Stop, rewrite, start again. In that order: the exchange still knows the old symbol set until
        // we resubscribe, and the socket callback must not read the lookup while it is being replaced.
        List<Task> taskList = [];
        foreach (var entry in newContent)
        {
            taskList.Add(Task.Run(entry.Key.StopAsync));
        }
        await Task.WhenAll(taskList).ConfigureAwait(false);

        List<Subscription> emptied = [];
        foreach (var entry in newContent)
        {
            entry.Key.SetSymbols(entry.Value);
            if (entry.Value.Count == 0)
                emptied.Add(entry.Key);
        }

        // Drop the subscriptions that have no symbols left, and the bundles that ran out of subscriptions
        foreach (Subscription subscription in emptied)
        {
            subscription.SubscriptionBundle?.SubscriptionList.Remove(subscription);
            subscription.SubscriptionBundle = null;
        }
        SubscriptionBundleList.RemoveAll(x => x.SubscriptionList.Count == 0);

        taskList.Clear();
        foreach (var entry in newContent)
        {
            if (entry.Value.Count > 0)
                taskList.Add(Task.Run(entry.Key.StartAsync));
        }
        await Task.WhenAll(taskList).ConfigureAwait(false);

        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} {TickerType} now serving " +
            $"{currentBySymbol.Count - removed.Count + added.Count} symbols over {SubscriptionBundleList.Count} bundles");
    }


    /// <summary>
    /// The symbols a subscription will serve after this synchronisation, starting from what it serves now.
    /// </summary>
    private static List<CryptoSymbol> ContentOf(Dictionary<Subscription, List<CryptoSymbol>> newContent, Subscription subscription)
    {
        if (!newContent.TryGetValue(subscription, out List<CryptoSymbol>? content))
        {
            content = [.. subscription.SymbolList];
            newContent[subscription] = content;
        }
        return content;
    }


    /// <summary>
    /// An existing subscription of the same quote that still has room for one more symbol.
    /// </summary>
    private Subscription? FindSubscriptionWithRoom(CryptoSymbol symbol, Dictionary<Subscription, List<CryptoSymbol>> newContent)
    {
        foreach (var bundle in SubscriptionBundleList)
        {
            foreach (var subscription in bundle.SubscriptionList)
            {
                if (!subscription.BaseName.StartsWith(symbol.QuoteData.Name + "#", StringComparison.Ordinal))
                    continue;

                int count = newContent.TryGetValue(subscription, out List<CryptoSymbol>? content)
                    ? content.Count
                    : subscription.SymbolList.Count;
                if (count < ExchangeOptions.SymbolLimitPerSubscription)
                    return subscription;
            }
        }
        return null;
    }


    private SubscriptionBundle AddBundle()
    {
        SubscriptionBundle bundle = new();
        SubscriptionBundleList.Add(bundle);
        return bundle;
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
