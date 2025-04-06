using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Zones;

namespace CryptoScanBot.Core.Signal;

public class SignalPrepare
{
    private enum SignalPrepareKind
    {
        Dlz,
        Fvg,
        Indicator,
    }

    // prepare - strategy + intervallist
    // advantage 1: It makes it easier to seperate from the remaining code
    // advantage 2: this saves a call to the prepare indicators (otherwise long + short call)
    // advantage 3: prepare the fvg and dlz for other intervals than the signal interval (1h vs 1m)
    private static Dictionary<SignalPrepareKind, SortedList<string, CryptoInterval>> Preparing { get; set; } = [];

    public static bool ZoneDlzActive() => Preparing.ContainsKey(SignalPrepareKind.Dlz);
    public static bool ZoneFvgActive() => Preparing.ContainsKey(SignalPrepareKind.Fvg);

    private static void Add(SignalPrepareKind kind, string intervalName)
    {
        CryptoInterval interval = GlobalData.IntervalListPeriodName[intervalName];
        var key = kind;
        Preparing.TryAdd(key, []);
        Preparing[key].TryAdd(intervalName, interval);
    }

    public static void Prepare()
    {
        // Default setup
        Preparing.Clear();


        foreach (AlgorithmDefinition strategyDef in RegisterAlgorithms.AlgorithmDefinitionList.Values)
        {
            // long or short does not matter for the prepare
            if (GlobalData.Settings.Signal.Long.Strategy.Contains(strategyDef.Name) || GlobalData.Settings.Signal.Short.Strategy.Contains(strategyDef.Name))
            {
                if (strategyDef.Strategy < CryptoSignalStrategy.DominantLevelNear)
                {
                    foreach (string intervalName in GlobalData.Settings.Signal.Long.Interval)
                    {
                        Add(SignalPrepareKind.Indicator, intervalName);
                    }
                    foreach (string intervalName in GlobalData.Settings.Signal.Short.Interval)
                    {
                        Add(SignalPrepareKind.Indicator, intervalName);
                    }
                }
                else if (strategyDef.Strategy == CryptoSignalStrategy.FairValueGap)
                {
                    // These are seperate intervals on which the FVG is calculated
                    foreach (string intervalName in GlobalData.Settings.Signal.ZonesFvg.IntervalList)
                    {
                        Add(SignalPrepareKind.Fvg, intervalName);
                        Add(SignalPrepareKind.Indicator, "1m");
                        //Add(SignalPrepareKind.Indicator, intervalName); // extra
                    }
                }
                else if (strategyDef.Strategy == CryptoSignalStrategy.DominantLevel || strategyDef.Strategy == CryptoSignalStrategy.DominantLevelNear)
                {
                    // These are seperate intervals on which the DLZ is calculated
                    foreach (string intervalName in GlobalData.Settings.Signal.ZonesDlz.IntervalList)
                    {
                        Add(SignalPrepareKind.Dlz, intervalName);
                        Add(SignalPrepareKind.Indicator, "1m");
                        //Add(SignalPrepareKind.Indicator, intervalName); // extra
                    }
                }
            }
        }

        // Remove the unused items
        foreach (var item in Preparing.ToList())
        {
            if (item.Value.Count == 0)
            {
                Preparing.Remove(item.Key);
            }
        }
    }



    public static Dictionary<CryptoIntervalPeriod, List<CryptoCandle>> Execute(CryptoSymbol symbol, CryptoCandle lastCandle1m, long lastCandle1mCloseTime)
    {
        Dictionary<CryptoIntervalPeriod, List<CryptoCandle>> lastCandleList = [];

        // Automaticly scan for new dlz zones
        foreach (var interval in Preparing[SignalPrepareKind.Dlz].Values)
        {
            if (lastCandle1mCloseTime % interval.Duration == 0)
            {
                CryptoSymbolInterval symbolInterval = symbol.Data.Get(interval.IntervalPeriod);

                // Scan for new zones if candle is outside of the previous primary trend
                decimal valueLow = lastCandle1m.GetLowValue(false);
                decimal valueHigh = lastCandle1m.GetHighValue(false);
                if (symbolInterval.DlzAdmin.LastSwingLow == null || valueLow < symbolInterval.DlzAdmin.LastSwingLow ||
                   symbolInterval.DlzAdmin.LastSwingHigh == null || valueHigh > symbolInterval.DlzAdmin.LastSwingHigh)
                {
                    // avoid duplicate calculation (kind of a weak attemp)
                    symbolInterval.DlzAdmin.LastSwingLow = valueLow;
                    symbolInterval.DlzAdmin.LastSwingHigh = valueHigh;
                    GlobalData.ThreadZoneCalculate?.AddToQueue(symbol, interval);
                }
            }
        }

        // Automaticly scan for new fvg zones
        foreach (var interval in Preparing[SignalPrepareKind.Fvg].Values)
        {
            if (lastCandle1mCloseTime % interval.Duration == 0)
            {
                ZoneFvg.ScanForNew(symbol, interval, lastCandle1mCloseTime);
            }
        }

        // Prepare all the indicators on each interval
        foreach (var interval in Preparing[SignalPrepareKind.Indicator].Values)
        {
            if (lastCandle1mCloseTime % interval.Duration == 0)
            {
                // candle of interval starts at
                long candleOpenTime = lastCandle1mCloseTime - lastCandle1mCloseTime % interval.Duration;
                List<CryptoCandle>? history = CandleIndicatorData.CalculateCandles(symbol, interval, candleOpenTime, out string _);

                if (history != null)
                {
                    lastCandleList.TryAdd(interval.IntervalPeriod, history);
                    CandleIndicatorData.CalculateIndicators(symbol, interval, history);
                }
            }
        }

        return lastCandleList;
    }
}
