using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.SuperTrendBreakout.Config;

public class SuperTrendBreakoutConfigView : IConfigView
{
    private readonly StrategySuperTrendBreakoutTabViewModel _viewModel = new();

    public string TabHeader => SuperTrendBreakoutPlugin.StrategyInternal.ToUpper();
    public string StrategyName => SuperTrendBreakoutPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategySuperTrendBreakoutTabView.axaml shows.
    public string StrategyTitle => "SUPERTRENDBREAKOUT - SuperTrend flip at a zone";
    public string StrategyDescription => "The SuperTrend flips direction while price is near a dominant liquidity zone";

    public object CreateSettingsView()
    {
        return new StrategySuperTrendBreakoutTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static SuperTrendBreakoutSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as SuperTrendBreakoutSettings ?? SuperTrendBreakoutPlugin.Settings;
}
