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
    /// A filled list can only NARROW that: candles are fetched and kept for the intervals ticked on
    /// the Signals tab, so a strategy that asks for one that is not ticked would be looking at
    /// candles that never arrive. The effective list is therefore the intersection of the two.
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
