using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Tbo.Config;

public class TboConfigView : IConfigView
{
    private readonly StrategyTboTabViewModel _viewModel = new();

    public string TabHeader => TboPlugin.StrategyInternal.ToUpper();
    public string StrategyName => TboPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyTboTabView.axaml shows.
    public string StrategyTitle => "TBO – Trending break out";
    public string StrategyDescription => "Our reconstruction of the indicator behind the TBT signals: a two-EMA cloud for the trend, a pivot level for the break";
    public string WikiUrl => "https://github.com/CryptoMarius/CryptoScanBot/wiki/Trending-Breakout-(TBO)";

    public object CreateSettingsView()
    {
        return new StrategyTboTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static TboSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as TboSettings ?? TboPlugin.Settings;
}
