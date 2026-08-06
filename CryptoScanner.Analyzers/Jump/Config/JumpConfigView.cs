using Avalonia.Controls;

using CryptoScanner.Core.Contracts;

namespace CryptoScanner.Analyzers.Jump.Config;

public class JumpConfigView : IConfigView
{
    private readonly StrategyJumpTabViewModel _viewModel = new();

    public string TabHeader => JumpPlugin.StrategyInternal.ToUpper();

    public object CreateSettingsView()
    {
        return new StrategyJumpTabView { DataContext = _viewModel };
    }

    public void LoadConfig()
    {
        _viewModel.LoadConfig(JumpPlugin.Settings);
    }

    public void SaveConfig()
    {
        _viewModel.SaveConfig(JumpPlugin.Settings);
    }
}
