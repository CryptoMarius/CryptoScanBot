using CommunityToolkit.Mvvm.ComponentModel;

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

    public IntervalViewModel()
    {
    }


    public void LoadConfig(List<string> intervalList, bool showHigherIntervalsOnly = false)
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

            bool isEnabled = true;
            if (showHigherIntervalsOnly)
                isEnabled = interval.IntervalPeriod >= CryptoIntervalPeriod.interval1h;

            var item = new IntervalItem(interval.Name, intervalList.Contains(interval.Name), isEnabled);
            target.Add(item);
        }
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
}
