using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Vbs.Config;

public class VbsConfigView : IConfigView
{
    private readonly StrategyVbsTabViewModel _viewModel = new();

    public string TabHeader => VbsPlugin.StrategyInternal.ToUpper();
    public string StrategyName => VbsPlugin.StrategyInternal.ToLower();

    public object CreateSettingsView()
    {
        return new StrategyVbsTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static VbsSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as VbsSettings ?? VbsPlugin.Settings;
}
