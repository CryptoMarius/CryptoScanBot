using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyStorsiSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _addRsiAmount = 0;

    [ObservableProperty]
    private bool _checkBollingerBandsCondition = false;

    [ObservableProperty]
    private bool _skipFirstSignal = false;

    [ObservableProperty]
    private bool _checkMacdRecovery = false;

    [ObservableProperty]
    private bool _onlyIfLux5m = false;

    [ObservableProperty]
    private double _bbMinPercentage = 1.50;

    [ObservableProperty]
    private double _bbMaxPercentage = 100.0;

    [ObservableProperty]
    private bool _checkTrendPrimaryDirection = false;

    [ObservableProperty]
    private int _TrendPrimaryDirectionCount = 2;

    [ObservableProperty]
    private bool _checkTrendSecondaryDirection = false;

    [ObservableProperty]
    private int _trendSecondaryDirectionCount = 2;


    public void LoadConfig(string caption, SettingsSignalStrategyStoRsi settings)
    {
        BbMinPercentage = settings.BBMinPercentage;
        BbMaxPercentage = settings.BBMaxPercentage;
        SkipFirstSignal = settings.SkipFirstSignal;
        AddRsiAmount = settings.AddRsiAmount;
        CheckMacdRecovery = settings.CheckMacdRecovery;
        CheckBollingerBandsCondition = settings.CheckBollingerBandsCondition;
        OnlyIfLux5m = settings.OnlyIfLux5m;
        CheckTrendPrimaryDirection = settings.CheckTrendPrimaryDirection;
        TrendPrimaryDirectionCount = settings.TrendPrimaryDirectionCount;
        CheckTrendSecondaryDirection = settings.CheckTrendSecondaryDirection;
        TrendSecondaryDirectionCount = settings.TrendSecondaryDirectionCount;
    }

    public void SaveConfig(SettingsSignalStrategyStoRsi settings)
    {
        settings.BBMinPercentage = BbMinPercentage;
        settings.BBMaxPercentage = BbMaxPercentage;
        settings.SkipFirstSignal = SkipFirstSignal;
        settings.AddRsiAmount = AddRsiAmount;
        settings.CheckMacdRecovery = CheckMacdRecovery;
        settings.CheckBollingerBandsCondition = CheckBollingerBandsCondition;
        settings.OnlyIfLux5m = OnlyIfLux5m;
        settings.CheckTrendPrimaryDirection = CheckTrendPrimaryDirection;
        settings.TrendPrimaryDirectionCount = TrendPrimaryDirectionCount;
        settings.CheckTrendSecondaryDirection = CheckTrendSecondaryDirection;
        settings.TrendSecondaryDirectionCount = TrendSecondaryDirectionCount;
    }
}
