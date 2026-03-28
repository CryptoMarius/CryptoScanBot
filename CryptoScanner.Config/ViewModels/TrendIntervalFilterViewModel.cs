using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

using System.Collections.ObjectModel;

namespace CryptoScanner.Config.ViewModels;

public partial class IntervalCheckboxViewModel : ObservableObject
{
    [ObservableProperty]
    private string _intervalName = "";

    [ObservableProperty]
    private bool _isChecked = false;

    [ObservableProperty]
    private string _displayText = "";
}

public partial class TrendIntervalFilterViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<IntervalCheckboxViewModel> _intervals = [];

    // Split the flat list into two halves for a two-column top-to-bottom layout
    public IEnumerable<IntervalCheckboxViewModel> IntervalsColumn1 => Intervals.Take((Intervals.Count + 1) / 2);
    public IEnumerable<IntervalCheckboxViewModel> IntervalsColumn2 => Intervals.Skip((Intervals.Count + 1) / 2);


    public void LoadConfig(SettingsTextualIntervalTrend settings, CryptoTradeSide side)
    {
        string trendText = side == CryptoTradeSide.Long ? "bullish" : "bearish";

        Intervals.Clear();
        foreach (var interval in GlobalData.IntervalList)
        {
            Intervals.Add(new IntervalCheckboxViewModel
            {
                IntervalName = interval.Name,
                DisplayText = $"{interval.Name} interval={trendText}",
                IsChecked = settings.List.Contains(interval.Name),
            });
        }

        // Notify the two column views after the list is fully populated
        OnPropertyChanged(nameof(IntervalsColumn1));
        OnPropertyChanged(nameof(IntervalsColumn2));
    }

    public void SaveConfig(SettingsTextualIntervalTrend settings)
    {
        settings.List.Clear();
        foreach (var item in Intervals)
        {
            if (item.IsChecked)
                settings.List.Add(item.IntervalName);
        }
    }
}
