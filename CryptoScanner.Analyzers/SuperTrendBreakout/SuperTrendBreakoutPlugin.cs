using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Core.Signal.Indicators;

namespace CryptoScanner.Analyzers.SuperTrendBreakout;

public class SuperTrendBreakoutPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "SuperTrendBreakout";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    // DEBUG only: the signal classes read CryptoData.SuperTrend*, which does not exist in a
    // production build. This plugin is registered behind #if DEBUG in AnalyzerRegistration as well,
    // so nobody reads this empty list there.
    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
#if DEBUG
        new("supertrendbreakout",
            typeof(Signal.SignalSuperTrendBreakoutLong),
            typeof(Signal.SignalSuperTrendBreakoutShort)
        ),
#endif
    ];

    public static SuperTrendBreakoutSettings Settings { get; internal set; } = new();
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not SuperTrendBreakoutSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new SuperTrendBreakoutSettings();
        return Settings;
    }

    public bool RequiresDlzZones => true;

    // Read from CandleData.SuperTrend / SuperTrendUpperBand / SuperTrendLowerBand, so DEBUG only
    // for the same reason. In a production build this list is empty and no SuperTrendHub is created.
    public IReadOnlyList<IndicatorKey> RequiredIndicators { get; } =
    [
#if DEBUG
        IndicatorKey.SuperTrend(10, 3.0),
#endif
    ];

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = new Config.SuperTrendBreakoutConfigView();
}
