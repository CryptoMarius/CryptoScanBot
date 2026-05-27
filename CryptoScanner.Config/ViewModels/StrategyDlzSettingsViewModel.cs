using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Config.ViewModels;

public partial class StrategyDlzSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _candleCount = 500; // int, max: 6000

    [ObservableProperty]
    private int _candleCountZoom = 125; // int, max: 6000

    [ObservableProperty]
    private decimal _warnPercentage = 1.0m; // decimal

    [ObservableProperty]
    private decimal _nearZonePercentage = 0.25m;

    [ObservableProperty]
    private int _maxTouches = 2;

    [ObservableProperty]
    private int _rejectionLookback = 1;

    [ObservableProperty]
    private bool _disqualifyOnMitigation = false;

    public void LoadConfig(SettingsSignalStrategyZones settings)
    {
        CandleCount = settings.CandleCount;
        CandleCountZoom = settings.CandleCountZoom;
        WarnPercentage = settings.WarnPercentage;
        NearZonePercentage = settings.NearZonePercentage;
        MaxTouches = settings.MaxTouches;
        RejectionLookback = settings.RejectionLookback;
        DisqualifyOnMitigation = settings.DisqualifyOnMitigation;
    }

    public void SaveConfig(SettingsSignalStrategyZones settings)
    {
        settings.CandleCount = CandleCount;
        settings.CandleCountZoom = CandleCountZoom;
        settings.WarnPercentage = WarnPercentage;
        settings.NearZonePercentage = NearZonePercentage;
        settings.MaxTouches = MaxTouches;
        settings.RejectionLookback = RejectionLookback;
        settings.DisqualifyOnMitigation = DisqualifyOnMitigation;
    }
}
