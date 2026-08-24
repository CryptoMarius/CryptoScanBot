using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.Dlz.Signal;

public class SignalDominantLevelLong : SignalCreateBase
{


    public override bool IsSignal()
    {
        ExtraText = "";
        bool result = false;
        decimal closestZone = 100;
        var settings = DlzPlugin.Settings;
        CryptoSymbolData symbolData = Symbol.Data;


        //GlobalData.AddTextToLogTab($"{Symbol.Name} Strategy {SignalSide} zones {symbolData.ZoneListLong.Count}");
        foreach (var intervalName in settings.IntervalList)
        {
            if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out var interval))
            {
                var symbolIntervalData = symbolData.Get(interval.IntervalPeriod);

                // Capture reference so a concurrent DlzZones swap mid-loop does not cause IndexOutOfRangeException
                var longOpen = symbolIntervalData.Dlz.Zones.LongOpen;
                int index = 0;
                decimal distance = 100m;
                while (index < longOpen.Count) // sorted on Zone.Top (descending)
                {
                    var zone = longOpen[index];
                    if (CandleLast.Candle.OpenTime >= zone.OpenTime) // emulator..
                    {
                        // Report-only, the same arrangement the Near variants and the FVG signal
                        // class already use: the lifetime of a zone is decided by the touch and
                        // weakening rules in ZoneInvalidation, applied on every zone-interval candle
                        // by ZoneDlz.InvalidateRealtime and again by CheckAndMarkBrokenZones during
                        // a recalculation.
                        //
                        // This class used to decide it too, and with the older, coarser rule: close
                        // the zone outright on the first touch, and close it when the whole candle
                        // sat below the zone. Neither touched TouchCount or ReachedMidpoint, so
                        // MaxTouches never applied to a zone this path reached first - and it
                        // reached 1,793 of the 4,075 zone closures of emulator run 237. The
                        // walked-away-below case needs nothing here either: ZoneInvalidation closes
                        // a demand zone on a body close through the floor, which is the same event
                        // and a little earlier.
                        {
                            // Signal when the candle touched the zone..
                            // Throttled through AlarmDate, exactly as SignalDominantLevelNearLong
                            // throttles its proximity alarm: a zone now survives its first touch
                            // (MaxTouches decides that), so without this it would report on every
                            // candle of the same test.
                            if (CandleLast.Candle.Low <= zone.Top
                                && (zone.AlarmDate == null || CandleLast.Candle.OpenTime > zone.AlarmDate?.AddHours(1)))
                            {
                                if (settings.ZoneStartApply && zone.Strength == CryptoZoneStrength.Weak)
                                {
                                    //GlobalData.AddTextToLogTab($"DLZ diag {Symbol.Name} long zone TOUCHED but WEAK (no signal): {zone.Description} {zone.Bottom}..{zone.Top}");
                                }
                                else
                                {
                                    result = true;
                                    Interval = interval; // Report different interval back
                                    zone.AlarmDate = CandleLast.Candle.OpenTime;
                                    ExtraText = $"{zone.Description} {zone.Bottom} .. {zone.Top}";
                                    //GlobalData.AddTextToLogTab($"DLZ diag {Symbol.Name} long zone SIGNAL: {zone.Description} {zone.Bottom}..{zone.Top}, price low={CandleLast.Candle.Low}");
                                    GlobalData.AddTextToLogTab($"{zone.ZoneText("Touched dlz zone")}");
                                    GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                                }
                            }


                            // Show the distance to the next available zone (for the symbol grid)
                            if (zone.CloseTime == null)
                            {
                                if (settings.ZoneStartApply && zone.Strength == CryptoZoneStrength.Weak)
                                {
                                    // nothing
                                }
                                else
                                {

                                    decimal dist = 100m * (CandleLast.Candle.Low - zone.Top) / CandleLast.Candle.Close;
                                    if (dist < distance)
                                        distance = dist;
                                }
                            }
                        }
                    }

                    // Defensive only: this class no longer closes a zone, ZoneDlz.InvalidateRealtime
                    // moves them out of the open list itself. If one does turn up here it is moved to
                    // the closed list rather than dropped - both charts draw the closed list too, so a
                    // zone that is only removed vanishes from the chart instead of ending where it ended.
                    if (zone.CloseTime != null)
                    {
                        longOpen.RemoveAt(index);
                        symbolIntervalData.Dlz.Zones.LongClosed.Add(zone);
                        GlobalData.AddTextToLogTab($"{zone.ZoneText("Removed dlz zone")}");
                    }
                    else index++;


                    // The list is sorted on zone.top (descending) and break if there are no more reachable zones (save some looping time)
                    if (CandleLast.Candle.Low > zone.Top)
                        break;
                }

                symbolIntervalData.Dlz.ZoneDistance.BestLongZone = distance;
                if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == Symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
                    ScannerLog.Logger.Info($"{Symbol.Name} {intervalName} {symbolIntervalData.Dlz.Zones.LongOpen.Count} long zones, closest {distance}");
                if (distance < closestZone)
                    closestZone = distance;
            }
        }

        symbolData.DlzZoneDistance.BestLongZone = closestZone;
        if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == Symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
            ScannerLog.Logger.Info($"{Symbol.Name} closest long zone {closestZone}");

        return result;
    }

}