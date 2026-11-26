using CryptoScanner.Analyzers.ImpulseBox.Chart;
using CryptoScanner.Analyzers.ImpulseBox.Config;
using CryptoScanner.Analyzers.ImpulseBox.Indicators;
using CryptoScanner.Analyzers.ImpulseBox.Signal;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.ImpulseBox;

/// <summary>
/// Impulse box: a strong move followed by a box of bodies. In against the impulse when the box
/// breaks the other way, or when a break with the impulse fails. The exit is the one of the trading
/// settings.
/// </summary>
public class ImpulseBoxPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "ImpulseBox";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            StrategyInternal.ToLower(),
            typeof(ImpulseBoxLong),
            typeof(ImpulseBoxShort)
        ),
    ];

    public static ImpulseBoxSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new ImpulseBoxSettings();
        return Settings;
    }

    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not ImpulseBoxSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IIndicatorExtension? CreateIndicatorExtension() => new ImpulseBoxIndicatorExtension();

    public IChartOverlay? ChartOverlay { get; } = new ImpulseBoxChartOverlay();
    public IConfigView? ConfigView { get; } = new ImpulseBoxConfigView();
}
