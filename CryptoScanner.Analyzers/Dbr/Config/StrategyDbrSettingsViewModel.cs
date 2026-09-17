using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.Dbr.Config;

public partial class StrategyDbrSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _bandLength = 20;

    [ObservableProperty]
    private double _outerMult = 3.2;

    [ObservableProperty]
    private double _bbMinPercentage = 1.50;

    [ObservableProperty]
    private double _bbMaxPercentage = 0.0;

    [ObservableProperty]
    private bool _useRsiFilter = false;

    [ObservableProperty]
    private bool _requireStochOsOb = false;

    [ObservableProperty]
    private bool _allowStack = true;

    [ObservableProperty]
    private bool _useStopLoss = true;

    [ObservableProperty]
    private double _maxCandleSizeRatio = 0.0;

    [ObservableProperty]
    private double _maxCandleVolumeRatio = 0.0;

    [ObservableProperty]
    private int _candleAverageLength = 20;

    [ObservableProperty]
    private double _largeCandleRetracementPart = 0.0;

    [ObservableProperty]
    private int _bandBreakConfirmationCount = 0;

    public void LoadConfig(DbrSettings settings)
    {
        BandLength = settings.BandLength;
        OuterMult = settings.OuterMult;
        BbMinPercentage = settings.BBMinPercentage;
        BbMaxPercentage = settings.BBMaxPercentage;
        UseRsiFilter = settings.UseRsiFilter;
        RequireStochOsOb = settings.RequireStochOsOb;
        AllowStack = settings.AllowStack;
        UseStopLoss = settings.UseStopLoss;
        MaxCandleSizeRatio = settings.MaxCandleSizeRatio;
        MaxCandleVolumeRatio = settings.MaxCandleVolumeRatio;
        CandleAverageLength = settings.CandleAverageLength;
        LargeCandleRetracementPart = settings.LargeCandleRetracementPart;
        BandBreakConfirmationCount = settings.BandBreakConfirmationCount;
    }

    public void SaveConfig(DbrSettings settings)
    {
        settings.BandLength = BandLength;
        settings.OuterMult = OuterMult;
        settings.BBMinPercentage = BbMinPercentage;
        settings.BBMaxPercentage = BbMaxPercentage;
        settings.UseRsiFilter = UseRsiFilter;
        settings.RequireStochOsOb = RequireStochOsOb;
        settings.AllowStack = AllowStack;
        settings.UseStopLoss = UseStopLoss;
        settings.MaxCandleSizeRatio = MaxCandleSizeRatio;
        settings.MaxCandleVolumeRatio = MaxCandleVolumeRatio;
        settings.CandleAverageLength = CandleAverageLength;
        settings.LargeCandleRetracementPart = LargeCandleRetracementPart;
        settings.BandBreakConfirmationCount = BandBreakConfirmationCount;
    }
}
