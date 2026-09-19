using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

using System.Collections.ObjectModel;

namespace CryptoScanner.Config.ViewModels;

public partial class IntervalItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isChecked;

    [ObservableProperty]
    private bool _isEnabled;

    public IntervalItem(string name, bool isChecked = false, bool isEnabled = true)
    {
        _name = name;
        _isChecked = isChecked;
        _isEnabled = isEnabled;
    }
}

public partial class IntervalViewModel : ObservableObject
{
    // Column 1: Minutes
    [ObservableProperty]
    private ObservableCollection<IntervalItem> _minuteIntervals = [];

    // Column 2: Hours
    [ObservableProperty]
    private ObservableCollection<IntervalItem> _hourIntervals = [];

    // Column 3: Days/Weeks
    [ObservableProperty]
    private ObservableCollection<IntervalItem> _dayIntervals = [];

    // Same-tab counterparts (Long ↔ Short)
    public IntervalViewModel? LongCounterpart { get; set; }
    public IntervalViewModel? ShortCounterpart { get; set; }

    // Cross-tab counterparts (Analyzer ↔ Trader)
    public IntervalViewModel? CrossTabLongCounterpart { get; set; }
    public IntervalViewModel? CrossTabShortCounterpart { get; set; }

    [ObservableProperty]
    private string _crossTabLabel = "Trading";

    /// <summary>
    /// Shows the "Use custom intervals" switch above the list. Only the strategy tabs turn this on;
    /// the Analyzer and Trader interval boxes have no fallback to follow, so they always apply.
    /// </summary>
    [ObservableProperty]
    private bool _showUseCustom;

    /// <summary>
    /// Off means the strategy runs on the intervals ticked for its side, which the settings file
    /// expresses as an empty IntervalList. Same arrangement as UseCustomEntryConditions.
    /// </summary>
    [ObservableProperty]
    private bool _useCustom;

    /// <summary>The list itself is only editable while the switch above it is on.</summary>
    public bool IntervalsEnabled => !ShowUseCustom || UseCustom;

    partial void OnShowUseCustomChanged(bool value) => OnPropertyChanged(nameof(IntervalsEnabled));

    partial void OnUseCustomChanged(bool value) => OnPropertyChanged(nameof(IntervalsEnabled));

    public IntervalViewModel()
    {
    }


    public void LoadConfig(List<string> intervalList, CryptoIntervalPeriod showFromInterval = CryptoIntervalPeriod.interval1m)
    {
        DayIntervals.Clear();
        HourIntervals.Clear();
        MinuteIntervals.Clear();
        foreach (var interval in GlobalData.IntervalListPeriod.Values)
        {
            ObservableCollection<IntervalItem> target;
            if (interval.IntervalPeriod < CryptoIntervalPeriod.interval1h)
                target = MinuteIntervals;
            else if (interval.IntervalPeriod < CryptoIntervalPeriod.interval1d)
                target = HourIntervals;
            else
                target = DayIntervals;

            bool isEnabled = interval.IntervalPeriod >= showFromInterval;
            var item = new IntervalItem(interval.Name, intervalList.Contains(interval.Name), isEnabled);
            target.Add(item);
        }

        CopyFromLongCommand.NotifyCanExecuteChanged();
        CopyFromShortCommand.NotifyCanExecuteChanged();
        CopyFromCrossTabLongCommand.NotifyCanExecuteChanged();
        CopyFromCrossTabShortCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// The interval list of one strategy: the switch is shown, and it starts out on when the
    /// strategy names intervals of its own.
    /// </summary>
    public void LoadStrategyConfig(List<string> intervalList, CryptoIntervalPeriod showFromInterval = CryptoIntervalPeriod.interval1m)
    {
        ShowUseCustom = true;
        UseCustom = intervalList.Count > 0;
        LoadConfig(intervalList, showFromInterval);
    }

    /// <summary>
    /// Writes the interval list of one strategy. A switch that is off stores an empty list, which is
    /// what makes the strategy follow the intervals ticked for its side.
    /// </summary>
    public void SaveStrategyConfig(List<string> intervalList)
    {
        if (!UseCustom)
        {
            intervalList.Clear();
            return;
        }
        SaveConfig(intervalList);
    }

    public void SaveConfig(List<string> intervalList)
    {
        ObservableCollection<IntervalItem>[] source = [DayIntervals, HourIntervals, MinuteIntervals];

        intervalList.Clear();
        foreach (var item in source)
        {
            foreach (var interval in item)
            {
                if (interval.IsChecked)
                {
                    intervalList.Add(interval.Name);
                }
            }
        }
    }


    private IEnumerable<IntervalItem> AllIntervals =>
        MinuteIntervals.Concat(HourIntervals).Concat(DayIntervals);


    // ---- Select all / none ----

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in AllIntervals)
        {
            if (item.IsEnabled)
                item.IsChecked = true;
        }
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var item in AllIntervals)
            item.IsChecked = false;
    }


    // ---- Copy from sibling ----

    [RelayCommand(CanExecute = nameof(CanCopyFromLong))]
    private void CopyFromLong() => CopyFrom(LongCounterpart);
    private bool CanCopyFromLong() => LongCounterpart != null && !ReferenceEquals(LongCounterpart, this);

    [RelayCommand(CanExecute = nameof(CanCopyFromShort))]
    private void CopyFromShort() => CopyFrom(ShortCounterpart);
    private bool CanCopyFromShort() => ShortCounterpart != null && !ReferenceEquals(ShortCounterpart, this);


    // ---- Copy from cross-tab (Analyzer ↔ Trader) ----

    [RelayCommand(CanExecute = nameof(CanCopyFromCrossTabLong))]
    private void CopyFromCrossTabLong() => CopyFrom(CrossTabLongCounterpart);
    private bool CanCopyFromCrossTabLong() => CrossTabLongCounterpart != null;

    [RelayCommand(CanExecute = nameof(CanCopyFromCrossTabShort))]
    private void CopyFromCrossTabShort() => CopyFrom(CrossTabShortCounterpart);
    private bool CanCopyFromCrossTabShort() => CrossTabShortCounterpart != null;


    private void CopyFrom(IntervalViewModel? source)
    {
        if (source == null || ReferenceEquals(source, this))
            return;

        var checked_ = new HashSet<string>(
            source.AllIntervals.Where(i => i.IsChecked).Select(i => i.Name),
            StringComparer.OrdinalIgnoreCase);

        foreach (var item in AllIntervals)
        {
            if (item.IsEnabled)
                item.IsChecked = checked_.Contains(item.Name);
        }
    }
}
