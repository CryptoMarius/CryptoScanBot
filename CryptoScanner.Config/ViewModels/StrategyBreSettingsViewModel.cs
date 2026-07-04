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
    private int _rsiLength = 14;

    [ObservableProperty]
    private int _rsiOverbought = 70;

    [ObservableProperty]
    private int _rsiOversold = 30;

    [ObservableProperty]
    private bool _useStochFilter = false;

    [ObservableProperty]
    private int _stochLength = 14;

    [ObservableProperty]
    private int _stochKLength = 3;

    [ObservableProperty]
    private int _stochDLength = 3;

    [ObservableProperty]
    private int _stochOverbought = 80;

    [ObservableProperty]
    private int _stochOversold = 20;

    [ObservableProperty]
    private bool _allowStack = true;

    [ObservableProperty]
    private bool _useStopLoss = true;


    public void LoadConfig(string caption, SettingsSignalStrategyBre settings)
    {
        BandLength = settings.BandLength;
        OuterMult = settings.OuterMult;
        DidoLength = settings.DidoLength;
        DidoMult = settings.DidoMult;
        UseTrendFilter = settings.UseTrendFilter;
        HmaLength = settings.HmaLength;
        UseRsiFilter = settings.UseRsiFilter;
        RsiLength = settings.RsiLength;
        RsiOverbought = settings.RsiOverbought;
        RsiOversold = settings.RsiOversold;
        UseStochFilter = settings.UseStochFilter;
        StochLength = settings.StochLength;
        StochKLength = settings.StochKLength;
        StochDLength = settings.StochDLength;
        StochOverbought = settings.StochOverbought;
        StochOversold = settings.StochOversold;
        AllowStack = settings.AllowStack;
        UseStopLoss = settings.UseStopLoss;
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
        settings.RsiLength = RsiLength;
        settings.RsiOverbought = RsiOverbought;
        settings.RsiOversold = RsiOversold;
        settings.UseStochFilter = UseStochFilter;
        settings.StochLength = StochLength;
        settings.StochKLength = StochKLength;
        settings.StochDLength = StochDLength;
        settings.StochOverbought = StochOverbought;
        settings.StochOversold = StochOversold;
        settings.AllowStack = AllowStack;
        settings.UseStopLoss = UseStopLoss;
    }
}
