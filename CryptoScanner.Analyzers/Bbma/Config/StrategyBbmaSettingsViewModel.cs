using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.Bbma.Config;

public partial class StrategyBbmaSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _reentryStrict = true;

    [ObservableProperty]
    private int _reentryMinCandlesAfterTrigger = 3;

    [ObservableProperty]
    private int _htfSetupLookback = 10;

    [ObservableProperty]
    private bool _htfSetupExtremeInvalidates = true;

    [ObservableProperty]
    private bool _takeProfitAtOuterBand = true;

    [ObservableProperty]
    private bool _takeProfitOnHtfBand = true;

    [ObservableProperty]
    private bool _takeProfitBandOrder = true;

    [ObservableProperty]
    private int _stopLookbackCandles = 3;

    [ObservableProperty]
    private bool _stopBeyondReentryCandle = true;

    [ObservableProperty]
    private decimal _stopMarginPercentage = 0.1m;


    public void LoadConfig(BbmaSettings settings)
    {
        ReentryStrict = settings.ReentryStrict;
        ReentryMinCandlesAfterTrigger = settings.ReentryMinCandlesAfterTrigger;
        HtfSetupLookback = settings.HtfSetupLookback;
        HtfSetupExtremeInvalidates = settings.HtfSetupExtremeInvalidates;
        TakeProfitAtOuterBand = settings.TakeProfitAtOuterBand;
        TakeProfitOnHtfBand = settings.TakeProfitOnHtfBand;
        TakeProfitBandOrder = settings.TakeProfitBandOrder;
        StopLookbackCandles = settings.StopLookbackCandles;
        StopBeyondReentryCandle = settings.StopBeyondReentryCandle;
        StopMarginPercentage = settings.StopMarginPercentage;
    }

    public void SaveConfig(BbmaSettings settings)
    {
        settings.ReentryStrict = ReentryStrict;
        settings.ReentryMinCandlesAfterTrigger = ReentryMinCandlesAfterTrigger;
        settings.HtfSetupLookback = HtfSetupLookback;
        settings.HtfSetupExtremeInvalidates = HtfSetupExtremeInvalidates;
        settings.TakeProfitAtOuterBand = TakeProfitAtOuterBand;
        settings.TakeProfitOnHtfBand = TakeProfitOnHtfBand;
        settings.TakeProfitBandOrder = TakeProfitBandOrder;
        settings.StopLookbackCandles = StopLookbackCandles;
        settings.StopBeyondReentryCandle = StopBeyondReentryCandle;
        settings.StopMarginPercentage = StopMarginPercentage;
    }
}
