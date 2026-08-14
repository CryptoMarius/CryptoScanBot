using Alpaca.Markets;

using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Exchange.Alpaca.Spot;

/// <summary>
/// Cached answer to "is the stock market trading right now?".
///
/// Every other exchange in the scanner trades around the clock, so a kline subscription that has been
/// quiet for four minutes is a broken subscription (see SubscriptionManager.NeedsRestart). A stock
/// market is closed for about seventeen hours a day and the whole weekend, and without this the
/// scanner would take a healthy stream down and build it up again every four minutes of that.
///
/// The clock endpoint states the moment of the next open and the next close, so the answer stays
/// usable long after the call: as long as neither of those moments has passed, nothing has changed.
/// The refresh runs in the background, because the question is asked from a synchronous check.
/// </summary>
public static class MarketClock
{
    private static readonly SemaphoreSlim RefreshLock = new(1);

    // What the exchange said the last time we asked, and when that was
    private static bool _wasOpen;
    private static DateTime _nextOpenUtc = DateTime.MinValue;
    private static DateTime _nextCloseUtc = DateTime.MinValue;
    private static DateTime _answerUtc = DateTime.MinValue;
    private static DateTime _attemptUtc = DateTime.MinValue;

    // Do not ask more often than this. The moment of the next open or close is what really decides
    // when a new answer is needed; the interval only covers the half days (the exchange closes at
    // 13:00 on some holidays) and the calls that failed.
    private static readonly TimeSpan MinimumInterval = TimeSpan.FromMinutes(5);


    /// <summary>
    /// Is the stock market trading? True as long as nothing is known yet, so an unanswered clock
    /// behaves exactly like the exchanges that have no opening hours at all.
    /// </summary>
    public static bool IsOpen
    {
        get
        {
            DateTime now = DateTime.UtcNow;
            EnsureFresh(now);

            if (_answerUtc == DateTime.MinValue)
                return true;

            // It was open, so it stays open until the close it announced. It was closed, so it stays
            // closed until the open it announced.
            return _wasOpen ? now < _nextCloseUtc : now >= _nextOpenUtc;
        }
    }


    private static void EnsureFresh(DateTime now)
    {
        if (now - _attemptUtc < MinimumInterval)
            return;

        // Nothing changes until the moment the exchange announced, so leave it alone until then
        if (_answerUtc != DateTime.MinValue && (_wasOpen ? now < _nextCloseUtc : now < _nextOpenUtc))
            return;

        _attemptUtc = now;
        _ = Task.Run(RefreshAsync);
    }


    private static async Task RefreshAsync()
    {
        // Whoever is already asking, asks for all of us
        if (!await RefreshLock.WaitAsync(0))
            return;

        try
        {
            if (GlobalData.TradingApi.Key == "")
                return;

            using IAlpacaTradingClient client = Environments.Paper.GetAlpacaTradingClient(
                new SecretKey(GlobalData.TradingApi.Key, GlobalData.TradingApi.Secret));

            LimitRate.WaitForFairWeight(1);
            var clock = await client.GetClockAsync(ExchangeBase.CancellationToken);

            _wasOpen = clock.IsOpen;
            _nextOpenUtc = clock.NextOpenUtc;
            _nextCloseUtc = clock.NextCloseUtc;
            _answerUtc = DateTime.UtcNow;

            ScannerLog.Logger.Trace($"{ExchangeBase.ExchangeOptions.ExchangeName} market clock open={_wasOpen} " +
                $"next open={_nextOpenUtc.ToLocalTime()} next close={_nextCloseUtc.ToLocalTime()}");
        }
        catch (Exception error)
        {
            // Not fatal: without an answer everything behaves as it did before, as if the market never
            // closes. The attempt was already stamped, so this does not turn into a retry storm.
            ScannerLog.Logger.Error(error, "");
        }
        finally
        {
            RefreshLock.Release();
        }
    }
}
