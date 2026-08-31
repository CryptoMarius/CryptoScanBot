using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.CandlePattern;

/// <summary>
/// The classic candlestick reversal patterns as one strategy, with the pattern as a setting. A run
/// varies "Patterns" through SignalOverrides and nothing else changes, so the patterns can be put
/// side by side. Several at once is an OR, so a combination is one run as well.
/// <para>
/// The shapes themselves live in CandlePatternHelper in Core, shared with Tools/PatternScan, so the
/// offline measurement and the scanner cannot drift apart.
/// </para>
/// </summary>
public class CandlePatternPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "CandlePattern";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            StrategyInternal.ToLower(),
            typeof(Signal.CandlePatternLong),
            typeof(Signal.CandlePatternShort)
        ),
    ];

    public static CandlePatternStrategySettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new CandlePatternStrategySettings();
        return Settings;
    }

    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not CandlePatternStrategySettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = new Config.CandlePatternConfigView();
}
