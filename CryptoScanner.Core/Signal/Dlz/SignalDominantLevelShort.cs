using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Dlz;

public class SignalDominantLevelShort : SignalCreateBase
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

                // Capture reference so a concurrent DlzZones swap mid-loop does not cause IndexOutOfRangeException
                var shortOpen = symbolIntervalData.DlzZones.ShortOpen;
                int index = 0;
                decimal distance = 100m;
                while (index < shortOpen.Count) // sorted on Zone.Bottom (ascending)
                {
                    var zone = shortOpen[index];
                    if (CandleLast.Candle.OpenTime >= zone.OpenTime) // emulator..
                    {
                        // Close old invalid zone without notifications..
                        if (CandleLast.Candle.Low > zone.Top)
                        {
                            zone.CloseTime = CandleLast.Candle.OpenTime;
                            GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                            GlobalData.AddTextToLogTab($"DLZ diag {Symbol.Name} short zone BROKEN (price above): {zone.Description} {zone.Bottom}..{zone.Top}");
                            GlobalData.AddTextToLogTab($"{zone.ZoneText("Closed dlz zone")}");
                        }
                        else
                        {
                            // Close if the candle touched the zone..
                            if (CandleLast.Candle.High >= zone.Bottom)
                            {
                                zone.CloseTime = CandleLast.Candle.OpenTime;
                                if (GlobalData.Settings.Signal.ZonesDlz.ZoneStartApply && zone.Strength == CryptoZoneStrength.Weak)
                                {
                                    GlobalData.AddTextToLogTab($"DLZ diag {Symbol.Name} short zone TOUCHED but WEAK (no signal): {zone.Description} {zone.Bottom}..{zone.Top}");
                                }
                                else
                                {
                                    result = true;
                                    Interval = interval; // Report different interval back
                                    zone.AlarmDate = CandleLast.Candle.OpenTime;
                                    ExtraText = $"{zone.Description} {zone.Bottom} .. {zone.Top}";
                                    GlobalData.AddTextToLogTab($"DLZ diag {Symbol.Name} short zone SIGNAL: {zone.Description} {zone.Bottom}..{zone.Top}, price high={CandleLast.Candle.High}");
                                    GlobalData.AddTextToLogTab($"{zone.ZoneText("Closed dlz zone")}");
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
                        shortOpen.RemoveAt(index);
                        GlobalData.AddTextToLogTab($"{zone.ZoneText("Removed dlz zone")}");
                    }
                    else index++;


                    // The list is sorted on zone.bottom (ascending) and break if there are no more reachable zones (save some looping time)
                    if (CandleLast.Candle.High < zone.Bottom)
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