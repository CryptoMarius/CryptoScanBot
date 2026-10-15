using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

/// <summary>
/// The market state filter of one side: only step in while the market state of the quote, a score
/// from -100 to +100, lies in the band. See Barometer.MarketState for how the score is measured.
/// </summary>
public partial class MarketStateFilterViewModel : ObservableObject
{
    [ObservableProperty]
    private BarometerFilterRangeViewModel _scoreRange;

    [ObservableProperty]
    private string _interval = "4h";

    [ObservableProperty]
    private bool _log = false;

    [ObservableProperty]
    private string _header = "Market state filter (step in between)";

    public List<string> Intervals { get; } = ["1h", "2h", "3h", "4h"];

    public MarketStateFilterViewModel()
    {
        _scoreRange = new BarometerFilterRangeViewModel
        {
            Caption = "Score",
            MinValue = -100,
            MaxValue = 100,
            IsActive = false
        };
    }

    public void LoadConfig(SettingsTextualMarketState settings)
    {
        ScoreRange.IsActive = settings.Active;
        ScoreRange.MinValue = settings.Minimum;
        ScoreRange.MaxValue = settings.Maximum;
        Interval = Intervals.Contains(settings.Interval) ? settings.Interval : "4h";
        Log = settings.Log;
    }

    public void SaveConfig(SettingsTextualMarketState settings)
    {
        settings.Active = ScoreRange.IsActive;
        settings.Minimum = Math.Min(ScoreRange.MinValue, ScoreRange.MaxValue);
        settings.Maximum = Math.Max(ScoreRange.MinValue, ScoreRange.MaxValue);
        settings.Interval = Interval;
        settings.Log = Log;
    }
}
