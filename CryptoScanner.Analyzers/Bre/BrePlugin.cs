using CryptoScanner.Analyzers.Bre.Chart;
using CryptoScanner.Analyzers.Bre.Config;
using CryptoScanner.Analyzers.Bre.Signal;
using CryptoScanner.Core.Const;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Bre;

public class BrePlugin : IStrategyPlugin
{
    public string Name => Constants.StrategyBre.ToLower();

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new(CryptoSignalStrategy.Bre, Constants.StrategyBre.ToLower(), typeof(BreSignalLong), typeof(BreSignalShort)),
    ];

    public static BreSettings Settings { get; internal set; } = new();

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new BreSettings();
        return Settings;
    }
    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not BreSettings s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IChartOverlay? ChartOverlay { get; } = new BreChartOverlay();
    public IConfigView? ConfigView { get; } = new BreConfigView();
}
