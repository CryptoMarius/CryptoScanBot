using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Sbm;

//***************************************************
// SBMx (a special kind of STOBB)
//***************************************************
public class SbmPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "Sbm";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    private const string StrategyInternalNp = "Sbm.np";
    private const string StrategyInternalBb = "Sbm.bb";

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        // Based on the original
        new(CryptoSignalStrategy.Sbm1,
            "sbm1",
            typeof(Signal.SignalSbm1Long),
            typeof(Signal.SignalSbm1Short)
        ),

        // Based on ??
        new(CryptoSignalStrategy.Sbm2,
            "sbm2",
            typeof(Signal.SignalSbm2Long),
            typeof(Signal.SignalSbm2Short)
        ),

        // Based on BB expansion
        new(CryptoSignalStrategy.Sbm3,
            "sbm3",
            typeof(Signal.SignalSbm3Long),
            typeof(Signal.SignalSbm3Short)
        ),
    ];

    public static SettingsSignalStrategySbm Settings { get; internal set; } = new();
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not SettingsSignalStrategySbm s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new SettingsSignalStrategySbm();
        return Settings;
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = new Config.SbmConfigView();
}
