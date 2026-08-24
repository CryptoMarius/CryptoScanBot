using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Fvg.Config;

public partial class StrategyFvgSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private double _minimumPercentage = 0.25;

    [ObservableProperty]
    private decimal _nearZonePercentage = 0.25m;

    /// <summary>The values the touch level ComboBox offers; see CryptoZoneTouchLevel.</summary>
    public static CryptoZoneTouchLevel[] TouchLevels { get; } = Enum.GetValues<CryptoZoneTouchLevel>();

    [ObservableProperty]
    private CryptoZoneTouchLevel _touchLevel = CryptoZoneTouchLevel.Edge;

    [ObservableProperty]
    private int _maxTouches = 2;

    [ObservableProperty]
    private int _rejectionLookback = 1;

    [ObservableProperty]
    private bool _closeZonesPastMidpoint = false;


    public void LoadConfig(SettingsSignalStrategyFvg settings)
    {
        MinimumPercentage = settings.MinimumPercentage;
        NearZonePercentage = settings.NearZonePercentage;
        MaxTouches = settings.MaxTouches;
        TouchLevel = settings.TouchLevel;
        RejectionLookback = settings.RejectionLookback;
        CloseZonesPastMidpoint = settings.CloseZonesPastMidpoint;
    }

    public void SaveConfig(SettingsSignalStrategyFvg settings)
    {
        settings.MinimumPercentage = MinimumPercentage;
        settings.NearZonePercentage = NearZonePercentage;
        settings.MaxTouches = MaxTouches;
        settings.TouchLevel = TouchLevel;
        settings.RejectionLookback = RejectionLookback;
        settings.CloseZonesPastMidpoint = CloseZonesPastMidpoint;
    }
}
