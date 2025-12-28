using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Settings.ViewModels;

public partial class StrategySbmSettingsMethodsViewModel : ObservableObject
{
    // Filter on BB%
    [ObservableProperty]
    private double _bbMinPercentage = 1.5;

    [ObservableProperty]
    private double _bbMaxPercentage = 6.0;

    // MACD recovery candles
    [ObservableProperty]
    private int _candlesForMacdRecovery = 2;

    // MA200 and MA50
    [ObservableProperty]
    private bool _ma200AndMa50Crossing = true;

    [ObservableProperty]
    private int _ma200AndMa50Lookback = 30;

    [ObservableProperty]
    private bool _checkMa200AndMa50Percentage = true;

    [ObservableProperty]
    private decimal _ma200AndMa50Percentage = 0.25m;

    // MA200 and MA20
    [ObservableProperty]
    private bool _ma200AndMa20Crossing = true;

    [ObservableProperty]
    private int _ma200AndMa20Lookback = 15;

    [ObservableProperty]
    private bool _checkMa200AndMa20Percentage = true;

    [ObservableProperty]
    private decimal _ma200AndMa20Percentage = 0.50m;

    // MA50 and MA20
    [ObservableProperty]
    private bool _ma50AndMa20Crossing = true;

    [ObservableProperty]
    private int _ma50AndMa20Lookback = 10;

    [ObservableProperty]
    private bool _checkMa50AndMa20Percentage = true;

    [ObservableProperty]
    private decimal _ma50AndMa20Percentage = 0.25m;

    public void LoadConfig(SettingsSignalStrategySbm settings)
    {
        BbMinPercentage = settings.BBMinPercentage;
        BbMaxPercentage = settings.BBMaxPercentage;
        CandlesForMacdRecovery = settings.CandlesForMacdRecovery;

        Ma200AndMa50Crossing = settings.Ma200AndMa50Crossing;
        Ma200AndMa50Lookback = settings.Ma200AndMa50Lookback;
        CheckMa200AndMa50Percentage = settings.CheckMa200AndMa50Percentage;
        Ma200AndMa50Percentage = settings.Ma200AndMa50Percentage;

        Ma200AndMa20Crossing = settings.Ma200AndMa20Crossing;
        Ma200AndMa20Lookback = settings.Ma200AndMa20Lookback;
        CheckMa200AndMa20Percentage = settings.CheckMa200AndMa20Percentage;
        Ma200AndMa20Percentage = settings.Ma200AndMa20Percentage;

        Ma50AndMa20Crossing = settings.Ma50AndMa20Crossing;
        Ma50AndMa20Lookback = settings.Ma50AndMa20Lookback;
        CheckMa50AndMa20Percentage = settings.CheckMa50AndMa20Percentage;
        Ma50AndMa20Percentage = settings.Ma50AndMa20Percentage;
    }

    public void SaveConfig(SettingsSignalStrategySbm settings)
    {
        settings.BBMinPercentage = BbMinPercentage;
        settings.BBMaxPercentage = BbMaxPercentage;
        settings.CandlesForMacdRecovery = CandlesForMacdRecovery;

        settings.Ma200AndMa50Crossing = Ma200AndMa50Crossing;
        settings.Ma200AndMa50Lookback = Ma200AndMa50Lookback;
        settings.CheckMa200AndMa50Percentage = CheckMa200AndMa50Percentage;
        settings.Ma200AndMa50Percentage = Ma200AndMa50Percentage;

        settings.Ma200AndMa20Crossing = Ma200AndMa20Crossing;
        settings.Ma200AndMa20Lookback = Ma200AndMa20Lookback;
        settings.CheckMa200AndMa20Percentage = CheckMa200AndMa20Percentage;
        settings.Ma200AndMa20Percentage = Ma200AndMa20Percentage;

        settings.Ma50AndMa20Crossing = Ma50AndMa20Crossing;
        settings.Ma50AndMa20Lookback = Ma50AndMa20Lookback;
        settings.CheckMa50AndMa20Percentage = CheckMa50AndMa20Percentage;
        settings.Ma50AndMa20Percentage = Ma50AndMa20Percentage;
    }
}
