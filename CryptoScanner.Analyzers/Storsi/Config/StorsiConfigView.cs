using Avalonia.Controls;

using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.Storsi.Config;

public class StorsiConfigView : IConfigView
{
    private readonly StrategyStorsiTabViewModel _viewModel = new();

    public string TabHeader => StoRsiPlugin.StrategyInternal.ToUpper();

    public Control CreateSettingsView()
    {
        return new StrategyStorsiTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        _viewModel.LoadConfig(StoRsiPlugin.Settings);
    }

    public void SaveConfig()
    {
        _viewModel.SaveConfig(StoRsiPlugin.Settings);
    }
}
