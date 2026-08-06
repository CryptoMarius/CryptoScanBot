using Avalonia.Controls;

using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.Dbr.Config;

public class DbrConfigView : IConfigView
{
    private readonly StrategyDbrTabViewModel _viewModel = new();

    public string TabHeader => DbrPlugin.StrategyInternal.ToUpper();

    public object CreateSettingsView()
    {
        return new StrategyDbrTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        _viewModel.LoadConfig(DbrPlugin.Settings);
    }

    public void SaveConfig()
    {
        _viewModel.SaveConfig(DbrPlugin.Settings);
    }
}
