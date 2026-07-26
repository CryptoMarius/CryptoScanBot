using Avalonia.Controls;

using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.BbSqueeze.Config;

public class BbSqueezeConfigView : IConfigView
{
    private readonly StrategyBbSqueezeTabViewModel _viewModel = new();

    public string TabHeader => BbSqueezePlugin.StrategyInternal.ToUpper();

    public Control CreateSettingsView()
    {
        return new StrategyBbSqueezeTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        _viewModel.LoadConfig(BbSqueezePlugin.Settings);
    }

    public void SaveConfig()
    {
        _viewModel.SaveConfig(BbSqueezePlugin.Settings);
    }
}
