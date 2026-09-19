using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.CandlePattern.Config;

public class CandlePatternConfigView : IConfigView
{
    private readonly StrategyCandlePatternTabViewModel _viewModel = new();

    public string TabHeader => CandlePatternPlugin.StrategyInternal.ToUpper();
    public string StrategyName => CandlePatternPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyCandlePatternTabView.axaml shows.
    public string StrategyTitle => "CANDLEPATTERN – Classic candlestick reversal patterns";
    public string StrategyDescription => "Fires on any of the ticked shapes; the trade side decides whether a shape is read bullish or bearish";
    public string WikiUrl => "https://github.com/CryptoMarius/CryptoScanBot/wiki/Candle-Patterns-(CandlePatterns)";

    public object CreateSettingsView()
    {
        return new StrategyCandlePatternTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static CandlePatternStrategySettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as CandlePatternStrategySettings ?? CandlePatternPlugin.Settings;
}
