using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Smc.Config;

public partial class StrategySmcSettingsViewModel : ObservableObject
{
    // ---- Detector tuning (base + expansion) ----

    [ObservableProperty]
    private int _averageWindow = 20;

    [ObservableProperty]
    private decimal _baseMaxRangeFactor = 0.8m;

    [ObservableProperty]
    private decimal _expansionMinRangeFactor = 1.5m;

    [ObservableProperty]
    private decimal _expansionBodyFraction = 0.5m;

    [ObservableProperty]
    private decimal _strongExpansionFactor = 2.5m;

    [ObservableProperty]
    private int _baseMaxCandles = 6;

    [ObservableProperty]
    private int _maxBlocksPerInterval = 50;

    [ObservableProperty]
    private bool _requireOppositeBaseColor = false;

    // ---- Signal tuning (entry) ----

    [ObservableProperty]
    private bool _onlyStrong = true;

    /// <summary>The values the touch level ComboBox offers; see CryptoZoneTouchLevel.</summary>
    public static CryptoZoneTouchLevel[] TouchLevels { get; } = Enum.GetValues<CryptoZoneTouchLevel>();

    [ObservableProperty]
    private bool _closeZonesPastMidpoint = false;

    [ObservableProperty]
    private CryptoZoneTouchLevel _touchLevel = CryptoZoneTouchLevel.Midpoint;

    [ObservableProperty]
    private int _maxTouches = 0;

    [ObservableProperty]
    private int _rejectionLookback = 3;


    public void LoadConfig(SettingsSignalStrategySmc settings)
    {
        AverageWindow = settings.AverageWindow;
        BaseMaxRangeFactor = settings.BaseMaxRangeFactor;
        ExpansionMinRangeFactor = settings.ExpansionMinRangeFactor;
        ExpansionBodyFraction = settings.ExpansionBodyFraction;
        StrongExpansionFactor = settings.StrongExpansionFactor;
        BaseMaxCandles = settings.BaseMaxCandles;
        MaxBlocksPerInterval = settings.MaxBlocksPerInterval;
        RequireOppositeBaseColor = settings.RequireOppositeBaseColor;

        OnlyStrong = settings.OnlyStrong;
        MaxTouches = settings.MaxTouches;
        TouchLevel = settings.TouchLevel;
        CloseZonesPastMidpoint = settings.CloseZonesPastMidpoint;
        RejectionLookback = settings.RejectionLookback;
    }

    public void SaveConfig(SettingsSignalStrategySmc settings)
    {
        settings.AverageWindow = AverageWindow;
        settings.BaseMaxRangeFactor = BaseMaxRangeFactor;
        settings.ExpansionMinRangeFactor = ExpansionMinRangeFactor;
        settings.ExpansionBodyFraction = ExpansionBodyFraction;
        settings.StrongExpansionFactor = StrongExpansionFactor;
        settings.BaseMaxCandles = BaseMaxCandles;
        settings.MaxBlocksPerInterval = MaxBlocksPerInterval;
        settings.RequireOppositeBaseColor = RequireOppositeBaseColor;

        settings.OnlyStrong = OnlyStrong;
        settings.MaxTouches = MaxTouches;
        settings.TouchLevel = TouchLevel;
        settings.CloseZonesPastMidpoint = CloseZonesPastMidpoint;
        settings.RejectionLookback = RejectionLookback;
    }
}
