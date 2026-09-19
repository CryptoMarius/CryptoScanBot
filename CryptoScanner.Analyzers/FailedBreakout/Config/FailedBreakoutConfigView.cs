using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.FailedBreakout.Config;

public class FailedBreakoutConfigView : IConfigView
{
    private readonly StrategyFailedBreakoutTabViewModel _viewModel = new();

    public string TabHeader => FailedBreakoutPlugin.StrategyInternal.ToUpper();
    public string StrategyName => FailedBreakoutPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyFailedBreakoutTabView.axaml shows.
    public string StrategyTitle => "FAILEDBREAKOUT – The break that did not hold";
    public string StrategyDescription => "Price sets a new high or low over the lookback window and then closes back inside it";
    public string WikiUrl => "https://github.com/CryptoMarius/CryptoScanBot/wiki/Failed-Breakout-(FailedBreakout)";

    public object CreateSettingsView()
    {
        return new StrategyFailedBreakoutTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static FailedBreakoutSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as FailedBreakoutSettings ?? FailedBreakoutPlugin.Settings;
}
