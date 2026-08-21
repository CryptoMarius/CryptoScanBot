using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.Vbs.Config;

public partial class StrategyVbsSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _length = 90;

    [ObservableProperty]
    private double _mult = 2.5;

    [ObservableProperty]
    private double _bbMinPercentage = 1.50;

    [ObservableProperty]
    private double _bbMaxPercentage = 0.0;

    [ObservableProperty]
    private bool _useRsiFilter = true;

    [ObservableProperty]
    private bool _useStopLoss = true;

    // Stop-loss = Entry -/+ ACS%, ACS = AcsFactor * SMA((high-low)/close, AcsLength) * 100.
    [ObservableProperty]
    private double _acsFactor = 2.17;

    [ObservableProperty]
    private int _acsLength = 50;

    [ObservableProperty]
    private bool _useTakeProfit = false;

    [ObservableProperty]
    private double _riskRewardRatio = 1.0;

    [ObservableProperty]
    private bool _requireStochOsOb = false;

    [ObservableProperty]
    private int _bandBreakConfirmationCount = 0;

    public void LoadConfig(VbsSettings settings)
    {
        Length = settings.Length;
        Mult = settings.Mult;
        BbMinPercentage = settings.BBMinPercentage;
        BbMaxPercentage = settings.BBMaxPercentage;
        UseRsiFilter = settings.UseRsiFilter;
        UseStopLoss = settings.UseStopLoss;
        AcsFactor = settings.AcsFactor;
        AcsLength = settings.AcsLength;
        UseTakeProfit = settings.UseTakeProfit;
        RiskRewardRatio = settings.RiskRewardRatio;
        RequireStochOsOb = settings.RequireStochOsOb;
        BandBreakConfirmationCount = settings.BandBreakConfirmationCount;
    }

    public void SaveConfig(VbsSettings settings)
    {
        settings.Length = Length;
        settings.Mult = Mult;
        settings.BBMinPercentage = BbMinPercentage;
        settings.BBMaxPercentage = BbMaxPercentage;
        settings.UseRsiFilter = UseRsiFilter;
        settings.UseStopLoss = UseStopLoss;
        settings.AcsFactor = AcsFactor;
        settings.AcsLength = AcsLength;
        settings.UseTakeProfit = UseTakeProfit;
        settings.RiskRewardRatio = RiskRewardRatio;
        settings.RequireStochOsOb = RequireStochOsOb;
        settings.BandBreakConfirmationCount = BandBreakConfirmationCount;
    }
}
