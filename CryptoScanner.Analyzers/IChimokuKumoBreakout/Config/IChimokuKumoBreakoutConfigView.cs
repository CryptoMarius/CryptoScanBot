using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.IChimokuKumoBreakout.Config;

public class IChimokuKumoBreakoutConfigView : IConfigView
{
    private readonly StrategyIChimokuKumoBreakoutTabViewModel _viewModel = new();

    public string TabHeader => IChimokuKumoBreakoutPlugin.StrategyInternal.ToUpper();
    public string StrategyName => IChimokuKumoBreakoutPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyIChimokuKumoBreakoutTabView.axaml shows.
    public string StrategyTitle => "ICHIMOKU.KUMO.BREAKOUT - Ichimoku cloud breakout";
    public string StrategyDescription => "Price pushes through the Ichimoku cloud, a shift in the medium to long term sentiment";

    public object CreateSettingsView()
    {
        return new StrategyIChimokuKumoBreakoutTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static IChimokuKumoBreakoutSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as IChimokuKumoBreakoutSettings ?? IChimokuKumoBreakoutPlugin.Settings;
}
