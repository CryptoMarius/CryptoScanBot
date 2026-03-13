using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Other;

public class SignalDominantLevelNearShort : SignalCreateBase
{


    public override bool IsSignal()
    {
        ExtraText = "";
        bool result = false;

        decimal closestZone = 100;
        CryptoSymbolData symbolData = Symbol.Data;

        //GlobalData.AddTextToLogTab($"{Symbol.Name} Strategy {SignalSide} zones {symbolData.ZoneListShort.Count}");
        foreach (var intervalName in GlobalData.Settings.Signal.ZonesDlz.IntervalList)
        {
            if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out var interval))
            {
                var symbolIntervalData = symbolData.Get(interval.IntervalPeriod);

                int index = 0;
                decimal distance = 100m;
                while (index < symbolIntervalData.DlzZones.ShortOpen.Count) // sorted on Zone.Bottom (ascending)
                {
                    decimal? alarmPrice = null;
                    var zone = symbolIntervalData.DlzZones.ShortOpen[index];
                    if (CandleLast.Candle.OpenTime >= zone.OpenTime) // emulator..
                    {
                        // Close old invalid zone without notifications..
                        if (CandleLast.Candle.Low >= zone.Top)
                        {
                            zone.CloseTime = CandleLast.Candle.OpenTime;
                            GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                            GlobalData.AddTextToLogTab($"{zone.ZoneText("Closed dlz zone")}");
                        }
                        else
                        {
                            // If it is within a certain percentage signal it..
                            alarmPrice = zone.Bottom * (100 - GlobalData.Settings.Signal.ZonesDlz.WarnPercentage) / 100;
                            if (CandleLast.Candle.High >= alarmPrice)
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
                                        zone.AlarmDate = CandleLast.Candle.OpenTime;
                                        GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                                        decimal dist = 100m * (zone.Bottom - CandleLast.Candle.High) / CandleLast.Candle.Close;
                                        ExtraText = $"{zone.Description} {zone.Bottom} .. {zone.Top} ({dist:N2}%)";
                                    }
                                }
                            }


                            // Close if the candle touched the zone..
                            if (CandleLast.Candle.High >= zone.Bottom)
                            {
                                zone.CloseTime = CandleLast.Candle.OpenTime;
                                GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                                GlobalData.AddTextToLogTab($"{zone.ZoneText("Closed dlz zone")}");
                            }


                            // Show the distance to the next available zone (for the symbol grid)
                            if (zone.CloseTime == null)
                            {
                                if (GlobalData.Settings.Signal.ZonesDlz.ZoneStartApply && zone.Strength == CryptoZoneStrength.Weak)
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
                    if (zone.CloseTime != null)
                    {
                        symbolIntervalData.DlzZones.ShortOpen.RemoveAt(index);
                        GlobalData.AddTextToLogTab($"{zone.ZoneText("Removed dlz zone")}");
                    }
                    else index++;


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



    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Is just an alarm that the zone is becoming closeby
        return false;
    }
}