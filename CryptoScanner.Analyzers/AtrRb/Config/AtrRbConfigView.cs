using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.AtrRb.Config;

public class AtrRbConfigView : IConfigView
{
    private readonly StrategyAtrRbTabViewModel _viewModel = new();

    public string TabHeader => AtrRbPlugin.StrategyInternal.ToUpper();
    public string StrategyName => AtrRbPlugin.StrategyInternal.ToLower();
    public string WikiUrl => "https://github.com/CryptoMarius/CryptoScanBot/wiki/ATR-Range-Breakout-(AtrRb)";

    public object CreateSettingsView()
    {
        return new StrategyAtrRbTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static AtrRbSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as AtrRbSettings ?? AtrRbPlugin.Settings;
}
