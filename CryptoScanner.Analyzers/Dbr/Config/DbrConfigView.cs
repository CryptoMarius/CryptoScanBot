using Avalonia.Controls;

using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Dbr.Config;

public class DbrConfigView : IConfigView
{
    private readonly StrategyDbrTabViewModel _viewModel = new();

    public string TabHeader => DbrPlugin.StrategyInternal.ToUpper();
    public string StrategyName => DbrPlugin.StrategyInternal.ToLower();

    public object CreateSettingsView()
    {
        return new StrategyDbrTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static DbrSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as DbrSettings ?? DbrPlugin.Settings;
}
