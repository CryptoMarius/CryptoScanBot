using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using System.Text.Json.Serialization;

namespace CryptoScanner.Core.Settings.Strategy;

// Base class for the colors and soundfile

[Serializable]
public class SettingsSignalStrategyBase
{
    // Per-strategy entry condition overrides. When null the global entry
    // conditions from SettingsTrading apply; when set these take precedence.
    public SettingsEntryConditions? EntryConditions { get; set; } = null;

    /// <summary>A box of its own, like the Avalonia IntervalView it mirrors.</summary>
    private const string GroupIntervals = "Intervals";

    /// <summary>
    /// The intervals this strategy runs on. EMPTY means "whatever is ticked for the side", which is
    /// how every strategy behaved before this existed, so an untouched settings file keeps working.
    /// <para>
    /// A filled list REPLACES the side's list for this strategy (SignalExecute.IntervalsFor): the
    /// candles are there for every interval, CandleTools builds them all from the minute candles,
    /// and SignalPrepare prepares the indicators for the same list. An older text here said the two
    /// were intersected; they are not (open point 115).
    /// </para>
    /// </summary>
    [SettingCaption("Intervals", Group = GroupIntervals)]
    public List<string> IntervalList { get; set; } = [];

    /// <summary>
    /// The shortest interval this strategy accepts. Everything below it is greyed out in the
    /// interval box of both hosts. Read-only on purpose: it belongs to the strategy, not to the
    /// user, and a get-only property is skipped by the reflection based settings editor.
    /// </summary>
    [JsonIgnore]
    public virtual CryptoIntervalPeriod MinimumInterval => CryptoIntervalPeriod.interval1m;

    /// <summary>
    /// The zone kinds (dlz, fvg, smc) this strategy reads when it is enabled, as the names the
    /// RequireZone settings spell them. Empty for a strategy that never looks at a zone. SignalPrepare
    /// asks this to skip a zone kind that is configured but read by nobody (open point 32).
    /// </summary>
    [JsonIgnore]
    public virtual IEnumerable<string> RequiredZoneKinds => [];

    public bool PlaySound { get; set; } = false;
    public bool PlaySpeech { get; set; } = false;

    // Alpha 0x00 = fully transparent default, matching CryptoQuoteData.DisplayColor.
    // User can opt-in by configuring a non-zero alpha; until then the strategy color
    // has no visible effect on row/cell backgrounds.
    public CoreColor ColorLong { get; set; } = CoreColor.FromArgb(0x00, 0xFF, 0x95, 0xA5);
    public string SoundFileLong { get; set; } = "";

    public CoreColor ColorShort { get; set; } = CoreColor.FromArgb(0x00, 0xFF, 0x95, 0xA5);
    public string SoundFileShort { get; set; } = "";
}
