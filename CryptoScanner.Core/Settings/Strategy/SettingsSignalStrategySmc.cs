using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Settings.Strategy;

/// <summary>
/// Settings for the SMC (Smart Money Concepts) supply/demand strategy. Persisted in
/// appsettings.json under Signal.ZonesSmc — there is no graphical settings UI in this app,
/// all configuration lives in that JSON file (same as DLZ / FVG).
///
/// Two groups of knobs:
///   • Detector tuning — how a base + expansion is recognised (read by ZoneSmc.Detect)
///   • Signal tuning   — how the SignalOrderBlockNear* classes turn a zone into an alarm
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

    // How far outside the PROXIMAL edge (in %) price may still be for the entry alarm to
    // fire. Entry is at the proximal edge (Top for demand, Bottom for supply), NOT at the
    // 50% midpoint — a shallow bounce into a large zone must not be missed.
    public decimal NearZonePercentage { get; set; } = 0.25m;

    // Only fire on Strong zones (powerful expansion). Set false to also alarm on Weak zones.
    public bool OnlyStrong { get; set; } = true;

    // Maximum number of CE (50%) touches a zone may already have and still produce a signal.
    // 0 = only fresh (unmitigated) zones. 1 = also allow the first retest, etc.
    public int MaxTouches { get; set; } = 0;

    public SettingsSignalStrategySmc() : base()
    {
        SoundFileLong = "sound-dlz-oversold.wav";
        SoundFileShort = "sound-dlz-overbought.wav";

        IntervalList.Add("1h");
    }
}
