using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Settings.ViewModels;

public partial class TraderStopLossViewModel : ObservableObject
{
    [ObservableProperty]
    private decimal _stopLossPercentage = 0m; // decimal (EXACT match)

    [ObservableProperty]
    private decimal _stopLossLimitPercentage = 0m; // decimal (EXACT match)

    public void LoadConfig(SettingsTrading settings)
    {
        StopLossPercentage = settings.StopLossPercentage;
        StopLossLimitPercentage = settings.StopLossLimitPercentage;
    }

    public void SaveConfig(SettingsTrading settings)
    {
        settings.StopLossPercentage = StopLossPercentage;
        settings.StopLossLimitPercentage = StopLossLimitPercentage;
    }
}
