using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Tbo.Config;

public class TboConfigView : IConfigView
{
    private readonly StrategyTboTabViewModel _viewModel = new();

    public string TabHeader => TboPlugin.StrategyInternal.ToUpper();
    public string StrategyName => TboPlugin.StrategyInternal.ToLower();

    public object CreateSettingsView()
    {
        return new StrategyTboTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static TboSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as TboSettings ?? TboPlugin.Settings;
}
