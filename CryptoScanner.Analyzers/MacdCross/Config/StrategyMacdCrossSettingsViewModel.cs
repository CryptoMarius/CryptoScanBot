using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.MacdCross.Config;

public partial class StrategyMacdCrossSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _confirmationCandles = 0;

    [ObservableProperty]
    private decimal _minimumDistancePercentage = 0m;

    [ObservableProperty]
    private bool _requireCrossBeyondZeroLine = false;

    [ObservableProperty]
    private decimal _adxMinimum = 0m;

    [ObservableProperty]
    private decimal _adxRecentlyBelow = 0m;

    [ObservableProperty]
    private int _adxRecentlyWithinCandles = 10;

    [ObservableProperty]
    private decimal _relativeVolumeMinimum = 0m;

    [ObservableProperty]
    private int _relativeVolumeCandles = 3;

    [ObservableProperty]
    private int _relativeVolumeAverageCandles = 50;

    [ObservableProperty]
    private bool _exitOnCrossBack = true;

    [ObservableProperty]
    private int _exitConfirmationCandles = 0;


    public void LoadConfig(MacdCrossSettings settings)
    {
        ConfirmationCandles = settings.ConfirmationCandles;
        MinimumDistancePercentage = settings.MinimumDistancePercentage;
        RequireCrossBeyondZeroLine = settings.RequireCrossBeyondZeroLine;
        AdxMinimum = settings.AdxMinimum;
        AdxRecentlyBelow = settings.AdxRecentlyBelow;
        AdxRecentlyWithinCandles = settings.AdxRecentlyWithinCandles;
        RelativeVolumeMinimum = settings.RelativeVolumeMinimum;
        RelativeVolumeCandles = settings.RelativeVolumeCandles;
        RelativeVolumeAverageCandles = settings.RelativeVolumeAverageCandles;
        ExitOnCrossBack = settings.ExitOnCrossBack;
        ExitConfirmationCandles = settings.ExitConfirmationCandles;
    }

    public void SaveConfig(MacdCrossSettings settings)
    {
        settings.ConfirmationCandles = ConfirmationCandles;
        settings.MinimumDistancePercentage = MinimumDistancePercentage;
        settings.RequireCrossBeyondZeroLine = RequireCrossBeyondZeroLine;
        settings.AdxMinimum = AdxMinimum;
        settings.AdxRecentlyBelow = AdxRecentlyBelow;
        settings.AdxRecentlyWithinCandles = AdxRecentlyWithinCandles;
        settings.RelativeVolumeMinimum = RelativeVolumeMinimum;
        settings.RelativeVolumeCandles = RelativeVolumeCandles;
        settings.RelativeVolumeAverageCandles = RelativeVolumeAverageCandles;
        settings.ExitOnCrossBack = ExitOnCrossBack;
        settings.ExitConfirmationCandles = ExitConfirmationCandles;
    }
}
