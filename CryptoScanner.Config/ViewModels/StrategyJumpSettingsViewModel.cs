using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyJumpSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _useLowHighCalculation = false;

    [ObservableProperty]
    private int _candlesLookbackCount = 0;

    [ObservableProperty]
    private decimal _candlePercentage = 4m;



    public void LoadConfig(string caption, SettingsSignalStrategyJump settings)
    {
        UseLowHighCalculation = settings.UseLowHighCalculation;
        CandlesLookbackCount = settings.CandlesLookbackCount;
        CandlePercentage = settings.CandlePercentage;
    }

    public void SaveConfig(SettingsSignalStrategyJump settings)
    {
        settings.UseLowHighCalculation = UseLowHighCalculation;
        settings.CandlesLookbackCount = CandlesLookbackCount;
        settings.CandlePercentage = CandlePercentage;
    }
}
