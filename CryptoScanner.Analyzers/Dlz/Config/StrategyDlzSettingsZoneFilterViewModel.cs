using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Analyzers.Dlz.Config;

public partial class StrategyDlzSettingsZoneFilterViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _zoneStartApply = false;

    [ObservableProperty]
    private int _zoneStartCandleCount = 5;

    [ObservableProperty]
    private double _zoneStartPercentage = 2.5;

    public void LoadConfig(SettingsSignalStrategyDlz settings)
    {
        ZoneStartApply = settings.ZoneStartApply;
        ZoneStartCandleCount = settings.ZoneStartCandleCount;
        ZoneStartPercentage = settings.ZoneStartPercentage;
    }

    public void SaveConfig(SettingsSignalStrategyDlz settings)
    {
        settings.ZoneStartApply = ZoneStartApply;
        settings.ZoneStartCandleCount = settings.ZoneStartCandleCount;
        settings.ZoneStartPercentage = ZoneStartPercentage;
    }
}
