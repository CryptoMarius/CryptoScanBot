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

///// <summary>Compiled relative volume filter settings — grouped for readability.</summary>
//public class SettingsCompiledVolume
//{
//    public bool Active = false;
//    public decimal MinRelative = 0m;
//    public decimal MaxRelative = 999m;
//    public int Lookback = 20;
//    public bool Log = false;
//}

///// <summary>Compiled adaptive feedback filter settings — grouped for readability.</summary>
//public class SettingsCompiledFeedback
//{
//    public bool Active = false;
//    public int MaxDays = 7;
//    public int MinSignals = 5;
//    public decimal BlockThreshold = 40m;
//    public int ReEnableHours = 24;
//    public bool Log = false;
//}


// Compiled version of the SettingsTextual for signal (long/short) and trading (long/short)

[Serializable]
public class SettingsCompiled
{
    // The active intervals
    [JsonIgnore]
    public List<CryptoInterval> Interval { get; set; } = [];
    public SortedList<CryptoIntervalPeriod, bool> IntervalPeriod { get; set; } = [];


    // The active strategies
    public SortedList<CryptoSignalStrategy, bool> Strategy { get; set; } = [];


    // Interval trend + Value (bullisch, bearish)
    public Dictionary<CryptoIntervalPeriod, CryptoTrendIndicator> Trend { get; set; } = [];
    public bool TrendLog = false;

    // Primary market trend + Value (percentages)
    public List<(decimal minValue, decimal maxValue)> MarketTrend { get; set; } = [];
    public bool MarketTrendLog = false;

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


        Strategy.Clear();
        foreach (AlgorithmDefinition strategyDef in RegisterAlgorithms.AlgorithmDefinitionList.Values)
        {
            if (settings.Strategy.Contains(strategyDef.Name))
            {
                if (side == CryptoTradeSide.Long && strategyDef.AnalyzeLongType != null)
                    Strategy.Add(strategyDef.Strategy, true);
                if (side == CryptoTradeSide.Short && strategyDef.AnalyzeShortType != null)
                    Strategy.Add(strategyDef.Strategy, true);
            }
        }
    }


    public MatchBlackAndWhiteList InBlackList(string name)
    {
        if (BlackList.Count == 0)
            return MatchBlackAndWhiteList.Empty;

        if (BlackList.ContainsKey(name))
            return MatchBlackAndWhiteList.Present;
        else
            return MatchBlackAndWhiteList.NotPresent;
    }


    public MatchBlackAndWhiteList InWhiteList(string name)
    {
        if (WhiteList.Count == 0)
            return MatchBlackAndWhiteList.Empty;

        if (WhiteList.ContainsKey(name))
            return MatchBlackAndWhiteList.Present;
        else
            return MatchBlackAndWhiteList.NotPresent;
    }

}

