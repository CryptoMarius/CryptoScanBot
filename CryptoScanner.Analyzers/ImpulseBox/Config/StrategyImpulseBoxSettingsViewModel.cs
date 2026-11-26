using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.ImpulseBox.Config;

public partial class StrategyImpulseBoxSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private double _impulseAtrMultiplier = 3.0;

    [ObservableProperty]
    private int _impulseMaxCandles = 16;

    [ObservableProperty]
    private double _impulseMaxPullbackPercentage = 38.2;

    [ObservableProperty]
    private int _boxCandles = 24;

    [ObservableProperty]
    private double _boxMaxHeightPercentage = 50;

    [ObservableProperty]
    private int _boxExpiryCandles = 192;

    [ObservableProperty]
    private bool _tradeCounter = true;

    [ObservableProperty]
    private bool _tradeFakeBreak = true;

    [ObservableProperty]
    private int _fakeBreakWindowCandles = 8;

    [ObservableProperty]
    private bool _tradeContinuation = false;


    public void LoadConfig(ImpulseBoxSettings settings)
    {
        ImpulseAtrMultiplier = settings.ImpulseAtrMultiplier;
        ImpulseMaxCandles = settings.ImpulseMaxCandles;
        ImpulseMaxPullbackPercentage = settings.ImpulseMaxPullbackPercentage;
        BoxCandles = settings.BoxCandles;
        BoxMaxHeightPercentage = settings.BoxMaxHeightPercentage;
        BoxExpiryCandles = settings.BoxExpiryCandles;
        TradeCounter = settings.TradeCounter;
        TradeFakeBreak = settings.TradeFakeBreak;
        FakeBreakWindowCandles = settings.FakeBreakWindowCandles;
        TradeContinuation = settings.TradeContinuation;
    }

    public void SaveConfig(ImpulseBoxSettings settings)
    {
        settings.ImpulseAtrMultiplier = ImpulseAtrMultiplier;
        settings.ImpulseMaxCandles = ImpulseMaxCandles;
        settings.ImpulseMaxPullbackPercentage = ImpulseMaxPullbackPercentage;
        settings.BoxCandles = BoxCandles;
        settings.BoxMaxHeightPercentage = BoxMaxHeightPercentage;
        settings.BoxExpiryCandles = BoxExpiryCandles;
        settings.TradeCounter = TradeCounter;
        settings.TradeFakeBreak = TradeFakeBreak;
        settings.FakeBreakWindowCandles = FakeBreakWindowCandles;
        settings.TradeContinuation = TradeContinuation;
    }
}
