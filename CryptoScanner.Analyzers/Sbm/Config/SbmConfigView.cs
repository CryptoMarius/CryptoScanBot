using Avalonia.Controls;

using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.Sbm.Config;

public class SbmConfigView : IConfigView
{
    private readonly StrategySbmTabViewModel _viewModel = new();

    public string TabHeader => SbmPlugin.StrategyInternal.ToUpper();

    public Control CreateSettingsView()
    {
        return new StrategySbmTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        _viewModel.LoadConfig(SbmPlugin.Settings);
    }

    public void SaveConfig()
    {
        _viewModel.SaveConfig(SbmPlugin.Settings);
    }
}
