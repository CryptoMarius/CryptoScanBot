using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

using System.Collections.ObjectModel;

namespace CryptoScanner.Settings.ViewModels;

public partial class IntervalItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isEnabled;

    public IntervalItem(string name, bool isEnabled = false)
    {
        _name = name;
        _isEnabled = isEnabled;
    }
}

public partial class IntervalsViewModel : ObservableObject
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

    public IntervalsViewModel()
    {
        InitializeIntervals();
    }

    private void InitializeIntervals()
    {
        foreach (var interval in GlobalData.IntervalListPeriod.Values)
        {
            if (interval.IntervalPeriod < CryptoIntervalPeriod.interval1h)
                MinuteIntervals.Add(new IntervalItem(interval.Name, false));
            else if (interval.IntervalPeriod < CryptoIntervalPeriod.interval1d)
                HourIntervals.Add(new IntervalItem(interval.Name, false));
            else
                DayIntervals.Add(new IntervalItem(interval.Name, false));
        }
    }

    ///// <summary>
    ///// Get all enabled interval names
    ///// </summary>
    //public List<string> GetEnabledIntervals()
    //{
    //    var enabled = new List<string>();
        
    //    foreach (var interval in MinuteIntervals.Where(x => x.IsEnabled))
    //        enabled.Add(interval.Name);
        
    //    foreach (var interval in HourIntervals.Where(x => x.IsEnabled))
    //        enabled.Add(interval.Name);
        
    //    foreach (var interval in DayIntervals.Where(x => x.IsEnabled))
    //        enabled.Add(interval.Name);
        
    //    return enabled;
    //}

    //public void SetEnabledIntervals(IEnumerable<string> intervalNames)
    //{
    //    var nameSet = new HashSet<string>(intervalNames);

    //    foreach (var interval in MinuteIntervals)
    //        interval.IsEnabled = nameSet.Contains(interval.Name);

    //    foreach (var interval in HourIntervals)
    //        interval.IsEnabled = nameSet.Contains(interval.Name);

    //    foreach (var interval in DayIntervals)
    //        interval.IsEnabled = nameSet.Contains(interval.Name);
    //}
}
