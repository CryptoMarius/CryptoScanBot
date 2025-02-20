using CryptoScanBot.Core.Account;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Zones;

// FVG - Fair Value Gaps

public class ZoneFvg
{
    private static CryptoZone? ScanForLongFvg(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle prev2, CryptoCandle prev, CryptoCandle candle)
    {
        // 3 green candles in a row (only the strong fvg)..
        if (candle.Close > candle.Open && prev.Close > prev.Open && prev2.Close > prev2.Open)
        {
            // with specific boundaries
            if (candle.Low > prev2.High && prev.Close > prev2.High)
            {
                double perc = 100 * (double)((candle.Low - prev2.High) / prev2.High);
                if (perc > GlobalData.Settings.Signal.ZonesFvg.MinimumPercentage)
                {
                    CryptoZone zone = new()
                    {
                        Kind = CryptoZoneKind.FairValueGap,
                        Strength = CryptoZoneStrength.None,
                        CreateTime = candle.Date.AddSeconds(interval.Duration),
                        AccountId = GlobalData.ActiveAccount!.Id,
                        Account = GlobalData.ActiveAccount,
                        ExchangeId = symbol.Exchange.Id,
                        Exchange = symbol.Exchange,
                        SymbolId = symbol.Id,
                        Symbol = symbol,
                        IntervalId = interval.Id,
                        Interval = interval,
                        OpenTime = candle.OpenTime + interval.Duration,
                        Top = candle.Low,
                        Bottom = prev2.High,
                        Side = CryptoTradeSide.Long,
                        IsValid = false,
                        Description = $"{interval.Name} {perc:N2}%",
                    };
                    return zone;
                }
            }
        }
        return null;
    }

    private static CryptoZone? ScanForShortFvg(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle prev2, CryptoCandle prev, CryptoCandle candle)
    {
        // 3 red candles in a row (only the strong fvg)..
        if (candle.Open > candle.Close && prev.Open > prev.Close && prev2.Open > prev2.Close)
        {
            // with specific boundaries
            if (candle.High < prev2.Low && prev.Close < prev2.Low)
            {
                double perc = 100 * (double)((prev2.Low - candle.High) / candle.High);
                if (perc > GlobalData.Settings.Signal.ZonesFvg.MinimumPercentage)
                {
                    CryptoZone zone = new()
                    {
                        Kind = CryptoZoneKind.FairValueGap,
                        Strength = CryptoZoneStrength.None,
                        CreateTime = candle.Date.AddSeconds(interval.Duration),
                        AccountId = GlobalData.ActiveAccount!.Id,
                        Account = GlobalData.ActiveAccount,
                        ExchangeId = symbol.Exchange.Id,
                        Exchange = symbol.Exchange,
                        SymbolId = symbol.Id,
                        Symbol = symbol,
                        IntervalId = interval.Id,
                        Interval = interval,
                        OpenTime = candle.OpenTime + interval.Duration,
                        Top = prev2.Low,
                        Bottom = candle.High,
                        Side = CryptoTradeSide.Short,
                        IsValid = false,
                        Description = $"{interval.Name} {perc:N2}%",
                    };
                    return zone;
                }
            }
        }
        return null;
    }


    // FVG (just a quick approach)
    public static void ScanForNew(CryptoAccount account, CryptoSymbol symbol, 
        CryptoInterval interval, CryptoTradeSide side, long lastCandle1mCloseTime)
    {
        // GetSymbolData the last 3 candles
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);


        if (!symbolInterval.CandleList.TryGetValue(lastCandle1mCloseTime - 1 * interval.Duration, out CryptoCandle? candle))
            return;
        if (!symbolInterval.CandleList.TryGetValue(lastCandle1mCloseTime - 2 * interval.Duration, out CryptoCandle? prev))
            return;
        if (!symbolInterval.CandleList.TryGetValue(lastCandle1mCloseTime - 3 * interval.Duration, out CryptoCandle? prev2))
            return;

        // scan voor long FVG
        if (side == CryptoTradeSide.Long)
        {
            var zone = ScanForLongFvg(symbol, interval, prev2, prev, candle);
            if (zone != null)
            {
                //GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} {CryptoTradeSide.Long} FVG {prev2.High}..{candle.Low} {zone.Description}");
                var symbolData = account!.Data.GetSymbolData(symbol.Name);
                var symbolDataInterval = symbolData.Get(interval.IntervalPeriod);
                symbolDataInterval.FvgZones.LongOpen.Add(zone);
                GlobalData.ThreadSaveObjects!.AddToQueue(zone);

            }
        }

        // scan voor short FVG
        if (side == CryptoTradeSide.Short)
        {
            var zone = ScanForShortFvg(symbol, interval, prev2, prev, candle);
            if (zone != null)
            {
                //GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} {CryptoTradeSide.Short} FVG {candle.Low}..{prev2.High} {zone.Description}");
                var symbolData = account!.Data.GetSymbolData(symbol.Name);
                var symbolDataInterval = symbolData.Get(interval.IntervalPeriod);
                symbolDataInterval.FvgZones.ShortOpen.Add(zone);
                GlobalData.ThreadSaveObjects!.AddToQueue(zone);
            }
        }
    }


    private static void InvalidateLongZones(AccountSymbolInterval symbolIntervalData, 
        OrderedList<CryptoZone> zoneList, CryptoCandle candle)
    {
        int count = zoneList.Count;
        if (count == 0)
            return;

        int index = 0;
        //if (count > 10)
        //{
        //    var s = new CryptoZone()
        //    {
        //        Kind = CryptoZoneKind.ZoneFvg,
        //        CreateTime = candle.Date,
        //        AccountId = GlobalData.ActiveAccount!.Id,
        //        Account = GlobalData.ActiveAccount,
        //        ExchangeId = symbol.Exchange.Id,
        //        Exchange = symbol.Exchange,
        //        SymbolId = symbol.Id,
        //        Symbol = symbol,
        //        OpenTime = candle.OpenTime,
        //        Top = candle.High,
        //        Bottom = candle.Low,
        //        Side = CryptoTradeSide.Long,
        //        IsValid = false,
        //    };

        //    zonesFromDatabase = zoneList.BinarySearch(s);
        //    if (zonesFromDatabase < 0)
        //        zonesFromDatabase = 0;
        //    else if (zonesFromDatabase > zoneList.Count)
        //        zonesFromDatabase = count - 1;
        //}
        //if (count > 20)
        //    zonesFromDatabase = zonesFromDatabase; // debug

        while (index < zoneList.Count) // sorted on Zone.Top descending
        {
            var zone = zoneList[index];

            // situation (A candle way above the zone) The list is sorted on top value and if there are no more reachable zonesFromDatabase break (save some looping time)
            if (candle.Low > zone.Top)
                break;

            if (zone.CloseTime == null && candle.OpenTime >= zone.OpenTime) // emulator..
            {
                if (candle.High < zone.Bottom) // situation (C candle completely below zone) close without notifications..
                {
                    zone.CloseTime = candle.OpenTime + symbolIntervalData.Interval.Duration;
                    //GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                    //GlobalData.AddTextToLogTab($"{symbol.Name} Closed old zone {zone.Id} {zone.Side} {zone.Description}");
                }
                else if (candle.Low <= zone.Top) // situation (B candle sticks into zone) Close it
                {
                    zone.CloseTime = candle.OpenTime + symbolIntervalData.Interval.Duration;
                    //GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                    //GlobalData.AddTextToLogTab($"{symbol.Name} Closed fvg zone #{zone.Id} {zone.Side} {zone.Description}");
                }
            }

            //if (zone.CloseTime != null) // remove all closed zonesFromDatabase
            //{
            //    //zoneList.RemoveAt(index);
            //    //GlobalData.ThreadSaveObjects!.AddToQueue(zone);
            //    //symbolIntervalData.FvgZones.LongClosed.Add(zone);
            //    //GlobalData.AddTextToLogTab($"{symbol.Name} Removed fvg zone #{zone.Id} {zone.Side} {zone.Description}");
            //}
            //else index++;
            index++;
        }
    }


    private static void InvalidateShortZones(AccountSymbolInterval symbolIntervalData, 
        OrderedList<CryptoZone> zoneList, CryptoCandle candle)
    {
        int count = zoneList.Count;
        if (count == 0)
            return;

        int index = 0;
        //if (count > 10)
        //{
        //    var s = new CryptoZone()
        //    {
        //        Kind = CryptoZoneKind.ZoneFvg,
        //        CreateTime = candle.Date,
        //        AccountId = GlobalData.ActiveAccount!.Id,
        //        Account = GlobalData.ActiveAccount,
        //        ExchangeId = symbol.Exchange.Id,
        //        Exchange = symbol.Exchange,
        //        SymbolId = symbol.Id,
        //        Symbol = symbol,
        //        OpenTime = candle.OpenTime,
        //        Top = candle.High,
        //        Bottom = candle.Low,
        //        Side = CryptoTradeSide.Short,
        //        IsValid = false,
        //    };

        //    zonesFromDatabase = zoneList.BinarySearch(s); - this works a bit different than expected, need more time for this
        //    if (zonesFromDatabase < 0)
        //        zonesFromDatabase = 0;
        //    else if (zonesFromDatabase > zoneList.Count)
        //        zonesFromDatabase = count - 1;
        //}
        //if (count > 20)
        //    zonesFromDatabase = zonesFromDatabase; // debug

        while (index < zoneList.Count) // sorted on Zone.Bottom asscending
        {
            var zone = zoneList[index];

            // situation (A candle way below the zone) The list is sorted on bottom value and if there are no more reachable zonesFromDatabase break (save some looping time)
            if (candle.High < zone.Bottom)
                break;

            if (zone.CloseTime == null && candle.OpenTime >= zone.OpenTime)
            {
                if (candle.Low > zone.Top) // situation (C candle completely above zone) close without notifications..
                {
                    zone.CloseTime = candle.OpenTime + symbolIntervalData.Interval.Duration;
                    //GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                    //GlobalData.AddTextToLogTab($"{symbol.Name} Closed old fvg zone #{zone.Id} {zone.Side} {zone.Description}");
                }
                else if (candle.High >= zone.Bottom) // situation (B candle sticks into zone) Close it
                {
                    zone.CloseTime = candle.OpenTime + symbolIntervalData.Interval.Duration;
                    //GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                    //GlobalData.AddTextToLogTab($"{symbol.Name} Closed fvg zone #{zone.Id} {zone.Side} {zone.Description}");
                }
            }

            //if (zone.CloseTime != null) // remove all closed zonesFromDatabase
            //{
            //    //zoneList.RemoveAt(index);
            //    //GlobalData.ThreadSaveObjects!.AddToQueue(zone);
            //    //symbolIntervalData.FvgZones.ShortClosed.Add(zone);
            //    //GlobalData.AddTextToLogTab($"{symbol.Name} Removed fvg zone #{zone.Id} {zone.Side} {zone.Description}");
            //}
            //else index++;
            index++;
        }
    }



    private static void CreateFvgZones(CryptoSymbol symbol, CryptoInterval interval, long minDate,
        AccountSymbolInterval symbolIntervalData,
        OrderedList<CryptoZone> longZones, OrderedList<CryptoZone> shortZones)
    {

        // Scan for long and short fvg (in memory)
        CryptoCandle? prev = null;
        CryptoCandle? prev2 = null;
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        foreach (var candle in symbolInterval.CandleList.Values)
        {
            if (prev2 != null && prev != null && candle.OpenTime > minDate) // Need the last 3 candles
            {
                // scan for long FVG
                if (GlobalData.Settings.Signal.ZonesFvg.ShowSignalsLong)
                {
                    var zone = ScanForLongFvg(symbol, interval, prev2, prev, candle);
                    if (zone != null)
                    {
                        //GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} {CryptoTradeSide.Long} FVG {prev2.High}..{candle.Low} {zone.Description}");
                        longZones.Add(zone); // memory only
                                             //GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                    }
                    InvalidateLongZones(symbolIntervalData, longZones, candle); // Remove closed fvg
                }

                // scan for short FVG
                if (GlobalData.Settings.Signal.ZonesFvg.ShowSignalsShort)
                {
                    var zone = ScanForShortFvg(symbol, interval, prev2, prev, candle);
                    if (zone != null)
                    {
                        //GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} {CryptoTradeSide.Short} FVG {candle.Low}..{prev2.High} {zone.Description}");
                        shortZones.Add(zone); // memory only
                                              //GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                    }
                    InvalidateShortZones(symbolIntervalData, shortZones, candle); // Remove closed fvg
                }

            }
            prev2 = prev;
            prev = candle;
        }
    }


    private static void CalculateFvg(CryptoSymbol symbol, CryptoInterval interval, long minDate, AccountSymbolInterval symbolIntervalData)
    {
        // Collect old zones
        DatabaseStatistics dbStats = new();
        SortedList<(CryptoTradeSide, long?, decimal, decimal), CryptoZone> zonesFromDatabase = [];
        ZoneTools.CreateZoneIndex(zonesFromDatabase, symbolIntervalData.FvgZones.LongOpen, dbStats);
        ZoneTools.CreateZoneIndex(zonesFromDatabase, symbolIntervalData.FvgZones.ShortOpen, dbStats);
        ZoneTools.CreateZoneIndex(zonesFromDatabase, symbolIntervalData.FvgZones.LongClosed, dbStats);
        ZoneTools.CreateZoneIndex(zonesFromDatabase, symbolIntervalData.FvgZones.ShortClosed, dbStats);
        symbolIntervalData.FvgZones.ResetZones();

        // Create new zones 
        OrderedList<CryptoZone> longZones = new(new CompareZoneDescending());
        OrderedList<CryptoZone> shortZones = new(new CompareZoneAscending());
        CreateFvgZones(symbol, interval, minDate, symbolIntervalData, longZones, shortZones);

        // Rebuild
        ZoneTools.AddZonesToInternalLists(symbolIntervalData.FvgZones, zonesFromDatabase, longZones, dbStats);
        ZoneTools.AddZonesToInternalLists(symbolIntervalData.FvgZones, zonesFromDatabase, shortZones, dbStats);
        ZoneTools.DeleteRemainingZones(zonesFromDatabase, dbStats);
    }

    public static async Task CalculateFvgZonesAsync(AddTextEvent? sender, CryptoAccount account, CryptoSymbol symbol, CryptoInterval interval,
        SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory)
    {
        if (GlobalData.Settings.Signal.ZonesFvg.ShowSignalsLong || GlobalData.Settings.Signal.ZonesFvg.ShowSignalsShort)
        {
            AccountSymbol symbolData = account!.Data.GetSymbolData(symbol.Name);
            try
            {
                if (symbol.Exchange.IsIntervalSupported(interval.IntervalPeriod))
                {
                    sender?.Invoke($"Calculating fvg zones {symbol.Exchange.Name} {symbol.Name} {interval.Name}");
                    var symbolIntervalData = symbolData.Get(interval.IntervalPeriod);

                    // Determine date boundaries
                    long unixStartUp = CandleTools.GetUnixTime(DateTime.UtcNow, 0); // todo Emulator date?
                    long fetchFrom = IntervalTools.StartOfIntervalCandle(unixStartUp, interval.Duration);
                    fetchFrom -= GlobalData.Settings.Signal.ZonesDlz.CandleCount * interval.Duration;
                    
                    // Load candles from disk
                    if (!loadedCandlesInMemory.TryGetValue(interval.IntervalPeriod, out bool _))
                        await ZoneCandleEngine.LoadCandleDataFromDiskAsync(symbol, interval);
                    loadedCandlesInMemory.TryAdd(interval.IntervalPeriod, true); // in memory, nothing changed (save alway's)
                    
                    // Load candles from the exchange
                    if (await ZoneCandleEngine.FetchFrom(symbol, interval, fetchFrom, GlobalData.Settings.Signal.ZonesDlz.CandleCount))
                        loadedCandlesInMemory[interval.IntervalPeriod] = true;

                    CalculateFvg(symbol, interval, fetchFrom, symbolIntervalData);
                }
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Info($"ERROR {error}");
                GlobalData.AddTextToLogTab($"ERROR {error}");
            }
        }
    }
}
