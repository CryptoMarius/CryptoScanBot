using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Stobb.Config;

public class StobbConfigView : IConfigView
{
    private readonly StrategyStobbTabViewModel _viewModel = new();

    public string TabHeader => StobbPlugin.StrategyInternal.ToUpper();
    public string StrategyName => StobbPlugin.StrategyInternal.ToLower();

    public object CreateSettingsView()
    {
        return new StrategyStobbTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static StobbSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as StobbSettings ?? StobbPlugin.Settings;
}
