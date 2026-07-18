using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Config.ViewModels;

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

}
