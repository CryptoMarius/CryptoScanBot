using Avalonia.Controls;

using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.Baba.Config;

public class BabaConfigView : IConfigView
{
    private readonly StrategyBabaTabViewModel _viewModel = new();

    public string TabHeader => "Baba";

    public Control CreateSettingsView()
    {
        return new StrategyBabaTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        _viewModel.LoadConfig(BabaPlugin.Settings);
    }

    public void SaveConfig()
    {
        _viewModel.SaveConfig(BabaPlugin.Settings);
    }
}
