using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.Dlz.Signal;

public class SignalDominantLevelShort : SignalCreateBase
{


    public override bool IsSignal()
    {
        ExtraText = "";
        bool result = false;
        decimal closestZone = 100;
        var settings = DlzPlugin.Settings;
        CryptoSymbolData symbolData = Symbol.Data;

        //GlobalData.AddTextToLogTab($"{Symbol.Name} Strategy {SignalSide} zones {symbolData.ZoneListShort.Count}");
        foreach (var intervalName in settings.IntervalList)
        {
            if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out var interval))
            {
                var symbolIntervalData = symbolData.Get(interval.IntervalPeriod);

                // Capture reference so a concurrent DlzZones swap mid-loop does not cause IndexOutOfRangeException
                var shortOpen = symbolIntervalData.Dlz.Zones.ShortOpen;
                int index = 0;
                decimal distance = 100m;
                while (index < shortOpen.Count) // sorted on Zone.Bottom (ascending)
                {
                    var zone = shortOpen[index];
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
                        // sat above the zone. Neither touched TouchCount or ReachedMidpoint, so
                        // MaxTouches never applied to a zone this path reached first - and it
                        // reached 1,793 of the 4,075 zone closures of emulator run 237. The
                        // walked-away-above case needs nothing here either: ZoneInvalidation closes
                        // a supply zone on a body close through the ceiling, which is the same event
                        // and a little earlier.
                        {
                            // Signal when the candle touched the zone..
                            // Throttled through AlarmDate, exactly as SignalDominantLevelNearShort
                            // throttles its proximity alarm: a zone now survives its first touch
                            // (MaxTouches decides that), so without this it would report on every
                            // candle of the same test.
                            if (CandleLast.Candle.High >= zone.Bottom
                                && (zone.AlarmDate == null || CandleLast.Candle.OpenTime > zone.AlarmDate?.AddHours(1)))
                            {
                                if (settings.ZoneStartApply && zone.Strength == CryptoZoneStrength.Weak)
                                {
                                    //GlobalData.AddTextToLogTab($"DLZ diag {Symbol.Name} short zone TOUCHED but WEAK (no signal): {zone.Description} {zone.Bottom}..{zone.Top}");
                                }
                                else
                                {
                                    result = true;
                                    Interval = interval; // Report different interval back
                                    zone.AlarmDate = CandleLast.Candle.OpenTime;
                                    ExtraText = $"{zone.Description} {zone.Bottom} .. {zone.Top}";
                                    //GlobalData.AddTextToLogTab($"DLZ diag {Symbol.Name} short zone SIGNAL: {zone.Description} {zone.Bottom}..{zone.Top}, price high={CandleLast.Candle.High}");
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
                                    decimal dist = 100m * (zone.Bottom - CandleLast.Candle.High) / CandleLast.Candle.Close;
                                    if (dist < distance)
                                        distance = dist;
                                }
                            }
                        }
                    }

                    // Remove closed zones
                    // Defensive only: this class no longer closes a zone, ZoneDlz.InvalidateRealtime
                    // moves them out of the open list itself. If one does turn up here it is moved to
                    // the closed list rather than dropped - both charts draw the closed list too, so a
                    // zone that is only removed vanishes from the chart instead of ending where it ended.
                    if (zone.CloseTime != null)
                    {
                        shortOpen.RemoveAt(index);
                        symbolIntervalData.Dlz.Zones.ShortClosed.Add(zone);
                        GlobalData.AddTextToLogTab($"{zone.ZoneText("Removed dlz zone")}");
                    }
                    else index++;


                    // The list is sorted on zone.bottom (ascending) and break if there are no more reachable zones (save some looping time)
                    if (CandleLast.Candle.High < zone.Bottom)
                        break;
                }

                symbolIntervalData.Dlz.ZoneDistance.BestShortZone = distance;
                if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == Symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
                    ScannerLog.Logger.Info($"{Symbol.Name} {intervalName} {symbolIntervalData.Dlz.Zones.ShortOpen.Count} short zones, closest {distance}");
                if (distance < closestZone)
                    closestZone = distance;
            }
        }

        symbolData.DlzZoneDistance.BestShortZone = closestZone;
        if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == Symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
            ScannerLog.Logger.Info($"{Symbol.Name} closest short zone {closestZone}");

        return result;
    }

}