using Avalonia.Controls;

using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.Vbs.Config;

public class VbsConfigView : IConfigView
{
    private readonly StrategyVbsTabViewModel _viewModel = new();

    public string TabHeader => VbsPlugin.StrategyInternal.ToUpper();

    public object CreateSettingsView()
    {
        return new StrategyVbsTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        _viewModel.LoadConfig(VbsPlugin.Settings);
    }

    public void SaveConfig()
    {
        _viewModel.SaveConfig(VbsPlugin.Settings);
    }
}
