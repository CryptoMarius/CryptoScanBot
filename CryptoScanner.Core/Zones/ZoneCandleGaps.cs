using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Zones;

/// <summary>
/// What one zone walk found where it expected candles.
/// <para>
/// Every zone loop reads its candles by key - <c>CandleList.TryGetValue(loop, out candle)</c> - and
/// until 24-08-2026 a key that was not in memory simply fell through the <c>if</c>. A missing candle
/// was therefore indistinguishable from a candle in which nothing happened: a zone was not broken, a
/// pivot kept the width it had, and no line in the log said so. That did not matter while the zone
/// engine pulled the COMPLETE series into memory whatever window it had asked for; since the read is
/// bounded to the window (see <see cref="ZoneCandleWindows"/>) a walk that leaves that window finds
/// nothing there, and the answer changes without a word.
/// </para>
/// <para>
/// This counts the holes as the existing loop walks - no second pass, one increment per key - so the
/// loop keeps costing what it costs. Feed <see cref="Hit"/> where the candle was found and
/// <see cref="Miss"/> where it was not, then hand the result to
/// <see cref="ZoneCandleGaps.Report"/>.
/// </para>
/// </summary>
public struct CandleGapWalk
{
    /// <summary>Keys that were in memory.</summary>
    public int Present { get; private set; }

    /// <summary>Keys that were not, and were therefore read as "nothing happened".</summary>
    public int Missing { get; private set; }

    /// <summary>The longest run of consecutive missing keys.</summary>
    public int LongestGap { get; private set; }

    /// <summary>The first key that was missing; only meaningful when <see cref="Missing"/> &gt; 0.</summary>
    public CandleTime FirstMissing { get; private set; }

    private int currentGap;


    public void Hit()
    {
        Present++;
        currentGap = 0;
    }


    public void Miss(CandleTime key)
    {
        if (Missing == 0)
            FirstMissing = key;
        Missing++;
        currentGap++;
        if (currentGap > LongestGap)
            LongestGap = currentGap;
    }


    /// <summary>
    /// True when the hole is longer than a candle the exchange simply never published, so the walk
    /// stepped over an interruption rather than over a quiet minute. See
    /// <see cref="ZoneCandleGaps.ToleratedGap"/>.
    /// </summary>
    public readonly bool Interrupted => LongestGap > ZoneCandleGaps.ToleratedGap;
}


/// <summary>
/// One place that decides what a hole in a zone walk means and what to do about it.
/// <para>
/// Two different causes end in the same silent skip, and they need different answers:
/// </para>
/// <list type="number">
///   <item>
///     The walk reaches OUTSIDE the window this calculation loaded. <c>CheckAndMarkBrokenZones</c>
///     starts at the oldest OPEN zone and an open zone deliberately never ages out, so that start
///     can sit far before the 500-candle window. Here the candles do exist, they were only not
///     asked for - <see cref="EnsureHistoryLoadedAsync"/> asks for them, but only when the stretch
///     is long enough to be worth a database read.
///   </item>
///   <item>
///     The walk stays inside the window and the candles are not there either - candles.db is short,
///     or the exchange never published them. Nothing can be repaired from here; what matters is
///     that the run says so instead of quietly returning a different zone set.
///   </item>
/// </list>
/// </summary>
public static class ZoneCandleGaps
{
    /// <summary>
    /// A run of missing candles this short is accepted without a word. An exchange that publishes
    /// nothing for a minute without trades leaves exactly this kind of hole, asking for it again
    /// returns the same nothing, and one candle cannot move a zone boundary far enough to matter.
    /// Anything longer is an interruption: there the walk would conclude "nothing happened" over a
    /// stretch in which plenty happened, and that is what changes which zones survive.
    /// </summary>
    public const int ToleratedGap = 2;

    /// <summary>The worst gap already reported per symbol/interval/site, so a run cannot flood the log.</summary>
    private static readonly Dictionary<string, int> reportedGaps = [];

    /// <summary>Stretches already asked for, so a hole candles.db does not have is not re-read every pass.</summary>
    private static readonly HashSet<string> attemptedLoads = [];

    private static readonly object gapLock = new();


    /// <summary>Start of a new run: forget what was reported so a second run reports again.</summary>
    public static void Reset()
    {
        lock (gapLock)
        {
            reportedGaps.Clear();
            attemptedLoads.Clear();
        }
    }


    /// <summary>
    /// Record what <paramref name="walk"/> stepped over, and say so in the log when it stepped over
    /// an interruption. <paramref name="site"/> names the walk ("pivots", "broken", "zoom", ...) so
    /// the line points at the loop and not just at the symbol.
    /// </summary>
    public static void Report(CryptoSymbol? symbol, CryptoInterval interval, string site,
        in CandleGapWalk walk, CandleTime from, CandleTime to)
    {
        if (walk.Missing == 0)
            return;

        PipelineProfiler.RecordZoneCandleGap(walk.Missing, walk.LongestGap, walk.Interrupted);

        // A quiet candle or two: counted, not announced. The counters in the run summary still show
        // it, so "no lines in the log" never means "no holes at all".
        if (!walk.Interrupted)
            return;

        string key = $"{symbol?.Name}|{interval.Name}|{site}";
        lock (gapLock)
        {
            // Only when this walk got worse than what was already said about it. A symbol with one
            // permanent hole in its history would otherwise repeat the same line every recalculation.
            if (reportedGaps.TryGetValue(key, out int worst) && worst >= walk.LongestGap)
                return;
            reportedGaps[key] = walk.LongestGap;
        }

        GlobalData.AddTextToLogTab(
            $"ZONE GAP {symbol?.Name} {interval.Name} {site}: {walk.Missing} of " +
            $"{walk.Missing + walk.Present} candle(s) not in memory over " +
            $"{from.ToLocalTime():yyyy-MM-dd HH:mm} .. {to.ToLocalTime():yyyy-MM-dd HH:mm}, " +
            $"longest run {walk.LongestGap}, first at {walk.FirstMissing.ToLocalTime():yyyy-MM-dd HH:mm}. " +
            $"Those candles were read as 'nothing happened'.");
    }


    /// <summary>
    /// Make sure the candles from <paramref name="neededFrom"/> onwards are in memory before a walk
    /// starts there, and answer whether anything had to be read for it.
    /// <para>
    /// Deliberately NOT "fetch whatever is missing": going back to the exchange for every hole is
    /// what made this expensive in the scanner before. The rule is the same one
    /// <see cref="ToleratedGap"/> states - a candle or two is accepted as published-nothing, a
    /// longer stretch is an interruption and has to be read, because a walk that steps over it
    /// misses the zones that were broken in it.
    /// </para>
    /// </summary>
    public static async Task<bool> EnsureHistoryLoadedAsync(ZoneCandleWindows loadedCandlesInMemory,
        CryptoSymbol symbol, CryptoInterval interval, CandleTime neededFrom, string site)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (!symbolInterval.CandleList.TryGetFirstKey(out CandleTime firstInMemory))
            return false; // nothing in memory at all: the caller has no walk to protect

        if (neededFrom >= firstInMemory || interval.Duration == 0)
            return false; // the walk starts inside what is loaded

        uint missingMinutes = firstInMemory.Minutes - neededFrom.Minutes;
        int missingCandles = (int)(missingMinutes / interval.Duration);
        if (missingCandles <= ToleratedGap)
            return false;

        string key = $"{symbol.Name}|{interval.Name}|{neededFrom.Minutes}";
        lock (gapLock)
        {
            // Asked for once. If candles.db does not have this stretch either, repeating the query
            // on every recalculation would cost exactly what bounding the read just saved.
            if (!attemptedLoads.Add(key))
                return false;
        }

        GlobalData.AddTextToLogTab(
            $"ZONE GAP {symbol.Name} {interval.Name} {site}: walk starts at " +
            $"{neededFrom.ToLocalTime():yyyy-MM-dd HH:mm}, {missingCandles} candle(s) before the " +
            $"loaded window ({firstInMemory.ToLocalTime():yyyy-MM-dd HH:mm}). Reading that stretch.");

        PipelineProfiler.RecordZoneCandleRefetch(missingCandles);
        await ZoneCandleEngine.FetchFrom(loadedCandlesInMemory, symbol, interval,
            neededFrom, missingCandles + 1);
        return true;
    }
}
