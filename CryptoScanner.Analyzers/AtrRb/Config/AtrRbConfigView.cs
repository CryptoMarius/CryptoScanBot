using Avalonia.Controls;

using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.AtrRb.Config;

public class AtrRbConfigView : IConfigView
{
    private readonly StrategyAtrRbTabViewModel _viewModel = new();

    public string TabHeader => AtrRbPlugin.StrategyInternal.ToUpper();

    public Control CreateSettingsView()
    {
        return new StrategyAtrRbTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        _viewModel.LoadConfig(AtrRbPlugin.Settings);
    }

    public void SaveConfig()
    {
        _viewModel.SaveConfig(AtrRbPlugin.Settings);
    }
}
