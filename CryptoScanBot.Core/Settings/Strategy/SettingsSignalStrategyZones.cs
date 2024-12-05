using CryptoScanBot.Core.Enums;

namespace CryptoScanBot.Core.Settings.Strategy;

public class SettingsSignalStrategyZones : SettingsSignalStrategyBase
{
    public bool ShowZoneSignals { get; set; } = false;
    
    public bool UseHighLow { get; set; } = false; // advise = open/close
    public int CandleCount { get; set; } = 500; //3000; // 3000=150 day's back, 500=20.8 dagen
    public CryptoIntervalPeriod Interval { get; set; } = CryptoIntervalPeriod.interval1h;

    // Limits unzoomed box
    public bool ZonesApplyUnzoomed { get; set; } = false;
    public decimal MinimumUnZoomedPercentage { get; set; } = 0.0m;
    public decimal MaximumUnZoomedPercentage { get; set; } = 0.0m;

    // Limits zoomed box
    public bool ZoomLowerTimeFrames { get; set; } = true;
    public decimal MinimumZoomedPercentage { get; set; } = 0.2m;
    public decimal MaximumZoomedPercentage { get; set; } = 0.7m;

    // Signal percentage
    public decimal WarnPercentage { get; set; } = 1.0m;

    // Filter on start
    public bool ZoneStartApply { get; set; } = true;
    public int ZoneStartCandleCount { get; set; } = 5; // 5 candles back
    public decimal ZoneStartPercentage { get; set; } = 2.5m; // %

    public SettingsSignalStrategyZones() : base()
    {
        SoundFileLong = "sound-zones-long.wav";
        SoundFileShort = "sound-zones-short.wav";
    }

}