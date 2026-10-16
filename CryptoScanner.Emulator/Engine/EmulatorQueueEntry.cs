using CryptoScanner.Core.Core;
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

    /// <summary>
    /// Override Signal.{Side}.IntervalTrend.Inverted: true asks the trend of TrendIntervals AGAINST
    /// the side (bearish for long, bullish for short), for strategies that trade against the trend
    /// such as dbr (open point 51). Without this key a queue file could not reach it at all.
    /// </summary>
    public bool? TrendIntervalsInverted { get; set; }

    /// <summary>Override Signal.{Side}.Barometer.List. Key = interval, value = [min, max].</summary>
    public Dictionary<string, decimal[]>? Barometer { get; set; }

    /// <summary>Override Signal.{Side}.Barometer.ConsensusActive.</summary>
    public bool? BarometerConsensusActive { get; set; }

    /// <summary>Override Signal.{Side}.Barometer.MinConsensus.</summary>
    public int? BarometerMinConsensus { get; set; }

    /// <summary>
    /// Override Signal.{Side}.Barometer.BreadthActive: the percentage of the coins of the quote that
    /// rose over the last hour must lie between BarometerBreadthMinimum and BarometerBreadthMaximum.
    /// A dotted path in SignalOverrides cannot reach it, because Barometer is a field on
    /// SettingsTextual and ApplyDottedProperty only navigates properties (it skips silently).
    /// </summary>
    public bool? BarometerBreadthActive { get; set; }

    /// <summary>Override Signal.{Side}.Barometer.BreadthMinimum (0..100).</summary>
    public decimal? BarometerBreadthMinimum { get; set; }

    /// <summary>Override Signal.{Side}.Barometer.BreadthMaximum (0..100).</summary>
    public decimal? BarometerBreadthMaximum { get; set; }

    /// <summary>Override Signal.{Side}.SymbolTrend.List. Each entry is [min, max].</summary>
    public List<decimal[]>? SymbolTrend { get; set; }

    /// <summary>Override Signal.{Side}.SymbolTrendSecondary.List. Each entry is [min, max].</summary>
    public List<decimal[]>? SymbolTrendSecondary { get; set; }

    /// <summary>
    /// Override Signal.{Side}.MarketRsi: [min, max] switches the filter on and skips a signal while
    /// the market RSI lies in that band, an empty list switches it off.
    /// </summary>
    public decimal[]? MarketRsi { get; set; }

    /// <summary>
    /// Override Signal.{Side}.MarketState: [min, max] switches the filter on and only steps in while
    /// the market state score (-100..+100) lies in that band, an empty list switches it off.
    /// </summary>
    public decimal[]? MarketState { get; set; }

    /// <summary>
    /// Override Signal.{Side}.MarketState.Interval: the interval of the BTC trend ("1h", "2h", "3h",
    /// "4h"). Only changes the interval, not whether the filter is on.
    /// </summary>
    public string? MarketStateInterval { get; set; }

    /// <summary>
    /// Override Trading.{Side}.MarketExit: close a position once the market moved this many percent
    /// against it since the entry. Above zero switches it on, zero switches it off. Unlike the other
    /// keys this one sets the TRADER side: it decides about open positions, not about signals.
    /// </summary>
    public decimal? MarketExit { get; set; }

    /// <summary>Override Trading.{Side}.MarketExit.UseBtc: watch BTC instead of all coins.</summary>
    public bool? MarketExitBtc { get; set; }

    /// <summary>
    /// Override Signal.{Side}.BtcMove: [hours, min, max] only steps in while the % change of BTC over
    /// the last hours lies in the band; an empty list switches it off.
    /// </summary>
    public decimal[]? BtcMove { get; set; }

    /// <summary>Override Signal.{Side}.Weekdays: the UTC days to step in on (0 = Sunday .. 6), an empty list switches it off.</summary>
    public List<int>? Weekdays { get; set; }

    /// <summary>
    /// Override Signal.{Side}.MarketRsiCross: [level, hours] only steps in when the market RSI came out
    /// of the zone (long: from below the level, short: from above) within the last hours; [] switches it off.
    /// </summary>
    public decimal[]? MarketRsiCross { get; set; }

    /// <summary>
    /// Override Signal.{Side}.HigherInterval: the interval, e.g. "1h". Together with HigherIntervalRsi
    /// and HigherIntervalEma200; the filter is on as soon as one of those two is given and not empty/"any".
    /// </summary>
    public string? HigherInterval { get; set; }

    /// <summary>Override the RSI band of the higher interval: [min, max], [] for none.</summary>
    public decimal[]? HigherIntervalRsi { get; set; }

    /// <summary>Override the EMA200 condition of the higher interval: "above", "below" or "any".</summary>
    public string? HigherIntervalEma200 { get; set; }

    /// <summary>Override Trading.{Side}.EntryLimit: at most this many new positions per hour, 0 switches it off.</summary>
    public int? EntryLimitPerHour { get; set; }

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
            TrendIntervalsInverted = target?.TrendIntervalsInverted,
            Barometer = target?.Barometer,
            BarometerConsensusActive = target?.BarometerConsensusActive,
            BarometerMinConsensus = target?.BarometerMinConsensus,
            BarometerBreadthActive = target?.BarometerBreadthActive,
            BarometerBreadthMinimum = target?.BarometerBreadthMinimum,
            BarometerBreadthMaximum = target?.BarometerBreadthMaximum,
            SymbolTrend = target?.SymbolTrend,
            SymbolTrendSecondary = target?.SymbolTrendSecondary,
            MarketRsi = target?.MarketRsi,
            MarketState = target?.MarketState,
            MarketStateInterval = target?.MarketStateInterval,
            MarketExit = target?.MarketExit,
            MarketExitBtc = target?.MarketExitBtc,
            BtcMove = target?.BtcMove,
            Weekdays = target?.Weekdays,
            MarketRsiCross = target?.MarketRsiCross,
            HigherInterval = target?.HigherInterval,
            HigherIntervalRsi = target?.HigherIntervalRsi,
            HigherIntervalEma200 = target?.HigherIntervalEma200,
            EntryLimitPerHour = target?.EntryLimitPerHour,
        };

        if (mirrorSource != null)
        {
            result.Strategy ??= mirrorSource.Strategy;
            result.Intervals ??= mirrorSource.Intervals;
            result.TrendIntervals ??= mirrorSource.TrendIntervals;
            result.TrendIntervalsInverted ??= mirrorSource.TrendIntervalsInverted;
            result.Barometer ??= mirrorSource.Barometer;
            result.BarometerConsensusActive ??= mirrorSource.BarometerConsensusActive;
            result.BarometerMinConsensus ??= mirrorSource.BarometerMinConsensus;
            result.BarometerBreadthActive ??= mirrorSource.BarometerBreadthActive;
            result.BarometerBreadthMinimum ??= mirrorSource.BarometerBreadthMinimum;
            result.BarometerBreadthMaximum ??= mirrorSource.BarometerBreadthMaximum;
            result.SymbolTrend ??= mirrorSource.SymbolTrend;
            result.SymbolTrendSecondary ??= mirrorSource.SymbolTrendSecondary;
            result.MarketRsi ??= mirrorSource.MarketRsi;
            result.MarketState ??= mirrorSource.MarketState;
            result.MarketStateInterval ??= mirrorSource.MarketStateInterval;
            result.MarketExit ??= mirrorSource.MarketExit;
            result.MarketExitBtc ??= mirrorSource.MarketExitBtc;
            result.BtcMove ??= mirrorSource.BtcMove;
            result.Weekdays ??= mirrorSource.Weekdays;
            result.MarketRsiCross ??= mirrorSource.MarketRsiCross;
            result.HigherInterval ??= mirrorSource.HigherInterval;
            result.HigherIntervalRsi ??= mirrorSource.HigherIntervalRsi;
            result.HigherIntervalEma200 ??= mirrorSource.HigherIntervalEma200;
            result.EntryLimitPerHour ??= mirrorSource.EntryLimitPerHour;
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

        if (config.TrendIntervalsInverted.HasValue)
            signal.IntervalTrend.Inverted = config.TrendIntervalsInverted.Value;

        if (config.Barometer != null)
            signal.Barometer.List = config.Barometer.ToDictionary(kv => kv.Key, kv => (kv.Value[0], kv.Value[1]));

        if (config.BarometerConsensusActive.HasValue)
            signal.Barometer.ConsensusActive = config.BarometerConsensusActive.Value;

        if (config.BarometerMinConsensus.HasValue)
            signal.Barometer.MinConsensus = config.BarometerMinConsensus.Value;

        if (config.BarometerBreadthActive.HasValue)
            signal.Barometer.BreadthActive = config.BarometerBreadthActive.Value;

        if (config.BarometerBreadthMinimum.HasValue)
            signal.Barometer.BreadthMinimum = config.BarometerBreadthMinimum.Value;

        if (config.BarometerBreadthMaximum.HasValue)
            signal.Barometer.BreadthMaximum = config.BarometerBreadthMaximum.Value;

        if (config.SymbolTrend != null)
            signal.SymbolTrend.List = config.SymbolTrend.Select(a => (a[0], a[1])).ToList();

        if (config.SymbolTrendSecondary != null)
            signal.SymbolTrendSecondary.List = config.SymbolTrendSecondary.Select(a => (a[0], a[1])).ToList();

        // A new object rather than changing the existing one: the queue keeps the original by
        // reference and puts it back after the entry
        if (config.MarketRsi != null)
        {
            if (config.MarketRsi.Length != 0 && config.MarketRsi.Length != 2)
                throw new NotSupportedException($"MarketRsi needs [min, max] or [], not {config.MarketRsi.Length} values");
            signal.MarketRsi = config.MarketRsi.Length == 2
                ? new SettingsTextualMarketRsi { Active = true, Minimum = config.MarketRsi[0], Maximum = config.MarketRsi[1], Log = signal.MarketRsi.Log }
                : new SettingsTextualMarketRsi { Active = false, Log = signal.MarketRsi.Log };
        }

        // Also a new object, for the same reason
        if (config.MarketState != null || config.MarketStateInterval != null)
        {
            SettingsTextualMarketState current = signal.MarketState;
            SettingsTextualMarketState state = new()
            {
                Active = current.Active,
                Minimum = current.Minimum,
                Maximum = current.Maximum,
                Interval = current.Interval,
                Log = current.Log,
            };
            if (config.MarketState != null)
            {
                if (config.MarketState.Length != 0 && config.MarketState.Length != 2)
                    throw new NotSupportedException($"MarketState needs [min, max] or [], not {config.MarketState.Length} values");
                state.Active = config.MarketState.Length == 2;
                if (state.Active)
                {
                    state.Minimum = config.MarketState[0];
                    state.Maximum = config.MarketState[1];
                }
            }
            if (config.MarketStateInterval != null)
            {
                if (!GlobalData.IntervalListPeriodName.ContainsKey(config.MarketStateInterval))
                    throw new NotSupportedException($"MarketStateInterval \"{config.MarketStateInterval}\" is not an interval");
                state.Interval = config.MarketStateInterval;
            }
            signal.MarketState = state;
        }

        // The trader side, and a new object for the same reason as above
        if (config.MarketExit.HasValue)
        {
            if (config.MarketExit.Value < 0)
                throw new NotSupportedException($"MarketExit needs a percentage of zero or more, not {config.MarketExit.Value}");
            trading.MarketExit = new SettingsTextualMarketExit
            {
                Active = config.MarketExit.Value > 0,
                Percentage = config.MarketExit.Value > 0 ? config.MarketExit.Value : trading.MarketExit.Percentage,
                UseBtc = trading.MarketExit.UseBtc,
            };
        }
        if (config.MarketExitBtc.HasValue)
        {
            trading.MarketExit = new SettingsTextualMarketExit
            {
                Active = trading.MarketExit.Active,
                Percentage = trading.MarketExit.Percentage,
                UseBtc = config.MarketExitBtc.Value,
            };
        }

        // The entry filters of the market-regime study; new objects, for the same reason as above
        if (config.BtcMove != null)
        {
            if (config.BtcMove.Length != 0 && config.BtcMove.Length != 3)
                throw new NotSupportedException($"BtcMove needs [hours, min, max] or [], not {config.BtcMove.Length} values");
            signal.BtcMove = config.BtcMove.Length == 3
                ? new SettingsTextualBtcMove { Active = true, Hours = (int)config.BtcMove[0], Minimum = config.BtcMove[1], Maximum = config.BtcMove[2] }
                : new SettingsTextualBtcMove { Active = false };
        }
        if (config.Weekdays != null)
        {
            if (config.Weekdays.Any(d => d < 0 || d > 6))
                throw new NotSupportedException("Weekdays takes 0 (Sunday) .. 6 (Saturday)");
            signal.Weekdays = config.Weekdays.Count != 0
                ? new SettingsTextualWeekdays { Active = true, Days = [.. config.Weekdays] }
                : new SettingsTextualWeekdays { Active = false };
        }
        if (config.MarketRsiCross != null)
        {
            if (config.MarketRsiCross.Length != 0 && config.MarketRsiCross.Length != 2)
                throw new NotSupportedException($"MarketRsiCross needs [level, hours] or [], not {config.MarketRsiCross.Length} values");
            signal.MarketRsiCross = config.MarketRsiCross.Length == 2
                ? new SettingsTextualMarketRsiCross { Active = true, Level = config.MarketRsiCross[0], Hours = (int)config.MarketRsiCross[1] }
                : new SettingsTextualMarketRsiCross { Active = false };
        }
        if (config.HigherInterval != null || config.HigherIntervalRsi != null || config.HigherIntervalEma200 != null)
        {
            SettingsTextualHigherInterval current = signal.HigherInterval;
            SettingsTextualHigherInterval higher = new()
            {
                Active = current.Active,
                Interval = current.Interval,
                RsiMinimum = current.RsiMinimum,
                RsiMaximum = current.RsiMaximum,
                Ema200 = current.Ema200,
            };
            if (config.HigherInterval != null)
            {
                if (!GlobalData.IntervalListPeriodName.ContainsKey(config.HigherInterval))
                    throw new NotSupportedException($"HigherInterval \"{config.HigherInterval}\" is not an interval");
                higher.Interval = config.HigherInterval;
            }
            if (config.HigherIntervalRsi != null)
            {
                if (config.HigherIntervalRsi.Length != 0 && config.HigherIntervalRsi.Length != 2)
                    throw new NotSupportedException($"HigherIntervalRsi needs [min, max] or [], not {config.HigherIntervalRsi.Length} values");
                (higher.RsiMinimum, higher.RsiMaximum) = config.HigherIntervalRsi.Length == 2
                    ? (config.HigherIntervalRsi[0], config.HigherIntervalRsi[1]) : (0m, 100m);
            }
            if (config.HigherIntervalEma200 != null)
            {
                higher.Ema200 = config.HigherIntervalEma200.ToLowerInvariant() switch
                {
                    "above" => 1,
                    "below" => -1,
                    "any" or "" => 0,
                    _ => throw new NotSupportedException($"HigherIntervalEma200 knows above, below and any, not \"{config.HigherIntervalEma200}\""),
                };
            }
            higher.Active = higher.Ema200 != 0 || higher.RsiMinimum > 0 || higher.RsiMaximum < 100;
            signal.HigherInterval = higher;
        }
        if (config.EntryLimitPerHour.HasValue)
        {
            if (config.EntryLimitPerHour.Value < 0)
                throw new NotSupportedException($"EntryLimitPerHour needs zero or more, not {config.EntryLimitPerHour.Value}");
            trading.EntryLimit = new SettingsTextualEntryLimit
            {
                Active = config.EntryLimitPerHour.Value > 0,
                MaxPerHour = config.EntryLimitPerHour.Value > 0 ? config.EntryLimitPerHour.Value : trading.EntryLimit.MaxPerHour,
            };
        }
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
    /// Symbols left out of this run, by name (e.g. ["BTCUSDT.PERP", "ETHUSDT.PERP"]). Null or empty
    /// replays every symbol of the run configuration. Names are matched case-insensitively; a name
    /// that is not in the run configuration is ignored.
    /// <para>
    /// It is here because "does this strategy earn on this kind of coin" cannot be answered with a
    /// setting - there is no filter for it, and inventing one for a single strategy is the wrong
    /// order: first measure whether the difference is real, then decide whether it deserves a knob.
    /// Removing the coins from the run configuration by hand would work once, but it changes the
    /// baseline of every other entry in the same queue, so the comparison it was supposed to make
    /// disappears.
    /// </para>
    /// <para>
    /// The symbol list is part of the run configuration that is hashed for the duplicate check, so a
    /// run with symbols left out never counts as a repeat of the same entry with all of them.
    /// </para>
    /// </summary>
    public List<string>? ExcludeSymbols { get; set; }

    /// <summary>
    /// Switches the barometer measurement off for this run (see EmulatorRunConfig.CalculateBarometer).
    /// Null or omitted keeps it on. A run with barometer conditions refuses to start when it is off.
    /// </summary>
    public bool? CalculateBarometer { get; set; }

    /// <summary>
    /// Runs the trade half of every minute serially (true) or the whole pipeline in parallel (false),
    /// see EmulatorRunConfig.SerialTradeHalf. Null or omitted keeps whatever the run configuration says.
    /// </summary>
    public bool? SerialTradeHalf { get; set; }

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
    /// The stake of ONE entry, in quote currency, for this run only. Null or omitted leaves the
    /// amount from the settings file alone.
    /// <para>
    /// It is here rather than in <see cref="TradingOverrides"/> because it does not live in
    /// SettingsTrading at all: the amount sits on the quote coin, in Settings.QuoteCoins, which
    /// those overrides cannot reach.
    /// </para>
    /// <para>
    /// The reason to measure it: the runs so far entered with 15 on a starting capital of 10.000,
    /// so at their busiest moment 4,5% of the money was in the market and on average 0,9%. Every
    /// result we have is therefore a result on a tenth of a percent of the capital, and how it
    /// behaves when the stake goes up - until the balance starts refusing entries - is the largest
    /// unmeasured lever there is.
    /// </para>
    /// <para>
    /// It is applied to EVERY quote coin and put back when the run ends, through the same
    /// save-and-revert every other override uses, so the next run and the settings file are
    /// untouched. The entry PERCENTAGE is set to zero alongside it: with a percentage standing the
    /// amount is never read (TradeTools.GetEntryAmount), and an entry amount that is quietly
    /// ignored is the worst of both.
    /// </para>
    /// </summary>
    public decimal? EntryAmount { get; set; }

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
