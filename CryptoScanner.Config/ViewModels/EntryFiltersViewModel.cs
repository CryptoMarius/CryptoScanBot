using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Core.Settings;

namespace CryptoScanner.Config.ViewModels;

/// <summary>
/// The entry filters of one side that came out of the market-regime study: BTC move, weekdays,
/// market RSI cross and conditions on a higher interval. See Core Signal.EntryFilters.
/// </summary>
public partial class EntryFiltersViewModel : ObservableObject
{
    [ObservableProperty] private string _header = "Entry filters";

    // BTC move
    [ObservableProperty] private bool _btcMoveActive;
    [ObservableProperty] private int _btcMoveHours = 24;
    [ObservableProperty] private decimal _btcMoveMinimum = -100m;
    [ObservableProperty] private decimal _btcMoveMaximum = 100m;

    // Weekdays, UTC
    [ObservableProperty] private bool _weekdaysActive;
    [ObservableProperty] private bool _monday = true;
    [ObservableProperty] private bool _tuesday = true;
    [ObservableProperty] private bool _wednesday = true;
    [ObservableProperty] private bool _thursday = true;
    [ObservableProperty] private bool _friday = true;
    [ObservableProperty] private bool _saturday = true;
    [ObservableProperty] private bool _sunday = true;

    // Market RSI cross
    [ObservableProperty] private bool _marketRsiCrossActive;
    [ObservableProperty] private decimal _marketRsiCrossLevel = 35m;
    [ObservableProperty] private int _marketRsiCrossHours = 12;

    // Higher interval
    [ObservableProperty] private bool _higherIntervalActive;
    [ObservableProperty] private string _higherInterval = "1h";
    [ObservableProperty] private decimal _higherIntervalRsiMinimum;
    [ObservableProperty] private decimal _higherIntervalRsiMaximum = 100m;
    [ObservableProperty] private string _higherIntervalEma200 = "any";

    public List<string> Intervals { get; } = ["30m", "1h", "2h", "4h", "1d"];
    public List<string> Ema200Choices { get; } = ["any", "above", "below"];

    public void LoadConfig(SettingsTextual settings)
    {
        BtcMoveActive = settings.BtcMove.Active;
        BtcMoveHours = settings.BtcMove.Hours;
        BtcMoveMinimum = settings.BtcMove.Minimum;
        BtcMoveMaximum = settings.BtcMove.Maximum;

        WeekdaysActive = settings.Weekdays.Active;
        Sunday = settings.Weekdays.Days.Contains(0);
        Monday = settings.Weekdays.Days.Contains(1);
        Tuesday = settings.Weekdays.Days.Contains(2);
        Wednesday = settings.Weekdays.Days.Contains(3);
        Thursday = settings.Weekdays.Days.Contains(4);
        Friday = settings.Weekdays.Days.Contains(5);
        Saturday = settings.Weekdays.Days.Contains(6);

        MarketRsiCrossActive = settings.MarketRsiCross.Active;
        MarketRsiCrossLevel = settings.MarketRsiCross.Level;
        MarketRsiCrossHours = settings.MarketRsiCross.Hours;

        HigherIntervalActive = settings.HigherInterval.Active;
        HigherInterval = Intervals.Contains(settings.HigherInterval.Interval) ? settings.HigherInterval.Interval : "1h";
        HigherIntervalRsiMinimum = settings.HigherInterval.RsiMinimum;
        HigherIntervalRsiMaximum = settings.HigherInterval.RsiMaximum;
        HigherIntervalEma200 = settings.HigherInterval.Ema200 > 0 ? "above" : settings.HigherInterval.Ema200 < 0 ? "below" : "any";
    }

    public void SaveConfig(SettingsTextual settings)
    {
        settings.BtcMove.Active = BtcMoveActive;
        settings.BtcMove.Hours = Math.Max(1, BtcMoveHours);
        settings.BtcMove.Minimum = Math.Min(BtcMoveMinimum, BtcMoveMaximum);
        settings.BtcMove.Maximum = Math.Max(BtcMoveMinimum, BtcMoveMaximum);

        settings.Weekdays.Active = WeekdaysActive;
        List<int> days = [];
        if (Sunday) days.Add(0);
        if (Monday) days.Add(1);
        if (Tuesday) days.Add(2);
        if (Wednesday) days.Add(3);
        if (Thursday) days.Add(4);
        if (Friday) days.Add(5);
        if (Saturday) days.Add(6);
        settings.Weekdays.Days = days;

        settings.MarketRsiCross.Active = MarketRsiCrossActive;
        settings.MarketRsiCross.Level = MarketRsiCrossLevel;
        settings.MarketRsiCross.Hours = Math.Max(1, MarketRsiCrossHours);

        settings.HigherInterval.Active = HigherIntervalActive;
        settings.HigherInterval.Interval = HigherInterval;
        settings.HigherInterval.RsiMinimum = Math.Min(HigherIntervalRsiMinimum, HigherIntervalRsiMaximum);
        settings.HigherInterval.RsiMaximum = Math.Max(HigherIntervalRsiMinimum, HigherIntervalRsiMaximum);
        settings.HigherInterval.Ema200 = HigherIntervalEma200 == "above" ? 1 : HigherIntervalEma200 == "below" ? -1 : 0;
    }
}
