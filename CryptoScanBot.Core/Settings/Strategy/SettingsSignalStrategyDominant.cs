using CryptoScanBot.Core.Enums;

namespace CryptoScanBot.Core.Settings.Strategy;

public class SettingsSignalStrategyDominant : SettingsSignalStrategyBase
{
    public bool UseHighLow { get; set; } = false;
    public int CandleCount { get; set; } = 3000; // 150 day's
    public CryptoIntervalPeriod Interval { get; set; } = CryptoIntervalPeriod.interval1h;

    public decimal WarnPercentage { get; set; } = 1m;


    public SettingsSignalStrategyDominant() : base()
    {
        SoundFileLong = "sound-dominant-long.wav";
        SoundFileShort = "sound-dominant-short.wav";
    }

}