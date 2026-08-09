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
    public bool BarometerLog { get; set; }
    public List<TextualCheckItem> TrendIntervals { get; set; } = [];
    public bool TrendIntervalLog { get; set; }
    public bool MarketTrendActive { get; set; }
    public decimal MarketTrendMin { get; set; } = -100m;
    public decimal MarketTrendMax { get; set; } = 100m;
    public bool MarketTrendLog { get; set; }
    public bool MarketTrendSecondaryActive { get; set; }
    public decimal MarketTrendSecondaryMin { get; set; } = -100m;
    public decimal MarketTrendSecondaryMax { get; set; } = 100m;
    public bool MarketTrendSecondaryLog { get; set; }

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

        if (textual.MarketTrend.List.Count > 0)
        {
            side.MarketTrendActive = true;
            side.MarketTrendMin = textual.MarketTrend.List[0].minValue;
            side.MarketTrendMax = textual.MarketTrend.List[0].maxValue;
        }
        side.MarketTrendLog = textual.MarketTrend.Log;

        if (textual.MarketTrendSecondary.List.Count > 0)
        {
            side.MarketTrendSecondaryActive = true;
            side.MarketTrendSecondaryMin = textual.MarketTrendSecondary.List[0].minValue;
            side.MarketTrendSecondaryMax = textual.MarketTrendSecondary.List[0].maxValue;
        }
        side.MarketTrendSecondaryLog = textual.MarketTrendSecondary.Log;

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
            if (baro.IsActive)
                textual.Barometer.List[baro.Name] = (baro.Min, baro.Max);
        }
        textual.Barometer.ConsensusActive = BarometerConsensusActive;
        textual.Barometer.MinConsensus = BarometerMinConsensus;
        textual.Barometer.Log = BarometerLog;

        textual.IntervalTrend.List = TrendIntervals
            .Where(t => t.IsEnabled)
            .Select(t => t.Name)
            .ToList();
        textual.IntervalTrend.Log = TrendIntervalLog;

        textual.MarketTrend.List.Clear();
        if (MarketTrendActive)
        {
            textual.MarketTrend.List.Add((
                Math.Min(MarketTrendMin, MarketTrendMax),
                Math.Max(MarketTrendMin, MarketTrendMax)));
        }
        textual.MarketTrend.Log = MarketTrendLog;

        textual.MarketTrendSecondary.List.Clear();
        if (MarketTrendSecondaryActive)
        {
            textual.MarketTrendSecondary.List.Add((
                Math.Min(MarketTrendSecondaryMin, MarketTrendSecondaryMax),
                Math.Max(MarketTrendSecondaryMin, MarketTrendSecondaryMax)));
        }
        textual.MarketTrendSecondary.Log = MarketTrendSecondaryLog;
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
