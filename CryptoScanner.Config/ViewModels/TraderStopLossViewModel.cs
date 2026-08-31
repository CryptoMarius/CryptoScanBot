using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class TraderStopLossViewModel : ObservableObject
{
    private readonly Dictionary<string, CryptoProfitLockMethod> _profitLockMethodList = new()
    {
        { "Fixed stop above break even", CryptoProfitLockMethod.Fixed },
        { "Trailing behind the price", CryptoProfitLockMethod.TrailingPercentage }
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

    public Dictionary<string, CryptoProfitLockMethod> ProfitLockMethodList => _profitLockMethodList;

    /// <summary>Which of the two percentage fields belongs to the selected method.</summary>
    public bool IsFixedProfitLock => MoveSlToBreakEvenMethod == CryptoProfitLockMethod.Fixed;
    public bool IsTrailingProfitLock => MoveSlToBreakEvenMethod == CryptoProfitLockMethod.TrailingPercentage;

    partial void OnMoveSlToBreakEvenMethodChanged(CryptoProfitLockMethod value)
    {
        OnPropertyChanged(nameof(IsFixedProfitLock));
        OnPropertyChanged(nameof(IsTrailingProfitLock));
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
