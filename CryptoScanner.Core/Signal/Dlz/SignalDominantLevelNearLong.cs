using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Dlz;

public class SignalDominantLevelNearLong : SignalCreateBase
{


    public override bool IsSignal()
    {
        ExtraText = "";
        bool result = false;
        decimal closestZone = 100;
        CryptoSymbolData symbolData = Symbol.Data;


        //GlobalData.AddTextToLogTab($"{Symbol.Name} Strategy {SignalSide} zones {symbolData.ZoneListLong.Count}");
        foreach (var intervalName in GlobalData.Settings.Signal.ZonesDlz.IntervalList)
        {
            if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out var interval))
            {
                var symbolIntervalData = symbolData.Get(interval.IntervalPeriod);

                // Capture reference so a concurrent DlzZones swap mid-loop does not cause IndexOutOfRangeException
                var longOpen = symbolIntervalData.DlzZones.LongOpen;
                int index = 0;
                decimal distance = 100m;
                while (index < longOpen.Count) // sorted on Zone.Top (descending)
                {
                    decimal? alarmPrice = null;
                    var zone = longOpen[index];
                    if (CandleLast.Candle.OpenTime >= zone.OpenTime) // emulator..
                    {
                        // ALARM-only: zone closure is delegated to ZoneDlz.CheckAndMarkBrokenZones
                        // (during full recalc) and the realtime ZoneInvalidation path. The
                        // signal class only sets AlarmDate for proximity alarms.
                        alarmPrice = zone.Top * (100 + GlobalData.Settings.Signal.ZonesDlz.WarnPercentage) / 100;
                        if (CandleLast.Candle.Low <= alarmPrice)
                        {
                            if (zone.AlarmDate == null || CandleLast.Candle.OpenTime > zone.AlarmDate?.AddHours(1))
                            {
                                if (GlobalData.Settings.Signal.ZonesDlz.ZoneStartApply && zone.Strength == CryptoZoneStrength.Weak)
                                {
                                    // nothing
                                }
                                else
                                {
                                    result = true;
                                    Interval = interval; // Report different interval back
                                    zone.AlarmDate = CandleLast.Candle.OpenTime;
                                    GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                                    decimal dist = 100m * (CandleLast.Candle.Low - zone.Top) / CandleLast.Candle.Close;
                                    ExtraText = $"{zone.Description} {zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                                }
                            }
                        }


                        // Show the distance to the next available zone (for the symbol grid)
                        if (GlobalData.Settings.Signal.ZonesDlz.ZoneStartApply && zone.Strength == CryptoZoneStrength.Weak)
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

                    index++;

                    // The list is sorted on zone.top (descending) and break if there are no more reachable zones (save some looping time)
                    if (alarmPrice != null && alarmPrice > zone.Top)
                        break;
                }

                symbolIntervalData.DlzZoneDistance.BestLongZone = distance;
                if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == Symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
                    ScannerLog.Logger.Info($"{Symbol.Name} {intervalName} {symbolIntervalData.DlzZones.LongOpen.Count} long zones, closest {distance}");
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