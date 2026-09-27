using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.BbRsiEngulfing;

/// <summary>
/// Bollinger Bands, RSI and an engulfing candle: a long when an engulfing candle forms at the
/// lower band with an oversold RSI, a short at the upper band with an overbought RSI. The header
/// used to describe the Ichimoku Kumo breakout, copied along with the file (open point 112).
/// </summary>
public class BbRsiEngulfingPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "BbRsiEngulfing";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            StrategyInternal.ToLower(),
            typeof(Signal.BbRsiEngulfingLong),
            typeof(Signal.BbRsiEngulfingShort)
        ),
    ];

    public static BbRsiEngulfingSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new BbRsiEngulfingSettings();
        return Settings;
    }
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not BbRsiEngulfingSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = new Config.BbRsiEngulfingConfigView();
}
