using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Settings.ViewModels;

public partial class BarometerFilterViewModel : ObservableObject
{
    [ObservableProperty]
    private BarometerFilterRangeViewModel _interval15m;

    [ObservableProperty]
    private BarometerFilterRangeViewModel _interval30m;

    [ObservableProperty]
    private BarometerFilterRangeViewModel _interval1h;

    [ObservableProperty]
    private BarometerFilterRangeViewModel _interval4h;

    [ObservableProperty]
    private BarometerFilterRangeViewModel _interval1d;

    [ObservableProperty]
    private bool _log = true;

    public BarometerFilterViewModel()
    {
        _interval15m = new BarometerFilterRangeViewModel { Caption = "15m", MinValue = -999, MaxValue = 999, IsActive = false };
        _interval30m = new BarometerFilterRangeViewModel { Caption = "30m", MinValue = -999, MaxValue = 999, IsActive = false };
        _interval1h = new BarometerFilterRangeViewModel { Caption = "1h", MinValue = -999, MaxValue = 999, IsActive = false };
        _interval4h = new BarometerFilterRangeViewModel { Caption = "4h", MinValue = -999, MaxValue = 999, IsActive = false };
        _interval1d = new BarometerFilterRangeViewModel { Caption = "1d", MinValue = -999, MaxValue = 999, IsActive = false };
    }

    public void LoadConfig(SettingsTextualBarometer settings)
    {
        Log = settings.Log;

        LoadInterval("15m", Interval15m, settings.List);
        LoadInterval("30m", Interval30m, settings.List);
        LoadInterval("1h", Interval1h, settings.List);
        LoadInterval("4h", Interval4h, settings.List);
        LoadInterval("1d", Interval1d, settings.List);
    }

    private static void LoadInterval(string key, BarometerFilterRangeViewModel interval, 
        Dictionary<string, (decimal minValue, decimal maxValue)> list)
    {
        if (list.TryGetValue(key, out var value))
        {
            interval.IsActive = true;
            interval.MinValue = value.minValue;
            interval.MaxValue = value.maxValue;
        }
        else
        {
            interval.IsActive = false;
            interval.MinValue = -999;
            interval.MaxValue = 999;
        }
    }

    public void SaveConfig(SettingsTextualBarometer settings)
    {
        settings.List.Clear();

        SaveInterval("15m", Interval15m, settings.List);
        SaveInterval("30m", Interval30m, settings.List);
        SaveInterval("1h", Interval1h, settings.List);
        SaveInterval("4h", Interval4h, settings.List);
        SaveInterval("1d", Interval1d, settings.List);

        settings.Log = Log;
    }

    private static void SaveInterval(string key, BarometerFilterRangeViewModel interval, 
        Dictionary<string, (decimal minValue, decimal maxValue)> list)
    {
        if (interval.IsActive)
        {
            // Ensure min < max
            if (interval.MinValue > interval.MaxValue)
                list.Add(key, (interval.MaxValue, interval.MinValue));
            else
                list.Add(key, (interval.MinValue, interval.MaxValue));
        }
    }
}
