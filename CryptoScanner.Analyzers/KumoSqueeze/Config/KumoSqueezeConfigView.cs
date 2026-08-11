using Avalonia.Controls;

using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.KumoSqueeze.Config;

public class KumoSqueezeConfigView : IConfigView
{
    private readonly StrategyKumoSqueezeTabViewModel _viewModel = new();

    public string TabHeader => "KUMOSQUEEZE";
    public string StrategyName => "kumosqueeze";

    public object CreateSettingsView()
    {
        return new StrategyKumoSqueezeTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static KumoSqueezeSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as KumoSqueezeSettings ?? KumoSqueezePlugin.Settings;
}
