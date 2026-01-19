using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyDlzSettingsZoomedBoxViewModel : ObservableObject
{
    // EXACT SAME TYPES as SettingsSignalStrategyZones
    
    [ObservableProperty]
    private bool _zoomLowerTimeFrames = true; // bool

    [ObservableProperty]
    private double _minimumZoomedPercentage = 0.2; // double

    [ObservableProperty]
    private double _maximumZoomedPercentage = 0.7; // double

    public void LoadConfig(SettingsSignalStrategyZones settings)
    {
        ZoomLowerTimeFrames = settings.ZoomLowerTimeFrames;
        MinimumZoomedPercentage = settings.MinimumZoomedPercentage;
        MaximumZoomedPercentage = settings.MaximumZoomedPercentage;
    }

    public void SaveConfig(SettingsSignalStrategyZones settings)
    {
        settings.ZoomLowerTimeFrames = ZoomLowerTimeFrames;
        settings.MinimumZoomedPercentage = MinimumZoomedPercentage;
        settings.MaximumZoomedPercentage = MaximumZoomedPercentage;
    }
}
