using Avalonia.Controls;

using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Smc.Config;

public class SmcConfigView : IConfigView
{
    private readonly StrategySmcTabViewModel _viewModel = new();

    public string TabHeader => SmcPlugin.StrategyInternal.ToUpper();
    public string StrategyName => SmcPlugin.StrategyInternal.ToLower();

    public object CreateSettingsView()
    {
        return new StrategySmcTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static SettingsSignalStrategySmc ToConcrete(SettingsSignalStrategyBase settings)
        => settings as SettingsSignalStrategySmc ?? SmcPlugin.Settings;
}
