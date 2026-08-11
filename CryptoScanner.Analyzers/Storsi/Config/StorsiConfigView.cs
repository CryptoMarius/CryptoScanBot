using Avalonia.Controls;

using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Storsi.Config;

public class StorsiConfigView : IConfigView
{
    private readonly StrategyStorsiTabViewModel _viewModel = new();

    public string TabHeader => StorsiPlugin.StrategyInternal.ToUpper();
    public string StrategyName => StorsiPlugin.StrategyInternal.ToLower();

    public object CreateSettingsView()
    {
        return new StrategyStorsiTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static StoRsiSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as StoRsiSettings ?? StorsiPlugin.Settings;
}
