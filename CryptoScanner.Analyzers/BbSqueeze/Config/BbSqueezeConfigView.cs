using Avalonia.Controls;

using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.BbSqueeze.Config;

public class BbSqueezeConfigView : IConfigView
{
    private readonly StrategyBbSqueezeTabViewModel _viewModel = new();

    public string TabHeader => BbSqueezePlugin.StrategyInternal.ToUpper();
    public string StrategyName => BbSqueezePlugin.StrategyInternal.ToLower();

    public object CreateSettingsView()
    {
        return new StrategyBbSqueezeTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static BbSqueezeSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as BbSqueezeSettings ?? BbSqueezePlugin.Settings;
}
