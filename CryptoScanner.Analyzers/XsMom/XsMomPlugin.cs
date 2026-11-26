using CryptoScanner.Analyzers.XsMom.Chart;
using CryptoScanner.Analyzers.XsMom.Config;
using CryptoScanner.Analyzers.XsMom.Signal;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.XsMom;

/// <summary>
/// Cross-sectional momentum: a ranking of all coins of a quote on their return over a number of days,
/// long the strongest and short the weakest, rebalanced once a week (SignalCreateBase.IsExitSignal
/// closes what dropped out). See XsMom.md.
/// </summary>
public class XsMomPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "XsMom";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            StrategyInternal.ToLower(),
            typeof(XsMomLong),
            typeof(XsMomShort)
        ),
    ];

    public static XsMomSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new XsMomSettings();
        return Settings;
    }

    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not XsMomSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IChartOverlay? ChartOverlay { get; } = new XsMomChartOverlay();
    public IConfigView? ConfigView { get; } = new XsMomConfigView();
}
