using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Trend;

public class TrendPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "Trend";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(StrategyInternal.ToLower(),
            typeof(Signal.SignalTrendLong),
            typeof(Signal.SignalTrendShort)
        ),

        // Same logic, but driven by the secondary (fine) trend slot instead of the primary (rough) one
        new("trend.secondary",
            typeof(Signal.SignalTrendSecondaryLong),
            typeof(Signal.SignalTrendSecondaryShort)
        ),
    ];

    public static TrendSettings Settings { get; internal set; } = new();
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not TrendSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new TrendSettings();
        return Settings;
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = null; // new Config.SignalConfigView();
}
