using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Other;

public class SignalDominantLevelShort : SignalCreateBase
{
    public SignalDominantLevelShort(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }


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
                    var zone = symbolIntervalData.DlzZones.ShortOpen[index];
                    if (CandleLast.OpenTime >= zone.OpenTime) // emulator..
                    {
                        // Close old invalid zone without notifications..
                        if (CandleLast.Low > zone.Top)
                        {
                            zone.CloseTime = CandleLast.OpenTime;
                            GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                            GlobalData.AddTextToLogTab($"{Symbol.Name} Closed old dlz zone #{zone.Id} {zone.Side} {zone.Description}");
                        }
                        else
                        {
                            // Close if the candle touched the zone..
                            if (CandleLast.High >= zone.Bottom)
                            {
                                zone.CloseTime = CandleLast.OpenTime;
                                if (GlobalData.Settings.Signal.ZonesDlz.ZoneStartApply && zone.Strength == CryptoZoneStrength.Weak)
                                {
                                    // nothing
                                }
                                else
                                {
                                    result = true;
                                    zone.AlarmDate = CandleLast.Date;
                                    ExtraText = $"{zone.Description} {zone.Bottom} .. {zone.Top}";
                                    GlobalData.AddTextToLogTab($"{Symbol.Name} Closed dlz zone #{zone.Id} {zone.Side} {zone.Description}");
                                }
                                GlobalData.ThreadSaveObjects!.AddToQueue(zone);
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
                                    decimal dist = 100m * (zone.Bottom - CandleLast.High) / CandleLast.Close;
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
                        GlobalData.AddTextToLogTab($"{Symbol.Name} Removed dlz zone #{zone.Id} {zone.Side} {zone.Description}");
                    }
                    else index++;


                    // The list is sorted on zone.bottom (ascending) and break if there are no more reachable zones (save some looping time)
                    if (CandleLast.High < zone.Bottom)
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