using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class TraderFuturesViewModel : ObservableObject
{
    [ObservableProperty]
    private decimal _leverage = 1m; // decimal (EXACT match)

    [ObservableProperty]
    private int _crossOrIsolated = 1; // int (EXACT match) - 0=Cross, 1=Isolated

    public void LoadConfig(SettingsTrading settings)
    {
        Leverage = settings.Leverage;
        CrossOrIsolated = settings.CrossOrIsolated;
    }

    public void SaveConfig(SettingsTrading settings)
    {
        settings.Leverage = Leverage;
        settings.CrossOrIsolated = CrossOrIsolated;
    }
}
