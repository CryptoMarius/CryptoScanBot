using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.KumoSqueeze;

[Serializable]
public class KumoSqueezeSettings : SettingsSignalStrategyBase
{
    // NOTE: the declaration order, the sub-headings and the separators below follow
    // StrategyKumoSqueezeSettingsView.axaml, because that order is what the Blazor hosts render.
    // Serialization is by name, so moving a property does not affect an existing settings file.

    // Maximum BB width percentage to qualify as a squeeze (BandWidth < SMA(BandWidth, 20))
    [SettingCaption("BB squeeze max %", SubHeader = "Bollinger Squeeze")]
    public double BBSqueezeMaxPercentage { get; set; } = 2.0;

    // Minimum number of consecutive candles the BB must stay squeezed before breakout
    [SettingCaption("Squeeze min candles")]
    public int SqueezeMinCandles { get; set; } = 6;

    // Ichimoku Tenkan-sen period
    [SettingCaption("Tenkan period", SeparatorBefore = true, SubHeader = "Ichimoku Cloud")]
    public int TenkanPeriod { get; set; } = 9;

    // Ichimoku Kijun-sen period
    [SettingCaption("Kijun period")]
    public int KijunPeriod { get; set; } = 26;

    // Ichimoku Senkou Span B period
    [SettingCaption("Senkou Span B period")]
    public int SenkouBPeriod { get; set; } = 52;

    // Use RSI > 50 / < 50 as additional filter
    [SettingCaption("RSI direction filter (above/below 50)", SeparatorBefore = true, SubHeader = "Optional Filters")]
    public bool UseRsiFilter { get; set; } = true;

    // Use Tenkan > Kijun (long) / Tenkan < Kijun (short) filter
    [SettingCaption("Tenkan/Kijun cross filter")]
    public bool UseTenkanKijunFilter { get; set; } = true;

    // Use volume spike as confirmation filter
    [SettingCaption("Volume spike filter")]
    public bool UseVolumeFilter { get; set; } = true;

    // Volume must exceed this multiplier x SMA(Volume, VolumeSmaLength)
    [SettingCaption("Volume multiplier", Indented = true, VisibleWhen = nameof(UseVolumeFilter))]
    public double VolumeMultiplier { get; set; } = 1.5;

    // Number of candles used for the volume SMA
    [SettingCaption("Volume SMA length", Indented = true, VisibleWhen = nameof(UseVolumeFilter))]
    public int VolumeSmaLength { get; set; } = 20;

    // Use MACD histogram direction as additional confirmation
    [SettingCaption("MACD histogram direction filter")]
    public bool UseMacdFilter { get; set; } = false;

    // Number of MACD histogram bars that must confirm the breakout direction
    [SettingCaption("MACD confirm candles", Indented = true, VisibleWhen = nameof(UseMacdFilter))]
    public int MacdConfirmCandles { get; set; } = 2;

    public KumoSqueezeSettings() : base()
    {
        SoundFileLong = "sound-signal-oversold.wav";
        SoundFileShort = "sound-signal-overbought.wav";

        // Breakout strategy: disable all mean-reversion entry conditions
        EntryConditions = new SettingsEntryConditions();
    }
}
