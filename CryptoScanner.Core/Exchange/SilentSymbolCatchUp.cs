using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange;

/// <summary>
/// The inactivity check one level deeper than <see cref="SubscriptionManager.NeedsRestart"/>: per
/// SYMBOL instead of per subscription.
/// <para>
/// A subscription serves up to a hundred symbols, and as long as one of them trades the bundle
/// counts as alive. A single symbol that stops delivering inside a living bundle was therefore never
/// noticed: on Binance Perpetual STORJUSDT fell silent on 26-08-2026 09:01 UTC and stayed that way
/// for 454 minutes, while the fill logic drew a straight line at 3923 and the market moved 4,8%
/// (open point 34). Kucoin Perpetual showed the same fault as a real gap of 480 minutes (point 38).
/// </para>
/// <para>
/// What this does: for every symbol in the kline subscriptions that has delivered nothing real for
/// longer than <see cref="ExchangeOptions.MaximumTickerInactivity"/>, ask the exchange over REST
/// (<see cref="ICandle.GetCandlesForAllIntervalsAsync"/>, the same catch-up the hourly refresh
/// does). Silence is measured from the first invented minute on a cached ticker
/// (<see cref="CryptoSymbolInterval.SynthesizedFrom"/>) or from the close of the newest 1m candle
/// on the others.
/// </para>
/// <para>
/// What keeps it from becoming a REST storm - the risk that kept this on the list for a month: a
/// thin coin can legitimately go hours without a trade (HIP-3 markets on HyperLiquid: 30 to over
/// 300 minutes, see the note at NeedsRestart). So an ask that brings nothing back doubles the wait
/// before the next one, up to <see cref="MaxWait"/>; an ask that did bring real candles resets the
/// wait to the inactivity limit; and a round asks for at most <see cref="MaxPerRound"/> symbols,
/// the rest wait for the next check five minutes later. A symbol that trades again leaves the
/// schedule, so its next outage starts at the short wait. A full connection loss is not handled
/// here at all: a subscription that needs a restart, or lost its connection, is skipped, because
/// then every symbol in it is silent and the restart is the right answer.
/// </para>
/// </summary>
public static class SilentSymbolCatchUp
{
    /// <summary>How many symbols one round asks for at most.</summary>
    public const int MaxPerRound = 10;

    /// <summary>The longest wait between two asks for a symbol that keeps bringing nothing back.</summary>
    public static readonly TimeSpan MaxWait = TimeSpan.FromHours(4);

    // Per symbol name: the moment it may be asked again, and the wait that produced that moment.
    private static readonly Dictionary<string, (DateTime NotBefore, TimeSpan Wait)> Schedule = [];
    private static readonly object ScheduleLock = new();

    /// <summary>How often a symbol was asked for since the start of the session (for the log and the report).</summary>
    public static int AskedCount;

    /// <summary>How often an ask brought real candles back.</summary>
    public static int RepairedCount;


    /// <summary>
    /// Since when the symbol delivered nothing real (UTC), or null when it has no 1m candle yet.
    /// On a cached ticker the flat candles keep the list growing, so the first invented minute is
    /// the answer there; on every other ticker it is the close of the newest 1m candle.
    /// </summary>
    public static DateTime? SilentSince(CryptoSymbol symbol)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
        if (symbolInterval.SynthesizedFrom.HasValue)
            return symbolInterval.SynthesizedFrom.Value.ToDateTime();

        if (!symbolInterval.CandleList.TryGetLastCandle(out CryptoCandle candle))
            return null;
        return (candle.OpenTime + 1u).ToDateTime();
    }


    /// <summary>
    /// The wait before the next ask. Brought something back: the inactivity limit again. Brought
    /// nothing: twice the previous wait (or twice the limit when there was none), capped.
    /// </summary>
    public static TimeSpan NextWait(bool broughtSomething, TimeSpan? previousWait, TimeSpan maxInactivity)
    {
        if (broughtSomething)
            return maxInactivity;

        TimeSpan doubled = TimeSpan.FromTicks(2 * (previousWait ?? maxInactivity).Ticks);
        return doubled > MaxWait ? MaxWait : doubled;
    }


    /// <summary>
    /// The symbols in the kline subscriptions that have been silent longer than
    /// <paramref name="maxInactivity"/>, in subscription order. Subscriptions that are broken as a
    /// whole are left to the restart.
    /// </summary>
    public static List<CryptoSymbol> FindSilent(SubscriptionManager klineTicker, DateTime now, TimeSpan maxInactivity)
    {
        List<CryptoSymbol> silent = [];
        // Over copies, for the same reason NeedsRestart walks copies: the synchronisation may be
        // removing entries on another thread.
        foreach (SubscriptionBundle bundle in klineTicker.SubscriptionBundleList.ToList())
        {
            foreach (Subscription subscription in bundle.SubscriptionList.ToList())
            {
                if (subscription.NeedsRestart || subscription.ConnectionIsLost || !subscription.IsExpectingData)
                    continue;

                foreach (CryptoSymbol symbol in subscription.SymbolList.ToList())
                {
                    DateTime? since = SilentSince(symbol);
                    if (since.HasValue && now - since.Value > maxInactivity)
                        silent.Add(symbol);
                }
            }
        }
        return silent;
    }


    /// <summary>
    /// One round: find the silent symbols, ask the exchange for the ones that are due, and schedule
    /// the next ask per symbol. Called from the data-stream check when no subscription needs a
    /// restart; never during startup, when every symbol is legitimately behind.
    /// </summary>
    public static async Task RunAsync(SubscriptionManager klineTicker, ICandle candleApi, ExchangeOptions options)
    {
        if (GlobalData.ApplicationStatus != CryptoApplicationStatus.Running || GlobalData.IsEmulatorMode)
            return;

        DateTime now = GlobalData.Clock.UtcNow;
        TimeSpan maxInactivity = options.MaximumTickerInactivity;
        List<CryptoSymbol> silent = FindSilent(klineTicker, now, maxInactivity);

        List<CryptoSymbol> due = [];
        int waiting = 0;
        lock (ScheduleLock)
        {
            // A symbol that trades again leaves the schedule, so its next outage starts short.
            HashSet<string> silentNames = [.. silent.Select(s => s.Name)];
            foreach (string name in Schedule.Keys.Where(k => !silentNames.Contains(k)).ToList())
                Schedule.Remove(name);

            foreach (CryptoSymbol symbol in silent)
            {
                if (Schedule.TryGetValue(symbol.Name, out var entry) && entry.NotBefore > now)
                {
                    waiting++;
                    continue;
                }
                if (due.Count >= MaxPerRound)
                {
                    waiting++;
                    continue;
                }
                due.Add(symbol);
            }
        }

        if (due.Count == 0)
            return;

        foreach (CryptoSymbol symbol in due)
        {
            if (ExchangeBase.CancellationToken.IsCancellationRequested)
                return;

            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
            DateTime since = SilentSince(symbol) ?? now;
            // The same two signs CandleBase.GetCandlesForAllIntervalsAsync reads to decide that a gap
            // was filled: an invented candle replaced by a real one, or - without invented candles -
            // the synchronisation pointer having moved. The pointer alone is not proof on a cached
            // ticker, it moves on every catch-up there.
            bool hadSynthesized = symbolInterval.SynthesizedFrom.HasValue;
            int replacedBefore = symbolInterval.SynthesizedReplaced;
            CandleTime? synchronizedBefore = symbolInterval.LastCandleSynchronized;

            try
            {
                await candleApi.GetCandlesForAllIntervalsAsync(symbol);
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, $"SilentSymbolCatchUp {symbol.Name}");
            }

            bool broughtSomething = symbolInterval.SynthesizedReplaced != replacedBefore
                || (!hadSynthesized && symbolInterval.LastCandleSynchronized != synchronizedBefore);

            TimeSpan wait;
            lock (ScheduleLock)
            {
                TimeSpan? previousWait = Schedule.TryGetValue(symbol.Name, out var entry) ? entry.Wait : null;
                wait = NextWait(broughtSomething, previousWait, maxInactivity);
                Schedule[symbol.Name] = (now + wait, wait);
            }

            Interlocked.Increment(ref AskedCount);
            if (broughtSomething)
                Interlocked.Increment(ref RepairedCount);

            double silentMinutes = (now - since).TotalMinutes;
            string outcome = broughtSomething
                ? "real candles came back"
                : $"nothing new, next ask in {wait.TotalMinutes:N0} minutes";
            GlobalData.AddTextToLogTab($"{symbol.Name} silent for {silentMinutes:N0} minutes while its subscription is alive, asked the exchange: {outcome}");
        }

        if (waiting > 0)
            ScannerLog.Logger.Trace($"SilentSymbolCatchUp: asked {due.Count}, {waiting} silent symbol(s) waiting for their turn");
    }
}
