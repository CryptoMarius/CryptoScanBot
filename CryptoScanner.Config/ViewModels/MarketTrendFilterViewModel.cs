using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

public partial class MarketTrendFilterViewModel : ObservableObject
{
    [ObservableProperty]
    private BarometerFilterRangeViewModel _trendRange;

    [ObservableProperty]
    private bool _log = true;

    public MarketTrendFilterViewModel()
    {
        _trendRange = new BarometerFilterRangeViewModel
        {
            Caption = "Trend%",
            MinValue = -100,
            MaxValue = 100,
            IsActive = false
        };
    }

    public void LoadConfig(SettingsTextualMarketTrend settings)
    {
        Log = settings.Log;

        if (settings.List.Any())
        {
            TrendRange.IsActive = true;
            TrendRange.MinValue = settings.List[0].minValue;
            TrendRange.MaxValue = settings.List[0].maxValue;
        }
        else
        {
            TrendRange.IsActive = false;
            TrendRange.MinValue = -100;
            TrendRange.MaxValue = 100;
        }
    }

    public void SaveConfig(SettingsTextualMarketTrend settings)
    {
        settings.List.Clear();
        
        if (TrendRange.IsActive)
        {
            // Ensure min < max
            if (TrendRange.MinValue > TrendRange.MaxValue)
                settings.List.Add((TrendRange.MaxValue, TrendRange.MinValue));
            else
                settings.List.Add((TrendRange.MinValue, TrendRange.MaxValue));
        }

        settings.Log = Log;
    }
}
