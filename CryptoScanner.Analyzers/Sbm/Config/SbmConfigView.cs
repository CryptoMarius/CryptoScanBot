using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Sbm.Config;

public class SbmConfigView : IConfigView
{
    private readonly StrategySbmTabViewModel _viewModel = new();

    public string TabHeader => SbmPlugin.StrategyInternal.ToUpper();
    public string StrategyName => SbmPlugin.StrategyInternal.ToLower();

    public object CreateSettingsView()
    {
        return new StrategySbmTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static SbmSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as SbmSettings ?? SbmPlugin.Settings;
}
