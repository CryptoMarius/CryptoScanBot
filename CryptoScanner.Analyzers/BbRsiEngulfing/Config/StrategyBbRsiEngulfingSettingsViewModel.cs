using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.BbRsiEngulfing.Config;

public partial class StrategyBbRsiEngulfingSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private double _bbMinPercentage = 1.50;

    [ObservableProperty]
    private double _bbMaxPercentage = 0.0;

    [ObservableProperty]
    private bool _useStrictEngulfing = false;

    public void LoadConfig(BbRsiEngulfingSettings settings)
    {
        BbMinPercentage = settings.BBMinPercentage;
        BbMaxPercentage = settings.BBMaxPercentage;
        UseStrictEngulfing = settings.UseStrictEngulfing;
    }

    public void SaveConfig(BbRsiEngulfingSettings settings)
    {
        settings.BBMinPercentage = BbMinPercentage;
        settings.BBMaxPercentage = BbMaxPercentage;
        settings.UseStrictEngulfing = UseStrictEngulfing;
    }
}
