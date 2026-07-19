using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.Baba.Config;

public partial class StrategyBabaSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _length = 90;

    [ObservableProperty]
    private double _mult = 2.5;

    [ObservableProperty]
    private int _atrLength = 14;

    [ObservableProperty]
    private double _atrMult = 1.0;

    [ObservableProperty]
    private bool _useVolumeSurge = false;

    [ObservableProperty]
    private int _volumeSurgeLength = 5;

    [ObservableProperty]
    private double _volumeSurgeThreshold = 1.05;

    [ObservableProperty]
    private double _volumeSurgeFactor = 0.031;

    [ObservableProperty]
    private bool _useRsiFilter = true;

    [ObservableProperty]
    private bool _useSlideFilter = false;

    [ObservableProperty]
    private int _slideWindow = 40;

    [ObservableProperty]
    private double _slideMinEfficiency = 0.35;

    [ObservableProperty]
    private double _slideMinMovePercent = 1.0;

    [ObservableProperty]
    private bool _useStopLoss = true;

    [ObservableProperty]
    private double _sLStdevFactor = 1.0;

    // Old ATR-based SL property — replaced by SLStdevFactor above.
    //[ObservableProperty]
    //private double _stopLossAtrFactor = 2.0;

    [ObservableProperty]
    private bool _requireStochOsOb = false;

    [ObservableProperty]
    private int _timeframeConsensusCount = 0;

    [ObservableProperty]
    private bool _onlyIfLux5m = false;

    [ObservableProperty]
    private int _lux5mPercentage = 50;

    [ObservableProperty]
    private bool _checkTrendPrimaryDirection = false;

    [ObservableProperty]
    private int _trendPrimaryDirectionCount = 2;

    [ObservableProperty]
    private bool _checkTrendSecondaryDirection = false;

    [ObservableProperty]
    private int _trendSecondaryDirectionCount = 2;

    [ObservableProperty]
    private bool _checkPriceAboveMa200 = false;

    [ObservableProperty]
    private decimal _ma200MinDistancePercentage = 0m;

    [ObservableProperty]
    private int _ma200ConfirmationCandles = 0;

    [ObservableProperty]
    private bool _useDlzZone = false;

    [ObservableProperty]
    private bool _useFvgZone = false;

    [ObservableProperty]
    private bool _useSmcZone = false;


    public void LoadConfig(BabaSettings settings)
    {
        Length = settings.Length;
        Mult = settings.Mult;
        AtrLength = settings.AtrLength;
        AtrMult = settings.AtrMult;
        UseVolumeSurge = settings.UseVolumeSurge;
        VolumeSurgeLength = settings.VolumeSurgeLength;
        VolumeSurgeThreshold = settings.VolumeSurgeThreshold;
        VolumeSurgeFactor = settings.VolumeSurgeFactor;
        UseRsiFilter = settings.UseRsiFilter;
        UseSlideFilter = settings.UseSlideFilter;
        SlideWindow = settings.SlideWindow;
        SlideMinEfficiency = settings.SlideMinEfficiency;
        SlideMinMovePercent = settings.SlideMinMovePercent;
        UseStopLoss = settings.UseStopLoss;
        SLStdevFactor = settings.SLStdevFactor;
        RequireStochOsOb = settings.RequireStochOsOb;
        TimeframeConsensusCount = settings.TimeframeConsensusCount;
        OnlyIfLux5m = settings.OnlyIfLux5m;
        Lux5mPercentage = settings.Lux5mPercentage;
        CheckTrendPrimaryDirection = settings.CheckTrendPrimaryDirection;
        TrendPrimaryDirectionCount = settings.TrendPrimaryDirectionCount;
        CheckTrendSecondaryDirection = settings.CheckTrendSecondaryDirection;
        TrendSecondaryDirectionCount = settings.TrendSecondaryDirectionCount;
        CheckPriceAboveMa200 = settings.CheckPriceAboveMa200;
        Ma200MinDistancePercentage = settings.Ma200MinDistancePercentage;
        Ma200ConfirmationCandles = settings.Ma200ConfirmationCandles;
        UseDlzZone = settings.UseDlzZone;
        UseFvgZone = settings.UseFvgZone;
        UseSmcZone = settings.UseSmcZone;
    }

    public void SaveConfig(BabaSettings settings)
    {
        settings.Length = Length;
        settings.Mult = Mult;
        settings.AtrLength = AtrLength;
        settings.AtrMult = AtrMult;
        settings.UseVolumeSurge = UseVolumeSurge;
        settings.VolumeSurgeLength = VolumeSurgeLength;
        settings.VolumeSurgeThreshold = VolumeSurgeThreshold;
        settings.VolumeSurgeFactor = VolumeSurgeFactor;
        settings.UseRsiFilter = UseRsiFilter;
        settings.UseSlideFilter = UseSlideFilter;
        settings.SlideWindow = SlideWindow;
        settings.SlideMinEfficiency = SlideMinEfficiency;
        settings.SlideMinMovePercent = SlideMinMovePercent;
        settings.UseStopLoss = UseStopLoss;
        settings.SLStdevFactor = SLStdevFactor;
        settings.RequireStochOsOb = RequireStochOsOb;
        settings.TimeframeConsensusCount = TimeframeConsensusCount;
        settings.OnlyIfLux5m = OnlyIfLux5m;
        settings.Lux5mPercentage = Lux5mPercentage;
        settings.CheckTrendPrimaryDirection = CheckTrendPrimaryDirection;
        settings.TrendPrimaryDirectionCount = TrendPrimaryDirectionCount;
        settings.CheckTrendSecondaryDirection = CheckTrendSecondaryDirection;
        settings.TrendSecondaryDirectionCount = TrendSecondaryDirectionCount;
        settings.CheckPriceAboveMa200 = CheckPriceAboveMa200;
        settings.Ma200MinDistancePercentage = Ma200MinDistancePercentage;
        settings.Ma200ConfirmationCandles = Ma200ConfirmationCandles;
        settings.UseDlzZone = UseDlzZone;
        settings.UseFvgZone = UseFvgZone;
        settings.UseSmcZone = UseSmcZone;
    }
}
