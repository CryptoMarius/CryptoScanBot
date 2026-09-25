using System;

using CommunityToolkit.Mvvm.ComponentModel;

namespace CryptoScanner.Analyzers.Mac.Config;

public partial class StrategyMacSettingsViewModel : ObservableObject
{
    // The four markers the strategy draws, in the order they appear on screen. Only the first is on:
    // it is the one the indicator itself calls an entry.
    [ObservableProperty]
    private bool _entryOnOpenMarker = true;

    [ObservableProperty]
    private bool _entryOnCrossMarker = false;

    [ObservableProperty]
    private bool _entryOnCloseMarker = false;

    [ObservableProperty]
    private bool _entryOnBreakMarker = false;

    [ObservableProperty]
    private int _breakoutEntriesPerRun = 3;

    /// <summary>The three speeds, plus Custom for the four numbers.</summary>
    public static MacSpeed[] Speeds { get; } = Enum.GetValues<MacSpeed>();

    [ObservableProperty]
    private MacSpeed _speed = MacSpeed.Standard;

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
    private bool _useRsiLevels = false;

    [ObservableProperty]
    private int _rsiLevelLength = 14;

    [ObservableProperty]
    private decimal _rsiLevelSupportCross = 35m;

    [ObservableProperty]
    private decimal _rsiLevelResistanceCross = 65m;

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
    private bool _exitOnSecondLineCross = false;

    [ObservableProperty]
    private bool _exitOnCloudFlip = false;

    [ObservableProperty]
    private int _exitConfirmationCandles = 0;


    public void LoadConfig(MacSettings settings)
    {
        EntryOnBreakMarker = settings.EntryOnBreakMarker;
        BreakoutEntriesPerRun = settings.BreakoutEntriesPerRun;
        EntryOnCloseMarker = settings.EntryOnCloseMarker;
        EntryOnOpenMarker = settings.EntryOnOpenMarker;
        EntryOnCrossMarker = settings.EntryOnCrossMarker;
        Speed = settings.Speed;
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
        RsiLevelLength = settings.RsiLevelLength;
        RsiLevelSupportCross = settings.RsiLevelSupportCross;
        RsiLevelResistanceCross = settings.RsiLevelResistanceCross;
        UseRsiFilter = settings.UseRsiFilter;
        RsiLongMinimum = settings.RsiLongMinimum;
        RsiShortMaximum = settings.RsiShortMaximum;
        UseVolumeFilter = settings.UseVolumeFilter;
        VolumeMultiplier = settings.VolumeMultiplier;
        VolumeAverageCandles = settings.VolumeAverageCandles;
        ExitOnSecondLineCross = settings.ExitOnSecondLineCross;
        ExitOnCloudFlip = settings.ExitOnCloudFlip;
        ExitConfirmationCandles = settings.ExitConfirmationCandles;
    }

    public void SaveConfig(MacSettings settings)
    {
        settings.EntryOnBreakMarker = EntryOnBreakMarker;
        settings.BreakoutEntriesPerRun = BreakoutEntriesPerRun;
        settings.EntryOnCloseMarker = EntryOnCloseMarker;
        settings.EntryOnOpenMarker = EntryOnOpenMarker;
        settings.EntryOnCrossMarker = EntryOnCrossMarker;
        settings.Speed = Speed;
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
        settings.RsiLevelLength = RsiLevelLength;
        settings.RsiLevelSupportCross = RsiLevelSupportCross;
        settings.RsiLevelResistanceCross = RsiLevelResistanceCross;
        settings.UseRsiFilter = UseRsiFilter;
        settings.RsiLongMinimum = RsiLongMinimum;
        settings.RsiShortMaximum = RsiShortMaximum;
        settings.UseVolumeFilter = UseVolumeFilter;
        settings.VolumeMultiplier = VolumeMultiplier;
        settings.VolumeAverageCandles = VolumeAverageCandles;
        settings.ExitOnSecondLineCross = ExitOnSecondLineCross;
        settings.ExitOnCloudFlip = ExitOnCloudFlip;
        settings.ExitConfirmationCandles = ExitConfirmationCandles;
    }
}
