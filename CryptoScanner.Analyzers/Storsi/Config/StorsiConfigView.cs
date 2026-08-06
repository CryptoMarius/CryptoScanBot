using Avalonia.Controls;

using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.Storsi.Config;

public class StorsiConfigView : IConfigView
{
    private readonly StrategyStorsiTabViewModel _viewModel = new();

    public string TabHeader => StorsiPlugin.StrategyInternal.ToUpper();

    public object CreateSettingsView()
    {
        return new StrategyStorsiTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        _viewModel.LoadConfig(StorsiPlugin.Settings);
    }

    public void SaveConfig()
    {
        _viewModel.SaveConfig(StorsiPlugin.Settings);
    }
}
