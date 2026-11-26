using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.ChochFvg.Config;

public partial class StrategyChochFvgSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _swingStrength = 3;

    [ObservableProperty]
    private int _lookbackCandles = 60;

    [ObservableProperty]
    private decimal _fibZoneStart = 50m;

    [ObservableProperty]
    private decimal _fibZoneEnd = 61.8m;

    [ObservableProperty]
    private int _entryExpiryCandles = 60;

    [ObservableProperty]
    private decimal _riskReward = 4m;

    [ObservableProperty]
    private decimal _minimumStopPercentage = 0.2m;

    [ObservableProperty]
    private bool _useSessionWindow = true;

    [ObservableProperty]
    private string _sessionStart = "09:30";

    [ObservableProperty]
    private string _sessionEnd = "11:30";


    public void LoadConfig(ChochFvgSettings settings)
    {
        SwingStrength = settings.SwingStrength;
        LookbackCandles = settings.LookbackCandles;
        FibZoneStart = settings.FibZoneStart;
        FibZoneEnd = settings.FibZoneEnd;
        EntryExpiryCandles = settings.EntryExpiryCandles;
        RiskReward = settings.RiskReward;
        MinimumStopPercentage = settings.MinimumStopPercentage;
        UseSessionWindow = settings.UseSessionWindow;
        SessionStart = settings.SessionStart;
        SessionEnd = settings.SessionEnd;
    }

    public void SaveConfig(ChochFvgSettings settings)
    {
        settings.SwingStrength = SwingStrength;
        settings.LookbackCandles = LookbackCandles;
        settings.FibZoneStart = FibZoneStart;
        settings.FibZoneEnd = FibZoneEnd;
        settings.EntryExpiryCandles = EntryExpiryCandles;
        settings.RiskReward = RiskReward;
        settings.MinimumStopPercentage = MinimumStopPercentage;
        settings.UseSessionWindow = UseSessionWindow;
        settings.SessionStart = SessionStart.Trim();
        settings.SessionEnd = SessionEnd.Trim();
    }
}
