using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Config.ViewModels;
using CryptoScanner.Core.Enums;

namespace CryptoScanner.Analyzers.CandlePattern.Config;

public partial class StrategyCandlePatternSettingsViewModel : ObservableObject
{
    /// <summary>
    /// The shapes this strategy fires on. Shared with the entry conditions, which pick from the very
    /// same list for the shape an entry waits for.
    /// </summary>
    [ObservableProperty]
    private CandlePatternListViewModel _patternListViewModel;

    /// <summary>The thresholds those shapes are measured against, shared for the same reason.</summary>
    [ObservableProperty]
    private CandlePatternShapeViewModel _shapeViewModel;

    [ObservableProperty]
    private int _precedingCandles = 3;

    [ObservableProperty]
    private decimal _precedingPercentage = 0m;

    [ObservableProperty]
    private decimal _minSizeFactor = 0m;

    [ObservableProperty]
    private int _trendEmaLength = 0;

    [ObservableProperty]
    private bool _stopBeyondPattern = false;

    [ObservableProperty]
    private decimal _riskReward = 2m;

    // The zone sources the pattern candle has to sit in, as one checkbox each. Three fixed boxes
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

    public StrategyCandlePatternSettingsViewModel()
    {
        _patternListViewModel = new();
        _shapeViewModel = new();
    }


    public void LoadConfig(CandlePatternStrategySettings settings)
    {
        PatternListViewModel.LoadConfig(settings.Patterns);
        ShapeViewModel.LoadConfig(settings.Shape);

        PrecedingCandles = settings.PrecedingCandles;
        PrecedingPercentage = settings.PrecedingPercentage;

        MinSizeFactor = settings.MinSizeFactor;
        TrendEmaLength = settings.TrendEmaLength;
        StopBeyondPattern = settings.StopBeyondPattern;
        RiskReward = settings.RiskReward;

        // Case-insensitive, because the list can also be typed by hand in the settings file or in
        // the emulator queue - where it is written "dlz" rather than "Dlz".
        static bool Has(List<string> names, CryptoZoneSource source)
            => names.Exists(z => z.Equals(source.ToString(), StringComparison.OrdinalIgnoreCase));

        RequireDlz = Has(settings.RequireZone, CryptoZoneSource.Dlz);
        RequireFvg = Has(settings.RequireZone, CryptoZoneSource.Fvg);
        RequireSmc = Has(settings.RequireZone, CryptoZoneSource.Smc);
        ZoneTolerancePercentage = settings.ZoneTolerancePercentage;
    }

    public void SaveConfig(CandlePatternStrategySettings settings)
    {
        settings.Patterns = PatternListViewModel.SaveConfig();
        ShapeViewModel.SaveConfig(settings.Shape);

        settings.PrecedingCandles = PrecedingCandles;
        settings.PrecedingPercentage = PrecedingPercentage;

        settings.MinSizeFactor = MinSizeFactor;
        settings.TrendEmaLength = TrendEmaLength;
        settings.StopBeyondPattern = StopBeyondPattern;
        settings.RiskReward = RiskReward;

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
