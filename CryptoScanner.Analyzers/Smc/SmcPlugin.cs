using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Smc;

// SMC supply/demand order block — price returns to a fresh/strong base zone.
public class SmcPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "Smc";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new("smc",
            typeof(Signal.SignalOrderBlockLong),
            typeof(Signal.SignalOrderBlockShort),
            IsZoneStrategy: true
        ),

        new("smc.rejection",
            typeof(Signal.SignalOrderBlockRejectionLong),
            typeof(Signal.SignalOrderBlockRejectionShort),
            IsZoneStrategy: true
        ),
    ];


    public static SettingsSignalStrategySmc Settings
    {
        get => GlobalData.Settings.Signal.ZonesSmc;
        set => GlobalData.Settings.Signal.ZonesSmc = value;
    }

    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not SettingsSignalStrategySmc s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = new Config.SmcConfigView();
}
