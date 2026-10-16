using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

using System.Text.Json.Serialization;

namespace CryptoScanner.Core.Settings;

public enum MatchBlackAndWhiteList
{
    Empty,
    Present,
    NotPresent
}

// Compiled version of the SettingsTextual for signal (long/short) and trading (long/short)

[Serializable]
public class SettingsCompiled
{
    // The active intervals
    [JsonIgnore]
    public List<CryptoInterval> Interval { get; set; } = [];
    public SortedList<CryptoIntervalPeriod, bool> IntervalPeriod { get; set; } = [];


    // The active strategies
    public SortedList<string, bool> Strategy { get; set; } = [];


    // Interval trend + Value (bullisch, bearish)
    public Dictionary<CryptoIntervalPeriod, CryptoTrendIndicator> Trend { get; set; } = [];
    public bool TrendLog = false;

    // Primary market trend + Value (percentages)
    public List<(decimal minValue, decimal maxValue)> SymbolTrend { get; set; } = [];
    public bool SymbolTrendLog = false;

    // Secondary market trend + Value (percentages). Evaluated as an additional INTERSECT filter
    // (both Primary and Secondary ranges must contain the current trend value).
    public List<(decimal minValue, decimal maxValue)> SymbolTrendSecondary { get; set; } = [];
    public bool SymbolTrendSecondaryLog = false;

    // Market RSI band in which the signal is skipped (mirrors SettingsTextualMarketRsi)
    public bool MarketRsiActive = false;
    public decimal MarketRsiMinimum = 45m;
    public decimal MarketRsiMaximum = 55m;
    public bool MarketRsiLog = false;

    // Market state band in which this side may step in (mirrors SettingsTextualMarketState)
    public bool MarketStateActive = false;
    public decimal MarketStateMinimum = -100m;
    public decimal MarketStateMaximum = 100m;
    public CryptoIntervalPeriod MarketStateInterval = CryptoIntervalPeriod.interval4h;
    public bool MarketStateLog = false;

    // Close a position once the market moved this far against it (mirrors SettingsTextualMarketExit)
    public bool MarketExitActive = false;
    public decimal MarketExitPercentage = 3m;
    public bool MarketExitUseBtc = false;

    // Entry filters from the market-regime study (mirror the SettingsTextual classes of the same name)
    public bool BtcMoveActive = false;
    public int BtcMoveHours = 24;
    public decimal BtcMoveMinimum = -100m;
    public decimal BtcMoveMaximum = 100m;
    public bool WeekdaysActive = false;
    public HashSet<int> WeekdaysAllowed = [0, 1, 2, 3, 4, 5, 6];
    public bool MarketRsiCrossActive = false;
    public decimal MarketRsiCrossLevel = 35m;
    public int MarketRsiCrossHours = 12;
    public bool HigherIntervalActive = false;
    public string HigherIntervalName = "1h";
    public CryptoIntervalPeriod HigherIntervalPeriod = CryptoIntervalPeriod.interval1h;
    public decimal HigherIntervalRsiMinimum = 0m;
    public decimal HigherIntervalRsiMaximum = 100m;
    public int HigherIntervalEma200 = 0;
    public bool EntryLimitActive = false;
    public int EntryLimitMaxPerHour = 10;

    // Via interval + Value (ranged)
    // Minimale barometer om de meldingen te genereren
    public Dictionary<CryptoIntervalPeriod, (decimal minValue, decimal maxValue)> Barometer { get; set; } = [];
    public bool BarometerLog = false;
    // Whether the consensus check is enabled (mirrors SettingsTextualBarometer.ConsensusActive)
    public bool BarometerConsensusActive = false;
    // Minimum number of higher-timeframe barometers that must align with the signal direction (0 = disabled)
    public int BarometerMinConsensus = 0;
    // The market breadth condition (mirrors SettingsTextualBarometer.Breadth*)
    public bool BarometerBreadthActive = false;
    public decimal BarometerBreadthMinimum = 0m;
    public decimal BarometerBreadthMaximum = 100m;

    //// Relative volume filter
    //public SettingsCompiledVolume Volume = new();

    //// Adaptive feedback filter
    //public SettingsCompiledFeedback Feedback = new();


    // The black- and whitelist
    public SortedList<string, bool> BlackList { get; } = [];
    public SortedList<string, bool> WhiteList { get; } = [];



    public void IndexStrategyInternally(SettingsTextual settings, CryptoTradeSide side)
    {
        // Old setup
        Interval.Clear();
        IntervalPeriod.Clear();

        Barometer.Clear();
        Trend.Clear();

        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            // Interval
            if (settings.Interval.Contains(interval.Name))
            {
                Interval.Add(interval);
                IntervalPeriod.Add(interval.IntervalPeriod, true);
            }

            // Interval trend (up/down)
            if (settings.IntervalTrend.List.Contains(interval.Name))
            {
                // With the trend, or - Inverted - against it (open point 51).
                bool wantBullish = side == CryptoTradeSide.Long;
                if (settings.IntervalTrend.Inverted)
                    wantBullish = !wantBullish;
                Trend.Add(interval.IntervalPeriod, wantBullish ? CryptoTrendIndicator.Bullish : CryptoTrendIndicator.Bearish);
            }

            // Barometer (ranged)
            if (settings.Barometer.List.TryGetValue(interval.Name, out var value))
                Barometer.Add(interval.IntervalPeriod, value);
        }
        TrendLog = settings.IntervalTrend.Log;
        BarometerLog = settings.Barometer.Log;
        BarometerConsensusActive = settings.Barometer.ConsensusActive;
        BarometerMinConsensus = settings.Barometer.MinConsensus;
        BarometerBreadthActive = settings.Barometer.BreadthActive;
        BarometerBreadthMinimum = settings.Barometer.BreadthMinimum;
        BarometerBreadthMaximum = settings.Barometer.BreadthMaximum;

        //Volume.Active = settings.Volume.IsActive;
        //Volume.MinRelative = settings.Volume.MinRelVol;
        //Volume.MaxRelative = settings.Volume.MaxRelVol;
        //Volume.Lookback = settings.Volume.Lookback;
        //Volume.Log = settings.Volume.Log;

        //Feedback.Active = settings.Feedback.IsActive;
        //Feedback.MaxDays = settings.Feedback.MaxLookbackDays;
        //Feedback.MinSignals = settings.Feedback.MinSignals;
        //Feedback.BlockThreshold = settings.Feedback.BlockThresholdPercent;
        //Feedback.ReEnableHours = settings.Feedback.ReEnableHours;
        //Feedback.Log = settings.Feedback.Log;


        // Market trend% (min..max), er is maar 1 aanwezig
        SymbolTrend.Clear();
        if (settings.SymbolTrend.List.Count != 0)
        {
            foreach (var (minValue, maxValue) in settings.SymbolTrend.List)
                SymbolTrend.Add((minValue, maxValue));
        }
        SymbolTrendLog = settings.SymbolTrend.Log;

        // Secondary market trend% (min..max) — optional extra INTERSECT filter
        SymbolTrendSecondary.Clear();
        if (settings.SymbolTrendSecondary.List.Count != 0)
        {
            foreach (var (minValue, maxValue) in settings.SymbolTrendSecondary.List)
                SymbolTrendSecondary.Add((minValue, maxValue));
        }
        SymbolTrendSecondaryLog = settings.SymbolTrendSecondary.Log;

        // Market RSI band in which the signal is skipped
        MarketRsiActive = settings.MarketRsi.Active;
        MarketRsiMinimum = Math.Min(settings.MarketRsi.Minimum, settings.MarketRsi.Maximum);
        MarketRsiMaximum = Math.Max(settings.MarketRsi.Minimum, settings.MarketRsi.Maximum);
        MarketRsiLog = settings.MarketRsi.Log;

        // Market state band in which this side may step in
        MarketStateActive = settings.MarketState.Active;
        MarketStateMinimum = Math.Min(settings.MarketState.Minimum, settings.MarketState.Maximum);
        MarketStateMaximum = Math.Max(settings.MarketState.Minimum, settings.MarketState.Maximum);
        MarketStateInterval = GlobalData.IntervalListPeriodName.TryGetValue(settings.MarketState.Interval, out CryptoInterval? marketStateInterval)
            ? marketStateInterval.IntervalPeriod : CryptoIntervalPeriod.interval4h;
        MarketStateLog = settings.MarketState.Log;

        // Close a position once the market moved this far against it
        MarketExitActive = settings.MarketExit.Active && settings.MarketExit.Percentage > 0;
        MarketExitPercentage = settings.MarketExit.Percentage;
        MarketExitUseBtc = settings.MarketExit.UseBtc;

        // Entry filters from the market-regime study
        BtcMoveActive = settings.BtcMove.Active && settings.BtcMove.Hours > 0;
        BtcMoveHours = settings.BtcMove.Hours;
        BtcMoveMinimum = Math.Min(settings.BtcMove.Minimum, settings.BtcMove.Maximum);
        BtcMoveMaximum = Math.Max(settings.BtcMove.Minimum, settings.BtcMove.Maximum);
        WeekdaysActive = settings.Weekdays.Active;
        WeekdaysAllowed = [.. settings.Weekdays.Days];
        MarketRsiCrossActive = settings.MarketRsiCross.Active;
        MarketRsiCrossLevel = settings.MarketRsiCross.Level;
        MarketRsiCrossHours = Math.Max(1, settings.MarketRsiCross.Hours);
        HigherIntervalActive = settings.HigherInterval.Active;
        HigherIntervalName = settings.HigherInterval.Interval;
        HigherIntervalPeriod = GlobalData.IntervalListPeriodName.TryGetValue(settings.HigherInterval.Interval, out CryptoInterval? higherInterval)
            ? higherInterval.IntervalPeriod : CryptoIntervalPeriod.interval1h;
        HigherIntervalRsiMinimum = Math.Min(settings.HigherInterval.RsiMinimum, settings.HigherInterval.RsiMaximum);
        HigherIntervalRsiMaximum = Math.Max(settings.HigherInterval.RsiMinimum, settings.HigherInterval.RsiMaximum);
        HigherIntervalEma200 = Math.Sign(settings.HigherInterval.Ema200);
        EntryLimitActive = settings.EntryLimit.Active && settings.EntryLimit.MaxPerHour > 0;
        EntryLimitMaxPerHour = settings.EntryLimit.MaxPerHour;


        Strategy.Clear();
        foreach (AlgorithmDefinition strategyDef in RegisterAlgorithms.AlgorithmDefinitionList.Values)
        {
            if (settings.Strategy.Contains(strategyDef.Name))
            {
                if (side == CryptoTradeSide.Long && strategyDef.AnalyzeLongType != null)
                    Strategy.Add(strategyDef.Name, true);
                if (side == CryptoTradeSide.Short && strategyDef.AnalyzeShortType != null)
                    Strategy.Add(strategyDef.Name, true);
            }
        }
    }


    /// <summary>
    /// Whether a rule covers this symbol. A rule without a product covers the PAIR, so "BTCUSDT"
    /// blocks BTCUSDT.SPOT, BTCUSDT.PERP and BTCUSDT.INVERSE alike - which is what someone typing a
    /// coin means. A rule that does name a product ("BTCUSDT.PERP") covers only that one.
    /// <para>
    /// This is also why the lists needed no migration when the product moved into the name: a rule
    /// written before that day keeps meaning exactly what it meant. A deployed market is the one
    /// exception - its old name carried the deployer IN FRONT of the base (XYZGOLDUSDC), so a rule
    /// typed in that era is matched against that spelling too.
    /// </para>
    /// </summary>
    private static bool Covers(SortedList<string, bool> list, string name)
    {
        if (list.ContainsKey(name))
            return true;

        // The rule named no product, so compare on the pair alone
        int dot = name.IndexOf(CryptoProduct.Separator);
        if (dot <= 0)
            return false;
        if (list.ContainsKey(name[..dot]))
            return true;

        // Legacy spelling of a deployed market: the deployer used to sit in front of the base
        // (GOLDUSDC.XYZ was called XYZGOLDUSDC). The reserved product codes never did, so a rule
        // like "PERPBTCUSDT" stays the nonsense it looks like.
        string product = name[(dot + 1)..];
        return product.Length > 0 && !CryptoProduct.IsReserved(product)
            && list.ContainsKey(product + name[..dot]);
    }


    public MatchBlackAndWhiteList InBlackList(string name)
    {
        if (BlackList.Count == 0)
            return MatchBlackAndWhiteList.Empty;

        if (Covers(BlackList, name))
            return MatchBlackAndWhiteList.Present;
        else
            return MatchBlackAndWhiteList.NotPresent;
    }


    public MatchBlackAndWhiteList InWhiteList(string name)
    {
        if (WhiteList.Count == 0)
            return MatchBlackAndWhiteList.Empty;

        if (Covers(WhiteList, name))
            return MatchBlackAndWhiteList.Present;
        else
            return MatchBlackAndWhiteList.NotPresent;
    }

}

