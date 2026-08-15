using CryptoScanner.Analyzers.AtrRb.Chart;
using CryptoScanner.Analyzers.AtrRb.Config;
using CryptoScanner.Analyzers.AtrRb.Signal;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.AtrRb;

public class AtrRbPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "AtrRb";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            StrategyInternal.ToLower(),
            typeof(AtrRbSignalLong),
            typeof(AtrRbSignalShort)
        ),
    ];

    public static AtrRbSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new AtrRbSettings();
        return Settings;
    }
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not AtrRbSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IChartOverlay? ChartOverlay { get; } = new AtrRbChartOverlay();
    public IConfigView? ConfigView { get; } = new AtrRbConfigView();

}
