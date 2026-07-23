using CryptoScanner.Analyzers.Dbr.Chart;
using CryptoScanner.Analyzers.Dbr.Config;
using CryptoScanner.Analyzers.Dbr.Signal;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Dbr;

// Donchian-breakout-reversion
public class DbrPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "Dbr";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            CryptoSignalStrategy.Dbr,
            StrategyInternal.ToLower(),
            typeof(DbrSignalLong),
            typeof(DbrSignalShort)
        ),
    ];

    public static DbrSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new DbrSettings();
        return Settings;
    }
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not DbrSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IChartOverlay? ChartOverlay { get; } = new DbrChartOverlay();
    public IConfigView? ConfigView { get; } = new DbrConfigView();
}
