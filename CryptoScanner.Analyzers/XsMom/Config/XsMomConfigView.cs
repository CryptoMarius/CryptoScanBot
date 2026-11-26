using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.XsMom.Config;

public class XsMomConfigView : IConfigView
{
    private readonly StrategyXsMomTabViewModel _viewModel = new();

    public string TabHeader => XsMomPlugin.StrategyInternal;
    public string StrategyName => XsMomPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyXsMomTabView.axaml shows.
    public string StrategyTitle => "XSMOM – Cross-sectional momentum";
    public string StrategyDescription => "A weekly ranking of all coins on their return: long the strongest, short the weakest, closed when they drop out";
    // No wiki page yet.
    public string? WikiUrl => null;

    public object CreateSettingsView()
    {
        return new StrategyXsMomTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static XsMomSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as XsMomSettings ?? XsMomPlugin.Settings;
}
