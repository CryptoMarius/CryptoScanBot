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
    public int ConnectionLostCount = 0;
    public bool ErrorDuringStartup = false;

    // Name of this subscription, for example "USDT#0 (50)" - the quote, a sequence number and the
    // number of symbols it serves. The SubscriptionBundle it lives in has no name of its own.
    public string Name = "";
    internal CryptoTickerType TickerType;
    public SubscriptionBundle? SubscriptionBundle;
    internal UpdateSubscription? _subscription;

    // Deze worden niet gebruikt bij de userticker
    // Symbols  = the scanner names ("BTCUSDT"), only used to build SymbolOverview for the log
    // SymbolList = the symbols this subscription serves, Subscribe() takes their ExchangeName
    public List<string> Symbols = [];
    public List<CryptoSymbol> SymbolList = [];
    public string SymbolOverview = "";

    // Lookup from exchange symbol name to CryptoSymbol — avoids the global exchange-dictionary
    // lookup in socket callbacks. Filled in SubscriptionManager.CreateTheSubscriptions right next to
    // SymbolList and Symbols, so the three always describe the same set. Whoever changes SymbolList afterwards has
    // to update this one as well, otherwise updates for the added symbols are silently dropped in
    // the callback. Replace it as a whole in that case, a Dictionary cannot be written to while a
    // socket callback is reading it.
    public Dictionary<string, CryptoSymbol> SymbolByExchangeName = [];

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
            ScannerLog.Logger.Trace($"{TickerType} subscription {Name} started");
        }
        else
        {
            if (_subscription != null)
            {
                _subscription.Exception -= TickerException;
                _subscription.ConnectionLost -= TickerConnectionLost;
                _subscription.ConnectionRestored -= TickerConnectionRestored;
                _subscription = null;
            }

            // todo, nakijken!
            //socketClient.Dispose();
            //socketClient = null;

            ConnectionLostCount++;
            NeedsRestart = true;
            ErrorDuringStartup = true;

            ScannerLog.Logger.Trace($"{TickerType} subscription {Name} error {subscriptionResult?.Error?.Message} {SymbolOverview}");
            GlobalData.AddTextToLogTab($"{TickerType} subscription {Name} error {subscriptionResult?.Error?.Message} {SymbolOverview}");
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
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} {TickerType} subscription {Name} connection lost");
        IScannerSession _scannerSession = GlobalData.GetService<IScannerSession>()
            ?? throw new InvalidOperationException("IScannerSession not registered in services");
        _scannerSession.ConnectionWasLost("");
    }


    internal void TickerConnectionRestored(TimeSpan timeSpan)
    {
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} {TickerType} subscription {Name} connection restored");
        IScannerSession _scannerSession = GlobalData.GetService<IScannerSession>()
            ?? throw new InvalidOperationException("IScannerSession not registered in services");
        _scannerSession.ConnectionWasRestored("");
    }


    internal void TickerException(Exception ex)
    {
        GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} {TickerType} subscription {Name} connection error {ex.Message} | Stack trace: {ex.StackTrace}");
    }

}