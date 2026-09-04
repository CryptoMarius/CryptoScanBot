using CryptoScanner.Core.Settings;

using System.Text.Json;
using System.Text.Json.Serialization;

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
    /// Switches the barometer measurement off for this run (see EmulatorRunConfig.CalculateBarometer).
    /// Null or omitted keeps it on. A run with barometer conditions refuses to start when it is off.
    /// </summary>
    public bool? CalculateBarometer { get; set; }

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
    /// The first day of the replay window for this run, inclusive, UTC. Null or omitted keeps the
    /// date from the run configuration. Written and read as "yyyy-MM-dd", like the run configuration.
    /// <para>
    /// It is here so one queue can measure the same settings over different periods in a single
    /// pass - the first half of the year against the second half, say. Whether a variant beats the
    /// run it is compared with can only be trusted when it does so in BOTH halves; a difference
    /// that changes sign between them is chance. Before this field that comparison needed a new
    /// batch per period, started by hand, which is exactly what cannot happen on a ten-day queue
    /// with nobody at the machine.
    /// </para>
    /// <para>
    /// The candles come from the local database, and the "Fetch candles" step fills it for the
    /// window of the run configuration only. A period that reaches outside that window is replayed
    /// with whatever candles happen to be there, so the queue loop warns about it in the log.
    /// </para>
    /// </summary>
    [JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// The last day of the replay window for this run, inclusive, UTC. Null or omitted keeps the
    /// date from the run configuration. See <see cref="FromDate"/>.
    /// </summary>
    [JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateTime? ToDate { get; set; }

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
    /// Whether the paper balances constrain this run: with it off nothing is refused for lack of
    /// money and the balance may run negative, so entries and DCA levels are taken that the account
    /// could not actually pay for. Null or omitted keeps whatever the run configuration says.
    /// <para>
    /// It has to live here rather than in <see cref="TradingOverrides"/>, even though it IS a
    /// property of SettingsTrading: the queue applies its overrides first and RunOnceAsync then
    /// calls ApplyRunOverrides, which assigns Settings.Trading.UseAssetManagement from the run
    /// configuration and overwrites whatever the entry asked for. An override there is silently
    /// ignored, which is worse than not being able to set it at all - the run looks like a
    /// measurement and is a copy of its own reference.
    /// </para>
    /// <para>
    /// The reason to measure it: the live HyperLiquid scanner runs with it OFF while every emulator
    /// run so far ran with it ON, and nobody knows what that difference is worth.
    /// </para>
    /// </summary>
    public bool? UseAssetManagement { get; set; }

    /// <summary>
    /// Trading/risk config (SL, TP, DCA). Takes precedence over the entry-level
    /// StopLossPercentage/TpList/DcaList properties.
    /// </summary>
    public EmulatorTradingConfig? Trading { get; set; }

    /// <summary>
    /// Runs this entry even when the same configuration was already measured on this build.
    /// <para>
    /// Without it an entry whose configuration checksum matches an earlier completed run is recorded
    /// as a duplicate and not replayed - see <see cref="EmulatorRunFingerprint"/>. That check is
    /// deliberately blunt, so this is the way out when a run has to be repeated anyway: verifying
    /// that a replay is still deterministic, or re-measuring after something outside the settings
    /// changed (the candle database, for instance).
    /// </para>
    /// </summary>
    public bool Force { get; set; }
}
