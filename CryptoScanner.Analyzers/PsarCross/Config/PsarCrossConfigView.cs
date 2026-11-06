using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.PsarCross.Config;

public class PsarCrossConfigView : IConfigView
{
    private readonly StrategyPsarCrossTabViewModel _viewModel = new();

    public string TabHeader => PsarCrossPlugin.StrategyInternal;
    public string StrategyName => PsarCrossPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyPsarCrossTabView.axaml shows.
    public string StrategyTitle => "PSARCROSS – In on the flip, out on the flip back";
    public string StrategyDescription => "The parabolic SAR flipping to the other side of the price opens the position, the SAR flipping back closes it";
    public string WikiUrl => "https://github.com/CryptoMarius/CryptoScanBot/wiki/Parabolic-SAR-Cross-(PsarCross)";

    public object CreateSettingsView()
    {
        return new StrategyPsarCrossTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static PsarCrossSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as PsarCrossSettings ?? PsarCrossPlugin.Settings;
}
