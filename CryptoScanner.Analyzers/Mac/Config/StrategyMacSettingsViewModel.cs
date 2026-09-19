using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.Mac.Config;

public partial class StrategyMacSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _entryOnBreakout = true;

    [ObservableProperty]
    private bool _entryOnCloudCross = false;

    [ObservableProperty]
    private bool _entryOnSpringboard = false;

    [ObservableProperty]
    private int _fastEmaLength = 20;

    [ObservableProperty]
    private int _secondEmaLength = 40;

    [ObservableProperty]
    private int _mediumSmaLength = 50;

    [ObservableProperty]
    private int _slowSmaLength = 150;

    [ObservableProperty]
    private bool _requirePriceOutsideCloud = true;

    [ObservableProperty]
    private decimal _minimumCloudWidthPercentage = 0m;

    [ObservableProperty]
    private bool _requireCloudWidening = false;

    [ObservableProperty]
    private decimal _minimumSlowLineSlopePercentage = 0m;

    [ObservableProperty]
    private int _slowLineLookbackCandles = 10;

    [ObservableProperty]
    private int _pivotLeftCandles = 5;

    [ObservableProperty]
    private int _pivotRightCandles = 5;

    [ObservableProperty]
    private int _pivotMaximumAgeCandles = 0;

    [ObservableProperty]
    private decimal _breakoutBufferPercentage = 0m;

    [ObservableProperty]
    private bool _useRsiFilter = false;

    [ObservableProperty]
    private decimal _rsiLongMinimum = 50m;

    [ObservableProperty]
    private decimal _rsiShortMaximum = 50m;

    [ObservableProperty]
    private bool _useVolumeFilter = false;

    [ObservableProperty]
    private decimal _volumeMultiplier = 3m;

    [ObservableProperty]
    private int _volumeAverageCandles = 20;

    [ObservableProperty]
    private bool _exitOnCloudFlip = false;

    [ObservableProperty]
    private int _exitConfirmationCandles = 0;


    public void LoadConfig(MacSettings settings)
    {
        EntryOnBreakout = settings.EntryOnBreakout;
        EntryOnCloudCross = settings.EntryOnCloudCross;
        EntryOnSpringboard = settings.EntryOnSpringboard;
        FastEmaLength = settings.FastEmaLength;
        SecondEmaLength = settings.SecondEmaLength;
        MediumSmaLength = settings.MediumSmaLength;
        SlowSmaLength = settings.SlowSmaLength;
        RequirePriceOutsideCloud = settings.RequirePriceOutsideCloud;
        MinimumCloudWidthPercentage = settings.MinimumCloudWidthPercentage;
        RequireCloudWidening = settings.RequireCloudWidening;
        MinimumSlowLineSlopePercentage = settings.MinimumSlowLineSlopePercentage;
        SlowLineLookbackCandles = settings.SlowLineLookbackCandles;
        PivotLeftCandles = settings.PivotLeftCandles;
        PivotRightCandles = settings.PivotRightCandles;
        PivotMaximumAgeCandles = settings.PivotMaximumAgeCandles;
        BreakoutBufferPercentage = settings.BreakoutBufferPercentage;
        UseRsiFilter = settings.UseRsiFilter;
        RsiLongMinimum = settings.RsiLongMinimum;
        RsiShortMaximum = settings.RsiShortMaximum;
        UseVolumeFilter = settings.UseVolumeFilter;
        VolumeMultiplier = settings.VolumeMultiplier;
        VolumeAverageCandles = settings.VolumeAverageCandles;
        ExitOnCloudFlip = settings.ExitOnCloudFlip;
        ExitConfirmationCandles = settings.ExitConfirmationCandles;
    }

    public void SaveConfig(MacSettings settings)
    {
        settings.EntryOnBreakout = EntryOnBreakout;
        settings.EntryOnCloudCross = EntryOnCloudCross;
        settings.EntryOnSpringboard = EntryOnSpringboard;
        settings.FastEmaLength = FastEmaLength;
        settings.SecondEmaLength = SecondEmaLength;
        settings.MediumSmaLength = MediumSmaLength;
        settings.SlowSmaLength = SlowSmaLength;
        settings.RequirePriceOutsideCloud = RequirePriceOutsideCloud;
        settings.MinimumCloudWidthPercentage = MinimumCloudWidthPercentage;
        settings.RequireCloudWidening = RequireCloudWidening;
        settings.MinimumSlowLineSlopePercentage = MinimumSlowLineSlopePercentage;
        settings.SlowLineLookbackCandles = SlowLineLookbackCandles;
        settings.PivotLeftCandles = PivotLeftCandles;
        settings.PivotRightCandles = PivotRightCandles;
        settings.PivotMaximumAgeCandles = PivotMaximumAgeCandles;
        settings.BreakoutBufferPercentage = BreakoutBufferPercentage;
        settings.UseRsiFilter = UseRsiFilter;
        settings.RsiLongMinimum = RsiLongMinimum;
        settings.RsiShortMaximum = RsiShortMaximum;
        settings.UseVolumeFilter = UseVolumeFilter;
        settings.VolumeMultiplier = VolumeMultiplier;
        settings.VolumeAverageCandles = VolumeAverageCandles;
        settings.ExitOnCloudFlip = ExitOnCloudFlip;
        settings.ExitConfirmationCandles = ExitConfirmationCandles;
    }
}
