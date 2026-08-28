using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class TraderEntryConditionsViewModel : ObservableObject
{
    // Entry conditions (all bool - EXACT match)
    [ObservableProperty]
    private bool _checkIncreasingRsi = false;

    [ObservableProperty]
    private bool _checkIncreasingMacd = false;

    [ObservableProperty]
    private bool _checkIncreasingStoch = false;

    [ObservableProperty]
    private bool _checkFurtherPriceMove = false;

    [ObservableProperty]
    private bool _checkTrendPrimaryDirection = false;

    [ObservableProperty]
    private int _TrendPrimaryDirectionCount = 2;

    [ObservableProperty]
    private bool _checkTrendSecondaryDirection = false;

    [ObservableProperty]
    private int _trendSecondaryDirectionCount = 2;

    [ObservableProperty]
    private bool _checkPriceAboveMa200 = false;

    [ObservableProperty]
    private decimal _ma200MinDistancePercentage = 0m;

    [ObservableProperty]
    private int _ma200ConfirmationCandles = 0;

    [ObservableProperty]
    private int _entryWaitMinutes = 0; // minutes; 0 = off

    [ObservableProperty]
    private decimal _entryMaxAdversePercentage = 0m; // %; 0 = no limit

    [ObservableProperty]
    private bool _waitForStochRecovery = false;

    [ObservableProperty]
    private bool _waitForRsiRecovery = false;

    // Stoch OS/OB strength gates
    [ObservableProperty]
    private int _stochExtremeLookback = 20;

    [ObservableProperty]
    private int _stochMinExtremeBars = 0;

    [ObservableProperty]
    private decimal _stochMinExtremeArea = 0m;

    [ObservableProperty]
    private decimal _stochMinExtremeZScore = 0m;

    public void LoadConfig(SettingsTrading settings)
    {
        LoadConfig(settings.EntryConditions);
    }

    public void LoadConfig(SettingsEntryConditions ec)
    {
        CheckIncreasingRsi = ec.CheckIncreasingRsi;
        CheckIncreasingMacd = ec.CheckIncreasingMacd;
        CheckIncreasingStoch = ec.CheckIncreasingStoch;
        CheckFurtherPriceMove = ec.CheckFurtherPriceMove;
        CheckTrendPrimaryDirection = ec.CheckTrendPrimaryDirection;
        TrendPrimaryDirectionCount = ec.TrendPrimaryDirectionCount;
        CheckTrendSecondaryDirection = ec.CheckTrendSecondaryDirection;
        TrendSecondaryDirectionCount = ec.TrendSecondaryDirectionCount;
        CheckPriceAboveMa200 = ec.CheckPriceAboveMa200;
        Ma200MinDistancePercentage = ec.Ma200MinDistancePercentage;
        Ma200ConfirmationCandles = ec.Ma200ConfirmationCandles;
        EntryWaitMinutes = ec.EntryWaitMinutes;
        EntryMaxAdversePercentage = ec.EntryMaxAdversePercentage;
        WaitForStochRecovery = ec.WaitForStochRecovery;
        WaitForRsiRecovery = ec.WaitForRsiRecovery;

        StochExtremeLookback = ec.StochExtremeLookback;
        StochMinExtremeBars = ec.StochMinExtremeBars;
        StochMinExtremeArea = ec.StochMinExtremeArea;
        StochMinExtremeZScore = ec.StochMinExtremeZScore;
    }

    public void SaveConfig(SettingsTrading settings)
    {
        SaveConfig(settings.EntryConditions);
    }

    public void SaveConfig(SettingsEntryConditions ec)
    {
        ec.CheckIncreasingRsi = CheckIncreasingRsi;
        ec.CheckIncreasingMacd = CheckIncreasingMacd;
        ec.CheckIncreasingStoch = CheckIncreasingStoch;
        ec.CheckFurtherPriceMove = CheckFurtherPriceMove;
        ec.CheckTrendPrimaryDirection = CheckTrendPrimaryDirection;
        ec.TrendPrimaryDirectionCount = TrendPrimaryDirectionCount;
        ec.CheckTrendSecondaryDirection = CheckTrendSecondaryDirection;
        ec.TrendSecondaryDirectionCount = TrendSecondaryDirectionCount;
        ec.CheckPriceAboveMa200 = CheckPriceAboveMa200;
        ec.Ma200MinDistancePercentage = Ma200MinDistancePercentage;
        ec.Ma200ConfirmationCandles = Ma200ConfirmationCandles;
        ec.EntryWaitMinutes = EntryWaitMinutes;
        ec.EntryMaxAdversePercentage = EntryMaxAdversePercentage;
        ec.WaitForStochRecovery = WaitForStochRecovery;
        ec.WaitForRsiRecovery = WaitForRsiRecovery;

        ec.StochExtremeLookback = StochExtremeLookback;
        ec.StochMinExtremeBars = StochMinExtremeBars;
        ec.StochMinExtremeArea = StochMinExtremeArea;
        ec.StochMinExtremeZScore = StochMinExtremeZScore;
    }
}
