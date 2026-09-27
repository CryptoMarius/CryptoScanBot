using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Dlz.Config;

public partial class StrategyDlzSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _candleCountZoom = 125; // int, max: 6000

    [ObservableProperty]
    private decimal _nearZonePercentage = 0.25m;

    /// <summary>The values the touch level ComboBox offers; see CryptoZoneTouchLevel.</summary>
    public static CryptoZoneTouchLevel[] TouchLevels { get; } = Enum.GetValues<CryptoZoneTouchLevel>();

    [ObservableProperty]
    private CryptoZoneTouchLevel _touchLevel = CryptoZoneTouchLevel.Edge;

    [ObservableProperty]
    private int _maxTouches = 2;

    [ObservableProperty]
    private bool _closeZonesPastMidpoint = false;

    public void LoadConfig(SettingsSignalStrategyDlz settings)
    {
        CandleCountZoom = settings.CandleCountZoom;
        NearZonePercentage = settings.NearZonePercentage;
        MaxTouches = settings.MaxTouches;
        TouchLevel = settings.TouchLevel;
        CloseZonesPastMidpoint = settings.CloseZonesPastMidpoint;
    }

    public void SaveConfig(SettingsSignalStrategyDlz settings)
    {
        settings.CandleCountZoom = CandleCountZoom;
        settings.NearZonePercentage = NearZonePercentage;
        settings.MaxTouches = MaxTouches;
        settings.TouchLevel = TouchLevel;
        settings.CloseZonesPastMidpoint = CloseZonesPastMidpoint;
    }
}
