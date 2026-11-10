using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Fvg.Config;

public partial class StrategyFvgSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private double _minimumPercentage = 0.25;

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
    private CryptoZoneCloseRule _closeRule = CryptoZoneCloseRule.BodyThrough;

    /// <summary>The number of visits only matters when visits can close the zone.</summary>
    public bool UsesVisitCount => CloseRule != CryptoZoneCloseRule.BodyThrough;

    /// <summary>The touch level only decides what a visit is under CryptoZoneCloseRule.Touches.</summary>
    public bool UsesTouchLevel => CloseRule == CryptoZoneCloseRule.Touches;


    public void LoadConfig(SettingsSignalStrategyFvg settings)
    {
        MinimumPercentage = settings.MinimumPercentage;
        MaxTouches = settings.MaxTouches;
        TouchLevel = settings.TouchLevel;
        CloseRule = settings.CloseRule;
    }

    public void SaveConfig(SettingsSignalStrategyFvg settings)
    {
        settings.MinimumPercentage = MinimumPercentage;
        settings.MaxTouches = MaxTouches;
        settings.TouchLevel = TouchLevel;
        settings.CloseRule = CloseRule;
    }
}
