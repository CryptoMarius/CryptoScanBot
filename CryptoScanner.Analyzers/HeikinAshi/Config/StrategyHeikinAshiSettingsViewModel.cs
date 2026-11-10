using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.HeikinAshi.Config;

public partial class StrategyHeikinAshiSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _higherInterval = "1w";

    [ObservableProperty]
    private bool _exitOnSignal = true;

    [ObservableProperty]
    private bool _exitOnOppositeWick = true;

    [ObservableProperty]
    private decimal _wickMinimumPercentage = 5m;

    [ObservableProperty]
    private bool _stopBeyondSignalCandle = true;


    public void LoadConfig(HeikinAshiSettings settings)
    {
        HigherInterval = settings.HigherInterval;
        ExitOnSignal = settings.ExitOnSignal;
        ExitOnOppositeWick = settings.ExitOnOppositeWick;
        WickMinimumPercentage = settings.WickMinimumPercentage;
        StopBeyondSignalCandle = settings.StopBeyondSignalCandle;
    }

    public void SaveConfig(HeikinAshiSettings settings)
    {
        settings.HigherInterval = (HigherInterval ?? "").Trim();
        settings.ExitOnSignal = ExitOnSignal;
        settings.ExitOnOppositeWick = ExitOnOppositeWick;
        settings.WickMinimumPercentage = Math.Max(0m, WickMinimumPercentage);
        settings.StopBeyondSignalCandle = StopBeyondSignalCandle;
    }
}
