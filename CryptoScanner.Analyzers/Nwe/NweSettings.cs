using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Nwe;

[Serializable]
public class NweSettings : SettingsSignalStrategyBase
{
    // Groupbox headers, spelled exactly as the Avalonia views do.
    private const string GroupEnvelope = "Settings Nadaraya Watson Envelope";

    // configuration:
    [SettingCaption("BandWidth", Group = GroupEnvelope)]
    public double BandWidth { get; set; } = 8.0;

    [SettingCaption("Multiplication", Group = GroupEnvelope)]
    public decimal Multiplication { get; set; } = 3.0m;

    [SettingCaption("With RSI oversold/overbought conditions")]
    public bool IncludeRsi { get; set; } = false;

    [SettingCaption("With SBM conditions MA-lines")]
    public bool IncludeSoftSbm { get; set; } = false;

    [SettingCaption("With SBM conditions percentages/crossings")]
    public bool IncludeSbmPercAndCrossing { get; set; } = false;

    // Volume-klimax op de signaalcandle: filter losse "stille drift door de band"-tikken weg.
    [SettingCaption("Require volume climax on signal candle")]
    public bool RequireVolumeClimax { get; set; } = false;

    [SettingCaption("Lookback (candles)", Indented = true, EnabledWhen = nameof(RequireVolumeClimax))]
    public int VolumeClimaxLookback { get; set; } = 20;

    [SettingCaption("Multiplier (x avg)", Indented = true, EnabledWhen = nameof(RequireVolumeClimax))]
    public decimal VolumeClimaxMultiplier { get; set; } = 1.5m;

    public NweSettings() : base()
    {
        SoundFileLong = "sound-nwe-oversold.wav";
        SoundFileShort = "sound-nwe-overbought.wav";
    }

}