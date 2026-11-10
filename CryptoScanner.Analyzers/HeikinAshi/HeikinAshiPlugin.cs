using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Core.Signal.Indicators;

namespace CryptoScanner.Analyzers.HeikinAshi;

/// <summary>
/// The Heikin Ashi trend: in on the colour change of the Heikin Ashi candle with the higher interval
/// on the same colour, out on the first Heikin Ashi candle of the other colour or with a wick on the
/// other side. The Heikin Ashi candles are calculated in the strategy from the real candles, so it
/// declares no indicators. No chart overlay of its own.
/// </summary>
public class HeikinAshiPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "HeikinAshi";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            StrategyInternal.ToLower(),
            typeof(Signal.HeikinAshiLong),
            typeof(Signal.HeikinAshiShort)
        ),
    ];

    /// <summary>Nothing from the hub: the Heikin Ashi candles come from the real candles.</summary>
    public IReadOnlyList<IndicatorKey> RequiredIndicators { get; } = [];

    public static HeikinAshiSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new HeikinAshiSettings();
        return Settings;
    }

    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not HeikinAshiSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = new Config.HeikinAshiConfigView();
}
