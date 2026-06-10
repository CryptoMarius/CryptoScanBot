using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyAtrRbSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _length = 20;

    [ObservableProperty]
    private double _outerMult = 4.2;

    [ObservableProperty]
    private double _innerMult = 1.0;

    [ObservableProperty]
    private int _breakLookback = 5;


    public void LoadConfig(string caption, SettingsSignalStrategyAtrRb settings)
    {
        Length = settings.Length;
        OuterMult = settings.OuterMult;
        InnerMult = settings.InnerMult;
        BreakLookback = settings.BreakLookback;
    }

    public void SaveConfig(SettingsSignalStrategyAtrRb settings)
    {
        settings.Length = Length;
        settings.OuterMult = OuterMult;
        settings.InnerMult = InnerMult;
        settings.BreakLookback = BreakLookback;
    }
}
