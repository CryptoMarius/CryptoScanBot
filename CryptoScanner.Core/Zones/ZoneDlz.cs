using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

using Dapper;

using System.Diagnostics;

namespace CryptoScanner.Core.Zones;

// DLZ - Dominant Liquidity ZonesDlz

public class ZoneDlz
{

    public static void LoadAllZones()
    {
        foreach (var symbol in GlobalData.ActiveExchange!.SymbolListName.Values.ToList())
        {
            symbol.Data.ResetFvgData();
            symbol.Data.ResetDlzData();
            symbol.Data.ResetSmcData();
            symbol.Data.ResetTrendData();
        }

        using var database = new CryptoDatabase();
        // Startup load is for the live scanner: only live zones (EmulatorRunId IS NULL). Emulator
        // zones belong to a specific run and are loaded per run by LoadZonesForSymbol instead.
        string sql = "select * from zone where exchangeid=exchangeid and CloseTime is null and EmulatorRunId is null order by OpenTime";
        foreach (CryptoZone zone in database.Connection.Query<CryptoZone>(sql, new { exchangeid = GlobalData.ActiveExchange!.Id }))
        {
            PutZoneInMemory(zone);
        }
    }


    /// <summary>
    /// Loads a symbol's zones into memory, scoped to one source: <paramref name="emulatorRunId"/> set →
    /// that emulator run's zones; null → live zones only (EmulatorRunId IS NULL). The engine passes
    /// GlobalData.CurrentEmulatorRunId (the active run during a replay, null when live); the chart passes
    /// the run it is viewing. Scoping keeps each run isolated/reproducible — a run never sees another
    /// run's (possibly already-closed) zones — while letting a finished run's zones be shown later.
    /// </summary>
    public static void LoadZonesForSymbol(CryptoSymbol symbol, int? emulatorRunId)
    {
        CryptoSymbolData symbolData = symbol.Data;
        symbolData.ResetFvgData();
        symbolData.ResetDlzData();
        symbolData.ResetSmcData();
        symbolData.ResetTrendData();

        using var database = new CryptoDatabase();

        string runFilter = emulatorRunId.HasValue ? "and EmulatorRunId = @RunId " : "and EmulatorRunId is null ";
        string sql = "select * from zone where SymbolId = @SymbolId " + runFilter + "order by OpenTime"; //and Kind=1
        foreach (CryptoZone zone in database.Connection.Query<CryptoZone>(sql, new { SymbolId = symbol.Id, RunId = emulatorRunId }))
        {
            PutZoneInMemory(zone);
        }

        RebuildCommittedStoreFromLoadedZones(symbol);
    }


    /// <summary>
    /// Puts the committed store back in step with the zones that were just loaded.
    /// <para>
    /// The marker survives a restart (CandleDatabase.SymbolInterval.DlzMarker) but the store it
    /// vouches for does not - it is a plain in-memory list. Restoring only the marker would be
    /// worse than restoring nothing: the next pass would believe the settled part was already
    /// accounted for, hand the reconciliation nothing but its tail, and everything else would be
    /// treated as gone. So the store is rebuilt here from the zones themselves, which is where
    /// those settled verdicts have been sitting all along.
    /// </para>
    /// <para>
    /// A marker with nothing behind it is not trusted. That can be a database that was cleared
    /// while the candle store was kept, or simply a stretch of history without a single dominant
    /// pivot. Telling those apart is not worth the risk of silently dropping zones, and the cost of
    /// being wrong is only one full rescan.
    /// </para>
    /// </summary>
    internal static void RebuildCommittedStoreFromLoadedZones(CryptoSymbol symbol)
    {
        foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
        {
            CryptoSymbolIntervalDlz dlz = symbolInterval.Dlz;
            dlz.CommittedZones = [];
            if (dlz.CommittedPivotMarker == null)
                continue;

            CandleTime marker = dlz.CommittedPivotMarker.Value;
            foreach (var list in new[] { dlz.Zones.LongOpen, dlz.Zones.ShortOpen,
                                         dlz.Zones.LongClosed, dlz.Zones.ShortClosed })
            {
                foreach (CryptoZone zone in list)
                {
                    if (zone.OpenTime <= marker)
                        dlz.CommittedZones.Add(zone);
                }
            }

            if (dlz.CommittedZones.Count == 0)
                dlz.CommittedPivotMarker = null;
        }
    }


    private static void PutZoneInMemory(CryptoZone zone)
    {
        if (GlobalData.ExchangeListId.TryGetValue(zone.ExchangeId, out Model.CryptoExchange? exchange))
        {
            zone.Exchange = exchange;
            if (exchange.SymbolListId.TryGetValue(zone.SymbolId, out CryptoSymbol? symbol))
            {
                zone.Symbol = symbol;
                CryptoSymbolData symbolData = symbol.Data;

                if (GlobalData.IntervalListId.TryGetValue(zone.IntervalId, out CryptoInterval? interval))
                {
                    zone.Interval = interval;
                    var symbolInterval = symbolData.Get(interval.IntervalPeriod);

                    if (zone.Kind == CryptoZoneKind.FairValueGap)
                    {
                        symbolInterval.Fvg.Zones.Add(zone);
                    }
                    else if (zone.Kind == CryptoZoneKind.OrderBlock)
                    {
                        // SMC zones use a flat list; TouchCount/IsMitigated are recomputed by
                        // the next ZoneSmc.Detect (they are [Computed], not persisted).
                        symbolInterval.Smc.Zones.Add(zone);
                    }
                    else
                    {
                        symbolInterval.Dlz.Zones.Add(zone);

                        // Creation date is the date of the last swing point (SH/SL)
                        // TODO: The last swing low and high are now extracted from the boundaries of the zone, that is not 100% correct
                        //
                        // Which is why this seeds the TRIGGER range and no longer the swing values.
                        // A zone boundary is not a swing, so writing it into LastSwingLow/High claimed
                        // an accuracy this path does not have; all it is really for is giving the first
                        // candle after a restart something to compare against, so the scanner does not
                        // queue a full recalculation for every symbol at once. The real swings arrive
                        // with the first CalculatePivots and take the trigger range with them.
                        CandleTime timeLastSwingPoint = zone.OpenTime;
                        if (symbolInterval.Dlz.TimeLastSwingPoint == null || timeLastSwingPoint > symbolInterval.Dlz.TimeLastSwingPoint)
                        {
                            symbolInterval.Dlz.TimeLastSwingPoint = timeLastSwingPoint;
                            if (symbolInterval.Dlz.TriggerRangeLow == null || zone.Bottom > symbolInterval.Dlz.TriggerRangeLow)
                                symbolInterval.Dlz.TriggerRangeLow = zone.Bottom;
                            if (symbolInterval.Dlz.TriggerRangeHigh == null || zone.Top > symbolInterval.Dlz.TriggerRangeHigh)
                                symbolInterval.Dlz.TriggerRangeHigh = zone.Top;
                        }
                    }
                }
            }
        }
    }


    internal static void CreateZonesFromZigZag(CryptoSymbol symbol, CryptoInterval interval,
        List<ZigZagResult> zigZagList, List<CryptoZone> zones, CandleTime? afterTime = null)
    {
        foreach (var zigZag in zigZagList)
        {
            if (afterTime != null && zigZag.Candle.OpenTime <= afterTime)
                continue;

            if (zigZag.Dominant && !zigZag.Dummy) //  && zigZag.IsValid all newZones (also the closed ones)
            {
                CryptoZone zone = new()
                {
                    Kind = CryptoZoneKind.DominantLevel,
                    Strength = zigZag.Strength, // depending on the percentage of the zone "intro"
                    ExchangeId = symbol.Exchange.Id,
                    Exchange = symbol.Exchange,
                    SymbolId = symbol.Id,
                    Symbol = symbol,
                    IntervalId = interval.Id,
                    Interval = interval,
                    OpenTime = zigZag.Candle.OpenTime,
                    Top = zigZag.Top,
                    Bottom = zigZag.Bottom,
                    Side = zigZag.PointType == 'L' ? CryptoTradeSide.Long : CryptoTradeSide.Short,
                    IsValid = zigZag.IsValid,
                    CloseTime = zigZag.CloseDate,
                    Description = $"{interval.Name}: {zigZag.Percentage:N2}% {zigZag.NiceIntro}",
                };
                zones.Add(zone);
            }
        }
    }


    //private static void CombineAndSaveZones(CryptoSymbol symbol, CryptoInterval interval,
    //    List<ZigZagResult> zigZagList, DatabaseStatistics statistics)
    //{
    //}


    /// <summary>
    /// Splits the live zones on <paramref name="mergeFrom"/>: everything older is carried straight
    /// into <paramref name="carryInto"/> untouched, everything at or after it goes into
    /// <paramref name="index"/> to be reconciled against what this pass produced.
    /// <para>
    /// The older ones rest on pivots that can no longer move, so a recalculation would produce them
    /// unchanged - reconciling them is work with a guaranteed outcome. Keeping them out is what makes
    /// the cost of an incremental pass depend on the tail instead of on the whole history.
    /// </para>
    /// </summary>
    internal static void SplitOnMergeBoundary(CryptoSymbolIntervalZones live, CandleTime mergeFrom,
        CryptoSymbolIntervalZones carryInto,
        SortedList<(CryptoTradeSide, CandleTime?, decimal, decimal), CryptoZone> index,
        DatabaseStatistics statistics)
    {
        foreach (var list in new[] { live.LongOpen, live.ShortOpen, live.LongClosed, live.ShortClosed })
        {
            List<CryptoZone> recent = [];
            foreach (CryptoZone zone in list)
            {
                if (zone.OpenTime < mergeFrom)
                    carryInto.Add(zone);
                else
                    recent.Add(zone);
            }
            ZoneTools.CreateZoneIndex(recent, index, statistics);
        }
    }


    /// <summary>
    /// Swaps every freshly minted zone in <paramref name="rebuilt"/> for the live zone that already
    /// describes the same thing, matched on the key the merge itself uses (side, open time, top,
    /// bottom).
    /// <para>
    /// The provisional tail is rebuilt on every pass, and without this each rebuild would hand the
    /// merge a brand new CryptoZone carrying TouchCount 0 and no CloseTime. The merge would read
    /// that as a modification and replace the live zone, throwing away the touches it had collected
    /// - and the incremental broken-zone check only scans candles after the cursor, so they would
    /// never be counted again. Same geometry means the same zone, so it keeps its object.
    /// </para>
    /// <para>
    /// A zone whose geometry DID change gets a different key, keeps the new instance, and is a new
    /// zone as far as anything downstream is concerned. Which is correct: that is a different level.
    /// </para>
    /// </summary>
    private static void KeepLiveInstanceWhereUnchanged(CryptoSymbolIntervalZones live,
        List<CryptoZone> rebuilt)
    {
        if (rebuilt.Count == 0)
            return;

        Dictionary<(CryptoTradeSide, CandleTime, decimal, decimal), CryptoZone> byKey = [];
        foreach (var list in new[] { live.LongOpen, live.ShortOpen, live.LongClosed, live.ShortClosed })
        {
            foreach (CryptoZone zone in list)
                byKey[(zone.Side, zone.OpenTime, zone.Top, zone.Bottom)] = zone;
        }

        for (int index = 0; index < rebuilt.Count; index++)
        {
            CryptoZone zone = rebuilt[index];
            if (byKey.TryGetValue((zone.Side, zone.OpenTime, zone.Top, zone.Bottom),
                    out CryptoZone? existing))
                rebuilt[index] = existing;
        }
    }


    private static bool UnzoomedPercentageBelowMinimum(ZigZagResult zigZag)
    {
        if (GlobalData.Settings.Signal.ZonesDlz.ZonesApplyUnzoomed)
        {
            var value = GlobalData.Settings.Signal.ZonesDlz.MinimumUnZoomedPercentage;
            if (value > 0 && zigZag.Percentage < value)
            {
                zigZag.Dominant = false;
                //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Unzoomed box ignored {zigZag.Percentage:N2} < {value:N2} {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");
                return true;
            }
        }
        return false;
    }


    private static bool UnzoomedPercentageAboveMaximum(ZigZagResult zigZag) //, CryptoSymbol symbol, CryptoInterval interval)
    {
        if (GlobalData.Settings.Signal.ZonesDlz.ZonesApplyUnzoomed)
        {
            var value = GlobalData.Settings.Signal.ZonesDlz.MaximumUnZoomedPercentage;
            if (value > 0 && zigZag.Percentage > value)
            {
                zigZag.Dominant = false;
                //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Unzoomed box ignored {zigZag.Percentage:N2} > {value:N2} {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");
                return true;
            }
        }
        return false;
    }


    private static bool ZoomedPercentageBelowMinimum(ZigZagResult zigZag) //, CryptoSymbol symbol, CryptoInterval interval)
    {
        var value = GlobalData.Settings.Signal.ZonesDlz.MinimumZoomedPercentage;
        if (value > 0 && zigZag.Percentage < value)
        {
            zigZag.Dominant = false;
            //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Zoomed box ignored {zigZag.Percentage:N2} < {value:N2} {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");
            return true;
        }
        return false;
    }

    private static bool ZoomedPercentageAboveMaximum(ZigZagResult zigZag) //, CryptoSymbol symbol, CryptoInterval interval)
    {
        var value = GlobalData.Settings.Signal.ZonesDlz.MaximumZoomedPercentage;
        if (value > 0 && zigZag.Percentage > value)
        {
            zigZag.Dominant = false;
            //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Zoomed box ignored {zigZag.Percentage:N2} > {value:N2} {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");
            return true;
        }
        return false;
    }


    public static async Task MakeDominantAndZoomInAsync(CryptoSymbol symbol, CryptoInterval interval,
        ZigZagResult zigZag, decimal top, decimal bottom,
        ZoneCandleWindows loadedCandlesInMemory)
    {
        zigZag.Top = top;
        zigZag.Bottom = bottom;
        zigZag.IsValid = true;
        zigZag.Dominant = true;
        zigZag.Percentage = (double)(100 * ((zigZag.Top - zigZag.Bottom) / zigZag.Bottom));
        //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Dominant pivot at {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");

        // Borrow the wick from the neighbour if higher or lower
        if (zigZag.PointType == 'L')
        {
            CryptoSymbolInterval symbolInterval = symbol!.GetSymbolInterval(interval!.IntervalPeriod);
            if (symbolInterval.CandleList.TryGetValue(zigZag.Candle.OpenTime + interval.Duration, out CryptoCandle candle))
            {
                if (candle.Low < zigZag.Bottom)
                {
                    zigZag.Top = Math.Max(candle.Close, candle.Open);
                    zigZag.Bottom = candle.Low;
                }
            }
            if (symbolInterval.CandleList.TryGetValue(zigZag.Candle.OpenTime - interval.Duration, out candle))
            {
                if (candle.Low < zigZag.Bottom)
                {
                    zigZag.Top = Math.Max(candle.Close, candle.Open);
                    zigZag.Bottom = candle.Low;
                }
            }
        }
        else if (zigZag.PointType == 'H')
        {
            CryptoSymbolInterval symbolInterval = symbol!.GetSymbolInterval(interval!.IntervalPeriod);
            if (symbolInterval.CandleList.TryGetValue(zigZag.Candle.OpenTime + interval.Duration, out CryptoCandle candle))
            {
                if (candle.High > zigZag.Top)
                {
                    zigZag.Top = candle.High;
                    zigZag.Bottom = Math.Min(candle.Close, candle.Open);
                }
            }
            if (symbolInterval.CandleList.TryGetValue(zigZag.Candle.OpenTime - interval.Duration, out candle))
            {
                if (candle.High > zigZag.Top)
                {
                    zigZag.Top = candle.High;
                    zigZag.Bottom = Math.Min(candle.Close, candle.Open);
                }
            }
        }
        if (zigZag.Top != top || zigZag.Bottom != bottom)
        {
            zigZag.Percentage = (double)(100 * ((zigZag.Top - zigZag.Bottom) / zigZag.Bottom));
            //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Dominant pivot corrected {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");
        }

        // Is the (unzoomed) percentage between the configured limits?
        // Or is the percentage alread below the zoom-limit? (saves time)
        if (UnzoomedPercentageBelowMinimum(zigZag)
            || UnzoomedPercentageAboveMaximum(zigZag)
            || ZoomedPercentageBelowMinimum(zigZag))
        {
            zigZag.IsValid = false;
            return; // (mark the point as not dominant + exit)
        }



        // If the found percentage is obove 0.7% zoom in on the lower intervals (withing the boundaries of the current candle)
        if (GlobalData.Settings.Signal.ZonesDlz.ZoomLowerTimeFrames && zigZag.Percentage >= GlobalData.Settings.Signal.ZonesDlz.MaximumZoomedPercentage)
        {
            CryptoIntervalPeriod zoom = interval!.IntervalPeriod;
            CandleTime unixStart = zigZag.Candle.OpenTime;
            CandleTime unixEinde = zigZag.Candle.OpenTime + interval.Duration;
            //DateTime unixStartDebug = CandleTools.GetUnixDate(unixStart);
            //DateTime unixEindeDebug = CandleTools.GetUnixDate(unixEinde);

            while (zigZag.Percentage >= GlobalData.Settings.Signal.ZonesDlz.MaximumZoomedPercentage && zoom > CryptoIntervalPeriod.interval1m)
            {
                zoom--;
                PipelineProfiler.RecordDlzZoomStep();
                //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Dominant pivot zooming {zoom} {zigZag.Percentage:N2}%");

                // Is IntervalList supported by Exchange
                CryptoSymbolInterval zoomInterval = symbol!.GetSymbolInterval(zoom);
                //if (symbol.Exchange.IsIntervalSupported(zoomInterval.IntervalPeriod))
                {
                    //// Load candles from disk if needed
                    //if (!loadedCandlesInMemory.TryGetValue(zoomInterval.IntervalPeriod, out bool _))
                    //    await ZoneCandleEngine.ReadCandlesFromDiskAsync(symbol, zoomInterval.Interval);
                    //loadedCandlesInMemory.TryAdd(zoomInterval.IntervalPeriod, true); // in memory, alway's save

                    //// Load candles from the exchange if needed
                    //int count = interval.Duration / zoomInterval.Interval.Duration;
                    //if (await ZoneCandleEngine.FetchFrom(symbol, zoomInterval.Interval, unixStart, count))
                    //    loadedCandlesInMemory[zoomInterval.Interval.IntervalPeriod] = true; // in memory, alway's save

                    int count = (int)interval.Duration / (int)zoomInterval.Interval.Duration;
                    await ZoneCandleEngine.FetchFrom(loadedCandlesInMemory, symbol, zoomInterval.Interval, unixStart, count);

                    CandleTime loop = IntervalTools.StartOfIntervalCandle(unixStart, zoomInterval.Interval.Duration);
                    while (loop < unixEinde && zigZag.Percentage >= GlobalData.Settings.Signal.ZonesDlz.MaximumZoomedPercentage)
                    {
                        //DateTime loopDebug = CandleTools.GetUnixDate(loop);
                        if (loop >= zigZag.Candle.OpenTime) // really?
                        {
                            if (zoomInterval.CandleList.TryGetValue(loop, out CryptoCandle candle))
                            {
                                if (zigZag.PointType == 'L')
                                {
                                    decimal bodyTop = Math.Max(candle.Open, candle.Close);
                                    if (bodyTop < zigZag.Top)
                                    {
                                        double percentage = (double)(100 * ((bodyTop - zigZag.Bottom) / zigZag.Bottom));
                                        if (percentage >= GlobalData.Settings.Signal.ZonesDlz.MinimumUnZoomedPercentage)
                                        {
                                            zigZag.Top = bodyTop;
                                            zigZag.Percentage = percentage;
                                            //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Dominant pivot zoomed {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2} {zoomInterval.Interval.Name}");
                                        }
                                    }
                                }
                                else // High
                                {
                                    decimal bodyBottom = Math.Min(candle.Open, candle.Close);
                                    if (bodyBottom > zigZag.Bottom)
                                    {
                                        double percentage = (double)(100 * ((zigZag.Top - bodyBottom) / bodyBottom));
                                        if (percentage >= GlobalData.Settings.Signal.ZonesDlz.MinimumUnZoomedPercentage)
                                        {
                                            zigZag.Bottom = bodyBottom;
                                            zigZag.Percentage = percentage;
                                            //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Dominant pivot zoomed {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2} {zoomInterval.Interval.Name}");
                                        }
                                    }
                                }
                            }
                        }
                        loop += zoomInterval.Interval.Duration;
                    }
                }

                if (zigZag.Percentage <= GlobalData.Settings.Signal.ZonesDlz.MaximumZoomedPercentage)
                    break;
            }
        }

        // Is the zoomed percentage between the configured limits?
        if (ZoomedPercentageBelowMinimum(zigZag) || ZoomedPercentageAboveMaximum(zigZag))
        {
            zigZag.IsValid = false;
            return; // (mark the point as not dominant + exit)
        }
    }

    /// <summary>
    /// Marks the dominant pivots. Judging a triple means looking at the candidate in the middle and
    /// the pivot that confirms it, so a pivot can only be judged once its confirmer has arrived.
    /// <para>
    /// Incremental callers pass <paramref name="committedUpTo"/> and collect the result in the two
    /// lists. The split is what makes the outcome independent of how often the caller asks:
    /// </para>
    /// <list type="bullet">
    /// <item><description><paramref name="settled"/> - the confirmer lies before
    /// <see cref="ZigZagIndicator.SettledCount"/> and can never change again, so this verdict is
    /// final. The caller records it once and never recomputes it.</description></item>
    /// <item><description><paramref name="provisional"/> - the confirmer is still in the mutable
    /// tail. The verdict may look different after the next candle, so the caller must REPLACE its
    /// previous provisional set with this one instead of adding to it.</description></item>
    /// </list>
    /// <para>
    /// A time cursor alone cannot express "this triple has been judged", which is what made the
    /// result depend on the calling rhythm. The pivot list stays mutable at its right edge, so the
    /// pivot that confirms a triple today need not be the one that confirms it tomorrow: a triple
    /// whose confirmer was still a dummy got skipped and then fell behind the cursor forever
    /// (missing), and a triple whose confirmer changed got judged twice (duplicate). Inside the
    /// settled region that ambiguity does not exist, so there a time cursor is exact - and outside
    /// it nothing is remembered at all. See ZoneDlzIncrementalTests.
    /// </para>
    /// </summary>
    /// <returns>
    /// The confirming pivot time of the last SETTLED triple this pass judged, which is where the
    /// caller's cursor belongs. Unchanged from <paramref name="committedUpTo"/> when this pass
    /// judged nothing settled. Callers doing a full calculation ignore it.
    /// </returns>
    public static async Task<CandleTime?> CalculateDlzAsync(AddTextEvent? sender,
        CryptoSymbol symbol, CryptoInterval interval, ZigZagIndicator indicator,
        ZoneCandleWindows loadedCandlesInMemory,
        CandleTime? committedUpTo = null, List<ZigZagResult>? settled = null,
        List<ZigZagResult>? provisional = null)
    {
        //GlobalData.AddTextToLogTab($"{data.Symbol.Name} Calculating newZones");

        // A local, deliberately. This method awaits inside the loop, so the continuation can resume
        // on a different thread - which rules out anything static, ThreadStatic included.
        CandleTime? lastSettledConfirmer = committedUpTo;
        int settledCount = indicator.SettledCount;
        ZigZagResult? previous = null;
        ZigZagResult? previous2 = null;
        for (int index = 0; index < indicator.ZigZagList.Count; index++)
        {
            ZigZagResult zigZag = indicator.ZigZagList[index];
            if (previous != null && previous2 != null && !zigZag.Dummy)
            {
                // Whether this verdict can still change. The confirmer decides, not the candidate:
                // the candidate is by definition older, so a settled confirmer implies a settled
                // candidate, while the reverse says nothing.
                bool isSettled = index < settledCount;

                // Already judged in an earlier call, and settled, so the answer cannot have moved.
                // Keep walking though - the sliding window has to stay intact.
                if (isSettled && committedUpTo != null && zigZag.Candle.OpenTime <= committedUpTo)
                {
                    PipelineProfiler.RecordDlzPivot(skipped: true, judged: false);
                    previous2 = previous;
                    previous = zigZag;
                    continue;
                }

                sender?.Invoke($"Calculating dlz zones {symbol.Exchange.Name} {symbol.Name} {interval.Name} {zigZag.Candle.Date}");

                bool judged = false;

                // Check: a dominant Low leading to a new Higher High
                if (zigZag.PointType == 'H' && previous.PointType == 'L' && previous2.PointType == 'H' && previous2.Value < zigZag.Value)
                {
                    long profZoomStart = Stopwatch.GetTimestamp();
                    await MakeDominantAndZoomInAsync(symbol, interval, previous,
                        Math.Max(previous.Candle.Open, previous.Candle.Close), previous.Candle.Low, loadedCandlesInMemory);
                    PipelineProfiler.RecordDlzZoom(Stopwatch.GetTimestamp() - profZoomStart);
                    // previous2 is the pivot immediately before previous, which is exactly the
                    // predecessor the grading needs to bound its look-back.
                    long profGradeStart = Stopwatch.GetTimestamp();
                    GradeIntro(symbol, interval, previous, previous2);
                    PipelineProfiler.RecordDlzGrade(Stopwatch.GetTimestamp() - profGradeStart);
                    (isSettled ? settled : provisional)?.Add(previous);
                    judged = true;
                }

                // Check: a dominant High leading to a new Lower Low
                if (zigZag.PointType == 'L' && previous.PointType == 'H' && previous2.PointType == 'L' && previous2.Value > zigZag.Value)
                {
                    long profZoomStart = Stopwatch.GetTimestamp();
                    await MakeDominantAndZoomInAsync(symbol, interval, previous,
                        previous.Candle.High, Math.Min(previous.Candle.Open, previous.Candle.Close), loadedCandlesInMemory);
                    PipelineProfiler.RecordDlzZoom(Stopwatch.GetTimestamp() - profZoomStart);
                    long profGradeStart = Stopwatch.GetTimestamp();
                    GradeIntro(symbol, interval, previous, previous2);
                    PipelineProfiler.RecordDlzGrade(Stopwatch.GetTimestamp() - profGradeStart);
                    (isSettled ? settled : provisional)?.Add(previous);
                    judged = true;
                }

                PipelineProfiler.RecordDlzPivot(skipped: false, judged: judged);

                // The cursor only ever advances over settled ground. Moving it into the tail would
                // freeze a verdict that is still allowed to change, which is the whole defect this
                // replaces.
                if (isSettled)
                    lastSettledConfirmer = zigZag.Candle.OpenTime;
            }
            previous2 = previous;
            previous = zigZag;
        }
        return lastSettledConfirmer;
    }


    /// <summary>
    /// Grades one dominant pivot Weak or Strong by how sharply price ran into it, looking back at
    /// most ZoneStartCandleCount candles and never past <paramref name="previousPivot"/>.
    /// <para>
    /// Called at the moment the pivot is marked dominant, and deliberately so. It used to be a
    /// second pass over the whole list with a cursor of its own, and that cursor could not be right:
    /// it graded the DOMINANT pivot, which by definition is older than the confirmer that made it
    /// dominant, so any cursor sitting at the newest candle skipped exactly the pivots that had just
    /// been marked - leaving Strength on its default of None. Folding it in means dominance and
    /// grading share one cursor and one walk, and the mismatch has nowhere left to live.
    /// </para>
    /// <para>
    /// Pure per pivot: it reads the pivot, its predecessor and the candles between them, and writes
    /// only Strength. So re-running it on the mutable tail costs nothing and changes nothing.
    /// </para>
    /// </summary>
    internal static void GradeIntro(CryptoSymbol symbol, CryptoInterval interval,
        ZigZagResult zigZag, ZigZagResult previousPivot)
    {
        // Determine if a liq. box/zone has an interesting intro
        if (!GlobalData.Settings.Signal.ZonesDlz.ZoneStartApply)
            return;

        decimal boxLimit;
        if (zigZag.PointType == 'L')
            boxLimit = zigZag.Bottom;
        else
            boxLimit = zigZag.Top;
        decimal price = boxLimit;

        var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CandleTime max = zigZag.Candle.OpenTime;
        CandleTime min = max - GlobalData.Settings.Signal.ZonesDlz.ZoneStartCandleCount * interval.Duration;
        // Is it on the right of the last zigzag point?
        if (min < previousPivot.Candle.OpenTime)
            min = previousPivot.Candle.OpenTime;
        while (min < max)
        {
            if (symbolInterval.CandleList.TryGetValue(min, out CryptoCandle candle))
            {
                if (zigZag.PointType == 'L')
                {
                    if (candle.High > price)
                        price = candle.High;
                }
                else
                {
                    if (candle.Low < price)
                        price = candle.Low;
                }
            }
            min += interval.Duration;
        }

        double perc = (double)(100 * Math.Abs(boxLimit - price) / Math.Min(boxLimit, price));
        //zigZag.NiceIntro = $"\n\r(intro {perc:N2}%)";
        //zigZag.NiceIntro += $"\n\r{price}\n\r{boxLimit}";

        if (perc <= GlobalData.Settings.Signal.ZonesDlz.ZoneStartPercentage)
            zigZag.Strength = CryptoZoneStrength.Weak;
        else
            zigZag.Strength = CryptoZoneStrength.Strong;
    }


    public static async Task<(CandleTime minDate, CandleTime maxDate)> LoadHistoricCandles(CryptoSymbol symbol, CryptoInterval interval,
        ZoneCandleWindows loadedCandlesInMemory)
    {
        // Determine the period (using the candlecount). One depth for the whole engine - see
        // CandleTools.CandleCountFetch for why the zones no longer have one of their own.
        int candleFetchCount = CandleTools.CandleCountFetch;
        CandleTime maxDate = CandleTime.AlignFromDateTime(GlobalData.Clock.UtcNow, interval.Duration);
        CandleTime minDate = maxDate - candleFetchCount * interval.Duration;
        // One candle further back than the window itself, and deliberately not by widening minDate:
        // that one bounds the pivot calculation and the incremental cursor check, while this only
        // bounds what is read. MakeDominantAndZoomInAsync looks at the candle BEFORE a dominant pivot
        // to borrow its wick, so for the oldest pivot in the list - which can sit exactly on minDate -
        // it reaches one candle outside the window. That candle used to be there because the read
        // pulled in the whole series regardless of what was asked; now it has to be asked for.
        await ZoneCandleEngine.FetchFrom(loadedCandlesInMemory, symbol, interval,
            minDate - interval.Duration, candleFetchCount + 1);

#if DEBUG
        var count = symbol.GetSymbolInterval(interval).CandleList.Count;
        GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} dlz from {minDate.ToLocalTime()} .. {maxDate.ToLocalTime()} candles = {count}");
#endif
        return (minDate, maxDate);
    }


    public static async Task CalculatePivots(CryptoSymbol symbol, CryptoInterval interval,
        CandleTime minDate, CandleTime maxDate, TrendZigZagIndicatorList trendZigZagIndicatorList)
    {
        await symbol.Data.CandleLock.WaitAsync();
        try
        {
            var candleList = symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList;

            // This list is shared with TrendCalculator and with the candle loading paths, and they do
            // not share a lock with this method - see the remarks on TrendZigZagIndicatorList for the
            // exception that used to cause. Values on a concurrent dictionary is already a snapshot,
            // so an indicator added or removed while this loop runs cannot disturb it. One added
            // halfway is fed by whoever added it, so missing it in this pass costs nothing.
            var indicators = trendZigZagIndicatorList.Values;

            // Calculate "indicators"
            CandleTime loop = minDate;
            while (loop <= maxDate)
            {
                if (candleList.TryGetValue(loop, out CryptoCandle candle))
                {
                    foreach (var trendZigZagIndicator in indicators)
                    {
                        trendZigZagIndicator.Calculate(candle, true);
                    }
                }
                loop += interval.Duration;
            }


            // Remember the last swing point for the automatic zone calculation
            CryptoSymbolInterval symbolIntervalData = symbol.Data.Get(interval.IntervalPeriod);

            var trend = GlobalData.Settings.Signal.ZonesDlz.ZigZag;
            var indicator = trendZigZagIndicatorList[(trend.TrendType, trend.UseHighLow)];
            if (indicator.LastSwingPoint != null)
                symbolIntervalData.Dlz.TimeLastSwingPoint = indicator.LastSwingPoint.Candle.OpenTime;
            // Writes the swing values, and re-seeds the trigger range only when they moved. Assigning
            // the two separately here is what made the same symbol queue a recalculation every hour
            // and end on nothing - see the remarks on CryptoSymbolIntervalZoneCalc.
            symbolIntervalData.Dlz.ApplySwingRange(
                indicator.LastSwingLow != null ? (decimal)indicator.LastSwingLow.Value : null,
                indicator.LastSwingHigh != null ? (decimal)indicator.LastSwingHigh.Value : null);

            // Same snapshot as above, for the same reason.
            foreach (var indicatorX in indicators)
                indicatorX.FinishBatch();
        }
        finally
        {
            symbol.Data.CandleLock.Release();
        }

    }

    internal static HashSet<CryptoZone> CheckAndMarkBrokenZones(CryptoInterval interval,
        CryptoCandleList candleList, CryptoSymbolIntervalZones zones,
        CandleTime? afterTime = null)
    {
        HashSet<CryptoZone> modified = [];

        CandleTime? startTime;
        if (afterTime != null)
        {
            // Incremental: only check candles we haven't seen yet
            startTime = afterTime.Value + interval.Duration;
        }
        else
        {
            // Full historical: start from the oldest open zone
            var oldest1 = zones.LongOpen.MinBy(z => z.OpenTime);
            var oldest2 = zones.ShortOpen.MinBy(z => z.OpenTime);
            startTime = null;
            if (oldest1 != null) startTime = oldest1.OpenTime;
            if (oldest2 != null && (startTime == null || oldest2.OpenTime < startTime))
                startTime = oldest2.OpenTime;
        }

        // TryGetFirstAndLastKey rather than Count > 0 plus Keys.Last(): Keys is the inherited
        // SortedDictionary collection and enumerates outside CryptoCandleList's lock, which throws
        // as soon as the kline stream adds a candle mid-scan. Same hole as the one that aborted
        // BulkCalculateCandles on Okx Futures. The first key is not needed here.
        if (startTime != null && candleList.TryGetFirstAndLastKey(out _, out CandleTime loopEnd))
        {
            // Loosened invalidation: a wick into the zone counts as a test (TouchCount++),
            // only a body-close through the far side or reaching MaxTouches closes the zone.
            // See ZoneInvalidation for the theoretical background.
            int maxTouches = GlobalData.Settings.Signal.ZonesDlz.MaxTouches;

            CandleTime loop = startTime.Value;

            while (loop <= loopEnd)
            {
                if (candleList.TryGetValue(loop, out CryptoCandle candle))
                {
                    List<CryptoZone> closed = [];
                    //CheckBrokenZones(zones, candle, touched);

                    // LongOpen sorted descending by Top. Stop as soon as candle.Low >= zone.Top
                    if (zones.LongOpen.Count > 0) //&& candle.Low < zones.LongOpen[0].Top
                    {
                        foreach (var zone in zones.LongOpen)
                        {
                            if (candle.Low >= zone.Top)
                                break;
                            if (candle.OpenTime >= zone.OpenTime + zone.Interval.Duration)
                            {
                                //if (candle.High <= zone.Bottom)
                                //    touched.Add(zone);
                                //if (candle.Low < zone.Top)
                                //    touched.Add(zone);
                                int oldTouchCount = zone.TouchCount;
                                bool oldMitigated = zone.IsMitigated;
                                if (ZoneInvalidation.ApplyToCandle(zone, candle, interval, maxTouches)
                                    && zone.CloseTime == candle.OpenTime + interval.Duration)
                                {
                                    closed.Add(zone);
                                    modified.Add(zone);
                                }
                                else if (zone.TouchCount != oldTouchCount || zone.IsMitigated != oldMitigated)
                                {
                                    modified.Add(zone);
                                }
                            }
                        }
                    }


                    // ShortOpen sorted ascending by Bottom. Stop as soon as candle.High <= zone.Bottom
                    if (zones.ShortOpen.Count > 0) //&& candle.High > zones.ShortOpen[0].Bottom
                    {
                        foreach (var zone in zones.ShortOpen)
                        {
                            if (candle.High <= zone.Bottom)
                                break;
                            if (candle.OpenTime >= zone.OpenTime + zone.Interval.Duration)
                            {
                                // Close old invalid zone without notifications..
                                //if (candle.Low >= zone.Top)
                                //    touched.Add(zone);
                                //if (candle.High > zone.Bottom)
                                //    touched.Add(zone);
                                int oldTouchCount = zone.TouchCount;
                                bool oldMitigated = zone.IsMitigated;
                                if (ZoneInvalidation.ApplyToCandle(zone, candle, interval, maxTouches)
                                    && zone.CloseTime == candle.OpenTime + interval.Duration)
                                {
                                    closed.Add(zone);
                                    modified.Add(zone);
                                }
                                else if (zone.TouchCount != oldTouchCount || zone.IsMitigated != oldMitigated)
                                {
                                    modified.Add(zone);
                                }
                            }
                        }
                    }


                    // Move newly closed zones to the closed list; CloseTime was set by ApplyToCandle
                    // and is picked up by AddZonesToInternalLists for DB save.
                    foreach (var zone in closed)
                    {
                        if (zone.Side == CryptoTradeSide.Long)
                        {
                            zones.LongOpen.Remove(zone);
                            zones.LongClosed.Add(zone);
                        }
                        else
                        {
                            zones.ShortOpen.Remove(zone);
                            zones.ShortClosed.Add(zone);
                        }
                    }

                    // Early exit when all zones are closed
                    if (zones.LongOpen.Count + zones.ShortOpen.Count == 0)
                        break;
                }
                loop += interval.Duration;
            }
        }

        return modified;
    }


    public static async Task CalculateZonesAsync(AddTextEvent? sender,
        CryptoSymbol symbol, CryptoInterval interval,
        ZoneCandleWindows loadedCandlesInMemory)
    {
        // Phase timers for the DLZ carve-out. Plain locals: one recalculation runs start to finish
        // on one thread, and the totals are handed to the profiler in a single call at the end.
        long profFeed = 0, profJudge = 0, profMerge = 0, profBroken = 0;
        bool profIncremental = false;
        long profMark = Stopwatch.GetTimestamp();

        try
        {
            var (minDate, maxDate) = await LoadHistoricCandles(symbol, interval, loadedCandlesInMemory);

            // Mark the dominant lows or highs
            //if (forceCalculation)
            {
                CryptoSymbolData symbolData = symbol.Data;
                var symbolIntervalData = symbolData.Get(interval.IntervalPeriod);
                CryptoSymbolIntervalZones zones = symbolIntervalData.Dlz.Zones;

                //if (symbol.Name == "1000PEPEUSDT")
                //    GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} " +
                //        $"{minDate.ToLocalTime():yyyy-MM-dd HH:mm} .. {maxDate.ToLocalTime():yyyy-MM-dd HH:mm} " +
                //        $"dlz zones long = {zones.LongOpen.Count} " +
                //        $"dlz zones short = {zones.ShortOpen.Count} ");

                // Reuse the shared cached ZigZag indicator across calls (one per queue-drain) instead of
                // rebuilding it from the full [minDate, maxDate] window every time — same hub instance
                // as TrendCalculator.CalculateBothAsync uses.
                var trend = GlobalData.Settings.Signal.ZonesDlz.ZigZag;
                var dlzCacheKey = (trend.TrendType, trend.UseHighLow);
                TrendZigZagIndicatorList trendZigZagIndicatorList = symbolIntervalData.ZigZagIndicators;
                if (!trendZigZagIndicatorList.TryGetValue(dlzCacheKey, out ZigZagIndicator? trendZigZagIndicator)
                    || trendZigZagIndicator.LastFedCandleTime == null)
                {
                    trendZigZagIndicator = new(trend.TrendType, trend.UseHighLow, 1.0);
                    trendZigZagIndicatorList[dlzCacheKey] = trendZigZagIndicator;
                    await CalculatePivots(symbol, interval, minDate, maxDate, trendZigZagIndicatorList);
                    trendZigZagIndicator.LastFedCandleTime = maxDate;
                }
                else if (trendZigZagIndicator.LastFedCandleTime!.Value < maxDate)
                {
                    CandleTime feedFrom = trendZigZagIndicator.LastFedCandleTime.Value + interval.Duration;
                    await CalculatePivots(symbol, interval, feedFrom, maxDate, trendZigZagIndicatorList);
                    trendZigZagIndicator.LastFedCandleTime = maxDate;
                }
                // else: already up to date — reuse the indicator as is, no feed needed.
                profFeed += Stopwatch.GetTimestamp() - profMark;
                profMark = Stopwatch.GetTimestamp();

                // How far back this calculation can speak with authority. Deliberately the oldest
                // PIVOT and not minDate. Since 2026-08-22 both follow CandleTools.CandleCountFetch
                // so they cannot disagree, but the pivots remain the honest answer to "could this
                // zone have been produced": they are what the calculation actually holds, while
                // minDate is what it asked for. Trimming, a restart or a gap can leave the list
                // shorter than the window, and then only the pivots know.
                //
                // No pivots at all means nothing could be produced, so nothing may be deleted either;
                // minDate is then only a fallback that keeps the previous behaviour.
                CandleTime judgedFrom = trendZigZagIndicator.OldestPivotTime ?? minDate;

                if (symbolIntervalData.Dlz.ProcessedCandleMarker != null && symbolIntervalData.Dlz.ProcessedCandleMarker.Value >= minDate)
                {
                    // ── Incremental path: only process new pivots since the cursor ──
                    profIncremental = true;
                    var cursor = symbolIntervalData.Dlz.ProcessedCandleMarker;
                    int openLongBefore = symbolIntervalData.Dlz.Zones.LongOpen.Count;
                    int openShortBefore = symbolIntervalData.Dlz.Zones.ShortOpen.Count;

                    // Age the committed store on the RIGHT edge of a zone, the same rule the
                    // reconciliation below now uses: a zone leaves once it has been broken and that
                    // break has itself scrolled out of the window. An open zone never ages out, however
                    // old its pivot is - it is still tradeable, and it is exactly the kind of level that
                    // held for months. Ageing on OpenTime (what this did before) pruned the history back
                    // to the candle window on every full calculation instead.
                    if (symbolIntervalData.Dlz.CommittedZones.Count > 0)
                        symbolIntervalData.Dlz.CommittedZones.RemoveAll(
                            zone => zone.CloseTime != null && zone.CloseTime.Value < minDate);
                    // The pivots this pass judged, split by whether that verdict can still change.
                    // Filtering the whole list on candle time afterwards would reintroduce the very
                    // mismatch the split fixes: a candidate pivot is always older than the confirmer
                    // that made it dominant, so a time filter drops exactly the zones that were just
                    // found. See ZoneDlzIncrementalTests.
                    List<ZigZagResult> settledPivots = [];
                    List<ZigZagResult> provisionalPivots = [];
                    symbolIntervalData.Dlz.CommittedPivotMarker = await CalculateDlzAsync(
                        sender, symbol, interval, trendZigZagIndicator, loadedCandlesInMemory,
                        symbolIntervalData.Dlz.CommittedPivotMarker, settledPivots, provisionalPivots);
                    profJudge += Stopwatch.GetTimestamp() - profMark;
                    profMark = Stopwatch.GetTimestamp();

                    // Settled verdicts are added to the store once and never recomputed. The ones
                    // added by THIS pass are kept apart: they are the only settled zones the merge
                    // below still has to hear about.
                    int committedBefore = symbolIntervalData.Dlz.CommittedZones.Count;
                    CreateZonesFromZigZag(symbol, interval, settledPivots,
                        symbolIntervalData.Dlz.CommittedZones);
                    List<CryptoZone> newlyCommitted = symbolIntervalData.Dlz.CommittedZones.GetRange(
                        committedBefore, symbolIntervalData.Dlz.CommittedZones.Count - committedBefore);

                    // The tail is rebuilt from scratch every pass. Adding to it instead would leave
                    // a copy behind of every zone that was judged twice, and would keep a zone that
                    // the next candle turned out to disprove. Together with the committed store this
                    // is exactly the set a full calculation produces.
                    List<CryptoZone> provisionalZones = [];
                    CreateZonesFromZigZag(symbol, interval, provisionalPivots, provisionalZones);
                    KeepLiveInstanceWhereUnchanged(symbolIntervalData.Dlz.Zones, provisionalZones);

                    List<CryptoZone> newZones = [.. newlyCommitted, .. provisionalZones];

                    // Only the part of the zone set this pass could have changed goes through the
                    // merge. Everything older rests on a settled pivot: the calculation would
                    // produce it identically and the merge would call it Untouched, so submitting
                    // it made the cost of a recalculation scale with how many zones EXIST instead
                    // of with how many changed - which is what an incremental calculation is for.
                    //
                    // The boundary is where the mutable tail starts, pulled back to cover the zones
                    // that just became settled (their pivot sits right before the tail). Zones older
                    // than that are carried across untouched: same objects, no index, no compare, no
                    // database write.
                    CandleTime mergeFrom = trendZigZagIndicator.TailStartTime ?? judgedFrom;
                    foreach (CryptoZone zone in newZones)
                    {
                        if (zone.OpenTime < mergeFrom)
                            mergeFrom = zone.OpenTime;
                    }

                    int weakNew = newZones.Count(z => z.Strength == CryptoZoneStrength.Weak);
                    {
                        DatabaseStatistics statistics = new();
                        CryptoSymbolIntervalZones finalZones = new();
                        SortedList<(CryptoTradeSide, CandleTime?, decimal, decimal), CryptoZone> oldZones = [];
                        SplitOnMergeBoundary(zones, mergeFrom, finalZones, oldZones, statistics);

                        ZoneTools.AddZonesToInternalLists(finalZones, oldZones, newZones, statistics);
                        // Zones whose pivot has aged out are carried instead of deleted; this pass
                        // could not have produced them, so their absence says nothing.
                        ZoneTools.DeleteRemainingZones(oldZones, statistics, finalZones, judgedFrom);
                        symbolIntervalData.Dlz.Zones = finalZones;
                    }

                    profMerge += Stopwatch.GetTimestamp() - profMark;
                    profMark = Stopwatch.GetTimestamp();

                    // Incremental broken-zone check: only scan candles after the cursor
                    // to avoid double-counting touches (TouchCount is not idempotent)
                    var modifiedZones = CheckAndMarkBrokenZones(interval, symbolIntervalData.CandleList,
                        symbolIntervalData.Dlz.Zones, afterTime: cursor);
                    profBroken += Stopwatch.GetTimestamp() - profMark;
                    profMark = Stopwatch.GetTimestamp();
                    foreach (var zone in modifiedZones)
                    {
                        if (zone.Id > 0)
                            GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                    }

                    int brokenCount = modifiedZones.Count(z => z.CloseTime != null);
                    // Diagnostics, deliberately switched off on 18-08-2026 - it was 40% of the log file. Left in place to switch back on.
                    // Switched back ON on 22-08-2026 for one night. This is the other half of the
                    // measurement: SignalPrepare logs that a recalculation was triggered, this line
                    // says what came out of it. A run of these ending on newZones=0, broken=0 for the
                    // same symbol is the waste the trigger split was meant to remove.
                    // SWITCH THIS BACK OFF after the measurement.
                    GlobalData.AddTextToLogTab($"DLZ diag {symbol.Name} {interval.Name} incremental: " +
                        $"pivots={trendZigZagIndicator.ZigZagList.Count}, newZones={newZones.Count} (weak={weakNew}), " +
                        $"broken={brokenCount}, open long {openLongBefore}→{symbolIntervalData.Dlz.Zones.LongOpen.Count}, " +
                        $"open short {openShortBefore}→{symbolIntervalData.Dlz.Zones.ShortOpen.Count}");
                }
                else
                {
                    // ── Full historical scan (first run or cursor invalidated) ──
                    // The two lists are collected here as well, and that is not decoration. The
                    // incremental pass that follows hands the merge its committed store PLUS the
                    // tail, and reconciles the result with DeleteRemainingZones. If this branch left
                    // the store empty, that very first incremental pass would submit only the tail
                    // and the reconciliation would delete every zone this full scan just produced.
                    // Seeding it here is what makes the hand-over between the two branches safe.
                    List<ZigZagResult> fullSettledPivots = [];
                    List<ZigZagResult> fullProvisionalPivots = [];
                    symbolIntervalData.Dlz.CommittedPivotMarker = await CalculateDlzAsync(sender, symbol,
                        interval, trendZigZagIndicator, loadedCandlesInMemory,
                        committedUpTo: null, settled: fullSettledPivots, provisional: fullProvisionalPivots);
                    profJudge += Stopwatch.GetTimestamp() - profMark;
                    profMark = Stopwatch.GetTimestamp();

                    symbolIntervalData.Dlz.CommittedZones = [];
                    CreateZonesFromZigZag(symbol, interval, fullSettledPivots,
                        symbolIntervalData.Dlz.CommittedZones);

                    // Index old zones for DB merge (must happen before zones are rebuilt)
                    DatabaseStatistics statistics = new();
                    SortedList<(CryptoTradeSide, CandleTime?, decimal, decimal), CryptoZone> oldZones = [];
                    ZoneTools.CreateZoneIndex(zones.LongOpen, oldZones, statistics);
                    ZoneTools.CreateZoneIndex(zones.ShortOpen, oldZones, statistics);
                    ZoneTools.CreateZoneIndex(zones.LongClosed, oldZones, statistics);
                    ZoneTools.CreateZoneIndex(zones.ShortClosed, oldZones, statistics);

                    // Create new zones from the zigzag (local list, never visible to other threads).
                    // Built from the SAME instances that went into the committed store, not from a
                    // second walk over ZigZagList. Two walks would mint two CryptoZone objects for
                    // one pivot: the live set would hold one and the store the other, and the next
                    // incremental pass would submit its copy - which carries TouchCount 0 and no
                    // CloseTime - and the merge would treat that as a modification and overwrite the
                    // touches the live zone had collected.
                    //
                    // It also settles a smaller difference in favour of the split. Walking ZigZagList
                    // emits every pivot still carrying Dominant = true, including one left over from
                    // an earlier pass whose triple no longer qualifies; nothing clears that flag but
                    // a zoom rejection or a reuse. The split emits what THIS pass actually judged. On
                    // a fresh indicator the two are identical, which is why the tests do not separate
                    // them - it only shows on a forced rescan over a cached indicator, and there the
                    // split is the answer that matches the pivots as they now stand.
                    List<CryptoZone> provisionalZones = [];
                    CreateZonesFromZigZag(symbol, interval, fullProvisionalPivots, provisionalZones);
                    List<CryptoZone> newZones = [.. symbolIntervalData.Dlz.CommittedZones, .. provisionalZones];

                    int dominantPivots = trendZigZagIndicator.ZigZagList.Count(z => z.Dominant && !z.Dummy);
                    int totalCreated = newZones.Count;
                    int weakCreated = newZones.Count(z => z.Strength == CryptoZoneStrength.Weak);
                    int openLongCreated = newZones.Count(z => z.Side == CryptoTradeSide.Long && z.CloseTime == null);
                    int openShortCreated = newZones.Count(z => z.Side == CryptoTradeSide.Short && z.CloseTime == null);

                    // Sorted temp object for broken-zone detection — still local, not the live reference
                    CryptoSymbolIntervalZones tempZones = new();
                    foreach (var zone in newZones)
                        tempZones.Add(zone);

                    // Check broken zones before DB comparison so CloseTime is set correctly on zone objects
                    profMerge += Stopwatch.GetTimestamp() - profMark;
                    profMark = Stopwatch.GetTimestamp();
                    CheckAndMarkBrokenZones(interval, symbolIntervalData.CandleList, tempZones);
                    profBroken += Stopwatch.GetTimestamp() - profMark;
                    profMark = Stopwatch.GetTimestamp();

                    int survivedLong = tempZones.LongOpen.Count;
                    int survivedShort = tempZones.ShortOpen.Count;

                    // Merge with DB state into a fresh object, then atomically replace the live reference.
                    // Other threads always see either the old complete object or the new complete one —
                    // never a half-built list (which was the source of null holes in OrderedList).
                    CryptoSymbolIntervalZones finalZones = new();
                    ZoneTools.AddZonesToInternalLists(finalZones, oldZones, newZones, statistics);
                    // Same as the incremental branch: even a full rescan only sees the pivots it
                    // holds, so anything older is out of its authority - carry it, do not prune the
                    // history back to the window.
                    ZoneTools.DeleteRemainingZones(oldZones, statistics, finalZones, judgedFrom);
                    symbolIntervalData.Dlz.Zones = finalZones;

                    // Diagnostics, deliberately switched off on 18-08-2026 - it was 40% of the log file. Left in place to switch back on.
                    //GlobalData.AddTextToLogTab($"DLZ diag {symbol.Name} {interval.Name} full scan: " +
                    //    $"zigzag pivots={trendZigZagIndicator.ZigZagList.Count}, dominant={dominantPivots}, " +
                    //    $"zones created={totalCreated} (weak={weakCreated}), " +
                    //    $"open created long={openLongCreated} short={openShortCreated}, " +
                    //    $"survived broken-check long={survivedLong} short={survivedShort}, " +
                    //    $"final open long={finalZones.LongOpen.Count} short={finalZones.ShortOpen.Count}");

                    if (statistics.Untouched != statistics.Total)
                    {
                        var count = symbol.GetSymbolInterval(interval).CandleList.Count;
                        GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} " +
                            $"mindate = {minDate.ToLocalTime():yyyy-MM-dd HH:mm}, " +
                            $"maxdate = {maxDate.ToLocalTime():yyyy-MM-dd HH:mm} " +
                            $"Candles = {count}," +
                            $"Zones calculated ({trend.TrendType}, {trend.UseHighLow}), " +
                            $"inserted={statistics.Inserted} " +
                            $"modified={statistics.Modified} deleted={statistics.Deleted} " +
                            $"untouched={statistics.Untouched} total={statistics.Total}");
                    }
                }

                symbolIntervalData.Dlz.ProcessedCandleMarker = maxDate;

                // Whatever is left over since the last mark is reconciliation work in both branches.
                profMerge += Stopwatch.GetTimestamp() - profMark;
                PipelineProfiler.RecordDlzPhases(profFeed, profJudge, profMerge, profBroken, profIncremental);
            }


            //GlobalData.AddTextToLogTab($"{data.Symbol.Name} points={data.Indicator.PivotList.Count} fib.points={data.IndicatorFib.PivotList.Count}");
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Info($"ERROR {error}");
            GlobalData.AddErrorToLogTab($"ERROR {error}");
        }
    }

}
