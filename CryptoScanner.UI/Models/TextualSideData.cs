using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.UI.Models;

/// <summary>
/// Editable snapshot of a <see cref="SettingsTextual"/> block (strategies, intervals,
/// barometer and trend filters). Shared by the analyzer long/short tabs and the
/// trader long/short tabs, which edit the exact same structure.
/// </summary>
public class TextualSideData
{
    public List<TextualCheckItem> Strategies { get; set; } = [];
    public List<TextualCheckItem> Intervals { get; set; } = [];
    public List<TextualBarometerRange> BarometerRanges { get; set; } = [];
    public bool BarometerConsensusActive { get; set; }
    public int BarometerMinConsensus { get; set; }
    public bool BarometerBreadthActive { get; set; }
    public decimal BarometerBreadthMinimum { get; set; }
    public decimal BarometerBreadthMaximum { get; set; } = 100m;
    public bool BarometerLog { get; set; }
    public List<TextualCheckItem> TrendIntervals { get; set; } = [];
    public bool TrendIntervalLog { get; set; }
    // The trend against the trade direction instead of with it (open point 51).
    public bool TrendIntervalInverted { get; set; }
    public bool SymbolTrendActive { get; set; }
    public decimal SymbolTrendMin { get; set; } = -100m;
    public decimal SymbolTrendMax { get; set; } = 100m;
    public bool SymbolTrendLog { get; set; }
    public bool SymbolTrendSecondaryActive { get; set; }
    public decimal SymbolTrendSecondaryMin { get; set; } = -100m;
    public decimal SymbolTrendSecondaryMax { get; set; } = 100m;
    public bool SymbolTrendSecondaryLog { get; set; }
    // The market RSI band in which a signal is skipped (off by default)
    public bool MarketRsiActive { get; set; }
    public decimal MarketRsiMin { get; set; } = 45m;
    public decimal MarketRsiMax { get; set; } = 55m;
    public bool MarketRsiLog { get; set; }
    // The market state band (-100..+100) in which this side steps in (off by default)
    public bool MarketStateActive { get; set; }
    public decimal MarketStateMin { get; set; } = -100m;
    public decimal MarketStateMax { get; set; } = 100m;
    public string MarketStateInterval { get; set; } = "4h";
    public bool MarketStateLog { get; set; }
    // Trader only: close a position once the market moved this far against it (off by default)
    public bool MarketExitActive { get; set; }
    public decimal MarketExitPercentage { get; set; } = 3m;
    public bool MarketExitUseBtc { get; set; }
    // Trader only: at most this many new positions per hour
    public bool EntryLimitActive { get; set; }
    public int EntryLimitMaxPerHour { get; set; } = 10;
    // Entry filters from the market-regime study (off by default)
    public bool BtcMoveActive { get; set; }
    public int BtcMoveHours { get; set; } = 24;
    public decimal BtcMoveMin { get; set; } = -100m;
    public decimal BtcMoveMax { get; set; } = 100m;
    public bool WeekdaysActive { get; set; }
    public bool[] WeekdaysAllowed { get; set; } = [true, true, true, true, true, true, true]; // 0 = Sunday
    public bool MarketRsiCrossActive { get; set; }
    public decimal MarketRsiCrossLevel { get; set; } = 35m;
    public int MarketRsiCrossHours { get; set; } = 12;
    public bool HigherIntervalActive { get; set; }
    public string HigherInterval { get; set; } = "1h";
    public decimal HigherIntervalRsiMin { get; set; }
    public decimal HigherIntervalRsiMax { get; set; } = 100m;
    public string HigherIntervalEma200 { get; set; } = "any";

    private static readonly string[] BarometerIntervals = ["15m", "30m", "1h", "4h", "1d"];

    public static TextualSideData LoadFrom(SettingsTextual textual)
    {
        var side = new TextualSideData();

        // Sort alphabetically by name so the UI lists e.g. sbm1/sbm2/sbm3, stobb/stobb.dlz/...
        // in a predictable order independent of the registration order in RegisterAlgorithms
        // (which is grouped by topic, not name) — same as the Avalonia StrategyViewModel.
        foreach (var algo in RegisterAlgorithms.AlgorithmDefinitionList.Values
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
        {
            side.Strategies.Add(new TextualCheckItem
            {
                Name = algo.Name,
                IsEnabled = textual.Strategy.Contains(algo.Name),
            });
        }

        foreach (var interval in GlobalData.IntervalList)
        {
            side.Intervals.Add(new TextualCheckItem
            {
                Name = interval.Name,
                IsEnabled = textual.Interval.Contains(interval.Name),
            });
        }

        foreach (var name in BarometerIntervals)
        {
            bool hasEntry = textual.Barometer.List.TryGetValue(name, out var range);
            side.BarometerRanges.Add(new TextualBarometerRange
            {
                Name = name,
                IsActive = hasEntry,
                Min = hasEntry ? range.minValue : -999m,
                Max = hasEntry ? range.maxValue : 999m,
            });
        }
        side.BarometerConsensusActive = textual.Barometer.ConsensusActive;
        side.BarometerMinConsensus = textual.Barometer.MinConsensus;
        side.BarometerBreadthActive = textual.Barometer.BreadthActive;
        side.BarometerBreadthMinimum = textual.Barometer.BreadthMinimum;
        side.BarometerBreadthMaximum = textual.Barometer.BreadthMaximum;
        side.BarometerLog = textual.Barometer.Log;

        foreach (var interval in GlobalData.IntervalList)
        {
            side.TrendIntervals.Add(new TextualCheckItem
            {
                Name = interval.Name,
                IsEnabled = textual.IntervalTrend.List.Contains(interval.Name),
            });
        }
        side.TrendIntervalLog = textual.IntervalTrend.Log;
        side.TrendIntervalInverted = textual.IntervalTrend.Inverted;

        if (textual.SymbolTrend.List.Count > 0)
        {
            side.SymbolTrendActive = true;
            side.SymbolTrendMin = textual.SymbolTrend.List[0].minValue;
            side.SymbolTrendMax = textual.SymbolTrend.List[0].maxValue;
        }
        side.SymbolTrendLog = textual.SymbolTrend.Log;

        if (textual.SymbolTrendSecondary.List.Count > 0)
        {
            side.SymbolTrendSecondaryActive = true;
            side.SymbolTrendSecondaryMin = textual.SymbolTrendSecondary.List[0].minValue;
            side.SymbolTrendSecondaryMax = textual.SymbolTrendSecondary.List[0].maxValue;
        }
        side.SymbolTrendSecondaryLog = textual.SymbolTrendSecondary.Log;

        side.MarketRsiActive = textual.MarketRsi.Active;
        side.MarketRsiMin = textual.MarketRsi.Minimum;
        side.MarketRsiMax = textual.MarketRsi.Maximum;
        side.MarketRsiLog = textual.MarketRsi.Log;

        side.MarketStateActive = textual.MarketState.Active;
        side.MarketStateMin = textual.MarketState.Minimum;
        side.MarketStateMax = textual.MarketState.Maximum;
        side.MarketStateInterval = textual.MarketState.Interval;
        side.MarketStateLog = textual.MarketState.Log;

        side.MarketExitActive = textual.MarketExit.Active;
        side.MarketExitPercentage = textual.MarketExit.Percentage;
        side.MarketExitUseBtc = textual.MarketExit.UseBtc;
        side.EntryLimitActive = textual.EntryLimit.Active;
        side.EntryLimitMaxPerHour = textual.EntryLimit.MaxPerHour;

        side.BtcMoveActive = textual.BtcMove.Active;
        side.BtcMoveHours = textual.BtcMove.Hours;
        side.BtcMoveMin = textual.BtcMove.Minimum;
        side.BtcMoveMax = textual.BtcMove.Maximum;
        side.WeekdaysActive = textual.Weekdays.Active;
        for (int day = 0; day < 7; day++)
            side.WeekdaysAllowed[day] = textual.Weekdays.Days.Contains(day);
        side.MarketRsiCrossActive = textual.MarketRsiCross.Active;
        side.MarketRsiCrossLevel = textual.MarketRsiCross.Level;
        side.MarketRsiCrossHours = textual.MarketRsiCross.Hours;
        side.HigherIntervalActive = textual.HigherInterval.Active;
        side.HigherInterval = textual.HigherInterval.Interval;
        side.HigherIntervalRsiMin = textual.HigherInterval.RsiMinimum;
        side.HigherIntervalRsiMax = textual.HigherInterval.RsiMaximum;
        side.HigherIntervalEma200 = textual.HigherInterval.Ema200 > 0 ? "above" : textual.HigherInterval.Ema200 < 0 ? "below" : "any";

        return side;
    }

    public void SaveTo(SettingsTextual textual)
    {
        textual.Strategy = Strategies
            .Where(s => s.IsEnabled)
            .Select(s => s.Name)
            .ToList();

        textual.Interval = Intervals
            .Where(i => i.IsEnabled)
            .Select(i => i.Name)
            .ToList();

        textual.Barometer.List.Clear();
        foreach (var baro in BarometerRanges)
        {
            // Ensure min < max, as in the Avalonia view model (BarometerFilterViewModel.SaveInterval)
            if (baro.IsActive)
                textual.Barometer.List[baro.Name] = (Math.Min(baro.Min, baro.Max), Math.Max(baro.Min, baro.Max));
        }
        textual.Barometer.ConsensusActive = BarometerConsensusActive;
        textual.Barometer.MinConsensus = BarometerMinConsensus;
        textual.Barometer.BreadthActive = BarometerBreadthActive;
        // Ensure min < max, as in the Avalonia view model
        textual.Barometer.BreadthMinimum = Math.Min(BarometerBreadthMinimum, BarometerBreadthMaximum);
        textual.Barometer.BreadthMaximum = Math.Max(BarometerBreadthMinimum, BarometerBreadthMaximum);
        textual.Barometer.Log = BarometerLog;

        textual.IntervalTrend.List = TrendIntervals
            .Where(t => t.IsEnabled)
            .Select(t => t.Name)
            .ToList();
        textual.IntervalTrend.Log = TrendIntervalLog;
        textual.IntervalTrend.Inverted = TrendIntervalInverted;

        textual.SymbolTrend.List.Clear();
        if (SymbolTrendActive)
        {
            textual.SymbolTrend.List.Add((
                Math.Min(SymbolTrendMin, SymbolTrendMax),
                Math.Max(SymbolTrendMin, SymbolTrendMax)));
        }
        textual.SymbolTrend.Log = SymbolTrendLog;

        textual.SymbolTrendSecondary.List.Clear();
        if (SymbolTrendSecondaryActive)
        {
            textual.SymbolTrendSecondary.List.Add((
                Math.Min(SymbolTrendSecondaryMin, SymbolTrendSecondaryMax),
                Math.Max(SymbolTrendSecondaryMin, SymbolTrendSecondaryMax)));
        }
        textual.SymbolTrendSecondary.Log = SymbolTrendSecondaryLog;

        textual.MarketRsi.Active = MarketRsiActive;
        textual.MarketRsi.Minimum = Math.Min(MarketRsiMin, MarketRsiMax);
        textual.MarketRsi.Maximum = Math.Max(MarketRsiMin, MarketRsiMax);
        textual.MarketRsi.Log = MarketRsiLog;

        textual.MarketState.Active = MarketStateActive;
        textual.MarketState.Minimum = Math.Min(MarketStateMin, MarketStateMax);
        textual.MarketState.Maximum = Math.Max(MarketStateMin, MarketStateMax);
        textual.MarketState.Interval = MarketStateInterval;
        textual.MarketState.Log = MarketStateLog;

        textual.MarketExit.Active = MarketExitActive;
        textual.MarketExit.Percentage = Math.Max(0m, MarketExitPercentage);
        textual.MarketExit.UseBtc = MarketExitUseBtc;
        textual.EntryLimit.Active = EntryLimitActive;
        textual.EntryLimit.MaxPerHour = Math.Max(1, EntryLimitMaxPerHour);

        textual.BtcMove.Active = BtcMoveActive;
        textual.BtcMove.Hours = Math.Max(1, BtcMoveHours);
        textual.BtcMove.Minimum = Math.Min(BtcMoveMin, BtcMoveMax);
        textual.BtcMove.Maximum = Math.Max(BtcMoveMin, BtcMoveMax);
        textual.Weekdays.Active = WeekdaysActive;
        textual.Weekdays.Days = [.. Enumerable.Range(0, 7).Where(day => WeekdaysAllowed[day])];
        textual.MarketRsiCross.Active = MarketRsiCrossActive;
        textual.MarketRsiCross.Level = MarketRsiCrossLevel;
        textual.MarketRsiCross.Hours = Math.Max(1, MarketRsiCrossHours);
        textual.HigherInterval.Active = HigherIntervalActive;
        textual.HigherInterval.Interval = HigherInterval;
        textual.HigherInterval.RsiMinimum = Math.Min(HigherIntervalRsiMin, HigherIntervalRsiMax);
        textual.HigherInterval.RsiMaximum = Math.Max(HigherIntervalRsiMin, HigherIntervalRsiMax);
        textual.HigherInterval.Ema200 = HigherIntervalEma200 == "above" ? 1 : HigherIntervalEma200 == "below" ? -1 : 0;
    }

    public static void SelectAll(List<TextualCheckItem> items)
    {
        foreach (var item in items)
            item.IsEnabled = true;
    }

    public static void SelectNone(List<TextualCheckItem> items)
    {
        foreach (var item in items)
            item.IsEnabled = false;
    }

    public static void CopyChecksFrom(List<TextualCheckItem> source, List<TextualCheckItem> target)
    {
        foreach (var targetItem in target)
        {
            var sourceItem = source.FirstOrDefault(s => s.Name == targetItem.Name);
            if (sourceItem != null)
                targetItem.IsEnabled = sourceItem.IsEnabled;
        }
    }
}

public class TextualCheckItem
{
    public string Name { get; set; } = "";
    public bool IsEnabled { get; set; }
}

public class TextualBarometerRange
{
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public decimal Min { get; set; }
    public decimal Max { get; set; }
}
