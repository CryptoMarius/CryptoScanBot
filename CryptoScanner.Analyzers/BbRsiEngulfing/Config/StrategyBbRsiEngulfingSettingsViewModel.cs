using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.BbRsiEngulfing.Config;

public partial class StrategyBbRsiEngulfingSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _useStrictEngulfing = false;

    public void LoadConfig(BbRsiEngulfingSettings settings)
    {
        UseStrictEngulfing = settings.UseStrictEngulfing;
    }

    public void SaveConfig(BbRsiEngulfingSettings settings)
    {
        settings.UseStrictEngulfing = UseStrictEngulfing;
    }
}
