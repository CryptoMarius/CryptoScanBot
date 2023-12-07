using System.Text.Json.Serialization;

using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Signal;


namespace CryptoSbmScanner.Settings;

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
    public List<CryptoInterval> Interval { get; set; } = new();
    public SortedList<CryptoIntervalPeriod, bool> IntervalPeriod { get; set; } = new();


    // Welke strategien zijn actief (en speciaal voor de CreateSignal een onderverdeling)
    public SortedList<CryptoSignalStrategy, bool> Strategy { get; set; } = new();
    public List<CryptoSignalStrategy> StrategySbmStob { get; set; } = new();
    public List<CryptoSignalStrategy> StrategyOthers { get; set; } = new();
    

    // Via interval + Value
    public Dictionary<CryptoIntervalPeriod, CryptoTrendIndicator> Trend { get; set; } = new();


    // Via interval + Value (ranged)
    public Dictionary<CryptoIntervalPeriod, (decimal minValue, decimal maxValue)> Barometer { get; set; } = new();


    // The black- and whitelist
    public SortedList<string, bool> BlackList { get; } = new();
    public SortedList<string, bool> WhiteList { get; } = new();



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

            // Trend *up/down)
            if (GlobalData.Settings.Trading.Long.Trend.Contains(interval.Name))
            {
                if (side == CryptoTradeSide.Long)
                    Trend.Add(interval.IntervalPeriod, CryptoTrendIndicator.trendBullish);
                if (side == CryptoTradeSide.Short)
                    Trend.Add(interval.IntervalPeriod, CryptoTrendIndicator.trendBearish);
            }

            // Barometer (ranged)
            if (settings.Barometer.TryGetValue(interval.Name, out var value))
            {
                if (side == CryptoTradeSide.Long)
                    Barometer.Add(interval.IntervalPeriod, (value, decimal.MaxValue));
                else if (side == CryptoTradeSide.Short)
                    Barometer.Add(interval.IntervalPeriod, (decimal.MinValue, value));
            }
        }

        Strategy.Clear();
        StrategySbmStob.Clear();
        StrategyOthers.Clear();
        foreach (AlgorithmDefinition strategyDef in SignalHelper.AlgorithmDefinitionList)
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
        if (!BlackList.Any())
            return MatchBlackAndWhiteList.Empty;

        if (BlackList.ContainsKey(name))
            return MatchBlackAndWhiteList.Present;
        else
            return MatchBlackAndWhiteList.NotPresent;
    }


    public MatchBlackAndWhiteList InWhiteList(string name)
    {
        if (!WhiteList.Any())
            return MatchBlackAndWhiteList.Empty;

        if (WhiteList.ContainsKey(name))
            return MatchBlackAndWhiteList.Present;
        else
            return MatchBlackAndWhiteList.NotPresent;
    }

}

