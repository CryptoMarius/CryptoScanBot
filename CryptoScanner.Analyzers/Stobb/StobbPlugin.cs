using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Stobb;

public class StobbPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "Stobb";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(CryptoSignalStrategy.Stobb,
            "stobb",
            typeof(Signal.SignalStobbLong),
            typeof(Signal.SignalStobbShort)
        ),
        new(CryptoSignalStrategy.StobbMulti,
            "stobb.multi",
            typeof(Signal.SignalStobbMultiLong),
            typeof(Signal.SignalStobbMultiShort)
        ),
    ];


    public static SettingsSignalStrategyStobb Settings { get; internal set; } = new();
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not SettingsSignalStrategyStobb s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new SettingsSignalStrategyStobb();
        return Settings;
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = null; // new Config.StorsiConfigView();
}
