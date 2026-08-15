using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Dlz.Config;

public class DlzConfigView : IConfigView
{
    private readonly StrategyDlzTabViewModel _viewModel = new();

    public string TabHeader => DlzPlugin.StrategyInternal.ToUpper();
    public string StrategyName => DlzPlugin.StrategyInternal.ToLower();

    public object CreateSettingsView()
    {
        return new StrategyDlzTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static SettingsSignalStrategyDlz ToConcrete(SettingsSignalStrategyBase settings)
        => settings as SettingsSignalStrategyDlz ?? DlzPlugin.Settings;
}
