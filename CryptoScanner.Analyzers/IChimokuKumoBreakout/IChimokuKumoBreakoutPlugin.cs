using CryptoScanner.Analyzers.IChimokuKumoBreakout.Signal;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.IChimokuKumoBreakout;

/// <summary>
/// The Ichimoku Kumo (Cloud) Breakout is a strategy where traders enter a 
/// position when the price forcefully pushes above or below the Ichimoku Cloud. 
/// It signals a massive shift in medium-to-long-term market sentiment and is 
/// generally validated when the Chikou Span (lagging line) clears historical 
/// price action in the direction of the breakout.
/// </summary>
public class IChimokuKumoBreakoutPlugin : IStrategyPlugin
{
    private const string StrategyInternal = "Ichimoku.Kumo.Breakout";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            CryptoSignalStrategy.IchimokuKumoBreakout,
            StrategyInternal.ToLower(),
            typeof(IchimokuKumoBreakoutLong),
            typeof(IchimokuKumoBreakoutShort)
        ),
    ];

    public static IChimokuKumoBreakoutSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new IChimokuKumoBreakoutSettings();
        return Settings;
    }
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not IChimokuKumoBreakoutSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = null;
}
