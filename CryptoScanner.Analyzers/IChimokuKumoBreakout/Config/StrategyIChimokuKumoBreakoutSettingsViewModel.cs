using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.IChimokuKumoBreakout.Config;

// The strategy had no settings of its own until it got its own BB range on 26-09-2026 (open
// point 109); before that it read stobb's.
public partial class StrategyIChimokuKumoBreakoutSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private double _bbMinPercentage = 1.50;

    [ObservableProperty]
    private double _bbMaxPercentage = 0.0;

    public void LoadConfig(IChimokuKumoBreakoutSettings settings)
    {
        BbMinPercentage = settings.BBMinPercentage;
        BbMaxPercentage = settings.BBMaxPercentage;
    }

    public void SaveConfig(IChimokuKumoBreakoutSettings settings)
    {
        settings.BBMinPercentage = BbMinPercentage;
        settings.BBMaxPercentage = BbMaxPercentage;
    }
}
