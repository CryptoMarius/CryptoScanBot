using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.FailedBreakout;

/// <summary>
/// The break that did not hold: a new high or low over the lookback window, followed by a close back
/// inside it. Reads only candles, no indicators.
/// </summary>
public class FailedBreakoutPlugin : IStrategyPlugin
{
    private const string StrategyInternal = "FailedBreakout";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            StrategyInternal.ToLower(),
            typeof(Signal.FailedBreakoutLong),
            typeof(Signal.FailedBreakoutShort)
        ),
    ];

    public static FailedBreakoutSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new FailedBreakoutSettings();
        return Settings;
    }

    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not FailedBreakoutSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = null;
}
