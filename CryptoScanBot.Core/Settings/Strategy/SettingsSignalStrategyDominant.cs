using CryptoScanBot.Core.Enums;

namespace CryptoScanBot.Core.Settings.Strategy;

public class SettingsSignalStrategyDominant : SettingsSignalStrategyBase
{
    public bool UseHighLow { get; set; } = false; // advise = open/close
    public int CandleCount { get; set; } = 3000; // 150 day's back

    public CryptoIntervalPeriod Interval { get; set; } = CryptoIntervalPeriod.interval1h;

    // Limits unzoomed box
    public decimal MinimumUnZoomedPercentage { get; set; } = 0.2m;
    public decimal MaximumUnZoomedPercentage { get; set; } = 10.0m;

    // Limits box after zooming
    public bool ZoomLowerTimeFrames { get; set; } = true;
    public decimal MinimumZoomedPercentage { get; set; } = 0.2m;
    public decimal MaximumZoomedPercentage { get; set; } = 0.7m;

    // Signal percentage
    public decimal WarnPercentage { get; set; } = 1.0m;

    public SettingsSignalStrategyDominant() : base()
    {
        SoundFileLong = "sound-zones-long.wav";
        SoundFileShort = "sound-zones-short.wav";
    }

}