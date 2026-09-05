using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.MacdCrossBand.Config;

public class MacdCrossBandConfigView : IConfigView
{
    private readonly StrategyMacdCrossBandTabViewModel _viewModel = new();

    public string TabHeader => MacdCrossBandPlugin.StrategyInternal.ToUpper();
    public string StrategyName => MacdCrossBandPlugin.StrategyInternal.ToLower();

    public object CreateSettingsView()
    {
        return new StrategyMacdCrossBandTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static MacdCrossBandSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as MacdCrossBandSettings ?? MacdCrossBandPlugin.Settings;
}
