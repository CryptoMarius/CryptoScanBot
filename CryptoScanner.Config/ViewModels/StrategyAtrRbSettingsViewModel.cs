using CommunityToolkit.Mvvm.ComponentModel;

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

}
