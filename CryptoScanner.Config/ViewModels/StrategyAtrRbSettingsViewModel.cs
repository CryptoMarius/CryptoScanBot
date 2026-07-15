using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyAtrRbSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _length = 20;

    [ObservableProperty]
    private double _outerMult = 4.2;

    [ObservableProperty]
    private double _innerMult = 1.0;

    [ObservableProperty]
    private int _breakLookback = 5;

    [ObservableProperty]
    private bool _useStopLoss = true;

    [ObservableProperty]
    private double _stopLossAtrFactor = 2.0;

    [ObservableProperty]
    private double _bBMinPercentage = 1.50;

    [ObservableProperty]
    private double _bBMaxPercentage = 0.0;

    [ObservableProperty]
    private bool _requireRsiOsOb = false;

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


    public void LoadConfig(string caption, SettingsSignalStrategyAtrRb settings)
    {
        Length = settings.Length;
        OuterMult = settings.OuterMult;
        InnerMult = settings.InnerMult;
        BreakLookback = settings.BreakLookback;
        UseStopLoss = settings.UseStopLoss;
        StopLossAtrFactor = settings.StopLossAtrFactor;
        BBMinPercentage = settings.BBMinPercentage;
        BBMaxPercentage = settings.BBMaxPercentage;
        RequireRsiOsOb = settings.RequireRsiOsOb;
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

    public void SaveConfig(SettingsSignalStrategyAtrRb settings)
    {
        settings.Length = Length;
        settings.OuterMult = OuterMult;
        settings.InnerMult = InnerMult;
        settings.BreakLookback = BreakLookback;
        settings.UseStopLoss = UseStopLoss;
        settings.StopLossAtrFactor = StopLossAtrFactor;
        settings.BBMinPercentage = BBMinPercentage;
        settings.BBMaxPercentage = BBMaxPercentage;
        settings.RequireRsiOsOb = RequireRsiOsOb;
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
