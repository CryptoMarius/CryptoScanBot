using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.Baba.Config;

public partial class StrategyBabaSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _length = 90;

    [ObservableProperty]
    private double _mult = 2.5;

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

    public void LoadConfig(BabaSettings settings)
    {
        Length = settings.Length;
        Mult = settings.Mult;
        UseRsiFilter = settings.UseRsiFilter;
        UseSlideFilter = settings.UseSlideFilter;
        SlideWindow = settings.SlideWindow;
        SlideMinEfficiency = settings.SlideMinEfficiency;
        SlideMinMovePercent = settings.SlideMinMovePercent;
        UseStopLoss = settings.UseStopLoss;
        SLStdevFactor = settings.SLStdevFactor;
        RequireStochOsOb = settings.RequireStochOsOb;
        TimeframeConsensusCount = settings.TimeframeConsensusCount;
    }

    public void SaveConfig(BabaSettings settings)
    {
        settings.Length = Length;
        settings.Mult = Mult;
        settings.UseRsiFilter = UseRsiFilter;
        settings.UseSlideFilter = UseSlideFilter;
        settings.SlideWindow = SlideWindow;
        settings.SlideMinEfficiency = SlideMinEfficiency;
        settings.SlideMinMovePercent = SlideMinMovePercent;
        settings.UseStopLoss = UseStopLoss;
        settings.SLStdevFactor = SLStdevFactor;
        settings.RequireStochOsOb = RequireStochOsOb;
        settings.TimeframeConsensusCount = TimeframeConsensusCount;
    }
}
