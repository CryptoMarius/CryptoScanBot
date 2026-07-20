using Avalonia.Controls;

using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.Dlz.Config;

public class DlzConfigView : IConfigView
{
    private readonly StrategyDlzTabViewModel _viewModel = new();

    public string TabHeader => DlzPlugin.StrategyInternal.ToUpper();

    public Control CreateSettingsView()
    {
        return new StrategyDlzTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        _viewModel.LoadConfig(DlzPlugin.Settings);
    }

    public void SaveConfig()
    {
        _viewModel.SaveConfig(DlzPlugin.Settings);
    }
}
