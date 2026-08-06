using Avalonia.Controls;

using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.Nwe.Config;

public class StorsiConfigView : IConfigView
{
    private readonly StrategyNweTabViewModel _viewModel = new();

    public string TabHeader => NwePlugin.StrategyInternal.ToUpper();

    public object CreateSettingsView()
    {
        return new StrategyNweTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        _viewModel.LoadConfig("NWE", NwePlugin.Settings);
    }

    public void SaveConfig()
    {
        _viewModel.SaveConfig(NwePlugin.Settings);
    }
}
