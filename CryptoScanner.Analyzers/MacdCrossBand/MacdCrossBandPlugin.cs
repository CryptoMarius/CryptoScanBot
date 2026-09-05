using CryptoScanner.Analyzers.Vbs;
using CryptoScanner.Analyzers.Vbs.Indicators;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Core.Signal.Indicators;

namespace CryptoScanner.Analyzers.MacdCrossBand;

/// <summary>
/// The MACD crossover, but only when the price has been at a band of Vbs, AtrRb or Dbr in the last
/// N candles. A variant of the MacdCross strategy: the same entry and the same exit, with one extra
/// question asked last.
/// <para>
/// Built to point at charts, not to trade: a cross on its own fires often, a cross right after
/// price stretched to a band is the rarer situation that is worth looking at.
/// </para>
/// </summary>
public class MacdCrossBandPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "MacdCrossBand";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            StrategyInternal.ToLower(),
            typeof(Signal.MacdCrossBandLong),
            typeof(Signal.MacdCrossBandShort)
        ),
    ];

    /// <summary>ADX(14), for the same optional trend-strength filters the plain MacdCross has.</summary>
    public IReadOnlyList<IndicatorKey> RequiredIndicators { get; } =
    [
        IndicatorKey.Adx(14),
    ];

    /// <summary>
    /// The VBS bands, so the VBS lookback has something to read. They are computed by the VBS
    /// plugin's own extension, which writes them into the shared per-candle slot (VbsCandleData) -
    /// this strategy borrows that extension rather than reimplementing the band maths, so the two
    /// always see identical bands. The AtrRb and Dbr lookbacks need nothing here: those compute
    /// their bands from the candle list at the moment they are asked.
    /// <para>
    /// Null when a VBS strategy is enabled as well: the hub then already runs that same extension,
    /// and a second one would compute the same VWMA pair twice for the same slot. The hub asks this
    /// once per settings generation (IndicatorConfiguration.Version), which is bumped whenever
    /// settings are applied, so enabling or disabling VBS afterwards is picked up.
    /// </para>
    /// </summary>
    public IIndicatorExtension? CreateIndicatorExtension()
    {
        string vbs = VbsPlugin.StrategyInternal.ToLower();
        bool vbsEnabled = GlobalData.Settings.Signal.Long.Strategy.Contains(vbs)
            || GlobalData.Settings.Signal.Short.Strategy.Contains(vbs);
        return vbsEnabled ? null : new VbsIndicatorExtension();
    }

    public static MacdCrossBandSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new MacdCrossBandSettings();
        return Settings;
    }

    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not MacdCrossBandSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    /// <summary>
    /// No overlay of its own: the three band strategies already draw their bands ("Vbs Bands",
    /// "AtrRb Bands", "Dbr Bands" in the chart's overlay list) straight from the candles, so they do
    /// not care which strategy is enabled.
    /// </summary>
    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = new Config.MacdCrossBandConfigView();
}
