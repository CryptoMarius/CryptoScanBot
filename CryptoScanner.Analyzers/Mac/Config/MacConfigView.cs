using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Mac.Config;

public class MacConfigView : IConfigView
{
    private readonly StrategyMacTabViewModel _viewModel = new();

    public string TabHeader => MacPlugin.StrategyInternal.ToUpper();
    public string StrategyName => MacPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyMacTabView.axaml shows.
    public string StrategyTitle => "MAC – Moving average cloud";
    public string StrategyDescription => "A four-line moving-average cloud sets the trend; entries on the cloud cross, on a break through the last pivot level, or on a pullback to the fast line";
    public string WikiUrl => "https://github.com/CryptoMarius/CryptoScanBot/wiki/Moving-Average-Cloud-(MAC)";

    public object CreateSettingsView()
    {
        return new StrategyMacTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static MacSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as MacSettings ?? MacPlugin.Settings;
}
