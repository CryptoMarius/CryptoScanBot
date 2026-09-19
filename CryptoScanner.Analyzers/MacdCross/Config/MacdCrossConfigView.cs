using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.MacdCross.Config;

public class MacdCrossConfigView : IConfigView
{
    private readonly StrategyMacdCrossTabViewModel _viewModel = new();

    public string TabHeader => MacdCrossPlugin.StrategyInternal.ToUpper();
    public string StrategyName => MacdCrossPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyMacdCrossTabView.axaml shows.
    public string StrategyTitle => "MACDCROSS – In on the cross, out on the cross back";
    public string StrategyDescription => "The MACD line crossing its signal line opens the position, the lines crossing back closes it";
    public string WikiUrl => "https://github.com/CryptoMarius/CryptoScanBot/wiki/MACD-Crossover-(MacdCross)";

    public object CreateSettingsView()
    {
        return new StrategyMacdCrossTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static MacdCrossSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as MacdCrossSettings ?? MacdCrossPlugin.Settings;
}
