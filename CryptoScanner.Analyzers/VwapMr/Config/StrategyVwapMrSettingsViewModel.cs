using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.VwapMr.Config;

public partial class StrategyVwapMrSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _stdevLength = 20;

    [ObservableProperty]
    private double _mult = 2.0;

    [ObservableProperty]
    private bool _requireReversalCandle = true;

    [ObservableProperty]
    private decimal _stopLossPercentage = 1.5m;

    [ObservableProperty]
    private bool _exitAtVwap = true;


    public void LoadConfig(VwapMrSettings settings)
    {
        StdevLength = settings.StdevLength;
        Mult = settings.Mult;
        RequireReversalCandle = settings.RequireReversalCandle;
        StopLossPercentage = settings.StopLossPercentage;
        ExitAtVwap = settings.ExitAtVwap;
    }

    public void SaveConfig(VwapMrSettings settings)
    {
        settings.StdevLength = StdevLength;
        settings.Mult = Mult;
        settings.RequireReversalCandle = RequireReversalCandle;
        settings.StopLossPercentage = StopLossPercentage;
        settings.ExitAtVwap = ExitAtVwap;
    }
}
