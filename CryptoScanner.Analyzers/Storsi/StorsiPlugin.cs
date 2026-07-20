using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Storsi;

public class StorsiPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "StoRsi";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(CryptoSignalStrategy.StoRsi,
            "storsi",
            typeof(Signal.StoRsiLong),
            typeof(Signal.StoRsiShort)
        ),
        new(CryptoSignalStrategy.StoRsiMulti,
            "storsi.multi",
            typeof(Signal.StoRsiMultiLong),
            typeof(Signal.StoRsiMultiShort)
        ),
    ];

    public static SettingsSignalStrategyStoRsi Settings { get; internal set; } = new();
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not SettingsSignalStrategyStoRsi s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new SettingsSignalStrategyStoRsi();
        return Settings;
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = new Config.StorsiConfigView();
}
