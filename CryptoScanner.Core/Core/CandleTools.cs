using CryptoScanner.Core.Const;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Core;

public static class CandleTools
{
    public static decimal GetHighValue(this CryptoCandle candle, bool useHighLow)
    {
        if (useHighLow)
            return candle.High;
        else
            return Math.Max(candle.Open, candle.Close);
    }

    public static decimal GetLowValue(this CryptoCandle candle, bool useHighLow)
    {
        if (useHighLow)
            return candle.Low;
        else
            return Math.Min(candle.Open, candle.Close);
    }


    /// <summary>
    /// Add the final candle in the right interval
    /// </summary>
    public static CryptoCandle CreateCandle(CryptoSymbol symbol, CryptoInterval interval, DateTime openTime,
        decimal open, decimal high, decimal low, decimal close, decimal quoteVolume, bool isFilled = false)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList candles = symbolInterval.CandleList;

        // The decimals of the SYMBOL are the starting point, but a single candle can carry a price
        // the symbol's tick size cannot express in an int (an old print far above today's price).
        // FitTickDecimals coarsens the tick size for this one candle instead of letting the setter
        // throw; see CryptoCandle.FitTickDecimals for the case that brought this to light.
        byte tickDecimals = CryptoCandle.FitTickDecimals(symbol.PriceDecimals, open, high, low, close);

        // Add the candle if it does not exist
        CandleTime candleOpenUnix = CandleTime.AlignFromDateTime(openTime, 1);
        if (!candles.TryGetValue(candleOpenUnix, out CryptoCandle candle))
        {
            // Create the candle
            candle = new CryptoCandle
            {
                TickDecimals = tickDecimals,
                OpenTime = candleOpenUnix,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = quoteVolume,
                IsFilled = isFilled,
            };
            candles.Add(candleOpenUnix, candle);
        }
        else
        {
            // Update the candle. A real candle arriving later for the same OpenTime (e.g. a
            // repeated GetCandles fetch) clears IsFilled again, since isFilled defaults to false.
            // The decimals go first: all four prices are re-assigned right below, so changing the
            // tick size here cannot leave a value that was stored against the previous one.
            candle.TickDecimals = tickDecimals;
            candle.Open = open;
            candle.High = high;
            candle.Low = low;
            candle.Close = close;
            // Candles are getting removed are some key..
            if (quoteVolume > candle.Volume)
                candle.Volume = quoteVolume;
            candle.IsFilled = isFilled;
            candles[candleOpenUnix] = candle;
        }

        if (GlobalData.Settings.General.DebugKLineReceive && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
            ScannerLog.Logger.Info($"Create candle {candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true, true)}");

        return candle!;
    }




    /// <summary>
    /// Calculate the candle using the candles from the lower timeframes
    /// </summary>
    public static void CalculateCandleForInterval(CryptoSymbol symbol,
        CryptoInterval lowerTimeFrame, CryptoInterval higherInterval, CandleTime higherIntervalOpenTime)
    {
        if (higherIntervalOpenTime % higherInterval.Duration != 0)
            throw new Exception("CalculateCandleForInterval time not correct..");
        if (higherInterval.Duration % lowerTimeFrame.Duration != 0)
            throw new Exception("CalculateCandleForInterval interval not matching..");

        // The higher timeframe and starttime + closetime
        CandleTime higherIntervalCloseTime = higherIntervalOpenTime + higherInterval.Duration;

        // The lower timeframe and starttime + closetime
        CryptoSymbolInterval lowerSymbolInterval = symbol.GetSymbolInterval(lowerTimeFrame.IntervalPeriod);
        CryptoCandleList lowerIntervalCandles = lowerSymbolInterval.CandleList;
        uint expectedLowerIntervalCandleCount = higherInterval.Duration / lowerTimeFrame.Duration;
        CandleTime lowerIntervalOpenTime = higherIntervalCloseTime - expectedLowerIntervalCandleCount * lowerTimeFrame.Duration;


        decimal open = 0;
        decimal high = decimal.MinValue;
        decimal low = decimal.MaxValue;
        decimal close = 0;
        decimal volume = 0;
        // True when at least one of the underlying lower-timeframe candles was itself gap-filled -
        // the aggregate then rests on synthesized data too, so it is just as unreliable.
        bool anyFilled = false;

        // The candle  in the higher timeframe contains x candles from the lower timeframe
        int candleCount = 0;
        bool firstCandle = true;
        CandleTime loop = lowerIntervalOpenTime;
        while (loop < higherIntervalCloseTime)
        {
            //#if DEBUG
            //DateTime loopDebug = GetUnixDate(loop);
            //#endif
            if (lowerIntervalCandles.TryGetValue(loop, out CryptoCandle candle))
            {
                // Open
                if (firstCandle)
                {
                    open = candle.Open;
                    firstCandle = false;
                }
                if (candle.High > high)
                    high = candle.High;
                if (candle.Low < low)
                    low = candle.Low;
                close = candle.Close;

                // Dat gaat  fout als niet de hele "periode" aangeboden wordt
                volume += candle.Volume;
                candleCount++;
                if (candle.IsFilled)
                    anyFilled = true;
            }
            //else break; // the lower interval is not complete, stop? (see remarks) --> volume fix?

            loop += lowerTimeFrame.Duration;
        }


        // If there was some data add candle to the higher timeframe list if needed
        if (candleCount == expectedLowerIntervalCandleCount)
        {
            // Create the higher timeframe candle (it will be added later when its data is fully calculated)
            CreateCandle(symbol, higherInterval, higherIntervalOpenTime.ToDateTime(), open, high, low, close, volume, anyFilled);
            UpdateCandleFetched(symbol, higherInterval);
            //GlobalData.Logger.Info(higherIntervalCandle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true, true));
        }
        //else
        //    GlobalData.AddTextToLogTab($"Unable to calculate a full candle {symbol.Name} {interval.Name} {GetUnixDate(higherIntervalOpenTime)} using the {lowerTimeFrame.Name}");
    }



    public static async Task<CryptoCandle> Process1mCandleAsync(CryptoSymbol symbol, DateTime openTime,
        decimal open, decimal high, decimal low, decimal close, decimal quoteVolume, bool isFilled = false)
    {
        // Guard against empty/invalid candles (any OHLC <= 0). A no-trade minute or a price that rounds
        // to 0 (too-small PriceDecimals) would otherwise be stored as an all-zero candle and corrupt the
        // higher timeframes. Skip it (don't store, don't touch LastPrice) and return the last valid candle;
        // the missing minute is back-filled as a flat candle (previous close) by BulkAddMissingCandles.
        // Central chokepoint, so every exchange's SubscriptionKLineTicker is covered by this one check.
        if (open <= 0 || high <= 0 || low <= 0 || close <= 0)
            return symbol.GetSymbolInterval(GlobalData.IntervalList[0].IntervalPeriod).CandleList.LastCandle;

        await symbol.Data.CandleLock.WaitAsync();
        try
        {
            // Last known price (and the price ticker will adjust)
            symbol.LastPrice = close;

            // Process the single 1m candle
            CryptoCandle candle = CreateCandle(symbol, GlobalData.IntervalList[0], openTime, open, high, low, close, quoteVolume, isFilled);
            // Update administration of the last processed candle
            UpdateCandleFetched(symbol, GlobalData.IntervalList[0]);

            // Calculate the higher timeframes
            CandleTime candle1mCloseTime = candle!.OpenTime + 1;
            foreach (CryptoInterval interval in GlobalData.IntervalList)
            {
                if (interval.ConstructFrom != null && candle1mCloseTime % interval.Duration == 0)
                {
                    var (targetComplete, targetStart) = IntervalTools.StartOfIntervalCandle3(candle.OpenTime, interval.ConstructFrom.Duration, interval.Duration);
                    if (targetComplete)
                    {
                        // Calculate the candle in the higher timeframe using the candles from the lower timeframes
                        CalculateCandleForInterval(symbol, interval.ConstructFrom, interval, targetStart);
                        // Update administration of the last processed candle
                        UpdateCandleFetched(symbol, interval);
                    }
                }
            }

            return candle;
        }
        finally
        {
            symbol.Data.CandleLock.Release();
        }
    }

    /// <summary>
    /// Generalized version of <see cref="Process1mCandleAsync"/> that accepts any base interval.
    /// Used by the emulator when replaying at a coarser resolution (e.g. 5m) for speed. The candle
    /// is inserted into <paramref name="baseInterval"/>'s CandleList, and every higher interval
    /// whose close time aligns is synthesised from its ConstructFrom chain — exactly as the 1m
    /// variant does, but starting higher in the tree.
    /// </summary>
    public static async Task<CryptoCandle> ProcessBaseCandleAsync(CryptoSymbol symbol, CryptoInterval baseInterval,
        DateTime openTime, decimal open, decimal high, decimal low, decimal close, decimal quoteVolume)
    {
        if (open <= 0 || high <= 0 || low <= 0 || close <= 0)
            return symbol.GetSymbolInterval(baseInterval.IntervalPeriod).CandleList.LastCandle;

        await symbol.Data.CandleLock.WaitAsync();
        try
        {
            symbol.LastPrice = close;

            CryptoCandle candle = CreateCandle(symbol, baseInterval, openTime, open, high, low, close, quoteVolume);
            UpdateCandleFetched(symbol, baseInterval);

            CandleTime candleCloseTime = candle!.OpenTime + baseInterval.Duration;
            foreach (CryptoInterval interval in GlobalData.IntervalList)
            {
                if (interval.Duration <= baseInterval.Duration)
                    continue;
                if (interval.ConstructFrom != null && candleCloseTime % interval.Duration == 0)
                {
                    var (targetComplete, targetStart) = IntervalTools.StartOfIntervalCandle3(candle.OpenTime, interval.ConstructFrom.Duration, interval.Duration);
                    if (targetComplete)
                    {
                        CalculateCandleForInterval(symbol, interval.ConstructFrom, interval, targetStart);
                        UpdateCandleFetched(symbol, interval);
                    }
                }
            }

            return candle;
        }
        finally
        {
            symbol.Data.CandleLock.Release();
        }
    }


    public static void BulkAddMissingCandles(CryptoSymbol symbol, CryptoInterval interval)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.LastCandleSynchronized == null)
            return;
        CryptoCandleList candleList = symbolInterval.CandleList;
        if (candleList.Count == 0)
            return;

        if (!candleList.TryGetFirstCandle(out CryptoCandle realCandle))
            return;
        CandleTime loop = realCandle.OpenTime;
        //GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Debug missing candle " + CandleTools.GetUnixDate(realCandle.OpenTime).ToLocalTime());

        while (loop < symbolInterval.LastCandleSynchronized)
        {
            // TODO: Replace with CandleTools.CreateCandle? (or optimize)
            if (candleList.TryGetValue(loop, out CryptoCandle candle))
            {
                realCandle = candle;
            }
            else
            {
                candle = new()
                {
                    OpenTime = loop,
                    // Same reasoning as in CreateCandle: the close being copied here comes from a
                    // candle that may itself have needed coarser decimals to fit.
                    TickDecimals = CryptoCandle.FitTickDecimals(symbol.PriceDecimals,
                        realCandle.Close, realCandle.Close, realCandle.Close, realCandle.Close),
                    Open = realCandle.Close,
                    High = realCandle.Close,
                    Low = realCandle.Close,
                    Close = realCandle.Close,
                    Volume = 0,
                    IsFilled = true,
                };
                candleList.Add(candle.OpenTime, candle);
                // Both lines below are debug output and belong INSIDE this guard. They used to sit
                // outside it, which made a single gap fill write two Info lines per candle regardless
                // of the DebugKLineReceive setting - one catch-up of five symbols put ten thousand
                // lines in the log in a single second.
                if (GlobalData.Settings.General.DebugKLineReceive && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
                {
                    ScannerLog.Logger.Info($"Debug BulkAddMissingCandles {candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true)}");
                    //realCandle = candle;

                    ScannerLog.Logger.Info($"DEBUG BulkAdd {symbol.Name} {interval.Name} First={realCandle.OpenTime.ToDateTime().ToLocalTime()} LastSync={symbolInterval.LastCandleSynchronized?.ToDateTime().ToLocalTime()} Count={candleList.Count}");
                }
            }

            loop += interval.Duration;
        }
    }


    public static void BulkCalculateCandles(CryptoSymbol symbol, CryptoInterval sourceInterval, CryptoInterval targetInterval, CandleTime fetchEndUnix)
    {
        //GlobalData.AddTextToLogTab($"{symbol.Name} BulkCalculateCandles {lowerTimeFrame.Name} {interval.Name}");
        CryptoSymbolInterval symbolSourceInterval = symbol.GetSymbolInterval(sourceInterval.IntervalPeriod);
        CryptoCandleList candleSourceInterval = symbolSourceInterval.CandleList;

        // Both boundaries in one read-locked pass. This used to be Count > 0 followed by
        // Keys.First() and Keys.Last(), and Keys is the inherited SortedDictionary collection: it
        // enumerates outside CryptoCandleList's own lock, so an Add from the kline stream bumped
        // the tree version mid-scan. That threw "Collection was modified after the enumerator was
        // instantiated" on Okx Futures (BSBUSDT 2m, 20-08-2026 17:12) and aborted the zone
        // calculation. Reading Count separately had the same hole: it can be answered from a state
        // the two key reads no longer share.
        //
        // Only the boundaries are taken under the lock. The loop below reads through TryGetValue,
        // which takes the read lock per candle and does not enumerate; holding the lock over the
        // whole aggregation would stall the stream thread for as long as it runs.
        if (candleSourceInterval.TryGetFirstAndLastKey(out CandleTime firstCandle, out CandleTime lastCandle))
        {
            var (firstComplete, firstCandleDate) = IntervalTools.StartOfIntervalCandle3(firstCandle, sourceInterval.Duration, targetInterval.Duration);
            //firstCandleDateDebug = GetUnixDate(firstCandleDate);
            if (!firstComplete || firstCandleDate < firstCandle) // Has candles targetComplete and will not be complete and will be flagged as error
            {
                firstCandleDate += targetInterval.Duration;
                //firstCandleDateDebug = GetUnixDate(firstCandleDate);
            }

            var (lastComplete, lastCandleDate) = IntervalTools.StartOfIntervalCandle3(lastCandle, sourceInterval.Duration, targetInterval.Duration);
            //lastCandleDateDebug = GetUnixDate(lastCandleDate);
            if (!lastComplete || lastCandleDate + targetInterval.Duration > fetchEndUnix) // Has candles targetComplete and will not be complete and will be flagged as error (also future candle)
            {
                lastCandleDate -= targetInterval.Duration;
                //lastCandleDateDebug = GetUnixDate(lastCandleDate);
            }

            // Bulk calculate all higher interval candles (ranging from the firstLowerCandle to the last candle)
            CandleTime loop = firstCandleDate;
            while (loop <= lastCandleDate)
            {
                CalculateCandleForInterval(symbol, sourceInterval, targetInterval, loop);
                loop += targetInterval.Duration;
            }

            UpdateCandleFetched(symbol, targetInterval);
        }
    }


    public static void UpdateCandleFetched(CryptoSymbol symbol, CryptoInterval interval)
    {
        var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.LastCandleSynchronized.HasValue)
        {
            var candles = symbolInterval.CandleList;
            if (candles.Count != 0)
            {
                while (candles.TryGetValue(symbolInterval.LastCandleSynchronized.Value, out CryptoCandle _))
                    symbolInterval.LastCandleSynchronized += interval.Duration;
            }
        }
    }


    /// <summary>
    /// Remove the outdated candle and data (from GetCandleFetchStart)
    /// </summary>
    public static async Task CleanCandleDataAsync(CryptoSymbol symbol, CandleTime? lastCandle1mCloseTime)
    {
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            if (lastCandle1mCloseTime == null || lastCandle1mCloseTime % interval.Duration == 0)
            {
                //await symbol.Lock("CleanCandleDataAsync");
                await symbol.Data.CandleLock.WaitAsync();
                try
                {
                    var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                    CandleTime startFetchUnix = GetCandleFetchStart(symbol, interval, GlobalData.Clock.UtcNow);

                    // Remove old candle objects. Both dictionaries are sorted by key, so a single
                    // forward scan collecting the stale keys (stopping at the first key that's
                    // still in range) is enough — no need to re-resolve Keys.First() from the root
                    // of the tree once per removed item.
                    //
                    // RemoveBefore does that scan under CryptoCandleList's own write lock. Doing it
                    // here over CandleList.Keys used the inherited SortedDictionary key collection,
                    // which bypasses that lock: a concurrent Add from the kline stream bumped the
                    // tree version mid-scan and threw "Collection was modified after the enumerator
                    // was instantiated". CandleLock below does not cover it - the writers take the
                    // list's own lock, not this semaphore.
                    symbolInterval.CandleList.RemoveBefore(startFetchUnix);

                    // Remove old candle indicator data
                    lock (symbolInterval.Data)
                    {
                        List<CandleTime> staleDataKeys = [];
                        foreach (CandleTime key in symbolInterval.Data.Keys)
                        {
                            if (key < startFetchUnix)
                                staleDataKeys.Add(key);
                            else
                                break;
                        }
                        foreach (CandleTime key in staleDataKeys)
                            symbolInterval.Data.Remove(key);
                    }

                    // The cached ZigZag indicators live for the whole run and are fed
                    // incrementally, so without this their PivotList/ZigZagList keep referencing
                    // CryptoCandle objects forever — keeping candles alive even after they are removed
                    // from CandleList/Data above. Trim them to the same window.
                    foreach (ZigZagIndicator indicator in symbolInterval.ZigZagIndicators.Values)
                        indicator.TrimBefore(startFetchUnix);
                }
                finally
                {
                    //symbol.Unlock("CleanCandleDataAsync");
                    symbol.Data.CandleLock.Release();
                }
            }
        }
    }


    /// <summary>
    /// Determine the (worst case) fetch date per interval
    /// currentTime = the current key + 1 minute extra
    /// </summary>
    public static void DetermineFetchStartDate(CryptoSymbol symbol)
    {
        CandleTime currentTime = CandleTime.AlignFromDateTime(GlobalData.Clock.UtcNow, 1) + 1;
        DetermineFetchStartDate(symbol, currentTime.ToDateTime());
    }


    /// <summary>
    /// Same as <see cref="DetermineFetchStartDate(CryptoSymbol)"/> but for a caller that is not
    /// working towards "now": the emulator fetches a historical window and needs the warmup to be
    /// measured backwards from the START of its run, not from the current clock.
    /// </summary>
    public static void DetermineFetchStartDate(CryptoSymbol symbol, DateTime fetchEndDate)
    {
        Dictionary<CryptoIntervalPeriod, CandleTime> fetchFrom = [];

        // Determine the (minimum) startdate per interval
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            CandleTime startTime = CandleTools.GetCandleFetchStart(symbol, interval, fetchEndDate);
            fetchFrom.Add(interval.IntervalPeriod, startTime);
        }


        // If the exchange does not support an interval than retrieve more
        // candles from a lower timeframe so we can calculate the candles.
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            CryptoInterval? lowerInterval = interval;
            while (lowerInterval != null && !symbol.Exchange.IsIntervalSupported(lowerInterval.IntervalPeriod))
            {
                lowerInterval = lowerInterval.ConstructFrom;
                if (lowerInterval != null)
                {
                    CandleTime startTime = fetchFrom[interval.IntervalPeriod];
                    if (startTime < fetchFrom[lowerInterval.IntervalPeriod])
                        fetchFrom[lowerInterval.IntervalPeriod] = startTime;
                }
            }
        }


        // Correct the startdate with what we already have collected..
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            if (symbolInterval.LastCandleSynchronized.HasValue)
            {
                CandleTime synchronizedTime = symbolInterval.LastCandleSynchronized.Value;
                // Huray, retrieve less candles, less work, less waiting key..
                if (synchronizedTime > fetchFrom[interval.IntervalPeriod])
                    fetchFrom[interval.IntervalPeriod] = synchronizedTime;
            }
            //ScannerLog.Logger.Debug($"DEBUG {symbol.Name} {interval.Name} LastCandleSynchronized={symbolInterval.LastCandleSynchronized?.ToDateTime().ToLocalTime()} NEW={fetchFrom[interval.IntervalPeriod].ToDateTime().ToLocalTime()}");
            symbolInterval.LastCandleSynchronized = fetchFrom[interval.IntervalPeriod];
        }
    }


    // We need 1 day + X hours because of the barometer calculation (we show ~5 hours in the display)
    // As soon as the barometer has been calculated it will be lowered to 1 day + 10 candles..
    private static long InitialCandleCountFetch = (24 + Constants.BarometerGraphHours) * 60;

    /// <summary>
    /// How many 1m candles the engine expects to have available. Read-only view on the same value
    /// <see cref="GetCandleFetchStart"/> uses, so the emulator's warmup/pruning depth cannot drift
    /// away from what the live scanner fetches. Anything reading back over a day of 1m history
    /// (the barometer, SignalCreate's 24-hour change) depends on this.
    /// </summary>
    public static long CandleCountFetch1m => InitialCandleCountFetch;

    public static void SetInitialCandleCountFetch(long value)
    {
        if (InitialCandleCountFetch != value)
        {
            GlobalData.AddTextToLogTab($"SetInitialCandleCountFetch from {InitialCandleCountFetch} to {value}");
            InitialCandleCountFetch = value;
        }
    }


    /// <summary>
    /// How many candles are kept per interval, for everything except 1m and the barometer. This is
    /// the single depth of the whole engine: indicators, trend, and the zones.
    /// <para>
    /// The zones used to have a depth of their own, ZonesDlz.CandleCount, and that was a promise the
    /// storage could not keep. Candles and pivots live together now - ZigZagIndicator.TrimBefore
    /// trims the pivot list on exactly this window, because a pivot holds a candle alive - so asking
    /// the zone calculation to look 3000 candles back did not give 3000 candles of zones. It gave
    /// 500 candles of pivots and 2500 candles of bookkeeping that said otherwise, and zones in that
    /// gap were deleted for the wrong reason: not "the calculation rejected it" but "the calculation
    /// never saw it". Removed on 2026-08-22; one number, no drift.
    /// </para>
    /// <para>
    /// Raising this is not free. It multiplies through every interval and every symbol at once, so
    /// it is memory across the board rather than a knob for one subject. That is exactly why the
    /// separate setting is gone instead of this being made configurable.
    /// </para>
    /// </summary>
    public const int CandleCountFetch = 500;


    public static CandleTime GetCandleFetchStart(CryptoSymbol symbol, CryptoInterval interval, DateTime currentTime)
    {
        CandleTime startTime = CandleTime.AlignFromDateTime(currentTime, 1);
        // The market barometer/climate is also a symbol so we must make an exception
        // This symbol needs a different amount of candles because of the length of the graph
        if (symbol.IsBarometerSymbol())
            startTime -= Constants.BarometerGraphHours * 60; // 60 minutes
        else
        {
            // For the 1m we need *initially* ~1 day plus some 6 or 7 hours of candles for the barometer graph
            if (interval.IntervalPeriod == CryptoIntervalPeriod.interval1m)
                startTime -= InitialCandleCountFetch * interval.Duration;
            else
                // 260 would be enough for calculating the standard indicator data.
                // But we extended that amount because of the markettrend calculation.
                //startTime = CandleTime.AlignFromDateTime(currentTime, 1) - 500 * interval.Duration;
                startTime -= CandleCountFetch * interval.Duration;
        }
        return startTime;
    }

}