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

    public void LoadConfig(SettingsSignalStrategyNwe settings)
    {
        IncludeRsi = settings.IncludeRsi;
        IncludeSoftSbm = settings.IncludeSoftSbm;
        IncludeSbmPercAndCrossing = settings.IncludeSbmPercAndCrossing;
        OnlyIfLux5m = settings.OnlyIfLux5m;
    }

    public void SaveConfig(SettingsSignalStrategyNwe settings)
    {
        settings.IncludeRsi = IncludeRsi;
        settings.IncludeSoftSbm = IncludeSoftSbm;
        settings.IncludeSbmPercAndCrossing = IncludeSbmPercAndCrossing;
        settings.OnlyIfLux5m = OnlyIfLux5m;
    }
}
