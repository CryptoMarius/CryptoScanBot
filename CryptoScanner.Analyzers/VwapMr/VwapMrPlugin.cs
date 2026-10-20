using CryptoScanner.Analyzers.VwapMr.Chart;
using CryptoScanner.Analyzers.VwapMr.Config;
using CryptoScanner.Analyzers.VwapMr.Indicators;
using CryptoScanner.Analyzers.VwapMr.Signal;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.VwapMr;

/// <summary>
/// VWAP mean reversion: bands of a stdev of the close around the day VWAP, in on a touch of a band
/// with a reversal candle, out when a candle closes back at the VWAP (SignalCreateBase.IsExitSignal).
/// </summary>
public class VwapMrPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "VwapMr";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            StrategyInternal.ToLower(),
            typeof(VwapMrLong),
            typeof(VwapMrShort)
        ),
    ];

    public static VwapMrSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new VwapMrSettings();
        return Settings;
    }

    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not VwapMrSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IIndicatorExtension? CreateIndicatorExtension() => new VwapMrIndicatorExtension();

    public IChartOverlay? ChartOverlay { get; } = new VwapMrChartOverlay();
    public IConfigView? ConfigView { get; } = new VwapMrConfigView();
}
