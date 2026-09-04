using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Core.Signal.Indicators;

namespace CryptoScanner.Analyzers.MacdCross;

/// <summary>
/// The MACD crossover: in when the MACD line crosses its signal line, out when the two cross back.
/// Reads the standard 12/26/9 MACD every hub already computes, and declares ADX(14) for its
/// optional trend-strength filters.
/// <para>
/// The first strategy with an exit rule of its own (SignalCreateBase.IsExitSignal). Stop loss and
/// take profit keep working next to it - this is an extra way out, not a replacement.
/// </para>
/// </summary>
public class MacdCrossPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "MacdCross";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            StrategyInternal.ToLower(),
            typeof(Signal.MacdCrossLong),
            typeof(Signal.MacdCrossShort)
        ),
    ];

    /// <summary>
    /// ADX(14) for the trend-strength filters. Declared here rather than switched on with the
    /// filters, because a registered plugin always gets what it declares: the filter can then never
    /// read a null because the indicator was not built. The cost is one Wilder smoothing per candle.
    /// </summary>
    public IReadOnlyList<IndicatorKey> RequiredIndicators { get; } =
    [
        IndicatorKey.Adx(14),
    ];

    public static MacdCrossSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new MacdCrossSettings();
        return Settings;
    }

    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not MacdCrossSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = new Config.MacdCrossConfigView();
}
