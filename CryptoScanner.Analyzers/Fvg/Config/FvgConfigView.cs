using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Fvg.Config;

public class FvgConfigView : IConfigView
{
    private readonly StrategyFvgTabViewModel _viewModel = new();

    public string TabHeader => FvgPlugin.StrategyInternal.ToUpper();
    public string StrategyName => FvgPlugin.StrategyInternal.ToLower();

    public object CreateSettingsView()
    {
        return new StrategyFvgTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static SettingsSignalStrategyFvg ToConcrete(SettingsSignalStrategyBase settings)
        => settings as SettingsSignalStrategyFvg ?? FvgPlugin.Settings;
}
