using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.HeikinAshi.Config;

public class HeikinAshiConfigView : IConfigView
{
    private readonly StrategyHeikinAshiTabViewModel _viewModel = new();

    public string TabHeader => HeikinAshiPlugin.StrategyInternal;
    public string StrategyName => HeikinAshiPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyHeikinAshiTabView.axaml shows.
    public string StrategyTitle => "HEIKINASHI – Heikin Ashi trend";
    public string StrategyDescription => "The Heikin Ashi candle turning with the higher interval on the same colour opens the position, the first candle of the other colour or with a wick on the other side closes it";
    // No wiki page yet.
    public string? WikiUrl => null;

    public object CreateSettingsView()
    {
        return new StrategyHeikinAshiTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static HeikinAshiSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as HeikinAshiSettings ?? HeikinAshiPlugin.Settings;
}
