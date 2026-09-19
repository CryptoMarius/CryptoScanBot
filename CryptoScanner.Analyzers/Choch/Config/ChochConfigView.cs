using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Choch.Config;

public class ChochConfigView : IConfigView
{
    private readonly StrategyChochTabViewModel _viewModel = new();

    public string TabHeader => ChochPlugin.StrategyInternal.ToUpper();
    public string StrategyName => ChochPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyChochTabView.axaml shows.
    public string StrategyTitle => "CHOCH - Change of Character";
    public string StrategyDescription => "Fires when the ZigZag structure changes character; the pullback variants wait for an opposite pivot and a break through it";

    public object CreateSettingsView()
    {
        return new StrategyChochTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static ChochSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as ChochSettings ?? ChochPlugin.Settings;
}
