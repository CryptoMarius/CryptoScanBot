using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// The subscription layout used to be decided once at startup and never revisited, so a symbol that
/// passed the volume threshold later never got a websocket subscription and had to lean on the hourly
/// REST catch-up. These tests cover SynchronizeSymbolsAsync, which brings the layout back in line.
///
/// Subscribe/StartAsync/StopAsync are stubbed out - the exchange side is not what is being tested here,
/// the bookkeeping of symbols over subscriptions and bundles is.
/// </summary>
[TestClass]
public class SubscriptionManagerTests
{
    /// <summary>
    /// Stands in for an exchange subscription: counts how often it was started and stopped instead of
    /// talking to a socket.
    /// </summary>
    public class FakeSubscription(ExchangeOptions exchangeOptions) : Subscription(exchangeOptions)
    {
        public int StartCount;
        public int StopCount;

        public override Task<WebSocketResult<UpdateSubscription>?> Subscribe()
            => Task.FromResult<WebSocketResult<UpdateSubscription>?>(null);

        public override Task StartAsync()
        {
            StartCount++;
            return Task.CompletedTask;
        }

        public override Task StopAsync()
        {
            StopCount++;
            return Task.CompletedTask;
        }
    }


    private const string Quote = "TSTQ";

    private static CryptoQuoteData PrepareQuote()
    {
        // A quote of its own, so the test cannot be disturbed by whatever the settings contain
        CryptoQuoteData quoteData = new()
        {
            Name = Quote,
            FetchCandles = true,
            MinimalVolume = 0, // every symbol has enough volume unless the test says otherwise
        };
        GlobalData.Settings.QuoteCoins[Quote] = quoteData;
        return quoteData;
    }

    private static CryptoSymbol AddSymbol(CryptoQuoteData quoteData, string baseAsset, double volume = 1000)
    {
        CryptoSymbol symbol = new()
        {
            Exchange = GlobalData.ActiveExchange!,
            Name = baseAsset + Quote,
            ExchangeName = $"{baseAsset}-{Quote}",
            Base = baseAsset,
            Quote = Quote,
            QuoteData = quoteData,
            Status = 1,
            Volume = volume,
        };
        quoteData.SymbolList.Add(symbol);
        return symbol;
    }

    private static SubscriptionManager CreateManager(int symbolsPerSubscription, int subscriptionsPerBundle = 10)
    {
        ExchangeOptions options = new() { ExchangeName = "TestExchange" };
        options.SetDefaultOptions("TestExchange", Quote, 500, true, symbolsPerSubscription, subscriptionsPerBundle);
        return new SubscriptionManager(options, typeof(FakeSubscription), CryptoTickerType.kline);
    }

    private static List<FakeSubscription> AllSubscriptions(SubscriptionManager manager)
    {
        List<FakeSubscription> result = [];
        foreach (var bundle in manager.SubscriptionBundleList)
        {
            foreach (var subscription in bundle.SubscriptionList)
                result.Add((FakeSubscription)subscription);
        }
        return result;
    }

    private static List<string> AllSymbolNames(SubscriptionManager manager)
    {
        List<string> result = [];
        foreach (var subscription in AllSubscriptions(manager))
        {
            foreach (var symbol in subscription.SymbolList)
                result.Add(symbol.Name);
        }
        result.Sort();
        return result;
    }

    [TestInitialize]
    public void Init() => TestBase.InitTestSession();

    [TestCleanup]
    public void Cleanup()
    {
        GlobalData.Settings.QuoteCoins.Remove(Quote);
    }


    [TestMethod]
    public async Task NewSymbolGetsASubscription()
    {
        CryptoQuoteData quoteData = PrepareQuote();
        AddSymbol(quoteData, "AAA");
        AddSymbol(quoteData, "BBB");

        SubscriptionManager manager = CreateManager(symbolsPerSubscription: 10);
        await manager.StartAsync();

        CollectionAssert.AreEqual(new List<string> { "AAATSTQ", "BBBTSTQ" }, AllSymbolNames(manager));

        // A coin passes the volume threshold an hour later
        AddSymbol(quoteData, "CCC");
        await manager.SynchronizeSymbolsAsync();

        CollectionAssert.AreEqual(new List<string> { "AAATSTQ", "BBBTSTQ", "CCCTSTQ" }, AllSymbolNames(manager),
            "the symbol that was added later should be part of a subscription");
        Assert.AreEqual(1, manager.SubscriptionBundleList.Count, "it fits in the existing subscription");
        Assert.AreEqual(2, AllSubscriptions(manager)[0].StartCount, "the subscription is resubscribed once");
    }


    [TestMethod]
    public async Task DroppedSymbolIsRemoved()
    {
        CryptoQuoteData quoteData = PrepareQuote();
        AddSymbol(quoteData, "AAA", volume: 100000);
        CryptoSymbol bbb = AddSymbol(quoteData, "BBB");

        SubscriptionManager manager = CreateManager(symbolsPerSubscription: 10);
        await manager.StartAsync();

        // Volume of BBB drops below the threshold, AAA stays well above it
        quoteData.MinimalVolume = 5000;
        bbb.VolumeAboveThreshold = false;

        await manager.SynchronizeSymbolsAsync();

        CollectionAssert.AreEqual(new List<string> { "AAATSTQ" }, AllSymbolNames(manager));
    }


    [TestMethod]
    public async Task NothingChangedMeansNoResubscribe()
    {
        CryptoQuoteData quoteData = PrepareQuote();
        AddSymbol(quoteData, "AAA");

        SubscriptionManager manager = CreateManager(symbolsPerSubscription: 10);
        await manager.StartAsync();
        int startCount = AllSubscriptions(manager)[0].StartCount;

        await manager.SynchronizeSymbolsAsync();

        Assert.AreEqual(startCount, AllSubscriptions(manager)[0].StartCount,
            "an unchanged symbol list must not disturb a running subscription");
    }


    [TestMethod]
    public async Task FullSubscriptionGetsAnExtraOne()
    {
        CryptoQuoteData quoteData = PrepareQuote();
        AddSymbol(quoteData, "AAA");
        AddSymbol(quoteData, "BBB");

        // Two symbols, two per subscription: full after the initial layout
        SubscriptionManager manager = CreateManager(symbolsPerSubscription: 2);
        await manager.StartAsync();
        Assert.AreEqual(1, AllSubscriptions(manager).Count);

        AddSymbol(quoteData, "CCC");
        await manager.SynchronizeSymbolsAsync();

        Assert.AreEqual(2, AllSubscriptions(manager).Count, "a second subscription is needed");
        CollectionAssert.AreEqual(new List<string> { "AAATSTQ", "BBBTSTQ", "CCCTSTQ" }, AllSymbolNames(manager));
    }


    [TestMethod]
    public async Task EmptiedSubscriptionAndBundleAreDropped()
    {
        CryptoQuoteData quoteData = PrepareQuote();
        AddSymbol(quoteData, "AAA", volume: 100000);
        CryptoSymbol bbb = AddSymbol(quoteData, "BBB");

        // One symbol per subscription, one subscription per bundle: two bundles
        SubscriptionManager manager = CreateManager(symbolsPerSubscription: 1, subscriptionsPerBundle: 1);
        await manager.StartAsync();
        Assert.AreEqual(2, manager.SubscriptionBundleList.Count);

        quoteData.MinimalVolume = 5000;
        bbb.Volume = 10;
        bbb.VolumeAboveThreshold = false;
        await manager.SynchronizeSymbolsAsync();

        Assert.AreEqual(1, manager.SubscriptionBundleList.Count, "the emptied bundle should be gone");
        CollectionAssert.AreEqual(new List<string> { "AAATSTQ" }, AllSymbolNames(manager));
    }


    [TestMethod]
    public async Task NameKeepsTrackOfTheSymbolCount()
    {
        CryptoQuoteData quoteData = PrepareQuote();
        AddSymbol(quoteData, "AAA");

        SubscriptionManager manager = CreateManager(symbolsPerSubscription: 10);
        await manager.StartAsync();
        Assert.IsTrue(AllSubscriptions(manager)[0].Name.EndsWith("(1)"), "one symbol");

        AddSymbol(quoteData, "BBB");
        await manager.SynchronizeSymbolsAsync();

        Assert.IsTrue(AllSubscriptions(manager)[0].Name.EndsWith("(2)"),
            $"the name should follow the symbol count, but is {AllSubscriptions(manager)[0].Name}");
    }


    [TestMethod]
    public async Task LookupFollowsTheAddedSymbol()
    {
        CryptoQuoteData quoteData = PrepareQuote();
        AddSymbol(quoteData, "AAA");

        SubscriptionManager manager = CreateManager(symbolsPerSubscription: 10);
        await manager.StartAsync();

        AddSymbol(quoteData, "BBB");
        await manager.SynchronizeSymbolsAsync();

        // The socket callback resolves an incoming update through this lookup
        var subscription = AllSubscriptions(manager)[0];
        Assert.IsTrue(subscription.SymbolByExchangeName.ContainsKey($"BBB-{Quote}"),
            "an update for the added symbol would be dropped without this entry");
        Assert.AreEqual(subscription.SymbolList.Count, subscription.SymbolByExchangeName.Count);
        Assert.AreEqual(subscription.SymbolList.Count, subscription.ExchangeNames.Count);
    }
}
