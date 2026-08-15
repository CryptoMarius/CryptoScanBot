using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Fvg;

// Dominant zones
public class FvgPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "Fvg";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new("fvg",
            typeof(Signal.SignalFairValueGapLong),
            typeof(Signal.SignalFairValueGapShort),
            IsZoneStrategy: true
        ),

    ];


    public static SettingsSignalStrategyFvg Settings
    {
        get => GlobalData.Settings.Signal.ZonesFvg;
        set => GlobalData.Settings.Signal.ZonesFvg = value;
    }

    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not SettingsSignalStrategyFvg s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = new Config.FvgConfigView();
}
