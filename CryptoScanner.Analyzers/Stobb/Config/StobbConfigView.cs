using Avalonia.Controls;

using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.Stobb.Config;

public class StobbConfigView : IConfigView
{
    private readonly StrategyStobbTabViewModel _viewModel = new();

    public string TabHeader => StobbPlugin.StrategyInternal.ToUpper();

    public Control CreateSettingsView()
    {
        return new StrategyStobbTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        _viewModel.LoadConfig(StobbPlugin.Settings);
    }

    public void SaveConfig()
    {
        _viewModel.SaveConfig(StobbPlugin.Settings);
    }
}
