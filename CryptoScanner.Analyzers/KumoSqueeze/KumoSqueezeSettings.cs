using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.KumoSqueeze;

[Serializable]
public class KumoSqueezeSettings : SettingsSignalStrategyBase
{
    // Maximum BB width percentage to qualify as a squeeze (BandWidth < SMA(BandWidth, 20))
    public double BBSqueezeMaxPercentage { get; set; } = 2.0;

    // Minimum number of consecutive candles the BB must stay squeezed before breakout
    public int SqueezeMinCandles { get; set; } = 6;

    // Volume must exceed this multiplier × SMA(Volume, 20)
    public double VolumeMultiplier { get; set; } = 1.5;

    // Number of candles used for the volume SMA
    public int VolumeSmaLength { get; set; } = 20;

    // Ichimoku Tenkan-sen period
    public int TenkanPeriod { get; set; } = 9;

    // Ichimoku Kijun-sen period
    public int KijunPeriod { get; set; } = 26;

    // Ichimoku Senkou Span B period
    public int SenkouBPeriod { get; set; } = 52;

    // Use RSI > 50 / < 50 as additional filter
    public bool UseRsiFilter { get; set; } = true;

    // Use Tenkan > Kijun (long) / Tenkan < Kijun (short) filter
    public bool UseTenkanKijunFilter { get; set; } = true;

    public KumoSqueezeSettings() : base()
    {
        SoundFileLong = "sound-signal-oversold.wav";
        SoundFileShort = "sound-signal-overbought.wav";
    }
}
