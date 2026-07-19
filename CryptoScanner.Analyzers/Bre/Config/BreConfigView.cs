using Avalonia.Controls;

using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.Bre.Config;

public class BreConfigView : IConfigView
{
    private readonly StrategyBreTabViewModel _viewModel = new();

    public string TabHeader => "Bre";

    public Control CreateSettingsView()
    {
        return new StrategyBreTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        _viewModel.LoadConfig(BrePlugin.Settings);
    }

    public void SaveConfig()
    {
        _viewModel.SaveConfig(BrePlugin.Settings);
    }
}
