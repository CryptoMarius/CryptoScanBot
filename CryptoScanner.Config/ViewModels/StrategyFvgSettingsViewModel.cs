using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyFvgSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private double _minimumPercentage = 0.25;

    [ObservableProperty]
    private decimal _nearZonePercentage = 0.25m;

    [ObservableProperty]
    private int _maxTouches = 2;

    [ObservableProperty]
    private int _rejectionLookback = 1;

    [ObservableProperty]
    private bool _disqualifyOnMitigation = false;


    public void LoadConfig(SettingsSignalStrategyFvg settings)
    {
        MinimumPercentage = settings.MinimumPercentage;
        NearZonePercentage = settings.NearZonePercentage;
        MaxTouches = settings.MaxTouches;
        RejectionLookback = settings.RejectionLookback;
        DisqualifyOnMitigation = settings.DisqualifyOnMitigation;
    }

    public void SaveConfig(SettingsSignalStrategyFvg settings)
    {
        settings.MinimumPercentage = MinimumPercentage;
        settings.NearZonePercentage = NearZonePercentage;
        settings.MaxTouches = MaxTouches;
        settings.RejectionLookback = RejectionLookback;
        settings.DisqualifyOnMitigation = DisqualifyOnMitigation;
    }
}
