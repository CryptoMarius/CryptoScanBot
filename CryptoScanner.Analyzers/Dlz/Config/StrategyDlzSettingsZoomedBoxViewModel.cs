using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Dlz.Config;

public partial class StrategyDlzSettingsZoomedBoxViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _zoomLowerTimeFrames = true;

    [ObservableProperty]
    private double _minimumZoomedPercentage = 0.2;

    [ObservableProperty]
    private double _maximumZoomedPercentage = 0.7;

    public void LoadConfig(SettingsSignalStrategyDlz settings)
    {
        ZoomLowerTimeFrames = settings.ZoomLowerTimeFrames;
        MinimumZoomedPercentage = settings.MinimumZoomedPercentage;
        MaximumZoomedPercentage = settings.MaximumZoomedPercentage;
    }

    public void SaveConfig(SettingsSignalStrategyDlz settings)
    {
        settings.ZoomLowerTimeFrames = ZoomLowerTimeFrames;
        settings.MinimumZoomedPercentage = MinimumZoomedPercentage;
        settings.MaximumZoomedPercentage = MaximumZoomedPercentage;
    }
}
