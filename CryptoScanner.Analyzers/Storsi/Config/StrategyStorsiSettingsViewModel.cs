using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Storsi.Config;

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
    private double _bbMinPercentage = 1.50;

    [ObservableProperty]
    private double _bbMaxPercentage = 100.0;

    public void LoadConfig(StoRsiSettings settings)
    {
        BbMinPercentage = settings.BBMinPercentage;
        BbMaxPercentage = settings.BBMaxPercentage;
        SkipFirstSignal = settings.SkipFirstSignal;
        AddRsiAmount = settings.AddRsiAmount;
        CheckMacdRecovery = settings.CheckMacdRecovery;
        CheckBollingerBandsCondition = settings.CheckBollingerBandsCondition;
    }

    public void SaveConfig(StoRsiSettings settings)
    {
        settings.BBMinPercentage = BbMinPercentage;
        settings.BBMaxPercentage = BbMaxPercentage;
        settings.SkipFirstSignal = SkipFirstSignal;
        settings.AddRsiAmount = AddRsiAmount;
        settings.CheckMacdRecovery = CheckMacdRecovery;
        settings.CheckBollingerBandsCondition = CheckBollingerBandsCondition;
    }
}
