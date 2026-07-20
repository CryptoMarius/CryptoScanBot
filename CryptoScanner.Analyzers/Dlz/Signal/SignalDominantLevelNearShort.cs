using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.Dlz.Signal;

public class SignalDominantLevelNearShort : SignalCreateBase
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
                var shortOpen = symbolIntervalData.DlzZones.ShortOpen;
                int index = 0;
                decimal distance = 100m;
                while (index < shortOpen.Count) // sorted on Zone.Bottom (ascending)
                {
                    decimal? alarmPrice = null;
                    var zone = shortOpen[index];
                    if (CandleLast.Candle.OpenTime >= zone.OpenTime) // emulator..
                    {
                        // ALARM-only: zone closure is delegated to ZoneDlz.CheckAndMarkBrokenZones
                        // and the realtime ZoneInvalidation path. The signal class only sets
                        // AlarmDate for proximity alarms.
                        alarmPrice = zone.Bottom * (100 - settings.WarnPercentage) / 100;
                        if (CandleLast.Candle.High >= alarmPrice)
                        {
                            if (zone.AlarmDate == null || CandleLast.Candle.OpenTime > zone.AlarmDate?.AddHours(1))
                            {
                                if (settings.ZoneStartApply && zone.Strength == CryptoZoneStrength.Weak)
                                {
                                    // nothing
                                }
                                else
                                {
                                    result = true;
                                    Interval = interval; // Report different interval back
                                    zone.AlarmDate = CandleLast.Candle.OpenTime;
                                    GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                                    decimal dist = 100m * (zone.Bottom - CandleLast.Candle.High) / CandleLast.Candle.Close;
                                    ExtraText = $"{zone.Description} {zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                                }
                            }
                        }


                        // Show the distance to the next available zone (for the symbol grid)
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

                    index++;

                    // The list is sorted on zone.bottom (ascending) and break if there are no more reachable zones (save some looping time)
                    if (alarmPrice != null && alarmPrice < zone.Bottom)
                        break;
                }

                symbolIntervalData.DlzZoneDistance.BestShortZone = distance;
                if (GlobalData.Settings.General.DebugSignalCreate && (GlobalData.Settings.General.DebugSymbol == Symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
                    ScannerLog.Logger.Info($"{Symbol.Name} {intervalName} {symbolIntervalData.DlzZones.ShortOpen.Count} short zones, closest {distance}");
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