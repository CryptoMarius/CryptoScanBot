using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.MacdCrossBand.Config;

public class MacdCrossBandConfigView : IConfigView
{
    private readonly StrategyMacdCrossBandTabViewModel _viewModel = new();

    public string TabHeader => MacdCrossBandPlugin.StrategyInternal.ToUpper();
    public string StrategyName => MacdCrossBandPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyMacdCrossBandTabView.axaml shows.
    public string StrategyTitle => "MACDCROSSBAND – The cross, but only after a band break";
    public string StrategyDescription => "The MACD line crossing its signal line while the price broke a Vbs, AtrRb or Dbr band in the last few candles";
    public string WikiUrl => "https://github.com/CryptoMarius/CryptoScanBot/wiki/MACD-Crossover-Band-(MacdCrossBand)";

    public object CreateSettingsView()
    {
        return new StrategyMacdCrossBandTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static MacdCrossBandSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as MacdCrossBandSettings ?? MacdCrossBandPlugin.Settings;
}
