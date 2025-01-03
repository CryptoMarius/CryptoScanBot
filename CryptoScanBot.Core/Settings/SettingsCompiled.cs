﻿using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Signal;

using System.Text.Json.Serialization;

namespace CryptoScanBot.Core.Settings;

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
    // Welke intervallen zijn actief
    [JsonIgnore]
    public List<CryptoInterval> Interval { get; set; } = [];
    public SortedList<CryptoIntervalPeriod, bool> IntervalPeriod { get; set; } = [];


    // Welke strategien zijn actief (en speciaal voor de CreateSignal een onderverdeling)
    public SortedList<CryptoSignalStrategy, bool> Strategy { get; set; } = [];
    public List<CryptoSignalStrategy> StrategySbmStob { get; set; } = [];
    public List<CryptoSignalStrategy> StrategyOthers { get; set; } = [];


    // Interval trend + Value (bullisch, bearish)
    public Dictionary<CryptoIntervalPeriod, CryptoTrendIndicator> Trend { get; set; } = [];
    public bool TrendLog = false;

    // Markt trend + Value (percentage)
    public List<(decimal minValue, decimal maxValue)> MarketTrend { get; set; } = [];
    public bool MarketTrendLog = false;

    // Via interval + Value (ranged)
    // Minimale barometer om de meldingen te genereren
    public Dictionary<CryptoIntervalPeriod, (decimal minValue, decimal maxValue)> Barometer { get; set; } = [];
    public bool BarometerLog = false;


    // The black- and whitelist
    public SortedList<string, bool> BlackList { get; } = [];
    public SortedList<string, bool> WhiteList { get; } = [];



    public void IndexStrategyInternally(SettingsTextual settings, CryptoTradeSide side)
    {
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


        // Market trend% (min..max), er is maar 1 aanwezig
        MarketTrend.Clear();
        if (settings.MarketTrend.List.Count != 0)
        {
            foreach (var (minValue, maxValue) in settings.MarketTrend.List)
                MarketTrend.Add((minValue, maxValue));
        }
        MarketTrendLog = settings.MarketTrend.Log;


        Strategy.Clear();
        StrategySbmStob.Clear();
        StrategyOthers.Clear();
        foreach (AlgorithmDefinition strategyDef in RegisterAlgorithms.AlgorithmDefinitionList.Values)
        {
            if (settings.Strategy.Contains(strategyDef.Name))
            {
                bool addStrategy = false;
                if (side == CryptoTradeSide.Long && strategyDef.AnalyzeLongType != null)
                    addStrategy = true;
                if (side == CryptoTradeSide.Short && strategyDef.AnalyzeShortType != null)
                    addStrategy = true;

                if (addStrategy)
                {
                    Strategy.Add(strategyDef.Strategy, true);
                    if (strategyDef.Strategy >= CryptoSignalStrategy.Sbm1 && strategyDef.Strategy <= CryptoSignalStrategy.Stobb)
                        StrategySbmStob.Add(strategyDef.Strategy);
                    else
                        StrategyOthers.Add(strategyDef.Strategy);
                }
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

