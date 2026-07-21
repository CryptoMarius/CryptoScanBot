// DEBUG-only, together with BbmaPlugin (the BBMA signal classes only exist in DEBUG builds).
using Avalonia.Controls;

using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.Bbma.Config;

public class BbmaConfigView : IConfigView
{
    private readonly StrategyBbmaTabViewModel _viewModel = new();

    public string TabHeader => BbmaPlugin.StrategyInternal.ToUpper();

    public Control CreateSettingsView()
    {
        return new StrategyBbmaTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        _viewModel.LoadConfig(BbmaPlugin.Settings);
    }

    public void SaveConfig()
    {
        _viewModel.SaveConfig(BbmaPlugin.Settings);
    }
}
