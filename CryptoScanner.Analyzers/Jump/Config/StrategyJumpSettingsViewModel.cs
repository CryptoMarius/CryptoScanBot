using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.Jump.Config;

public partial class StrategyJumpSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _useLowHighCalculation = false;

    [ObservableProperty]
    private int _candlesLookbackCount = 0;

    [ObservableProperty]
    private decimal _candlePercentage = 4m;



    public void LoadConfig(JumpSettings settings)
    {
        UseLowHighCalculation = settings.UseLowHighCalculation;
        CandlesLookbackCount = settings.CandlesLookbackCount;
        CandlePercentage = settings.CandlePercentage;
    }

    public void SaveConfig(JumpSettings settings)
    {
        settings.UseLowHighCalculation = UseLowHighCalculation;
        settings.CandlesLookbackCount = CandlesLookbackCount;
        settings.CandlePercentage = CandlePercentage;
    }
}
