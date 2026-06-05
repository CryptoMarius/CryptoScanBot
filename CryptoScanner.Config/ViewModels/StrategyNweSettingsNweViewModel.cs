using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyNweSettingsNweViewModel : ObservableObject
{
    [ObservableProperty]
    private double _bandWidth = 8.0;

    [ObservableProperty]
    private decimal _multiplication = 3.0m;

    public void LoadConfig(SettingsSignalStrategyNwe settings)
    {
        BandWidth = settings.BandWidth;
        Multiplication = settings.Multiplication;
    }

    public void SaveConfig(SettingsSignalStrategyNwe settings)
    {
        settings.BandWidth = BandWidth;
        settings.Multiplication = Multiplication;
    }
}
