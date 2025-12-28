using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Settings.ViewModels;

public partial class StrategyDlzSettingsZoneFilterViewModel : ObservableObject
{
    // EXACT SAME TYPES as SettingsSignalStrategyZones
    
    [ObservableProperty]
    private bool _zoneStartApply = false; // bool

    [ObservableProperty]
    private int _zoneStartCandleCount = 5; // int

    [ObservableProperty]
    private double _zoneStartPercentage = 2.5; // double

    public void LoadConfig(SettingsSignalStrategyZones settings)
    {
        ZoneStartApply = settings.ZoneStartApply;
        ZoneStartCandleCount = settings.ZoneStartCandleCount;
        ZoneStartPercentage = settings.ZoneStartPercentage;
    }

    public void SaveConfig(SettingsSignalStrategyZones settings)
    {
        settings.ZoneStartApply = ZoneStartApply;
        settings.ZoneStartCandleCount = ZoneStartCandleCount;
        settings.ZoneStartPercentage = ZoneStartPercentage;
    }
}
