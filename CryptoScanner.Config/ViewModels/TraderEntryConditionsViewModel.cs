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
    private bool _waitForStochRecovery = false;

    [ObservableProperty]
    private bool _waitForRsiRecovery = false;

    public void LoadConfig(SettingsTrading settings)
    {
        CheckIncreasingRsi = settings.CheckIncreasingRsi;
        CheckIncreasingMacd = settings.CheckIncreasingMacd;
        CheckIncreasingStoch = settings.CheckIncreasingStoch;
        CheckFurtherPriceMove = settings.CheckFurtherPriceMove;
        CheckTrendPrimaryDirection = settings.CheckTrendPrimaryDirection;
        TrendPrimaryDirectionCount = settings.TrendPrimaryDirectionCount;
        CheckTrendSecondaryDirection = settings.CheckTrendSecondaryDirection;
        TrendSecondaryDirectionCount = settings.TrendSecondaryDirectionCount;
        WaitForStochRecovery = settings.WaitForStochRecovery;
        WaitForRsiRecovery = settings.WaitForRsiRecovery;
    }

    public void SaveConfig(SettingsTrading settings)
    {
        settings.CheckIncreasingRsi = CheckIncreasingRsi;
        settings.CheckIncreasingMacd = CheckIncreasingMacd;
        settings.CheckIncreasingStoch = CheckIncreasingStoch;
        settings.CheckFurtherPriceMove = CheckFurtherPriceMove;
        settings.CheckTrendPrimaryDirection = CheckTrendPrimaryDirection;
        settings.TrendPrimaryDirectionCount = TrendPrimaryDirectionCount;
        settings.CheckTrendSecondaryDirection = CheckTrendSecondaryDirection;
        settings.TrendSecondaryDirectionCount = TrendSecondaryDirectionCount;
        settings.WaitForStochRecovery = WaitForStochRecovery;
        settings.WaitForRsiRecovery = WaitForRsiRecovery;
    }
}
