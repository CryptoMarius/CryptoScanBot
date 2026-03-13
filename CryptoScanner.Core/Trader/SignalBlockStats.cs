using CryptoScanner.Core.Core;

using System.Collections.Concurrent;

namespace CryptoScanner.Core.Trader;

/// <summary>
/// Thread-safe counters that track why signals are blocked from becoming positions.
/// Call Dump() to print a sorted summary to the log tab, Reset() to clear all counters.
/// </summary>
public static class SignalBlockStats
{
    private static readonly ConcurrentDictionary<string, long> Counts = new();

    // -----------------------------------------------------------------------
    // Counter keys — one constant per check point so callers stay consistent.
    // -----------------------------------------------------------------------

    // Global pre-loop checks
    public const string Cooldown = "cooldown (global buy cooldown)";
    public const string TradingRulesPause = "trading-rules pause";

    // Per-signal pre-entry checks
    public const string IntervalNotConfig = "interval not configured for trading";
    public const string StrategyNotConfig = "strategy not configured for trading";
    public const string NoCandles = "no candles on interval";
    public const string IndicatorsFailed = "indicator calculation failed";
    public const string GiveUp = "algorithm GiveUp (signal expired)";
    public const string NotAllowedYet = "algorithm AllowStepIn (waiting for confirmation)";

    // New-position checks
    public const string NewPositionsOff = "new positions disabled";
    public const string Barometer = "barometer conditions";
    public const string Whitelist = "whitelist";
    public const string Blacklist = "blacklist";
    public const string MinVolume = "minimum volume";
    public const string MinPrice = "minimum price";
    public const string TickPercentage = "tick percentage (barcode)";
    public const string TrendConditions = "trend conditions";
    public const string MarketTrend = "market trend conditions";
    public const string ExchangeNotSupported = "exchange not supported";
    public const string SlotsFull = "slots full";
    public const string FetchAssetsFailed = "fetch assets failed";
    public const string InsufficientAssets = "insufficient assets";
    public const string EntryQtyZero = "entry quantity <= 0";
    public const string EntryQtyMinimum = "entry quantity == minimum";
    public const string EntryValueMinimum = "entry value < minimum";
    public const string InsufficientBalance = "insufficient balance (real trading)";

    // DCA checks
    public const string DcaNotAllowedYet = "DCA AllowStepIn (waiting)";
    public const string DcaGiveUp = "DCA GiveUp (signal expired)";

    // Success
    public const string PositionCreated = "POSITION CREATED";
    public const string DcaCreated = "DCA CREATED";

    // -----------------------------------------------------------------------

    /// <summary>Increment the counter for a block reason.</summary>
    public static void Increment(string reason)
    {
        Counts.AddOrUpdate(reason, 1L, static (_, n) => n + 1L);
    }

    /// <summary>Reset all counters to zero.</summary>
    public static void Reset()
    {
        Counts.Clear();
    }

    /// <summary>
    /// Dump a sorted (descending by count) summary to the log tab.
    /// Call this from a button, timer, or on-demand command.
    /// </summary>
    public static void Dump()
    {
        var sorted = Counts.OrderByDescending(x => x.Value).ToList();
        if (sorted.Count == 0)
        {
            GlobalData.AddTextToLogTab("SignalBlockStats: no data recorded yet.");
            return;
        }

        long total = sorted.Sum(x => x.Value);
        GlobalData.AddTextToLogTab($"");
        GlobalData.AddTextToLogTab($"=== SignalBlockStats (total evaluations = {total:N0}) ===");
        foreach (var (reason, count) in sorted)
            GlobalData.AddTextToLogTab($"  {count,9:N0}  ({100.0 * count / total,5:F1}%)  {reason}");
        GlobalData.AddTextToLogTab($"======================================================");
        GlobalData.AddTextToLogTab($"");
    }
}
