using CryptoScanner.Analyzers.Storsi.Signal;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Storsi;

public class StoRsiPlugin : IStrategyPlugin
{
    public string Name => "storsi";

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(CryptoSignalStrategy.StoRsi, "storsi", typeof(StoRsiLong), typeof(StoRsiShort)),
        new(CryptoSignalStrategy.StoRsiMulti, "storsi.multi", typeof(StoRsiMultiLong), typeof(StoRsiMultiShort)),
    ];

    public static StorsiSettings Settings { get; internal set; } = new();
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not StorsiSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new StorsiSettings();
        return Settings;
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = new Config.StorsiConfigView();
}
