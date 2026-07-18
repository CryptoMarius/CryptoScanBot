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
    public CryptoSignalStrategy Strategy => CryptoSignalStrategy.Bre;
    public Type? AnalyzeLongType => typeof(BreSignalLong);
    public Type? AnalyzeShortType => typeof(BreSignalShort);

    public static BreSettings Settings { get; internal set; } = new();
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

    public static SettingsSignalStrategyBase CreateSettings()
    {
        Settings = new BreSettings();
        return Settings;
    }

    public IChartOverlay? ChartOverlay { get; } = new BreChartOverlay();
    public IConfigView? ConfigView { get; } = new BreConfigView();
}
