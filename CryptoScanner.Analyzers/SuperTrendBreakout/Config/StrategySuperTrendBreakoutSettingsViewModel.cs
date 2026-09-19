using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.SuperTrendBreakout.Config;

public partial class StrategySuperTrendBreakoutSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _zoneLookbackCandles = 5;

    [ObservableProperty]
    private bool _includeOpenZones = true;

    [ObservableProperty]
    private bool _includeClosedZones = true;

    [ObservableProperty]
    private int _closedZoneMaxAgeCandles = 10;

    public void LoadConfig(SuperTrendBreakoutSettings settings)
    {
        ZoneLookbackCandles = settings.ZoneLookbackCandles;
        IncludeOpenZones = settings.IncludeOpenZones;
        IncludeClosedZones = settings.IncludeClosedZones;
        ClosedZoneMaxAgeCandles = settings.ClosedZoneMaxAgeCandles;
    }

    public void SaveConfig(SuperTrendBreakoutSettings settings)
    {
        settings.ZoneLookbackCandles = ZoneLookbackCandles;
        settings.IncludeOpenZones = IncludeOpenZones;
        settings.IncludeClosedZones = IncludeClosedZones;
        settings.ClosedZoneMaxAgeCandles = ClosedZoneMaxAgeCandles;
    }
}
