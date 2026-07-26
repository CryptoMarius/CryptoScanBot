using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.Vbs.Config;

public partial class StrategyVbsSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _length = 90;

    [ObservableProperty]
    private double _mult = 2.5;

    [ObservableProperty]
    private bool _useRsiFilter = true;

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
    private double _bandMaxPercentage = 0;

    public void LoadConfig(VbsSettings settings)
    {
        Length = settings.Length;
        Mult = settings.Mult;
        UseRsiFilter = settings.UseRsiFilter;
        UseStopLoss = settings.UseStopLoss;
        SLStdevFactor = settings.SLStdevFactor;
        RequireStochOsOb = settings.RequireStochOsOb;
        BandMaxPercentage = settings.BandMaxPercentage;
    }

    public void SaveConfig(VbsSettings settings)
    {
        settings.Length = Length;
        settings.Mult = Mult;
        settings.UseRsiFilter = UseRsiFilter;
        settings.UseStopLoss = UseStopLoss;
        settings.SLStdevFactor = SLStdevFactor;
        settings.RequireStochOsOb = RequireStochOsOb;
        settings.BandMaxPercentage = BandMaxPercentage;
    }
}
