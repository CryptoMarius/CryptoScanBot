using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.ChochFvg.Config;

public class ChochFvgConfigView : IConfigView
{
    private readonly StrategyChochFvgTabViewModel _viewModel = new();

    public string TabHeader => ChochFvgPlugin.StrategyInternal;
    public string StrategyName => ChochFvgPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyChochFvgTabView.axaml shows.
    public string StrategyTitle => "CHOCH.FVG – Change of character into a fair value gap";
    public string StrategyDescription => "After the first close beyond the last swing against the trend, a limit on the middle of a fair value gap of that leg inside its fib zone, a stop behind the gap candle and a fixed risk/reward target";
    // No wiki page yet.
    public string? WikiUrl => null;

    public object CreateSettingsView()
    {
        return new StrategyChochFvgTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static ChochFvgSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as ChochFvgSettings ?? ChochFvgPlugin.Settings;
}
