using CryptoScanner.Analyzers.ChochFvg.Config;
using CryptoScanner.Analyzers.ChochFvg.Signal;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.ChochFvg;

/// <summary>
/// Change of character with a fair value gap: a limit on the middle of a gap of the leg that broke the
/// last swing against the trend, inside its fib zone, with a stop behind the gap candle and a fixed
/// risk/reward target. A plugin of its own next to Choch because it runs on 1m, which Choch keeps out
/// for all its variants, and because its entry, stop and target are its own.
/// </summary>
public class ChochFvgPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "ChochFvg";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            "choch.fvg",
            typeof(ChochFvgLong),
            typeof(ChochFvgShort),
            StandingEntry: true
        ),
    ];

    public static ChochFvgSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new ChochFvgSettings();
        return Settings;
    }

    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not ChochFvgSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    /// <summary>The targets of waiting orders it remembers, so a next emulator run starts empty.</summary>
    public void ResetRunState() => ChochFvgBase.Reset();

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = new ChochFvgConfigView();
}
