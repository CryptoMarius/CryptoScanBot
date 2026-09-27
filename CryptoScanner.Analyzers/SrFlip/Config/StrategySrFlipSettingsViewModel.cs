using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.SrFlip.Config;

public partial class StrategySrFlipSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _useHorizontal = true;

    [ObservableProperty]
    private int _minimumTouches = 2;

    [ObservableProperty]
    private bool _useSloped = true;

    [ObservableProperty]
    private int _historyCandles = 500;

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
        StopBeyondLevel = settings.StopBeyondLevel;
        RiskRewardRatio = settings.RiskRewardRatio;
    }

    public void SaveConfig(SrFlipSettings settings)
    {
        settings.UseHorizontal = UseHorizontal;
        settings.MinimumTouches = Math.Max(2, MinimumTouches);
        settings.UseSloped = UseSloped;
        settings.HistoryCandles = Math.Max(60, HistoryCandles);
        settings.StopBeyondLevel = StopBeyondLevel;
        settings.RiskRewardRatio = Math.Max(0m, RiskRewardRatio);
    }
}
