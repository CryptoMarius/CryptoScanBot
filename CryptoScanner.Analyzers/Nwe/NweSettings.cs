using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Nwe;

[Serializable]
public class NweSettings : SettingsSignalStrategyBase
{
    // configuration:
    public double BandWidth { get; set; } = 8.0;
    public decimal Multiplication { get; set; } = 3.0m;

    public bool IncludeRsi { get; set; } = false;
    public bool IncludeSoftSbm { get; set; } = false;
    public bool IncludeSbmPercAndCrossing { get; set; } = false;
    public bool OnlyIfLux5m { get; set; } = false;
    // Lux 5m threshold (percentage 1..100). Long needs Lux5mValue <= -Lux5mPercentage,
    // short needs Lux5mValue >= +Lux5mPercentage. Default 50 matches the previous
    // hard-coded behavior; raise toward 100 for stricter (rarer) signals.
    public int Lux5mPercentage { get; set; } = 50;

    // Volume-klimax op de signaalcandle: filter losse "stille drift door de band"-tikken weg.
    public bool RequireVolumeClimax { get; set; } = false;
    public int VolumeClimaxLookback { get; set; } = 20;
    public decimal VolumeClimaxMultiplier { get; set; } = 1.5m;

    public NweSettings() : base()
    {
        SoundFileLong = "sound-nwe-oversold.wav";
        SoundFileShort = "sound-nwe-overbought.wav";
    }

}