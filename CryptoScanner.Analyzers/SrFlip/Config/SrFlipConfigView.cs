using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.SrFlip.Config;

public class SrFlipConfigView : IConfigView
{
    private readonly StrategySrFlipTabViewModel _viewModel = new();

    public string TabHeader => SrFlipPlugin.StrategyInternal.ToUpper();
    public string StrategyName => SrFlipPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategySrFlipTabView.axaml shows.
    public string StrategyTitle => "SRFLIP – The support/resistance flip";
    public string StrategyDescription => "A broken level or line is retested from the other side and holds";
    public string WikiUrl => "https://github.com/CryptoMarius/CryptoScanBot/wiki/Support-Resistance-Flip-(SrFlip)";

    public object CreateSettingsView()
    {
        return new StrategySrFlipTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static SrFlipSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as SrFlipSettings ?? SrFlipPlugin.Settings;
}
