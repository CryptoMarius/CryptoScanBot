using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Dlz.Config;

public partial class StrategyDlzSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _candleCountZoom = 125; // int, max: 6000

    [ObservableProperty]
    private decimal _warnPercentage = 0.25m; // dlz.near, back on 06-10-2026

    [ObservableProperty]
    private decimal _nearZonePercentage = 0.25m;

    /// <summary>The values the touch level ComboBox offers; see CryptoZoneTouchLevel.</summary>
    public static CryptoZoneTouchLevel[] TouchLevels { get; } = Enum.GetValues<CryptoZoneTouchLevel>();

    [ObservableProperty]
    private CryptoZoneTouchLevel _touchLevel = CryptoZoneTouchLevel.Edge;

    [ObservableProperty]
    private int _maxTouches = 2;

    //[ObservableProperty]
    //private bool _closeZonesPastMidpoint = false;

    /// <summary>The values the "zone closes on" ComboBox offers; see CryptoZoneCloseRule.</summary>
    public static CryptoZoneCloseRule[] CloseRules { get; } = Enum.GetValues<CryptoZoneCloseRule>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UsesVisitCount))]
    [NotifyPropertyChangedFor(nameof(UsesTouchLevel))]
    private CryptoZoneCloseRule _closeRule = CryptoZoneCloseRule.Touches;

    /// <summary>The number of visits only matters when visits can close the zone.</summary>
    public bool UsesVisitCount => CloseRule != CryptoZoneCloseRule.BodyThrough;

    /// <summary>The touch level only decides what a visit is under CryptoZoneCloseRule.Touches.</summary>
    public bool UsesTouchLevel => CloseRule == CryptoZoneCloseRule.Touches;

    public void LoadConfig(SettingsSignalStrategyDlz settings)
    {
        CandleCountZoom = settings.CandleCountZoom;
        WarnPercentage = settings.WarnPercentage;
        NearZonePercentage = settings.NearZonePercentage;
        MaxTouches = settings.MaxTouches;
        TouchLevel = settings.TouchLevel;
        CloseRule = settings.CloseRule;
    }

    public void SaveConfig(SettingsSignalStrategyDlz settings)
    {
        settings.CandleCountZoom = CandleCountZoom;
        settings.WarnPercentage = WarnPercentage;
        settings.NearZonePercentage = NearZonePercentage;
        settings.MaxTouches = MaxTouches;
        settings.TouchLevel = TouchLevel;
        settings.CloseRule = CloseRule;
    }
}
