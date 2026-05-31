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
        Smc,
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
            if (GlobalData.Settings.Signal.Long.Strategy.Contains(strategyDef.Name) ||
                GlobalData.Settings.Signal.Short.Strategy.Contains(strategyDef.Name))
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

                    //// Combined DLZ strategies: also schedule zone recalculation on the DLZ intervals.
                    //// Without this, StoRsiDlz / StobbDlz (values < DominantLevel) would fall through
                    //// the plain-indicator branch and the zone worker would never be queued per candle.
                    //if (strategyDef.Strategy == CryptoSignalStrategy.StoRsiDlz ||
                    //    strategyDef.Strategy == CryptoSignalStrategy.StobbDlz)
                    //{
                    //    //foreach (string intervalName in GlobalData.Settings.Signal.ZonesDlz.IntervalList)
                    //    //{
                    //    //    Add(SignalPrepareKind.Dlz, intervalName);
                    //    //    Add(SignalPrepareKind.Indicator, "1m");
                    //    //}
                    //    Add(SignalPrepareKind.Indicator, "1m");
                    //}

                    //// Combined FVG strategies: same reasoning as above for FVG zones.
                    //if (strategyDef.Strategy == CryptoSignalStrategy.StoRsiFvg ||
                    //    strategyDef.Strategy == CryptoSignalStrategy.StobbFvg)
                    //{
                    //    //foreach (string intervalName in GlobalData.Settings.Signal.ZonesFvg.IntervalList)
                    //    //{
                    //    //    Add(SignalPrepareKind.Fvg, intervalName);
                    //    //    Add(SignalPrepareKind.Indicator, "1m");
                    //    //}
                    //    Add(SignalPrepareKind.Indicator, "1m");
                    //}
                }
                else if (strategyDef.Strategy == CryptoSignalStrategy.FairValueGap)
                {
                    // These are seperate intervals on which the FVG is calculated
                    //foreach (string intervalName in GlobalData.Settings.Signal.ZonesFvg.IntervalList)
                    //{
                    //    Add(SignalPrepareKind.Fvg, intervalName);
                    //    Add(SignalPrepareKind.Indicator, "1m");
                    //}
                    Add(SignalPrepareKind.Indicator, "1m");
                }
                else if (strategyDef.Strategy == CryptoSignalStrategy.DominantLevel
                    || strategyDef.Strategy == CryptoSignalStrategy.DominantLevelNear)
                {
                    // These are seperate intervals on which the DLZ is calculated
                    //foreach (string intervalName in GlobalData.Settings.Signal.ZonesDlz.IntervalList)
                    //{
                    //    //Add(SignalPrepareKind.Dlz, intervalName);
                    //}
                    Add(SignalPrepareKind.Indicator, "1m");
                }
                else if (strategyDef.Strategy == CryptoSignalStrategy.OrderBlock
                    || strategyDef.Strategy == CryptoSignalStrategy.OrderBlockRejection)
                {
                    // Separate intervals on which the SMC order blocks are calculated.
                    //foreach (string intervalName in GlobalData.Settings.Signal.ZonesSmc.IntervalList)
                    //{
                    //    Add(SignalPrepareKind.Smc, intervalName);
                    Add(SignalPrepareKind.Indicator, "1m");
                    //}
                }
            }
        }

        // These are seperate intervals on which the FVG is calculated
        foreach (string intervalName in GlobalData.Settings.Signal.ZonesFvg.IntervalList)
        {
            Add(SignalPrepareKind.Fvg, intervalName);
        }
        // These are seperate intervals on which the DLZ is calculated
        foreach (string intervalName in GlobalData.Settings.Signal.ZonesDlz.IntervalList)
        {
            Add(SignalPrepareKind.Dlz, intervalName);
        }
        // Separate intervals on which the SMC order blocks are calculated.
        foreach (string intervalName in GlobalData.Settings.Signal.ZonesSmc.IntervalList)
        {
            Add(SignalPrepareKind.Smc, intervalName);
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
                        // TODO: This is not 100% correct...
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


        // Recompute SMC order blocks on the zone-interval boundary. ZoneSmc.Detect is a cheap
        // full rebuild from the in-memory candles and now also writes the diff to the DB
        // through ThreadSaveObjects. The ZoneLock guards the SmcZones swap and the DB queueing
        // against the DLZ worker and any concurrent chart-driven Detect. Non-blocking try
        // (Wait(0)): if the lock is currently held (e.g. DLZ recalculation on this symbol),
        // skip this tick — the next 1m candle on the same interval boundary will retry.
        if (Preparing.TryGetValue(SignalPrepareKind.Smc, out indexList))
        {
            foreach (var interval in indexList.Values)
            {
                if (lastCandle1mCloseTime % interval.Duration == 0)
                {
                    if (symbol.Data.ZoneLock.Wait(0))
                    {
                        try
                        {
                            ZoneSmc.Detect(symbol, interval);
                        }
                        finally
                        {
                            symbol.Data.ZoneLock.Release();
                        }
                    }
                }
            }
        }

        return indicatorDataList;
    }
}
