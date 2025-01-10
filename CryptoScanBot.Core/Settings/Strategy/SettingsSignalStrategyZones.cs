using CryptoScanBot.Core.Enums;

namespace CryptoScanBot.Core.Settings.Strategy;

[Serializable]
public class SettingsSignalStrategyZones : SettingsSignalStrategyBase
{
    public bool ShowSignalsLong { get; set; } = false;
    public bool ShowSignalsShort { get; set; } = false;
    //public List<string> Interval { get; set; } = [];
    public CryptoIntervalPeriod Interval { get; set; } = CryptoIntervalPeriod.interval1h;

    public int CandleCount { get; set; } = 500; //3000; // 3000=150 day's back, 500=20.8 dagen
    public int CandleCountZoom { get; set; } = 125;

    // Limits unzoomed box
    public bool ZonesApplyUnzoomed { get; set; } = false;
    public double MinimumUnZoomedPercentage { get; set; } = 0.0;
    public double MaximumUnZoomedPercentage { get; set; } = 0.0;

    // Limits zoomed box
    public bool ZoomLowerTimeFrames { get; set; } = true;
    public double MinimumZoomedPercentage { get; set; } = 0.2;
    public double MaximumZoomedPercentage { get; set; } = 0.7;

    // Signal percentage
    public decimal WarnPercentage { get; set; } = 1.0m;

    // Filter on start
    public bool ZoneStartApply { get; set; } = false;
    public int ZoneStartCandleCount { get; set; } = 5; // 5 candles back
    public double ZoneStartPercentage { get; set; } = 2.5; // %

    public SettingsSignalStrategyZones() : base()
    {
        //Interval.Add("1h");
        SoundFileLong = "sound-zones-long.wav";
        SoundFileShort = "sound-zones-short.wav";
    }

}