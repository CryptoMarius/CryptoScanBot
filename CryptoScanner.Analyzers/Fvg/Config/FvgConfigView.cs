using Avalonia.Controls;

using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.Fvg.Config;

public class FvgConfigView : IConfigView
{
    private readonly StrategyFvgTabViewModel _viewModel = new();

    public string TabHeader => FvgPlugin.StrategyInternal.ToUpper();

    public object CreateSettingsView()
    {
        return new StrategyFvgTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        _viewModel.LoadConfig(FvgPlugin.Settings);
    }

    public void SaveConfig()
    {
        _viewModel.SaveConfig(FvgPlugin.Settings);
    }
}
