using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.BbRsiEngulfing.Config;

public class BbRsiEngulfingConfigView : IConfigView
{
    private readonly StrategyBbRsiEngulfingTabViewModel _viewModel = new();

    public string TabHeader => BbRsiEngulfingPlugin.StrategyInternal.ToUpper();
    public string StrategyName => BbRsiEngulfingPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyBbRsiEngulfingTabView.axaml shows.
    public string StrategyTitle => "BBRSIENGULFING - Bollinger Band + RSI + engulfing candle";
    public string StrategyDescription => "The previous candle closes outside the Bollinger band with RSI at its extreme, and the next one engulfs it";

    public object CreateSettingsView()
    {
        return new StrategyBbRsiEngulfingTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static BbRsiEngulfingSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as BbRsiEngulfingSettings ?? BbRsiEngulfingPlugin.Settings;
}
