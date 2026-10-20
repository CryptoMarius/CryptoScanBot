using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.VwapMr.Config;

public class VwapMrConfigView : IConfigView
{
    private readonly StrategyVwapMrTabViewModel _viewModel = new();

    public string TabHeader => VwapMrPlugin.StrategyInternal.ToUpper();
    public string StrategyName => VwapMrPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyVwapMrTabView.axaml shows.
    public string StrategyTitle => "VWAPMR – VWAP mean reversion";
    public string StrategyDescription => "A touch of a band around the day VWAP with a reversal candle opens the position, the price back at the VWAP closes it";
    // No wiki page yet.
    public string? WikiUrl => null;

    public object CreateSettingsView()
    {
        return new StrategyVwapMrTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static VwapMrSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as VwapMrSettings ?? VwapMrPlugin.Settings;
}
