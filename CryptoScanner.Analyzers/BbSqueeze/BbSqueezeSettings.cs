using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.BbSqueeze;

[Serializable]
public class BbSqueezeSettings : SettingsSignalStrategyBase
{
    // NOTE: the declaration order below is the order the settings appear on screen, and it follows
    // StrategyBbSqueezeSettingsView.axaml — which is why ReSqueezeGraceCandles sits with the two
    // other squeeze values instead of at the end. Serialization is by name, so moving a property
    // does not affect a settings file that was already written.

    // Maximum BB width percentage to qualify as a squeeze
    [SettingCaption("BB squeeze max %")]
    public double BBSqueezeMaxPercentage { get; set; } = 2.0;

    // Minimum number of consecutive candles the BB must stay squeezed
    [SettingCaption("Squeeze min candles")]
    public int SqueezeMinCandles { get; set; } = 6;

    // Number of candles after the signal to skip the re-squeeze GiveUp check
    [SettingCaption("Re-squeeze grace candles")]
    public int ReSqueezeGraceCandles { get; set; } = 2;

    // Use MACD histogram direction as confirmation filter
    [SettingCaption("Use MACD histogram direction filter", SeparatorBefore = true)]
    public bool UseMacdFilter { get; set; } = true;

    // Number of MACD histogram bars that must confirm the breakout direction
    [SettingCaption("MACD confirm candles", Indented = true, VisibleWhen = nameof(UseMacdFilter))]
    public int MacdConfirmCandles { get; set; } = 2;

    // Use volume spike as confirmation filter
    [SettingCaption("Use volume spike filter", SeparatorBefore = true)]
    public bool UseVolumeFilter { get; set; } = false;

    // Volume must exceed this multiplier x SMA(Volume, VolumeSmaLength)
    [SettingCaption("Volume multiplier", Indented = true, VisibleWhen = nameof(UseVolumeFilter))]
    public double VolumeMultiplier { get; set; } = 1.5;

    // Number of candles used for the volume SMA
    [SettingCaption("Volume SMA length", Indented = true, VisibleWhen = nameof(UseVolumeFilter))]
    public int VolumeSmaLength { get; set; } = 20;

    public BbSqueezeSettings() : base()
    {
        SoundFileLong = "sound-signal-oversold.wav";
        SoundFileShort = "sound-signal-overbought.wav";

        // Breakout strategy: disable all mean-reversion entry conditions
        EntryConditions = new SettingsEntryConditions();
    }
}
