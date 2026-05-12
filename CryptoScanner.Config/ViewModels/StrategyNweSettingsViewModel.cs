using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

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
    private bool _onlyIfLux5m = false; // bool

    [ObservableProperty]
    private bool _requireVolumeClimax = false; // bool

    [ObservableProperty]
    private int _volumeClimaxLookback = 20; // int

    [ObservableProperty]
    private decimal _volumeClimaxMultiplier = 1.5m; // decimal

    public void LoadConfig(SettingsSignalStrategyNwe settings)
    {
        IncludeRsi = settings.IncludeRsi;
        IncludeSoftSbm = settings.IncludeSoftSbm;
        IncludeSbmPercAndCrossing = settings.IncludeSbmPercAndCrossing;
        OnlyIfLux5m = settings.OnlyIfLux5m;
        RequireVolumeClimax = settings.RequireVolumeClimax;
        VolumeClimaxLookback = settings.VolumeClimaxLookback;
        VolumeClimaxMultiplier = settings.VolumeClimaxMultiplier;
    }

    public void SaveConfig(SettingsSignalStrategyNwe settings)
    {
        settings.IncludeRsi = IncludeRsi;
        settings.IncludeSoftSbm = IncludeSoftSbm;
        settings.IncludeSbmPercAndCrossing = IncludeSbmPercAndCrossing;
        settings.OnlyIfLux5m = OnlyIfLux5m;
        settings.RequireVolumeClimax = RequireVolumeClimax;
        settings.VolumeClimaxLookback = VolumeClimaxLookback;
        settings.VolumeClimaxMultiplier = VolumeClimaxMultiplier;
    }
}
