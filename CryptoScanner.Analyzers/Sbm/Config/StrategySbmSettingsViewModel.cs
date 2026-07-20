using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.Sbm.Config;

public partial class StrategySbmSettingsViewModel : ObservableObject
{
    // SBM1
    [ObservableProperty]
    private int _sbm1CandlesLookbackCount = 2;

    [ObservableProperty]
    private bool _useLowHigh = false;

    // SBM2
    [ObservableProperty]
    private int _sbm2CandlesLookbackCount = 2;

    [ObservableProperty]
    private decimal _sbm2BbPercentage = 2.50m;

    [ObservableProperty]
    private bool _sbm2UseLowHigh = false;

    // SBM3
    [ObservableProperty]
    private int _sbm3CandlesLookbackCount = 8;

    [ObservableProperty]
    private decimal _sbm3CandlesBbRecoveryPercentage = 225;



    public void LoadConfig(SettingsSignalStrategySbm settings)
    {
        // SBM1 signals
        Sbm1CandlesLookbackCount = settings.Sbm1CandlesLookbackCount;
        UseLowHigh = settings.UseLowHigh;

        // SBM2 signals
        Sbm2CandlesLookbackCount = settings.Sbm2CandlesLookbackCount;
        Sbm2BbPercentage = settings.Sbm2BbPercentage;
        Sbm2UseLowHigh = settings.Sbm2UseLowHigh;

        // SBM3 signals
        Sbm3CandlesLookbackCount = settings.Sbm3CandlesLookbackCount;
        Sbm3CandlesBbRecoveryPercentage = settings.Sbm3CandlesBbRecoveryPercentage;
    }


    public void SaveConfig(SettingsSignalStrategySbm settings)
    {
        // SBM1 signals
        settings.Sbm1CandlesLookbackCount = Sbm1CandlesLookbackCount;
        settings.UseLowHigh = UseLowHigh;

        // SBM2 signals
        settings.Sbm2CandlesLookbackCount = Sbm2CandlesLookbackCount;
        settings.Sbm2BbPercentage = Sbm2BbPercentage;
        settings.Sbm2UseLowHigh = Sbm2UseLowHigh;

        // SBM3 signals
        settings.Sbm3CandlesLookbackCount = Sbm3CandlesLookbackCount;
        settings.Sbm3CandlesBbRecoveryPercentage = Sbm3CandlesBbRecoveryPercentage;
    }
}
