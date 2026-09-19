using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Nwe.Config;

public class NweConfigView : IConfigView
{
    private readonly StrategyNweTabViewModel _viewModel = new();

    public string TabHeader => NwePlugin.StrategyInternal.ToUpper();
    public string StrategyName => NwePlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyNweTabView.axaml shows.
    public string StrategyTitle => "NWE – Nadaraya-Watson Envelope";
    public string StrategyDescription => "Counter-trend reversal signals at statistical price extremes";
    public string WikiUrl => "https://github.com/CryptoMarius/CryptoScanBot/wiki/Nadaraya-Watson-Envelope-(NWE)";

    public object CreateSettingsView()
    {
        return new StrategyNweTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static NweSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as NweSettings ?? NwePlugin.Settings;
}
