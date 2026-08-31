using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Nwe.Config;

public class StorsiConfigView : IConfigView
{
    private readonly StrategyNweTabViewModel _viewModel = new();

    public string TabHeader => NwePlugin.StrategyInternal.ToUpper();
    public string StrategyName => NwePlugin.StrategyInternal.ToLower();

    public object CreateSettingsView()
    {
        return new StrategyNweTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static NweSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as NweSettings ?? NwePlugin.Settings;
}
