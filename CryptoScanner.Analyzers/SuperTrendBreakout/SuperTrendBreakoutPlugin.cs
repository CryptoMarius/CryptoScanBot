using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.SuperTrendBreakout;

public class SuperTrendBreakoutPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "SuperTrendBreakout";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(CryptoSignalStrategy.SuperTrendBreakout,
            "supertrendbreakout",
            typeof(Signal.SignalSuperTrendBreakoutLong),
            typeof(Signal.SignalSuperTrendBreakoutShort)
        ),
    ];

    public static SuperTrendBreakoutSettings Settings { get; internal set; } = new();
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not SuperTrendBreakoutSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new SuperTrendBreakoutSettings();
        return Settings;
    }

    public bool RequiresDlzZones => true;

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = null;
}
