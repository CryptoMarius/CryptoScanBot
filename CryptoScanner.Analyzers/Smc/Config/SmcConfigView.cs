using Avalonia.Controls;

using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.Smc.Config;

public class SmcConfigView : IConfigView
{
    private readonly StrategySmcTabViewModel _viewModel = new();

    public string TabHeader => SmcPlugin.StrategyInternal.ToUpper();

    public Control CreateSettingsView()
    {
        return new StrategySmcTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        _viewModel.LoadConfig(SmcPlugin.Settings);
    }

    public void SaveConfig()
    {
        _viewModel.SaveConfig(SmcPlugin.Settings);
    }
}
