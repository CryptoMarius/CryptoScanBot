using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Services;

namespace CryptoScanner.Core.Exchange;

/// <summary>
/// One subscription at the exchange, serving the symbols in <see cref="SymbolList"/>. Lives inside a
/// <see cref="SubscriptionBundle"/> together with the other subscriptions that share its socket client.
/// The exchange specific classes only implement <see cref="Subscribe"/> and the callback.
/// </summary>
public abstract class Subscription(ExchangeOptions exchangeOptions)
{
    internal ExchangeOptions ExchangeOptions = exchangeOptions;

    public int TickerCount = 0;
    public int TickerCountLast = 0;

    // Moment the subscription last delivered something, stored as UTC ticks. Kept in UTC so a daylight
    // saving switch cannot make the age of the last update jump an hour in either direction.
    private long _lastActivityTicks = 0;

    /// <summary>
    /// Moment (UTC) the subscription last delivered data, or the moment it was started when nothing
    /// was received yet. DateTime.MinValue as long as the subscription has never been started.
    /// </summary>
    public DateTime LastActivity => new(Interlocked.Read(ref _lastActivityTicks), DateTimeKind.Utc);

    protected void MarkActivity()
    {
        Interlocked.Exchange(ref _lastActivityTicks, GlobalData.Clock.UtcNow.Ticks);
    }

    protected void IncrementTickerCount()
    {
        if (Interlocked.Increment(ref TickerCount) > 999999999)
            Interlocked.Exchange(ref TickerCount, 0);
        MarkActivity();
    }

    public bool NeedsRestart = false;

    /// <summary>
    /// How often this subscription lost its connection since it was started. A statistic for the log
    /// and <see cref="DumpSubscriptionInfo"/>; it says nothing about the state right now, because the
    /// package reconnects and resubscribes on its own. Use <see cref="ConnectionIsLost"/> for that.
    /// </summary>
    public int ConnectionLostCount = 0;

    /// <summary>
    /// Is the connection down at this moment? Set when the connection is lost, cleared when it is
    /// restored - the package only raises that event after the resubscribe succeeded, a failed one
    /// arrives as <see cref="TickerResubscribingFailed"/> instead.
    ///
    /// CheckSubscriptions used to look at ConnectionLostCount, which only ever grows until the next
    /// restart. That made every refresh cycle tear down and rebuild a subscription that had been
    /// healthy again for an hour, and for the cached tickers a rebuild throws away the 1m candle that
    /// is being filled - the minute is then stored as a flat candle instead of the real one.
    /// </summary>
    public bool ConnectionIsLost = false;

    public bool ErrorDuringStartup = false;

    // Fixed part of the name, the quote plus a sequence number ("USDT#0"). The SubscriptionBundle it
    // lives in has no name of its own.
    public string BaseName = "";

    /// <summary>
    /// Name of this subscription including the number of symbols it currently serves ("USDT#0 (50)").
    /// Derived instead of stored so it stays correct after symbols are added or removed.
    /// </summary>
    public string Name => SymbolList.Count > 0 ? $"{BaseName} ({SymbolList.Count})" : BaseName;

    internal CryptoTickerType TickerType;
    public SubscriptionBundle? SubscriptionBundle;
    internal UpdateSubscription? _subscription;

    // Deze worden niet gebruikt bij de userticker
    // Symbols  = the scanner names ("BTCUSDT"), only used to build SymbolOverview for the log
    // SymbolList = the symbols this subscription serves, Subscribe() takes their ExchangeName
    public List<string> Symbols = [];
    public List<CryptoSymbol> SymbolList = [];
    public string SymbolOverview = "";

    /// <summary>
    /// Replace the set of symbols this subscription serves. Only call it while the subscription is
    /// stopped: the socket callback reads SymbolByExchangeName, and the exchange still knows the old
    /// set until we subscribe again. The lookup is swapped as a whole for that same reason.
    /// </summary>
    public void SetSymbols(List<CryptoSymbol> symbols)
    {
        Dictionary<string, CryptoSymbol> lookup = [];
        List<string> names = [];
        foreach (var symbol in symbols)
        {
            lookup[symbol.ExchangeName] = symbol;
            names.Add(symbol.Name);
        }

        SymbolList = symbols;
        Symbols = names;
        SymbolByExchangeName = lookup;
        SymbolOverview = string.Join(',', names);
    }

    // Lookup from exchange symbol name to CryptoSymbol — avoids the global exchange-dictionary
    // lookup in socket callbacks. Filled in SubscriptionManager.CreateTheSubscriptions right next to
    // SymbolList and Symbols, so the three always describe the same set. Whoever changes SymbolList afterwards has
    // to update this one as well, otherwise updates for the added symbols are silently dropped in
    // the callback. Replace it as a whole in that case, a Dictionary cannot be written to while a
    // socket callback is reading it.
    public Dictionary<string, CryptoSymbol> SymbolByExchangeName = [];

    /// <summary>
    /// Is this subscription supposed to be delivering data right now? True for every exchange that
    /// trades around the clock, which is all of them except the stock broker: that one is closed at
    /// night, in the weekend and on holidays. The silence check in
    /// <see cref="SubscriptionManager.NeedsRestart"/> leaves a subscription that says no alone,
    /// because restarting it every four minutes only means reconnecting for nothing.
    /// </summary>
    public virtual bool IsExpectingData => true;

    public abstract Task<WebSocketResult<UpdateSubscription>?> Subscribe();


    public virtual async Task StartAsync()
    {
        if (_subscription != null)
        {
            ScannerLog.Logger.Trace($"{TickerType} subscription {Name} already started");
            return;
        }

        NeedsRestart = false;
        ConnectionLostCount = 0;
        ConnectionIsLost = false;
        ErrorDuringStartup = false;
        ScannerLog.Logger.Trace($"{TickerType} subscription {Name} starting");

        // Give the subscription a fresh starting point, otherwise the silence check would fire
        // immediately on a ticker that has not had the chance to receive anything yet.
        MarkActivity();

        var subscriptionResult = await Subscribe();
        if (subscriptionResult is not null && subscriptionResult.Success)
        {
            _subscription = subscriptionResult.Data;
            _subscription.Exception += TickerException;
            _subscription.ConnectionLost += TickerConnectionLost;
            _subscription.ConnectionRestored += TickerConnectionRestored;
            _subscription.ResubscribingFailed += TickerResubscribingFailed;
            ScannerLog.Logger.Trace($"{TickerType} subscription {Name} started");
        }
        else
        {
            if (_subscription != null)
            {
                _subscription.Exception -= TickerException;
                _subscription.ConnectionLost -= TickerConnectionLost;
                _subscription.ConnectionRestored -= TickerConnectionRestored;
                _subscription.ResubscribingFailed -= TickerResubscribingFailed;
                _subscription = null;
            }

            // todo, nakijken!
            //socketClient.Dispose();
            //socketClient = null;

            ConnectionLostCount++;
            NeedsRestart = true;
            ErrorDuringStartup = true;

            ScannerLog.Logger.Trace($"{TickerType} subscription {Name} error {subscriptionResult?.Error?.Message} {SymbolOverview}");
            GlobalData.AddErrorToLogTab($"{TickerType} subscription {Name} error {subscriptionResult?.Error?.Message} {SymbolOverview}");
        }
    }


    public virtual async Task StopAsync()
    {
        if (_subscription == null)
        {
            ScannerLog.Logger.Trace($"{TickerType} subscription {Name} already stopped");
            return;
        }

        ScannerLog.Logger.Trace($"{TickerType} subscription {Name} stopping");
        _subscription.Exception -= TickerException;
        _subscription.ConnectionLost -= TickerConnectionLost;
        _subscription.ConnectionRestored -= TickerConnectionRestored;
        _subscription.ResubscribingFailed -= TickerResubscribingFailed;

        if (SubscriptionBundle!.SocketClient is not null)
            await SubscriptionBundle.SocketClient.UnsubscribeAsync(_subscription);

        _subscription = null;

        //SubscriptionBundle.SocketClient?.Dispose();
        //SubscriptionBundle.SocketClient = null;
        ScannerLog.Logger.Trace($"{TickerType} subscription {Name} stopped");
    }


    internal void TickerConnectionLost()
    {
        ConnectionLostCount++;
        ConnectionIsLost = true;
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} {TickerType} subscription {Name} connection lost");
        IScannerSession _scannerSession = GlobalData.GetService<IScannerSession>()
            ?? throw new InvalidOperationException("IScannerSession not registered in services");
        _scannerSession.ConnectionWasLost("");
    }


    internal void TickerConnectionRestored(TimeSpan timeSpan)
    {
        ConnectionIsLost = false;
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} {TickerType} subscription {Name} connection restored after {timeSpan.TotalSeconds:N0}s");
        IScannerSession _scannerSession = GlobalData.GetService<IScannerSession>()
            ?? throw new InvalidOperationException("IScannerSession not registered in services");
        _scannerSession.ConnectionWasRestored("", timeSpan);
    }


    /// <summary>
    /// The socket came back but the package could not resubscribe on it, so nothing is being delivered
    /// even though the connection itself is up. Only a restart of this subscription fixes that.
    /// </summary>
    internal void TickerResubscribingFailed(Error error)
    {
        NeedsRestart = true;
        GlobalData.AddErrorToLogTab($"{ExchangeOptions.ExchangeName} {TickerType} subscription {Name} resubscribing failed {error}");
    }


    internal void TickerException(Exception ex)
    {
        GlobalData.AddErrorToLogTab($"{ExchangeOptions.ExchangeName} {TickerType} subscription {Name} connection error {ex.Message} | Stack trace: {ex.StackTrace}");
    }

}