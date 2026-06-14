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
        settings.UseDlzZone = UseDlzZone;
        settings.UseFvgZone = UseFvgZone;
        settings.UseSmcZone = UseSmcZone;
    }
}
