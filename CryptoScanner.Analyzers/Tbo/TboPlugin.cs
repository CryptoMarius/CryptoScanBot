using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Tbo;

/// <summary>
/// TBO - a trending breakout strategy: a cloud of four moving averages for the direction and the
/// strength of the trend, pivot highs and lows for support and resistance, and three entries inside
/// that trend - the break of a level, the crossing of the two EMAs, or a pullback to the fast line.
/// <para>
/// Nothing here is proven yet: every filter is off by default and the strategy has to earn its
/// place on its own measurements.
/// </para>
/// </summary>
public class TboPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "Tbo";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            StrategyInternal.ToLower(),
            typeof(Signal.TboLong),
            typeof(Signal.TboShort)
        ),
    ];

    public static TboSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new TboSettings();
        return Settings;
    }

    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not TboSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    /// <summary>
    /// The cloud EMAs and the pivot levels. The lengths come from the settings, so the hubs have to
    /// be rebuilt after a settings change - which is what IndicatorConfiguration.Invalidate does.
    /// </summary>
    public IIndicatorExtension? CreateIndicatorExtension() => new Indicators.TboIndicatorExtension();

    /// <summary>
    /// The four lines and the two levels on the chart, so what the strategy reacts to can be seen
    /// instead of taken on trust.
    /// </summary>
    public IChartOverlay? ChartOverlay { get; } = new Chart.TboChartOverlay();
    public IConfigView? ConfigView { get; } = new Config.TboConfigView();
}
