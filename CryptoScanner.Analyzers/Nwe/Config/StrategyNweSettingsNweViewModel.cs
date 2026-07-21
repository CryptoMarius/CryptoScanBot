using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.Nwe.Config;

public partial class StrategyNweSettingsNweViewModel : ObservableObject
{
    [ObservableProperty]
    private double _bandWidth = 8.0;

    [ObservableProperty]
    private decimal _multiplication = 3.0m;

    public void LoadConfig(NweSettings settings)
    {
        BandWidth = settings.BandWidth;
        Multiplication = settings.Multiplication;
    }

    public void SaveConfig(NweSettings settings)
    {
        settings.BandWidth = BandWidth;
        settings.Multiplication = Multiplication;
    }
}
