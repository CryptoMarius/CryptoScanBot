using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.FvgLadder.Config;

public class FvgLadderConfigView : IConfigView
{
    private readonly StrategyFvgLadderTabViewModel _viewModel = new();

    public string TabHeader => FvgLadderPlugin.StrategyInternal.ToUpper();
    public string StrategyName => FvgLadderPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyFvgLadderTabView.axaml shows.
    public string StrategyTitle => "FVGLADDER – FVG & DCA (no stop-loss)";
    public string StrategyDescription => "Buy limits on bullish gap tops above the day EMA200, DCA's on doubling sizes, take profit above break-even, several positions per coin";
    public string WikiUrl => "https://github.com/CryptoMarius/CryptoScanBot/wiki/FVG-Ladder-(FvgLadder)";

    public object CreateSettingsView()
    {
        return new StrategyFvgLadderTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static FvgLadderSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as FvgLadderSettings ?? FvgLadderPlugin.Settings;
}
