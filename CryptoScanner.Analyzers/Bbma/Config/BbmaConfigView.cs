// DEBUG-only, together with BbmaPlugin (the BBMA signal classes only exist in DEBUG builds).
using Avalonia.Controls;

using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Bbma.Config;

public class BbmaConfigView : IConfigView
{
    private readonly StrategyBbmaTabViewModel _viewModel = new();

    public string TabHeader => "BBMA";
    public string StrategyName => BbmaPlugin.StrategyInternal.ToLower();

    public object CreateSettingsView()
    {
        return new StrategyBbmaTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static BbmaSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as BbmaSettings ?? BbmaPlugin.Settings;
}
