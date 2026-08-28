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
    public List<(decimal minValue, decimal maxValue)> MarketTrend { get; set; } = [];
    public bool MarketTrendLog = false;

    // Secondary market trend + Value (percentages). Evaluated as an additional INTERSECT filter
    // (both Primary and Secondary ranges must contain the current trend value).
    public List<(decimal minValue, decimal maxValue)> MarketTrendSecondary { get; set; } = [];
    public bool MarketTrendSecondaryLog = false;

    // Via interval + Value (ranged)
    // Minimale barometer om de meldingen te genereren
    public Dictionary<CryptoIntervalPeriod, (decimal minValue, decimal maxValue)> Barometer { get; set; } = [];
    public bool BarometerLog = false;
    // Whether the consensus check is enabled (mirrors SettingsTextualBarometer.ConsensusActive)
    public bool BarometerConsensusActive = false;
    // Minimum number of higher-timeframe barometers that must align with the signal direction (0 = disabled)
    public int BarometerMinConsensus = 0;

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
                if (side == CryptoTradeSide.Long)
                    Trend.Add(interval.IntervalPeriod, CryptoTrendIndicator.Bullish);
                if (side == CryptoTradeSide.Short)
                    Trend.Add(interval.IntervalPeriod, CryptoTrendIndicator.Bearish);
            }

            // Barometer (ranged)
            if (settings.Barometer.List.TryGetValue(interval.Name, out var value))
                Barometer.Add(interval.IntervalPeriod, value);
        }
        TrendLog = settings.IntervalTrend.Log;
        BarometerLog = settings.Barometer.Log;
        BarometerConsensusActive = settings.Barometer.ConsensusActive;
        BarometerMinConsensus = settings.Barometer.MinConsensus;

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
        MarketTrend.Clear();
        if (settings.MarketTrend.List.Count != 0)
        {
            foreach (var (minValue, maxValue) in settings.MarketTrend.List)
                MarketTrend.Add((minValue, maxValue));
        }
        MarketTrendLog = settings.MarketTrend.Log;

        // Secondary market trend% (min..max) — optional extra INTERSECT filter
        MarketTrendSecondary.Clear();
        if (settings.MarketTrendSecondary.List.Count != 0)
        {
            foreach (var (minValue, maxValue) in settings.MarketTrendSecondary.List)
                MarketTrendSecondary.Add((minValue, maxValue));
        }
        MarketTrendSecondaryLog = settings.MarketTrendSecondary.Log;


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

