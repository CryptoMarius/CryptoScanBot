using CryptoScanner.Analyzers.Baba.Chart;
using CryptoScanner.Analyzers.Baba.Config;
using CryptoScanner.Analyzers.Baba.Indicators;
using CryptoScanner.Analyzers.Baba.Signal;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Baba;

public class BabaPlugin : IStrategyPlugin
{
    public static string StrategyInternal = "Baba";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            CryptoSignalStrategy.Baba,
            StrategyInternal.ToLower(),
            typeof(BabaSignalLong),
            typeof(BabaSignalShort)
        ),
    ];

    public static BabaSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new BabaSettings();
        return Settings;
    }
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not BabaSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IIndicatorExtension? CreateIndicatorExtension() => new BabaIndicatorExtension();

    public IChartOverlay? ChartOverlay { get; } = new BabaChartOverlay();
    public IConfigView? ConfigView { get; } = new BabaConfigView();
}
