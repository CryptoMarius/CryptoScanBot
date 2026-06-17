using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyAtrRbSettingsViewModel : ObservableObject
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
    private bool _useCooldown = true;

    [ObservableProperty]
    private int _cooldownBars = 10;

    [ObservableProperty]
    private bool _useStopLoss = true;

    [ObservableProperty]
    private double _stopLossAtrFactor = 2.0;

    [ObservableProperty]
    private bool _useDlzZone = false;

    [ObservableProperty]
    private bool _useFvgZone = false;

    [ObservableProperty]
    private bool _useSmcZone = false;


    public void LoadConfig(string caption, SettingsSignalStrategyAtrRb settings)
    {
        Length = settings.Length;
        Mult = settings.Mult;
        AtrLength = settings.AtrLength;
        AtrMult = settings.AtrMult;
        UseRsiFilter = settings.UseRsiFilter;
        UseSlideFilter = settings.UseSlideFilter;
        SlideWindow = settings.SlideWindow;
        SlideMinEfficiency = settings.SlideMinEfficiency;
        SlideMinMovePercent = settings.SlideMinMovePercent;
        UseCooldown = settings.UseCooldown;
        CooldownBars = settings.CooldownBars;
        UseStopLoss = settings.UseStopLoss;
        StopLossAtrFactor = settings.StopLossAtrFactor;
        UseDlzZone = settings.UseDlzZone;
        UseFvgZone = settings.UseFvgZone;
        UseSmcZone = settings.UseSmcZone;
    }

    public void SaveConfig(SettingsSignalStrategyAtrRb settings)
    {
        settings.Length = Length;
        settings.Mult = Mult;
        settings.AtrLength = AtrLength;
        settings.AtrMult = AtrMult;
        settings.UseRsiFilter = UseRsiFilter;
        settings.UseSlideFilter = UseSlideFilter;
        settings.SlideWindow = SlideWindow;
        settings.SlideMinEfficiency = SlideMinEfficiency;
        settings.SlideMinMovePercent = SlideMinMovePercent;
        settings.UseCooldown = UseCooldown;
        settings.CooldownBars = CooldownBars;
        settings.UseStopLoss = UseStopLoss;
        settings.StopLossAtrFactor = StopLossAtrFactor;
        settings.UseDlzZone = UseDlzZone;
        settings.UseFvgZone = UseFvgZone;
        settings.UseSmcZone = UseSmcZone;
    }
}
