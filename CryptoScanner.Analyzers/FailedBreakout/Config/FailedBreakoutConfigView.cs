using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.FailedBreakout.Config;

public class FailedBreakoutConfigView : IConfigView
{
    private readonly StrategyFailedBreakoutTabViewModel _viewModel = new();

    public string TabHeader => FailedBreakoutPlugin.StrategyInternal.ToUpper();
    public string StrategyName => FailedBreakoutPlugin.StrategyInternal.ToLower();

    public object CreateSettingsView()
    {
        return new StrategyFailedBreakoutTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static FailedBreakoutSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as FailedBreakoutSettings ?? FailedBreakoutPlugin.Settings;
}
