using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.Nwe.Config;

public partial class StrategyNweSettingsViewModel : ObservableObject
{
    // EXACT SAME TYPES as SettingsSignalStrategyNwe

    [ObservableProperty]
    private bool _includeRsi = false; // bool

    [ObservableProperty]
    private bool _includeSoftSbm = false; // bool (this is IncludeSbmMaLines in UI)

    [ObservableProperty]
    private bool _includeSbmPercAndCrossing = false; // bool

    [ObservableProperty]
    private bool _requireVolumeClimax = false; // bool

    [ObservableProperty]
    private int _volumeClimaxLookback = 20; // int

    [ObservableProperty]
    private decimal _volumeClimaxMultiplier = 1.5m; // decimal

    public void LoadConfig(NweSettings settings)
    {
        IncludeRsi = settings.IncludeRsi;
        IncludeSoftSbm = settings.IncludeSoftSbm;
        IncludeSbmPercAndCrossing = settings.IncludeSbmPercAndCrossing;
        RequireVolumeClimax = settings.RequireVolumeClimax;
        VolumeClimaxLookback = settings.VolumeClimaxLookback;
        VolumeClimaxMultiplier = settings.VolumeClimaxMultiplier;
    }

    public void SaveConfig(NweSettings settings)
    {
        settings.IncludeRsi = IncludeRsi;
        settings.IncludeSoftSbm = IncludeSoftSbm;
        settings.IncludeSbmPercAndCrossing = IncludeSbmPercAndCrossing;
        settings.RequireVolumeClimax = RequireVolumeClimax;
        settings.VolumeClimaxLookback = VolumeClimaxLookback;
        settings.VolumeClimaxMultiplier = VolumeClimaxMultiplier;
    }
}
