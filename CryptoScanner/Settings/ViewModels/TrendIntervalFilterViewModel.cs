using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Settings.ViewModels;

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
