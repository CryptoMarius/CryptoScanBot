using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Dlz;

// Dominant zones
public class DlzPlugin : IStrategyPlugin
{
    public const string StrategyInternal = "Dlz";
    public string StrategyName => StrategyInternal.ToLower();
    public string StrategyNameCamelCase => StrategyInternal;

    public IReadOnlyList<StrategyRegistration> Strategies { get; } =
    [
        new("dlz",
            typeof(Signal.SignalDominantLevelLong),
            typeof(Signal.SignalDominantLevelShort),
            IsZoneStrategy: true
        ),

        //// Level approaching
        //new("dlz.near",
        //    typeof(Signal.SignalDominantLevelNearLong),
        //    typeof(Signal.SignalDominantLevelNearShort),
        //    IsZoneStrategy: true
        //),
#if DEBUG
        // Level approaching - back on 06-10-2026, in Debug builds only, to be measured again.
        new("dlz.near",
            typeof(Signal.SignalDominantLevelNearLong),
            typeof(Signal.SignalDominantLevelNearShort),
            IsZoneStrategy: true
        ),
#endif
    ];


    public static SettingsSignalStrategyDlz Settings
    {
        get => GlobalData.Settings.Signal.ZonesDlz;
        set => GlobalData.Settings.Signal.ZonesDlz = value;
    }

    public SettingsSignalStrategyBase SettingsBase
    {
        get => Settings;
        set
        {
            if (value is not SettingsSignalStrategyDlz s)
                throw new NotImplementedException();
            Settings = s;
        }
    }

    public IChartOverlay? ChartOverlay { get; } = null;
    public IConfigView? ConfigView { get; } = new Config.DlzConfigView();
}
