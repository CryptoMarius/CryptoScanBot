using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.SrFlip;

/// <summary>
/// The support/resistance flip: a broken level or line that is retested from the other side and
/// holds. Reads only candles; the levels come from CryptoScanner.Core.Trend.SupportResistance.
/// </summary>
public class SrFlipPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "SrFlip";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            StrategyInternal.ToLower(),
            typeof(Signal.SrFlipLong),
            typeof(Signal.SrFlipShort)
        ),
    ];

    public static SrFlipSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new SrFlipSettings();
        return Settings;
    }

    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not SrFlipSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = new Config.SrFlipConfigView();
}
