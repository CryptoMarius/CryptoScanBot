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

    // Number of MACD histogram bars that must confirm the breakout direction
    public int MacdConfirmCandles { get; set; } = 2;

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
