using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.BbSqueeze;

[Serializable]
public class BbSqueezeSettings : SettingsSignalStrategyBase
{
    // Maximum BB width percentage to qualify as a squeeze
    public double BBSqueezeMaxPercentage { get; set; } = 2.0;

    // Minimum number of consecutive candles the BB must stay squeezed
    public int SqueezeMinCandles { get; set; } = 6;

    // Use MACD histogram direction as confirmation filter
    public bool UseMacdFilter { get; set; } = true;

    // Number of MACD histogram bars that must confirm the breakout direction
    public int MacdConfirmCandles { get; set; } = 2;

    // Use volume spike as confirmation filter
    public bool UseVolumeFilter { get; set; } = false;

    // Volume must exceed this multiplier x SMA(Volume, VolumeSmaLength)
    public double VolumeMultiplier { get; set; } = 1.5;

    // Number of candles used for the volume SMA
    public int VolumeSmaLength { get; set; } = 20;

    // Number of candles after the signal to skip the re-squeeze GiveUp check
    public int ReSqueezeGraceCandles { get; set; } = 2;

    public BbSqueezeSettings() : base()
    {
        SoundFileLong = "sound-signal-oversold.wav";
        SoundFileShort = "sound-signal-overbought.wav";

        // Breakout strategy: disable all mean-reversion entry conditions
        EntryConditions = new SettingsEntryConditions();
    }
}
