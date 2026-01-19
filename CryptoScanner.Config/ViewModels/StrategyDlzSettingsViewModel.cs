using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyDlzSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _candleCount = 500; // int, max: 6000

    [ObservableProperty]
    private int _candleCountZoom = 125; // int, max: 6000

    [ObservableProperty]
    private decimal _warnPercentage = 1.0m; // decimal

    public void LoadConfig(SettingsSignalStrategyZones settings)
    {
        CandleCount = settings.CandleCount;
        CandleCountZoom = settings.CandleCountZoom;
        WarnPercentage = settings.WarnPercentage;
    }

    public void SaveConfig(SettingsSignalStrategyZones settings)
    {
        settings.CandleCount = CandleCount;
        settings.CandleCountZoom = CandleCountZoom;
        settings.WarnPercentage = WarnPercentage;
    }
}
