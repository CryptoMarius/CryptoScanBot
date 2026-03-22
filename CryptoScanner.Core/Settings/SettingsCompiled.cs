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
    // Welke intervallen zijn actief
    [JsonIgnore]
    public List<CryptoInterval> Interval { get; set; } = [];
    public SortedList<CryptoIntervalPeriod, bool> IntervalPeriod { get; set; } = [];


    // Welke strategien zijn actief (en speciaal voor de CreateSignal een onderverdeling)
    public SortedList<CryptoSignalStrategy, bool> Strategy { get; set; } = [];


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
    // Minimum number of higher-timeframe barometers that must align with the signal direction (0 = disabled)
    public int BarometerMinConsensus = 0;

    // Relative volume filter: RelVol = current_candle_volume / SMA(volume, VolumeLookback)
    public bool VolumeActive = false;
    public decimal VolumeMinRelative = 0m;
    public decimal VolumeMaxRelative = 999m;
    public int VolumeLookback = 20;
    public bool VolumeLog = false;


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
        BarometerMinConsensus = settings.Barometer.MinConsensus;

        VolumeActive = settings.Volume.IsActive;
        VolumeMinRelative = settings.Volume.MinRelVol;
        VolumeMaxRelative = settings.Volume.MaxRelVol;
        VolumeLookback = settings.Volume.Lookback;
        VolumeLog = settings.Volume.Log;


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

