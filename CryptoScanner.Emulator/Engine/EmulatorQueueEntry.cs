using CryptoScanner.Core.Settings;

using System.Text.Json;

namespace CryptoScanner.Emulator.Engine;


/// <summary>
/// Per-side filter configuration for an emulator queue entry. Each property overrides the
/// corresponding field on SettingsTextual for that side. Null/omitted fields keep defaults.
/// </summary>
public class EmulatorSideConfig
{
    /// <summary>When false, this side is disabled entirely (strategy list cleared).</summary>
    public bool? Enabled { get; set; }

    /// <summary>Override Signal/Trading.{Side}.Strategy for this side.</summary>
    public List<string>? Strategy { get; set; }

    /// <summary>Override Signal.{Side}.Interval for this side.</summary>
    public List<string>? Intervals { get; set; }

    /// <summary>Override Signal.{Side}.IntervalTrend.List for this side.</summary>
    public List<string>? TrendIntervals { get; set; }

    /// <summary>Override Signal.{Side}.Barometer.List. Key = interval, value = [min, max].</summary>
    public Dictionary<string, decimal[]>? Barometer { get; set; }

    /// <summary>Override Signal.{Side}.Barometer.ConsensusActive.</summary>
    public bool? BarometerConsensusActive { get; set; }

    /// <summary>Override Signal.{Side}.Barometer.MinConsensus.</summary>
    public int? BarometerMinConsensus { get; set; }

    /// <summary>Override Signal.{Side}.MarketTrend.List. Each entry is [min, max].</summary>
    public List<decimal[]>? MarketTrend { get; set; }

    /// <summary>Override Signal.{Side}.MarketTrendSecondary.List. Each entry is [min, max].</summary>
    public List<decimal[]>? MarketTrendSecondary { get; set; }

    /// <summary>
    /// Creates a resolved config by merging the target with a mirror source.
    /// Null fields on the target are filled from the source. Enabled is never mirrored.
    /// </summary>
    public static EmulatorSideConfig Resolve(EmulatorSideConfig? target, EmulatorSideConfig? mirrorSource)
    {
        var result = new EmulatorSideConfig
        {
            Enabled = target?.Enabled,
            Strategy = target?.Strategy,
            Intervals = target?.Intervals,
            TrendIntervals = target?.TrendIntervals,
            Barometer = target?.Barometer,
            BarometerConsensusActive = target?.BarometerConsensusActive,
            BarometerMinConsensus = target?.BarometerMinConsensus,
            MarketTrend = target?.MarketTrend,
            MarketTrendSecondary = target?.MarketTrendSecondary,
        };

        if (mirrorSource != null)
        {
            result.Strategy ??= mirrorSource.Strategy;
            result.Intervals ??= mirrorSource.Intervals;
            result.TrendIntervals ??= mirrorSource.TrendIntervals;
            result.Barometer ??= mirrorSource.Barometer;
            result.BarometerConsensusActive ??= mirrorSource.BarometerConsensusActive;
            result.BarometerMinConsensus ??= mirrorSource.BarometerMinConsensus;
            result.MarketTrend ??= mirrorSource.MarketTrend;
            result.MarketTrendSecondary ??= mirrorSource.MarketTrendSecondary;
        }

        return result;
    }

    /// <summary>
    /// Applies non-null overrides from this config onto the given signal/trading SettingsTextual.
    /// </summary>
    public static void Apply(EmulatorSideConfig config, SettingsTextual signal, SettingsTextual trading)
    {
        if (config.Strategy is { Count: > 0 })
        {
            signal.Strategy = new List<string>(config.Strategy);
            trading.Strategy = new List<string>(config.Strategy);
        }

        if (config.Enabled == false)
        {
            signal.Strategy = [];
            trading.Strategy = [];
        }

        if (config.Intervals is { Count: > 0 })
            signal.Interval = new List<string>(config.Intervals);

        if (config.TrendIntervals != null)
            signal.IntervalTrend.List = new List<string>(config.TrendIntervals);

        if (config.Barometer != null)
            signal.Barometer.List = config.Barometer.ToDictionary(kv => kv.Key, kv => (kv.Value[0], kv.Value[1]));

        if (config.BarometerConsensusActive.HasValue)
            signal.Barometer.ConsensusActive = config.BarometerConsensusActive.Value;

        if (config.BarometerMinConsensus.HasValue)
            signal.Barometer.MinConsensus = config.BarometerMinConsensus.Value;

        if (config.MarketTrend != null)
            signal.MarketTrend.List = config.MarketTrend.Select(a => (a[0], a[1])).ToList();

        if (config.MarketTrendSecondary != null)
            signal.MarketTrendSecondary.List = config.MarketTrendSecondary.Select(a => (a[0], a[1])).ToList();
    }
}


/// <summary>
/// Trading/risk configuration for an emulator queue entry: stop-loss, take-profit and DCA.
/// Takes precedence over the entry-level StopLossPercentage/TpList/DcaList properties.
/// </summary>
public class EmulatorTradingConfig
{
    public decimal? StopLossPercentage { get; set; }
    public List<CryptoTpEntry>? TpList { get; set; }
    public List<CryptoDcaEntry>? DcaList { get; set; }
}


/// <summary>
/// One item in the emulator queue file. Each entry describes a single run with explicit SL, TP
/// and DCA parameters — no matrix explosion. Future fields (e.g. interval overrides, indicator
/// tweaks) can be added here without breaking existing queue files.
/// </summary>
public class EmulatorQueueEntry
{
    /// <summary>Optional free-form label; used as part of the run label in the Results tab.</summary>
    public string Label { get; set; } = "";

    /// <summary>
    /// When set, this entry only runs for the named algorithm
    /// When empty/null, the entry runs for every selected algorithm.
    /// </summary>
    public string? Algorithm { get; set; }

    /// <summary>Stop-loss percentage for this run (e.g. 2.5).</summary>
    public decimal StopLossPercentage { get; set; } = 2m;

    /// <summary>Take-profit levels for this run. Empty list = use the scanner's default.</summary>
    public List<CryptoTpEntry> TpList { get; set; } = [];

    /// <summary>DCA ladder for this run. Empty list = no DCA.</summary>
    public List<CryptoDcaEntry> DcaList { get; set; } = [];

    /// <summary>
    /// Signal parameter overrides for this run. Outer key = settings section name on
    /// SettingsSignal, inner key = property name, value = the value to set. 
    /// Empty or omitted = no signal overrides.
    /// </summary>
    public Dictionary<string, Dictionary<string, JsonElement>> SignalOverrides { get; set; } = new();

    /// <summary>
    /// Trading parameter overrides for this run. Key = property name on SettingsTrading
    /// (e.g. "EntryOrderType"), value = the value to set. Empty or omitted = no trading overrides.
    /// </summary>
    public Dictionary<string, JsonElement> TradingOverrides { get; set; } = new();

    /// <summary>
    /// When set, overrides Signal.Long.Interval and Signal.Short.Interval for this run.
    /// Each entry is an interval name (e.g. "1m", "5m", "15m", "1h").
    /// Empty or omitted = use the scanner's default intervals.
    /// </summary>
    public List<string>? Intervals { get; set; }

    /// <summary>
    /// Restricts this run to a single trade side: "Long" or "Short".
    /// Legacy property — ignored when Long/Short per-side configs are present.
    /// </summary>
    public string? Side { get; set; }

    /// <summary>
    /// Per-side filter configuration for longs. Each non-null field overrides the
    /// corresponding SettingsTextual field. Set Enabled=false to disable longs entirely.
    /// </summary>
    public EmulatorSideConfig? Long { get; set; }

    /// <summary>
    /// Per-side filter configuration for shorts. Same structure as Long.
    /// </summary>
    public EmulatorSideConfig? Short { get; set; }

    /// <summary>
    /// Mirror mechanism: "Long" or "Short". When set, the named side's config fills
    /// null fields on the other side. Enabled is never mirrored.
    /// </summary>
    public string? MirrorFrom { get; set; }

    /// <summary>
    /// The base interval this run replays on ("1m", "5m", "15m", ...). Null or omitted keeps
    /// whatever the run configuration says, which is what the dropdown in the run-config window sets.
    /// <para>
    /// It is here so one queue can compare base intervals in a single pass. That matters more than it
    /// sounds: the base interval decides how often the strategy is evaluated AND how orders are
    /// filled, so two runs that differ only in this are not a small variation - they are two
    /// different measurements of the same strategy. Before this field the only way to compare them
    /// was to start the queue three times and change the dropdown in between, which is exactly the
    /// kind of manual step that ends up mislabelled.
    /// </para>
    /// </summary>
    public string? BaseInterval { get; set; }

    /// <summary>
    /// The paper-trading start capital this run gets, per traded quote coin. Null or omitted keeps
    /// whatever the run configuration says, which is what the field in the run-config window sets.
    /// <para>
    /// It is here for the same reason as <see cref="BaseInterval"/>: one queue can now compare
    /// position sizes in a single pass. Since the balances really do constrain trading, the start
    /// capital decides how many positions can be open at the same time - a run on 1.000 and the same
    /// run on 100.000 are two different measurements, not a small variation. A value of 0 or less is
    /// ignored, so a typo cannot silently start a run with no money.
    /// </para>
    /// </summary>
    public decimal? StartCapital { get; set; }

    /// <summary>
    /// Trading/risk config (SL, TP, DCA). Takes precedence over the entry-level
    /// StopLossPercentage/TpList/DcaList properties.
    /// </summary>
    public EmulatorTradingConfig? Trading { get; set; }
}
