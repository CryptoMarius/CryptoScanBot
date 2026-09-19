using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Stobb.Config;

public class StobbConfigView : IConfigView
{
    private readonly StrategyStobbTabViewModel _viewModel = new();

    public string TabHeader => StobbPlugin.StrategyInternal.ToUpper();
    public string StrategyName => StobbPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyStobbTabView.axaml shows.
    public string StrategyTitle => "STOBB – Stochastic + Bollinger Bands";
    public string StrategyDescription => "Mean reversion signals combining Stochastic with Bollinger Band position";
    public string WikiUrl => "https://github.com/CryptoMarius/CryptoScanBot/wiki/Stochastic-+-Bollinger-Bands-(STOBB)";

    public object CreateSettingsView()
    {
        return new StrategyStobbTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static StobbSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as StobbSettings ?? StobbPlugin.Settings;
}
