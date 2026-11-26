using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.ImpulseBox.Config;

public class ImpulseBoxConfigView : IConfigView
{
    private readonly StrategyImpulseBoxTabViewModel _viewModel = new();

    public string TabHeader => ImpulseBoxPlugin.StrategyInternal;
    public string StrategyName => ImpulseBoxPlugin.StrategyInternal.ToLower();

    // The same two strings the header of StrategyImpulseBoxTabView.axaml shows.
    public string StrategyTitle => "IMPULSEBOX – Impulse followed by a box";
    public string StrategyDescription => "A strong move, then a box of bodies; in against the move when the box breaks the other way or a break with the move fails";
    // No wiki page yet.
    public string? WikiUrl => null;

    public object CreateSettingsView()
    {
        return new StrategyImpulseBoxTabView { DataContext = _viewModel };
    }

    public void LoadConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.LoadConfig(ToConcrete(settings));
    }

    public void SaveConfig(SettingsSignalStrategyBase settings)
    {
        _viewModel.SaveConfig(ToConcrete(settings));
    }

    private static ImpulseBoxSettings ToConcrete(SettingsSignalStrategyBase settings)
        => settings as ImpulseBoxSettings ?? ImpulseBoxPlugin.Settings;
}
