using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.Config.ViewModels;

public partial class TraderStopLossViewModel : ObservableObject
{
    private readonly Dictionary<string, CryptoProfitLockMethod> _profitLockMethodList = new()
    {
        { "Fixed stop above break even", CryptoProfitLockMethod.Fixed },
        { "Trailing behind the price", CryptoProfitLockMethod.TrailingPercentage },
        { "Trailing on Keltner channel and PSAR", CryptoProfitLockMethod.TrailingKeltnerPsar },
        { "Break even after TP1 (Altrady)", CryptoProfitLockMethod.BreakEvenAfterFirstTakeProfit },
        { "Follow take profit (Altrady)", CryptoProfitLockMethod.FollowTakeProfit }
    };

    [ObservableProperty]
    private decimal _stopLossPercentage = 0m; // decimal (EXACT match)

    [ObservableProperty]
    private decimal _stopLossLimitPercentage = 0m; // decimal (EXACT match)

    [ObservableProperty]
    private bool _moveSlToBreakEven = false;

    [ObservableProperty]
    private decimal _moveSlToBreakEvenPercentage = 0.5m;

    [ObservableProperty]
    private decimal _moveSlToBreakEvenSlPercentage = 0.5m;

    [ObservableProperty]
    private CryptoProfitLockMethod _moveSlToBreakEvenMethod = CryptoProfitLockMethod.Fixed;

    [ObservableProperty]
    private decimal _moveSlToBreakEvenTrailPercentage = 1.5m;

    //public Dictionary<string, CryptoProfitLockMethod> ProfitLockMethodList => _profitLockMethodList;

    /// <summary>
    /// Trading through Altrady (set by TraderTabViewModel from the Trading choice): only the methods
    /// Altrady runs itself can be chosen then. A method that is already set stays in the list, marked,
    /// so nothing is changed behind the user's back.
    /// </summary>
    [ObservableProperty]
    private bool _usesAltrady;

    public Dictionary<string, CryptoProfitLockMethod> ProfitLockMethodList
    {
        get
        {
            if (!UsesAltrady)
                return _profitLockMethodList;
            Dictionary<string, CryptoProfitLockMethod> list = [];
            foreach (var (caption, method) in _profitLockMethodList)
            {
                if (ProfitLockCalculator.IsSupportedByAltrady(method))
                    list[caption] = method;
                else if (method == MoveSlToBreakEvenMethod)
                    list[caption + " (not with Altrady)"] = method;
            }
            return list;
        }
    }

    /// <summary>The chosen method does nothing while trading through Altrady.</summary>
    public bool ShowAltradyLockWarning => UsesAltrady && MoveSlToBreakEven && !ProfitLockCalculator.IsSupportedByAltrady(MoveSlToBreakEvenMethod);

    partial void OnUsesAltradyChanged(bool value)
    {
        OnPropertyChanged(nameof(ProfitLockMethodList));
        OnPropertyChanged(nameof(ShowAltradyLockWarning));
    }

    /// <summary>Which of the two percentage fields belongs to the selected method.</summary>
    // The Keltner/PSAR trail uses the fixed level as its floor, so it shows the same field.
    public bool IsFixedProfitLock => MoveSlToBreakEvenMethod != CryptoProfitLockMethod.TrailingPercentage
        && !ProfitLockCalculator.IsArmedByTakeProfit(MoveSlToBreakEvenMethod);
    public bool IsTrailingProfitLock => MoveSlToBreakEvenMethod == CryptoProfitLockMethod.TrailingPercentage;
    // The two Altrady protections are armed by a filled take profit, not by the trigger percentage
    public bool ShowProfitLockTrigger => MoveSlToBreakEven && !ProfitLockCalculator.IsArmedByTakeProfit(MoveSlToBreakEvenMethod);

    partial void OnMoveSlToBreakEvenMethodChanged(CryptoProfitLockMethod value)
    {
        OnPropertyChanged(nameof(ShowAltradyLockWarning));
        OnPropertyChanged(nameof(IsFixedProfitLock));
        OnPropertyChanged(nameof(IsTrailingProfitLock));
        OnPropertyChanged(nameof(ShowProfitLockTrigger));
    }

    partial void OnMoveSlToBreakEvenChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowAltradyLockWarning));
        OnPropertyChanged(nameof(ShowProfitLockTrigger));
    }

    public void LoadConfig(SettingsTrading settings)
    {
        StopLossPercentage = settings.StopLossPercentage;
        StopLossLimitPercentage = settings.StopLossLimitPercentage;
        MoveSlToBreakEven = settings.MoveSlToBreakEven;
        MoveSlToBreakEvenPercentage = settings.MoveSlToBreakEvenPercentage;
        MoveSlToBreakEvenSlPercentage = settings.MoveSlToBreakEvenSlPercentage;
        MoveSlToBreakEvenMethod = settings.MoveSlToBreakEvenMethod;
        MoveSlToBreakEvenTrailPercentage = settings.MoveSlToBreakEvenTrailPercentage;
    }

    public void SaveConfig(SettingsTrading settings)
    {
        settings.StopLossPercentage = StopLossPercentage;
        settings.StopLossLimitPercentage = StopLossLimitPercentage;
        settings.MoveSlToBreakEven = MoveSlToBreakEven;
        settings.MoveSlToBreakEvenPercentage = MoveSlToBreakEvenPercentage;
        settings.MoveSlToBreakEvenSlPercentage = MoveSlToBreakEvenSlPercentage;
        settings.MoveSlToBreakEvenMethod = MoveSlToBreakEvenMethod;
        settings.MoveSlToBreakEvenTrailPercentage = MoveSlToBreakEvenTrailPercentage;
    }
}
