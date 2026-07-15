using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyBreSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _bandLength = 20;

    [ObservableProperty]
    private double _outerMult = 3.2;

    [ObservableProperty]
    private int _didoLength = 20;

    [ObservableProperty]
    private double _didoMult = 1.0;

    [ObservableProperty]
    private bool _useTrendFilter = false;

    [ObservableProperty]
    private int _hmaLength = 55;

    [ObservableProperty]
    private bool _useRsiFilter = false;

    [ObservableProperty]
    private bool _requireStochOsOb = false;

    [ObservableProperty]
    private bool _allowStack = true;

    [ObservableProperty]
    private bool _useStopLoss = true;

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


    public void LoadConfig(string caption, SettingsSignalStrategyBre settings)
    {
        BandLength = settings.BandLength;
        OuterMult = settings.OuterMult;
        DidoLength = settings.DidoLength;
        DidoMult = settings.DidoMult;
        UseTrendFilter = settings.UseTrendFilter;
        HmaLength = settings.HmaLength;
        UseRsiFilter = settings.UseRsiFilter;
        RequireStochOsOb = settings.RequireStochOsOb;
        AllowStack = settings.AllowStack;
        UseStopLoss = settings.UseStopLoss;
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

    public void SaveConfig(SettingsSignalStrategyBre settings)
    {
        settings.BandLength = BandLength;
        settings.OuterMult = OuterMult;
        settings.DidoLength = DidoLength;
        settings.DidoMult = DidoMult;
        settings.UseTrendFilter = UseTrendFilter;
        settings.HmaLength = HmaLength;
        settings.UseRsiFilter = UseRsiFilter;
        settings.RequireStochOsOb = RequireStochOsOb;
        settings.AllowStack = AllowStack;
        settings.UseStopLoss = UseStopLoss;
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
