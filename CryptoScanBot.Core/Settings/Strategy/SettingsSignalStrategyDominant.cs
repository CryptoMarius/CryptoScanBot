using CryptoScanBot.Core.Enums;

namespace CryptoScanBot.Core.Settings.Strategy;

public class SettingsSignalStrategyDominant : SettingsSignalStrategyBase
{
    public int CandleCount { get; set; } = 3000; // 150 day's
    public CryptoIntervalPeriod Interval { get; set; } = CryptoIntervalPeriod.interval1h; 

    // todo percentage


    public SettingsSignalStrategyDominant() : base()
    {
        SoundFileLong = "sound-dominant-long.wav";
        SoundFileShort = "sound-dominant-short.wav";
    }

}