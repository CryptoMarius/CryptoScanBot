using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.Trend.Config;

public partial class StrategyTrendSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _invertDirection = false;

    [ObservableProperty]
    private bool _exitOnTrendRevert = false;

    public void LoadConfig(TrendSettings settings)
    {
        InvertDirection = settings.InvertDirection;
        ExitOnTrendRevert = settings.ExitOnTrendRevert;
    }

    public void SaveConfig(TrendSettings settings)
    {
        settings.InvertDirection = InvertDirection;
        settings.ExitOnTrendRevert = ExitOnTrendRevert;
    }
}
