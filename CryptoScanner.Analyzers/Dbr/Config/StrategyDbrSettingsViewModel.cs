using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.Dbr.Config;

public partial class StrategyDbrSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _bandLength = 20;

    [ObservableProperty]
    private double _outerMult = 3.2;

    [ObservableProperty]
    private bool _useRsiFilter = false;

    [ObservableProperty]
    private bool _requireStochOsOb = false;

    [ObservableProperty]
    private bool _allowStack = true;

    [ObservableProperty]
    private bool _useStopLoss = true;

    public void LoadConfig(DbrSettings settings)
    {
        BandLength = settings.BandLength;
        OuterMult = settings.OuterMult;
        UseRsiFilter = settings.UseRsiFilter;
        RequireStochOsOb = settings.RequireStochOsOb;
        AllowStack = settings.AllowStack;
        UseStopLoss = settings.UseStopLoss;
    }

    public void SaveConfig(DbrSettings settings)
    {
        settings.BandLength = BandLength;
        settings.OuterMult = OuterMult;
        settings.UseRsiFilter = UseRsiFilter;
        settings.RequireStochOsOb = RequireStochOsOb;
        settings.AllowStack = AllowStack;
        settings.UseStopLoss = UseStopLoss;
    }
}
