using CryptoScanner.Analyzers.Vbs.Chart;
using CryptoScanner.Analyzers.Vbs.Config;
using CryptoScanner.Analyzers.Vbs.Indicators;
using CryptoScanner.Analyzers.Vbs.Signal;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Vbs;

// VBS (VWAP Band Strategy)
public class VbsPlugin : IStrategyPlugin
{
    public static string StrategyInternal = "Vbs";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(
            CryptoSignalStrategy.Vbs,
            StrategyInternal.ToLower(),
            typeof(VbsSignalLong),
            typeof(VbsSignalShort)
        ),
    ];

    public static VbsSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new VbsSettings();
        return Settings;
    }
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not VbsSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IIndicatorExtension? CreateIndicatorExtension() => new VbsIndicatorExtension();

    public IChartOverlay? ChartOverlay { get; } = new VbsChartOverlay();
    public IConfigView? ConfigView { get; } = new VbsConfigView();
}
