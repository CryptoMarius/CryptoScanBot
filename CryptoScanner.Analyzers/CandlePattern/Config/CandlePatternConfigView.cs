using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.CandlePattern.Config;

public class CandlePatternConfigView : IConfigView
{
    private readonly StrategyCandlePatternTabViewModel _viewModel = new();

    public string TabHeader => CandlePatternPlugin.StrategyInternal.ToUpper();
    public string StrategyName => CandlePatternPlugin.StrategyInternal.ToLower();

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
