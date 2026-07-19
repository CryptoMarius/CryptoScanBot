// The BBMA signal classes are DEBUG-only (see the #if DEBUG guards in Signal/), so the
// plugin that registers them is too. In a release build the plugin simply does not exist
// and no BBMA tab or strategy shows up — same behavior as the old hardcoded registration.
using CryptoScanner.Analyzers.Bbma.Chart;
using CryptoScanner.Analyzers.Bbma.Config;
using CryptoScanner.Analyzers.Bbma.Signal;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Bbma;

public class BbmaPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "Bbma.Omni";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    // Only the Omni variant is registered. The original "bbma" long/short signals
    // (SignalBbMaLong/Short) never produced signals and stay unregistered; the classes
    // are kept in Signal/ for reference.
    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            CryptoSignalStrategy.BbmaOmni,
            StrategyInternal.ToLower(),
            typeof(SignalBbmaOmniLong),
            typeof(SignalBbmaOmniShort)
        ),
    ];

    public static BbmaSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new BbmaSettings();
        return Settings;
    }
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not BbmaSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IChartOverlay? ChartOverlay { get; } = new BbmaChartOverlay();
    public IConfigView? ConfigView { get; } = new BbmaConfigView();
}
