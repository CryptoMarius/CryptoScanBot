using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class TraderStopLossViewModel : ObservableObject
{
    [ObservableProperty]
    private decimal _stopLossPercentage = 0m; // decimal (EXACT match)

    [ObservableProperty]
    private decimal _stopLossLimitPercentage = 0m; // decimal (EXACT match)

    [ObservableProperty]
    private bool _moveSlToBreakEven = false;

    [ObservableProperty]
    private decimal _moveSlToBreakEvenPercentage = 0.5m;

    public void LoadConfig(SettingsTrading settings)
    {
        StopLossPercentage = settings.StopLossPercentage;
        StopLossLimitPercentage = settings.StopLossLimitPercentage;
        MoveSlToBreakEven = settings.MoveSlToBreakEven;
        MoveSlToBreakEvenPercentage = settings.MoveSlToBreakEvenPercentage;
    }

    public void SaveConfig(SettingsTrading settings)
    {
        settings.StopLossPercentage = StopLossPercentage;
        settings.StopLossLimitPercentage = StopLossLimitPercentage;
        settings.MoveSlToBreakEven = MoveSlToBreakEven;
        settings.MoveSlToBreakEvenPercentage = MoveSlToBreakEvenPercentage;
    }
}
