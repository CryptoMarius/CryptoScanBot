using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Settings.Strategy;

// Base class for the colors and soundfile

[Serializable]
public class SettingsSignalStrategyBase
{
    // Per-strategy entry condition overrides. When null the global entry
    // conditions from SettingsTrading apply; when set these take precedence.
    public SettingsEntryConditions? EntryConditions { get; set; } = null;

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
