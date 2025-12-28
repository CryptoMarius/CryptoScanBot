using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Settings.ViewModels;

public partial class StrategyDlzSettingsUnzoomedBoxViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _zonesApplyUnzoomed = false;

    [ObservableProperty]
    private double _minimumUnZoomedPercentage = 0.0; 

    [ObservableProperty]
    private double _maximumUnZoomedPercentage = 0.0; 

    public void LoadConfig(SettingsSignalStrategyZones settings)
    {
        ZonesApplyUnzoomed = settings.ZonesApplyUnzoomed;
        MinimumUnZoomedPercentage = settings.MinimumUnZoomedPercentage;
        MaximumUnZoomedPercentage = settings.MaximumUnZoomedPercentage;
    }

    public void SaveConfig(SettingsSignalStrategyZones settings)
    {
        settings.ZonesApplyUnzoomed = ZonesApplyUnzoomed;
        settings.MinimumUnZoomedPercentage = MinimumUnZoomedPercentage;
        settings.MaximumUnZoomedPercentage = MaximumUnZoomedPercentage;
    }
}
