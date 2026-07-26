using Avalonia.Controls;

using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.KumoSqueeze.Config;

public class KumoSqueezeConfigView : IConfigView
{
    private readonly StrategyKumoSqueezeTabViewModel _viewModel = new();

    public string TabHeader => "KUMOSQUEEZE";

    public Control CreateSettingsView()
    {
        return new StrategyKumoSqueezeTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        _viewModel.LoadConfig(KumoSqueezePlugin.Settings);
    }

    public void SaveConfig()
    {
        _viewModel.SaveConfig(KumoSqueezePlugin.Settings);
    }
}
