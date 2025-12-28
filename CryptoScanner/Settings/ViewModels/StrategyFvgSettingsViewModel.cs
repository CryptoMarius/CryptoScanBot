using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Settings.ViewModels;

public partial class StrategyFvgSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private double _minimumPercentage = 0.25;


    public void LoadConfig(SettingsSignalStrategyFvg settings)
    {
        MinimumPercentage = settings.MinimumPercentage;
    }

    public void SaveConfig(SettingsSignalStrategyFvg settings)
    {
        settings.MinimumPercentage = MinimumPercentage;
    }
}
