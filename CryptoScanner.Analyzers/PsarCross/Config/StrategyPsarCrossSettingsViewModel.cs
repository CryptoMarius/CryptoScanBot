using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.PsarCross.Config;

public partial class StrategyPsarCrossSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _confirmationCandles = 0;

    [ObservableProperty]
    private decimal _minimumDistancePercentage = 0m;

    [ObservableProperty]
    private decimal _relativeVolumeMinimum = 0m;

    [ObservableProperty]
    private int _relativeVolumeCandles = 3;

    [ObservableProperty]
    private int _relativeVolumeAverageCandles = 20;

    [ObservableProperty]
    private bool _exitOnFlipBack = true;

    [ObservableProperty]
    private int _exitConfirmationCandles = 0;


    public void LoadConfig(PsarCrossSettings settings)
    {
        ConfirmationCandles = settings.ConfirmationCandles;
        MinimumDistancePercentage = settings.MinimumDistancePercentage;
        RelativeVolumeMinimum = settings.RelativeVolumeMinimum;
        RelativeVolumeCandles = settings.RelativeVolumeCandles;
        RelativeVolumeAverageCandles = settings.RelativeVolumeAverageCandles;
        ExitOnFlipBack = settings.ExitOnFlipBack;
        ExitConfirmationCandles = settings.ExitConfirmationCandles;
    }

    public void SaveConfig(PsarCrossSettings settings)
    {
        settings.ConfirmationCandles = ConfirmationCandles;
        settings.MinimumDistancePercentage = MinimumDistancePercentage;
        settings.RelativeVolumeMinimum = RelativeVolumeMinimum;
        settings.RelativeVolumeCandles = RelativeVolumeCandles;
        settings.RelativeVolumeAverageCandles = RelativeVolumeAverageCandles;
        settings.ExitOnFlipBack = ExitOnFlipBack;
        settings.ExitConfirmationCandles = ExitConfirmationCandles;
    }
}
