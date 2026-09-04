using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Enums;

namespace CryptoScanner.Analyzers.FailedBreakout.Config;

public partial class StrategyFailedBreakoutSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _lookbackCandles = 20;

    [ObservableProperty]
    private int _breakWithinCandles = 3;

    [ObservableProperty]
    private decimal _minimumBreakPercentage = 0m;

    [ObservableProperty]
    private decimal _closeWithinRangePercentage = 50m;

    // The zone sources the breaking candle has to sit in, as one checkbox each. Three fixed boxes
    // rather than a list view: there are exactly three members and they are addressed through
    // nameof, so renaming one in CryptoZoneSource breaks the build instead of drifting apart from
    // the Photino host, which builds its checkboxes from the very same enum.
    [ObservableProperty]
    private bool _requireDlz = false;

    [ObservableProperty]
    private bool _requireFvg = false;

    [ObservableProperty]
    private bool _requireSmc = false;

    [ObservableProperty]
    private decimal _zoneTolerancePercentage = 0m;


    public void LoadConfig(FailedBreakoutSettings settings)
    {
        LookbackCandles = settings.LookbackCandles;
        BreakWithinCandles = settings.BreakWithinCandles;
        MinimumBreakPercentage = settings.MinimumBreakPercentage;
        CloseWithinRangePercentage = settings.CloseWithinRangePercentage;

        // Case-insensitive, because the list can also be typed by hand in the settings file or in
        // the emulator queue - where it is written "dlz" rather than "Dlz".
        static bool Has(List<string> names, CryptoZoneSource source)
            => names.Exists(z => z.Equals(source.ToString(), StringComparison.OrdinalIgnoreCase));

        RequireDlz = Has(settings.RequireZone, CryptoZoneSource.Dlz);
        RequireFvg = Has(settings.RequireZone, CryptoZoneSource.Fvg);
        RequireSmc = Has(settings.RequireZone, CryptoZoneSource.Smc);
        ZoneTolerancePercentage = settings.ZoneTolerancePercentage;
    }

    public void SaveConfig(FailedBreakoutSettings settings)
    {
        settings.LookbackCandles = LookbackCandles;
        settings.BreakWithinCandles = BreakWithinCandles;
        settings.MinimumBreakPercentage = MinimumBreakPercentage;
        settings.CloseWithinRangePercentage = CloseWithinRangePercentage;

        // In the order the enum declares its members, the same order both hosts show them in.
        List<string> zones = [];
        if (RequireDlz)
            zones.Add(nameof(CryptoZoneSource.Dlz));
        if (RequireFvg)
            zones.Add(nameof(CryptoZoneSource.Fvg));
        if (RequireSmc)
            zones.Add(nameof(CryptoZoneSource.Smc));
        settings.RequireZone = zones;
        settings.ZoneTolerancePercentage = ZoneTolerancePercentage;
    }
}
