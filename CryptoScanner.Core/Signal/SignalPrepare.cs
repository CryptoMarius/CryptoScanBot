using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.Core.Signal;

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
        Preparing.TryAdd(kind, []);
        Preparing[kind].TryAdd(intervalName, interval);
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
                if (strategyDef.Strategy < CryptoSignalStrategy.DominantLevel)
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
                    }
                }
                else if (strategyDef.Strategy == CryptoSignalStrategy.DominantLevel || strategyDef.Strategy == CryptoSignalStrategy.DominantLevelNear)
                {
                    // These are seperate intervals on which the DLZ is calculated
                    foreach (string intervalName in GlobalData.Settings.Signal.ZonesDlz.IntervalList)
                    {
                        Add(SignalPrepareKind.Dlz, intervalName);
                        Add(SignalPrepareKind.Indicator, "1m");
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



    public static CryptoIndicatorDataList Execute(CryptoSymbol symbol, CryptoCandle lastCandle1m, CandleTime lastCandle1mCloseTime)
    {
        CryptoIndicatorDataList indicatorDataList = [];

        // Prepare all the indicators on the requested intervals
        // The indexList contains only the checked intervals for the normal strategies
        if (Preparing.TryGetValue(SignalPrepareKind.Indicator, out SortedList<string, CryptoInterval>? indexList))
        {
            foreach (var interval in indexList.Values)
            {
                if (lastCandle1mCloseTime % interval.Duration == 0)
                {
                    // Remark: The candle in the requested interval could be missing in action.
                    // Its something I did not expect in the beginning of this application.

                    // To the start of the candle for that interval
                    CandleTime candleOpenTime = lastCandle1mCloseTime - interval.Duration;
                    indicatorDataList.CalculateIndicatorsForInterval(symbol, interval, candleOpenTime, interval.IntervalPeriod);
                }
            }
        }

        // Scan dlz zones
        // The indexList contains only the checked intervals for the dlz strategy
        if (Preparing.TryGetValue(SignalPrepareKind.Dlz, out indexList))
        {
            foreach (var interval in indexList.Values)
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
        }


        // Scan fvg zones
        // The indexList contains only the checked intervals for the fvg strategy
        if (Preparing.TryGetValue(SignalPrepareKind.Fvg, out indexList))
        {
            foreach (var interval in indexList.Values)
            {
                if (lastCandle1mCloseTime % interval.Duration == 0)
                {
                    ZoneFvg.ScanForNew(symbol, interval, lastCandle1mCloseTime);
                }
            }
        }

        return indicatorDataList;
    }
}
