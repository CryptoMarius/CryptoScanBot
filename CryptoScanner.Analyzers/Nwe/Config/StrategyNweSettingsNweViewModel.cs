using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.Nwe.Config;

public partial class StrategyNweSettingsNweViewModel : ObservableObject
{
    [ObservableProperty]
    private double _bbMinPercentage = 1.50;

    [ObservableProperty]
    private double _bbMaxPercentage = 0.0;

    [ObservableProperty]
    private bool _requireRecentStobbOrStorsi = true;

    [ObservableProperty]
    private double _bandWidth = 8.0;

    [ObservableProperty]
    private decimal _multiplication = 3.0m;

    public void LoadConfig(NweSettings settings)
    {
        BbMinPercentage = settings.BBMinPercentage;
        BbMaxPercentage = settings.BBMaxPercentage;
        RequireRecentStobbOrStorsi = settings.RequireRecentStobbOrStorsi;
        BandWidth = settings.BandWidth;
        Multiplication = settings.Multiplication;
    }

    public void SaveConfig(NweSettings settings)
    {
        settings.BBMinPercentage = BbMinPercentage;
        settings.BBMaxPercentage = BbMaxPercentage;
        settings.RequireRecentStobbOrStorsi = RequireRecentStobbOrStorsi;
        settings.BandWidth = BandWidth;
        settings.Multiplication = Multiplication;
    }
}
