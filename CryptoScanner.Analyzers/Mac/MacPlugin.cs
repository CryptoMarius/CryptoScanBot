using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Mac;

/// <summary>
/// MAC - a moving average cloud strategy: a cloud of four moving averages for the direction and the
/// strength of the trend, pivot highs and lows for support and resistance, and three entries inside
/// that trend - the break of a level, the crossing of the two EMAs, or a pullback to the fast line.
/// <para>
/// Nothing here is proven yet: every filter is off by default and the strategy has to earn its
/// place on its own measurements.
/// </para>
/// </summary>
public class MacPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "Mac";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    // The strategy was called "tbo" until 19-09-2026 and carried that here as a former name, so
    // stored settings and emulator runs from before the rename kept working. The old name is out of
    // the data since 19-09-2026 - settings, chart files, queue files and every run label and stored
    // setting in the emulator databases were migrated by Tools/RenameTboToMac - so the former name
    // goes with it and one strategy goes by one name. IStrategyPlugin still offers
    // FormerStrategyNames for a next rename; no plugin declares one today.

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            StrategyInternal.ToLower(),
            typeof(Signal.MacLong),
            typeof(Signal.MacShort)
        ),
    ];

    public static MacSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new MacSettings();
        return Settings;
    }

    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not MacSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    /// <summary>
    /// The cloud EMAs and the pivot levels. The lengths come from the settings, so the hubs have to
    /// be rebuilt after a settings change - which is what IndicatorConfiguration.Invalidate does.
    /// </summary>
    public IIndicatorExtension? CreateIndicatorExtension() => new Indicators.MacIndicatorExtension();

    /// <summary>
    /// The four lines and the two levels on the chart, so what the strategy reacts to can be seen
    /// instead of taken on trust.
    /// </summary>
    public IChartOverlay? ChartOverlay { get; } = new Chart.MacChartOverlay();
    public IConfigView? ConfigView { get; } = new Config.MacConfigView();
}
