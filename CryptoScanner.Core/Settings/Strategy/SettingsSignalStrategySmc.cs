using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Settings.Strategy;

/// <summary>
/// Settings for the SMC (Smart Money Concepts) supply/demand strategy. Persisted in
/// appsettings.json under Signal.ZonesSmc — there is no graphical settings UI in this app,
/// all configuration lives in that JSON file (same as DLZ / FVG).
///
/// Two groups of knobs:
///   • Detector tuning — how a base + expansion is recognised (read by ZoneSmc.Detect)
///   • Signal tuning   — how the SignalOrderBlock* classes turn a zone into an alarm/entry
///
/// Two entry flavours, increasing in confirmation:
///   • smc           — price TOUCHES the zone band (still no proof it holds)
///   • smc.rejection — price tested the zone AND closed back outside the proximal edge
///                     (the actual bounce/rejection → the entry-grade signal)
/// </summary>
[Serializable]
public class SettingsSignalStrategySmc : SettingsSignalStrategyBase
{
    // Intervals on which SMC zones are calculated (and on which the strategy reacts).
    public List<string> IntervalList { get; set; } = [];

    // ---- Detector tuning (base + expansion) ----

    // Trailing window used to compute the "average candle range" reference that base and
    // expansion are measured against (ATR-like mean of High-Low).
    public int AverageWindow { get; set; } = 20;

    // A candle belongs to a BASE when its range is at or below this fraction of the average.
    public decimal BaseMaxRangeFactor { get; set; } = 0.8m;

    // A candle is an EXPANSION (leg-out) when its range is at least this multiple of the
    // average range (and its body dominates the range, see ExpansionBodyFraction).
    public decimal ExpansionMinRangeFactor { get; set; } = 1.5m;

    // The expansion candle's body must be at least this fraction of its range so a huge-wick
    // indecision candle doesn't masquerade as an impulsive move.
    public decimal ExpansionBodyFraction { get; set; } = 0.5m;

    // At or above this range multiple the expansion is considered powerful → Strong zone,
    // otherwise Weak.
    public decimal StrongExpansionFactor { get; set; } = 2.5m;

    // Maximum number of consecutive small candles absorbed into one base.
    public int BaseMaxCandles { get; set; } = 6;

    // Cap on zones kept per interval (newest kept) to avoid overloading memory / chart.
    public int MaxBlocksPerInterval { get; set; } = 50;

    // ---- Signal tuning (entry) ----

    // Only fire on Strong zones (powerful expansion). Set false to also alarm on Weak zones.
    public bool OnlyStrong { get; set; } = true;

    // Maximum number of CE (50%) touches a zone may already have and still produce a signal.
    // 0 = only fresh (unmitigated) zones. 1 = also allow the first retest, etc.
    public int MaxTouches { get; set; } = 0;

    // How many candles back (including the current one) the smc.rejection variant may look
    // for the "tested the zone" wick. 1 = the rejection wick + close-back-outside must happen
    // on the same candle. 3 = the wick into the zone may be up to 2 candles before the
    // confirming close-back-outside candle.
    public int RejectionLookback { get; set; } = 3;

    public SettingsSignalStrategySmc() : base()
    {
        SoundFileLong = "sound-dlz-oversold.wav";
        SoundFileShort = "sound-dlz-overbought.wav";

        IntervalList.Add("1h");
    }
}
