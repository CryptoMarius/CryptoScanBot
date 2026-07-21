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