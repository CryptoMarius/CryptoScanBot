using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Core.Core;

public static class CandleTools
{
    ///// <summary>
    ///// Datum's kunnen afrondings problemen veroorzaken (op dit moment niet meer duidelijk waarom dat zo was?)
    ///// Het resultaat valt in het opgegeven interval (60, 120, etc)
    ///// NB: De candles bevatten altijd een datumtijd in UTC
    ///// </summary>
    //public static long GetUnixTime(DateTime datetime, long intervalDuration)
    //{
    //    DateTimeOffset dateTimeOffset = datetime.ToUniversalTime();
    //    long unix = dateTimeOffset.ToUnixTimeSeconds();
    //    if (intervalDuration != 0)
    //        unix -= unix % intervalDuration;
    //    return unix;
    //}

    //public static long GetUnixTime(long unixTime, long intervalDuration)
    //{
    //    long unix = unixTime;
    //    if (intervalDuration != 0)
    //        unix -= unix % intervalDuration;
    //    return unix;
    //}

    ///// <summary>
    ///// De reverse van de GetUnixTime
    ///// Oppassen: De candles bevatten altijd een datumtijd in UTC, deze moet dus ook
    ///// </summary>
    //public static DateTime GetUnixDate(long? unixDate)
    //{
    //    if (unixDate == null)
    //        throw new Exception("GetUnixDate null argument");
    //    DateTime datetime = DateTimeOffset.FromUnixTimeSeconds((long)unixDate).UtcDateTime;
    //    return datetime;
    //}

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
        decimal open, decimal high, decimal low, decimal close, decimal quoteVolume)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList candles = symbolInterval.CandleList;

        // Add the candle if it does not exist
        CandleTime candleOpenUnix = CandleTime.AlignFromDateTime(openTime, 1);
        if (!candles.TryGetValue(candleOpenUnix, out CryptoCandle? candle))
        {
            // Create the candle
            candle = new CryptoCandle
            {
                OpenTime = candleOpenUnix,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Volume = quoteVolume,
            };
            candles.Add(candleOpenUnix, candle);
        }
        else
        {
            // Update the candle
            candle!.Open = open;
            candle.High = high;
            candle.Low = low;
            candle.Close = close;
            // Candles are getting removed are some time..
            if (quoteVolume > candle.Volume)
                candle.Volume = quoteVolume;
        }

        if (GlobalData.Settings.General.DebugKLineReceive && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
            GlobalData.AddTextToLogTab($"Create candle {candle?.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true, true)}");

        if (symbolInterval.LastCandle == null || candle!.OpenTime >= symbolInterval.LastCandle.OpenTime)
            symbolInterval.LastCandle = candle;
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
        //CryptoSymbolInterval higherSymbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        //CryptoCandleList higherIntervalCandles = higherSymbolInterval.CandleList;
        //var (_, higherIntervalOpenTime) = IntervalTools.StartOfIntervalCandle3(targetOpenTime, lowerTimeFrame.Duration, interval.Duration);
        CandleTime higherIntervalCloseTime = higherIntervalOpenTime + higherInterval.Duration;
        //#if DEBUG
        //        DateTime higherIntervalOpenTimeDebug = GetUnixDate(higherIntervalOpenTime);
        //        DateTime higherIntervalCloseTimeDebug = GetUnixDate(higherIntervalCloseTime);
        //#endif

        // The lower timeframe and starttime + closetime
        CryptoSymbolInterval lowerSymbolInterval = symbol.GetSymbolInterval(lowerTimeFrame.IntervalPeriod);
        CryptoCandleList lowerIntervalCandles = lowerSymbolInterval.CandleList;
        uint expectedLowerIntervalCandleCount = higherInterval.Duration / lowerTimeFrame.Duration;
        CandleTime lowerIntervalOpenTime = higherIntervalCloseTime - expectedLowerIntervalCandleCount * lowerTimeFrame.Duration;
        //long lowerIntervalCloseTime = lowerIntervalOpenTime + lowerTimeFrame.Duration; dont need it
        //#if DEBUG
        //        DateTime candleSourceStartDebug = GetUnixDate(lowerIntervalOpenTime);
        //        //DateTime candleSourceCloseDebug = GetUnixDate(lowerIntervalCloseTime); // ????? just the first candle.. dont need it
        //#endif


        decimal open = 0;
        decimal high = decimal.MinValue;
        decimal low = decimal.MaxValue;
        decimal close = 0;
        decimal volume = 0;

        // The candle  in the higher timeframe contains x candles from the lower timeframe
        int candleCount = 0;
        bool firstCandle = true;
        CandleTime loop = lowerIntervalOpenTime;
        while (loop < higherIntervalCloseTime)
        {
            //#if DEBUG
            //DateTime loopDebug = GetUnixDate(loop);
            //#endif
            if (lowerIntervalCandles.TryGetValue(loop, out CryptoCandle? candle))
            {
                // Open
                if (firstCandle)
                {
                    open = candle!.Open;
                    firstCandle = false;
                }
                if (candle!.High > high)
                    high = candle.High;
                if (candle.Low < low)
                    low = candle.Low;
                close = candle.Close;

                // Dat gaat  fout als niet de hele "periode" aangeboden wordt
                volume += candle.Volume;
                candleCount++;
            }
            //else break; // the lower interval is not complete, stop? (see remarks) --> volume fix?

            loop += lowerTimeFrame.Duration;
        }


        // If there was some data add candle to the higher timeframe list if needed
        if (candleCount == expectedLowerIntervalCandleCount)
        {
            // Create the higher timeframe candle (it will be added later when its data is fully calculated)
            var higherIntervalCandle = CreateCandle(symbol, higherInterval, higherIntervalOpenTime.ToDateTime(), open, high, low, close, volume);
            UpdateCandleFetched(symbol, higherInterval);
            //GlobalData.Logger.Info(higherIntervalCandle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true, true));
        }
        //else
        //    GlobalData.AddTextToLogTab($"Unable to calculate a full candle {symbol.Name} {interval.Name} {GetUnixDate(higherIntervalOpenTime)} using the {lowerTimeFrame.Name}");
    }



    /// <summary>
    /// Add the 1m candle and calculate all other finished timeframes as well
    /// </summary>
    public static async Task<CryptoCandle> Process1mCandleAsync(CryptoSymbol symbol, DateTime openTime,
        decimal open, decimal high, decimal low, decimal close, decimal quoteVolume)
    {
        await symbol.Data.CandleLock.WaitAsync();
        try
        {
            // Last known price (and the price ticker will adjust)
            if (!GlobalData.BackTest)
            {
                symbol.LastPrice = close;
            }

            // Process the single 1m candle
            CryptoCandle? candle = CreateCandle(symbol, GlobalData.IntervalList[0], openTime, open, high, low, close, quoteVolume);
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

    public static void BulkAddMissingCandles(CryptoSymbol symbol, CryptoInterval interval)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList candleList = symbolInterval.CandleList;

        if (candleList.Count != 0)
        {
            CryptoCandle stickOld = candleList.Values.First();
            //GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Debug missing candle " + CandleTools.GetUnixDate(stickOld.OpenTime).ToLocalTime());
            CandleTime unixTime = stickOld.OpenTime;
            while (unixTime < symbolInterval.LastCandleSynchronized)
            {
                // TODO: Replace with CandleTools.CreateCandle? (or optimize)
                if (!candleList.TryGetValue(unixTime, out CryptoCandle? candle))
                {
                    candle = new()
                    {
                        OpenTime = unixTime,
                        Open = stickOld.Close,
                        High = stickOld.Close,
                        Low = stickOld.Close,
                        Close = stickOld.Close,
                        Volume = 0,
                    };
                    candleList.Add(candle.OpenTime, candle);
                    if (GlobalData.Settings.General.DebugKLineReceive && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
                        GlobalData.AddTextToLogTab($"Debug BulkAddMissingCandles {candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true)}");

                    GlobalData.AddTextToLogTab($"Debug BulkAddMissingCandles {candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true)}");
                }
                stickOld = candle;
                unixTime += interval.Duration;
            }
        }
    }


    public static void BulkCalculateCandles(CryptoSymbol symbol, CryptoInterval sourceInterval, CryptoInterval targetInterval, CandleTime fetchEndUnix)
    {
        //GlobalData.AddTextToLogTab($"{symbol.Name} BulkCalculateCandles {lowerTimeFrame.Name} {interval.Name}");
        CryptoSymbolInterval symbolSourceInterval = symbol.GetSymbolInterval(sourceInterval.IntervalPeriod);
        CryptoCandleList candleSourceInterval = symbolSourceInterval.CandleList;
        if (candleSourceInterval.Count > 0)
        {
            //DateTime firstCandleDateDebug;
            //DateTime lastCandleDateDebug;
            //DateTime fetchEndUnixDate = CandleTools.GetUnixDate(fetchEndUnix);

            CandleTime firstCandle = candleSourceInterval.Keys.First();
            var (firstComplete, firstCandleDate) = IntervalTools.StartOfIntervalCandle3(firstCandle, sourceInterval.Duration, targetInterval.Duration);
            //firstCandleDateDebug = GetUnixDate(firstCandleDate);
            if (!firstComplete || firstCandleDate < firstCandle) // Has candles targetComplete and will not be complete and will be flagged as error
            {
                firstCandleDate += targetInterval.Duration;
                //firstCandleDateDebug = GetUnixDate(firstCandleDate);
            }

            CandleTime lastCandle = candleSourceInterval.Keys.Last();
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
                while (candles.TryGetValue((CandleTime)symbolInterval.LastCandleSynchronized, out CryptoCandle? _))
                    symbolInterval.LastCandleSynchronized += interval.Duration;
            }
        }
    }


    public static async Task CleanCandleDataAsync(CryptoSymbol symbol, CandleTime? lastCandle1mCloseTime)
    {
        // We nemen aardig wat geheugen in beslag door alles in het geheugen te berekenen, probeer in
        // ieder geval de CandleData te clearen. Vanaf x candles terug tot de eerste de beste die null is.

        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            if (lastCandle1mCloseTime == null || lastCandle1mCloseTime % interval.Duration == 0)
            {
                //await symbol.Lock("CleanCandleDataAsync");
                await symbol.Data.CandleLock.WaitAsync();
                try
                {
                    CryptoCandleList candles = symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList;
                    if (candles.Count > 0)
                    {
                        CandleTime firstOpenTime = candles.Keys.First();
                        lastCandle1mCloseTime ??= candles.Keys.Last();
                        CandleTime unix = lastCandle1mCloseTime.Value - 62 * interval.Duration;

                        // Remove old indicator data
                        while (unix >= firstOpenTime)
                        {
                            if (candles.TryGetValue(unix, out CryptoCandle? c))
                            {
                                if (c != null && c.CandleData != null)
                                {
                                    c.CandleData = null;
                                    //GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} candledata {c.DateLocal} removed");
                                }
                                else break;
                            }
                            unix -= interval.Duration;
                        }


                        //// Remove old indicator data
                        //for (int i = candles.Count - 62; i > 0; i--)
                        //{
                        //    CryptoCandle c = candles.Values[i];
                        //    if (c != null && c.CandleData != null)
                        //        c.CandleData = null;
                        //    else break;
                        //}


                        // Remove old candles
                        CandleTime startFetchUnix = CandleIndicatorData.GetCandleFetchStart(symbol, interval, DateTime.UtcNow);
                        //DateTime startFetchUnixDate = CandleTools.GetUnixDate(startFetchUnix);
                        while (candles.Count > 0)
                        {
                            CryptoCandle c = candles.Values.First();
                            if (c.OpenTime < startFetchUnix)
                            {
                                candles.Remove(c.OpenTime);
                                //GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} candle {c.DateLocal} removed");

                            }
                            else break;
                        }
                    }
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
    /// </summary>
    public static CandleTime[] DetermineFetchStartDate(CryptoSymbol symbol, CandleTime fetchEndUnix)
    {
        // TODO: Find a better place, problem is the method "Interval.GetExchangeInterval" which is exchange specific
        DateTime fetchEndDate = fetchEndUnix.ToDateTime();

        // Determine the maximum startdate per interval
        // Calculate what we need for the (full) calculation of the indicators (and markettrend)
        CandleTime[] fetchFrom = new CandleTime[Enum.GetNames(typeof(CryptoIntervalPeriod)).Length];
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            CandleTime startFromUnixTime = CandleIndicatorData.GetCandleFetchStart(symbol, interval, fetchEndDate);
            fetchFrom[(int)interval.IntervalPeriod] = startFromUnixTime;
        }


        // If the exchange does not support the interval than retrieve more
        // candles from a lower timeframe so we can calculate the candles.
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            CryptoInterval? lowerInterval = interval;
            while (!symbol.Exchange.IsIntervalSupported(lowerInterval.IntervalPeriod))
            {
                lowerInterval = lowerInterval.ConstructFrom;
                CandleTime startFromUnixTime = fetchFrom[(int)interval!.IntervalPeriod];
                if (startFromUnixTime < fetchFrom[(int)lowerInterval!.IntervalPeriod])
                    fetchFrom[(int)lowerInterval!.IntervalPeriod] = startFromUnixTime;
            }
        }


        // Correct the (worst case) startdate with what we previously collected..
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            if (symbolInterval.LastCandleSynchronized.HasValue)
            {
                CandleTime alreadyFetched = symbolInterval.LastCandleSynchronized.Value;
                // Huray, retrieve less candles, less work, more free time
                if (alreadyFetched > fetchFrom[(int)interval.IntervalPeriod])
                    fetchFrom[(int)interval.IntervalPeriod] = alreadyFetched;
            }
            symbolInterval.LastCandleSynchronized = fetchFrom[(int)interval.IntervalPeriod];
        }

        return fetchFrom; // result not really needed..
    }

}