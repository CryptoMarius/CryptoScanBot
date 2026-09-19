using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Jump.Config;

public class JumpConfigView : IConfigView
{
    private readonly StrategyJumpTabViewModel _viewModel = new();

    public string TabHeader => JumpPlugin.StrategyInternal.ToUpper();
    public string StrategyName => JumpPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyJumpTabView.axaml shows.
    public string StrategyTitle => "JUMP – Jump";
    public string StrategyDescription => "Detects sudden large price movements";
    public string WikiUrl => "https://github.com/CryptoMarius/CryptoScanBot/wiki/Candle-Jump-(Jump)";

    public object CreateSettingsView()
    {
        return new StrategyJumpTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static JumpSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as JumpSettings ?? JumpPlugin.Settings;
}
