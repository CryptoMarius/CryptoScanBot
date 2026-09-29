using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.SrFlip.Config;

public partial class StrategySrFlipSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _useHorizontal = true;

    [ObservableProperty]
    private int _minimumTouches = 3;

    [ObservableProperty]
    private bool _useSloped = true;

    [ObservableProperty]
    private int _historyCandles = 500;

    [ObservableProperty]
    private bool _waitForConfirmation = true;

    [ObservableProperty]
    private int _confirmationCandles = 3;

    [ObservableProperty]
    private decimal _volumeFactor = 1.5m;

    [ObservableProperty]
    private bool _stopBeyondLevel = true;

    [ObservableProperty]
    private decimal _riskRewardRatio = 2m;


    public void LoadConfig(SrFlipSettings settings)
    {
        UseHorizontal = settings.UseHorizontal;
        MinimumTouches = settings.MinimumTouches;
        UseSloped = settings.UseSloped;
        HistoryCandles = settings.HistoryCandles;
        WaitForConfirmation = settings.WaitForConfirmation;
        ConfirmationCandles = settings.ConfirmationCandles;
        VolumeFactor = settings.VolumeFactor;
        StopBeyondLevel = settings.StopBeyondLevel;
        RiskRewardRatio = settings.RiskRewardRatio;
    }

    public void SaveConfig(SrFlipSettings settings)
    {
        settings.UseHorizontal = UseHorizontal;
        settings.MinimumTouches = Math.Max(2, MinimumTouches);
        settings.UseSloped = UseSloped;
        settings.HistoryCandles = Math.Max(60, HistoryCandles);
        settings.WaitForConfirmation = WaitForConfirmation;
        settings.ConfirmationCandles = Math.Max(1, ConfirmationCandles);
        settings.VolumeFactor = Math.Max(0m, VolumeFactor);
        settings.StopBeyondLevel = StopBeyondLevel;
        settings.RiskRewardRatio = Math.Max(0m, RiskRewardRatio);
    }
}
