using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.MacdCross.Config;

public class MacdCrossConfigView : IConfigView
{
    private readonly StrategyMacdCrossTabViewModel _viewModel = new();

    public string TabHeader => MacdCrossPlugin.StrategyInternal.ToUpper();
    public string StrategyName => MacdCrossPlugin.StrategyInternal.ToLower();

    public object CreateSettingsView()
    {
        return new StrategyMacdCrossTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static MacdCrossSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as MacdCrossSettings ?? MacdCrossPlugin.Settings;
}
