using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.AtrRb.Config;

public partial class StrategyAtrRbSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _length = 20;

    [ObservableProperty]
    private double _outerMult = 4.2;

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

    public void LoadConfig(AtrRbSettings settings)
    {
        Length = settings.Length;
        OuterMult = settings.OuterMult;
        BreakLookback = settings.BreakLookback;
        UseStopLoss = settings.UseStopLoss;
        StopLossAtrFactor = settings.StopLossAtrFactor;
        BBMinPercentage = settings.BBMinPercentage;
        BBMaxPercentage = settings.BBMaxPercentage;
        RequireRsiOsOb = settings.RequireRsiOsOb;
        RequireStochOsOb = settings.RequireStochOsOb;
    }

    public void SaveConfig(AtrRbSettings settings)
    {
        settings.Length = Length;
        settings.OuterMult = OuterMult;
        settings.BreakLookback = BreakLookback;
        settings.UseStopLoss = UseStopLoss;
        settings.StopLossAtrFactor = StopLossAtrFactor;
        settings.BBMinPercentage = BBMinPercentage;
        settings.BBMaxPercentage = BBMaxPercentage;
        settings.RequireRsiOsOb = RequireRsiOsOb;
        settings.RequireStochOsOb = RequireStochOsOb;
    }
}
