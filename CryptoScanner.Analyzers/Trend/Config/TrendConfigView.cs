using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Trend.Config;

public class TrendConfigView : IConfigView
{
    private readonly StrategyTrendTabViewModel _viewModel = new();

    public string TabHeader => TrendPlugin.StrategyInternal.ToUpper();
    public string StrategyName => TrendPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyTrendTabView.axaml shows.
    public string StrategyTitle => "TREND - Trend flip with a pullback entry";
    public string StrategyDescription => "Enters after the ZigZag trend flips, price pulls back to the pivot and closes back through it";

    public object CreateSettingsView()
    {
        return new StrategyTrendTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static TrendSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as TrendSettings ?? TrendPlugin.Settings;
}
