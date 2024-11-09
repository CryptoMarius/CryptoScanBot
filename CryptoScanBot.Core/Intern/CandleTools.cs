using System.Text;

using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Intern;


public static class CandleTools
{
    /*

    Geheugen besparen door alleen het aantal minuten vanaf 2000 (ofzo) te gebruiken
    Dat scheelt in de candle data en de indexen (die indexen zijn ook smelly)
    Maar dot kost wel (debug) werk, daar zit ik niet op te wachten.....

    https://stackoverflow.com/questions/249760/how-can-i-convert-a-unix-timestamp-to-datetime-and-vice-versa
    public Double CreatedEpoch
    {
      get
      {
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
        TimeSpan span = (this.Created.ToLocalTime() - epoch);
        return span.TotalSeconds;
      }
      set
      {
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
        this.Created = epoch.AddSeconds(value);
      }
    }

    */

    /// <summary>
    /// Datum's kunnen afrondings problemen veroorzaken (op dit moment niet meer duidelijk waarom dat zo was?)
    /// Het resultaat valt in het opgegeven interval (60, 120, etc)
    /// NB: De candles bevatten altijd een datumtijd in UTC
    /// </summary>
    static public long GetUnixTime(DateTime datetime, long intervalDuration)
    {
        DateTimeOffset dateTimeOffset = datetime.ToUniversalTime();
        long unix = dateTimeOffset.ToUnixTimeSeconds();
        if (intervalDuration != 0)
            unix -= unix % intervalDuration;
        return unix;
    }

    static public long GetUnixTime(long unixTime, long intervalDuration)
    {
        long unix = unixTime;
        if (intervalDuration != 0)
            unix -= unix % intervalDuration;
        return unix;
    }

    /// <summary>
    /// De reverse van de GetUnixTime
    /// Oppassen: De candles bevatten altijd een datumtijd in UTC, deze moet dus ook
    /// </summary>
    static public DateTime GetUnixDate(long? unixDate)
    {
        DateTime datetime = DateTimeOffset.FromUnixTimeSeconds((long)unixDate).UtcDateTime;
        return datetime;
    }


    static public decimal GetHighValue(this CryptoCandle candle, bool useHighLow)
    {
        if (useHighLow)
            return candle.High;
        else
            return Math.Max(candle.Open, candle.Close);
    }

    static public decimal GetLowValue(this CryptoCandle candle, bool useHighLow)
    {
        if (useHighLow)
            return candle.Low;
        else
            return Math.Min(candle.Open, candle.Close);
    }

    //public static decimal GetLowest(this CryptoCandle candle, bool useWicks)
    //{
    //    if (useWicks)
    //        return candle.Low;
    //    else
    //        return Math.Min(candle.Open, candle.Close);
    //}

    //public static decimal GetHighest(this CryptoCandle candle, bool useWicks)
    //{
    //    if (useWicks)
    //        return candle.High;
    //    else
    //        return Math.Max(candle.Open, candle.Close);
    //}

    /// <summary>
    /// Add the final candle in the right interval
    /// </summary>
    static public CryptoCandle CreateCandle(CryptoSymbol symbol, CryptoInterval interval, DateTime openTime, 
        decimal open, decimal high, decimal low, decimal close, decimal baseVolume, decimal quoteVolume, bool isDuplicated)
    {
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        SortedList<long, CryptoCandle> candles = symbolPeriod.CandleList;

        // Add the candle if it does not exist
        long candleOpenUnix = GetUnixTime(openTime, 60);
        if (!candles.TryGetValue(candleOpenUnix, out CryptoCandle? candle))
        {
            candle = new CryptoCandle();
            candles.Add(candleOpenUnix, candle);
        }

        candle.OpenTime = candleOpenUnix;
        candle.Open = open;
        candle.High = high;
        candle.Low = low;
        candle.Close = close;
        candle.Volume = quoteVolume;
#if SUPPORTBASEVOLUME
        candle.BaseVolume = baseVolume;
#endif
        candle.IsDuplicated = isDuplicated;

        if (GlobalData.Settings.General.DebugKLineReceive && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
            GlobalData.AddTextToLogTab($"Create candle {candle?.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true, true)}");
        return candle!;
    }

    /// <summary>
    /// Calculate the candle using the candles from the lower timeframes
    /// </summary>
    static public CryptoCandle? CalculateCandleForInterval(CryptoSymbol symbol, CryptoInterval sourceInterval, CryptoInterval targetInterval, long candle1mOpenTime)
    {
        // The higher timeframe & the starttime of it
        CryptoSymbolInterval targetSymbolInterval = symbol.GetSymbolInterval(targetInterval.IntervalPeriod);
        SortedList<long, CryptoCandle> candlesTargetTimeFrame = targetSymbolInterval.CandleList;
        var (_, candleTargetStart) = IntervalTools.StartOfIntervalCandle3(candle1mOpenTime, sourceInterval.Duration, targetInterval.Duration);
        long candleTargetClose = candleTargetStart + targetInterval.Duration;
#if DEBUG
        DateTime candleTargetStartDebug = GetUnixDate(candleTargetStart);
        DateTime candleTargetCloseDebug = GetUnixDate(candleTargetClose);
#endif

        // The lower timeframe & the starttime of the first of the range
        CryptoSymbolInterval lowerSymbolInterval = symbol.GetSymbolInterval(sourceInterval.IntervalPeriod);
        SortedList<long, CryptoCandle> candlesSource = lowerSymbolInterval.CandleList;
        int candleCountInSource = targetInterval.Duration / sourceInterval.Duration;
        long candleSourceStart = candleTargetClose - candleCountInSource * sourceInterval.Duration;
        long candleSourceClose = candleSourceStart + sourceInterval.Duration;
#if DEBUG
        DateTime candleSourceStartDebug = GetUnixDate(candleSourceStart);
        DateTime candleSourceCloseDebug = GetUnixDate(candleSourceClose); // ????? just the first candle..
#endif


        // Create the higher timeframe candle (it will be added later when its data is fully calculated)
        if (!candlesTargetTimeFrame.TryGetValue(candleTargetStart, out CryptoCandle? candleNew))
        {
            candleNew = new CryptoCandle()
            {
                OpenTime = candleTargetStart,
                Open = 0,
                High = decimal.MinValue,
                Low = decimal.MaxValue,
                Close = 0,
                Volume = 0,
#if SUPPORTBASEVOLUME
                BaseVolume = 0,
#endif
            };
        }

        // reset volume && error status
        decimal volume = 0;
#if SUPPORTBASEVOLUME
        decimal baseVolume = 0;
#endif
        candleNew.IsDuplicated = false; // ? Should we reset this?

        // The candle  in the higher timeframe contains x candles from the lower timeframe
        bool firstCandle = true;
        int candleCount = candleCountInSource;
        long loop = candleSourceStart;
        while (loop < candleTargetClose)
        {
            DateTime loopDebug = GetUnixDate(loop);
            if (candlesSource.TryGetValue(loop, out CryptoCandle? candle))
            {
                candleCount--;

                // Open is 
                if (firstCandle)
                    candleNew.Open = candle.Open;

                // De close bijwerken
                candleNew.Close = candle.Close;

                // High en low bijwerken
                if (candle.High > candleNew.High)
                    candleNew.High = candle.High;
                if (candle.Low < candleNew.Low)
                    candleNew.Low = candle.Low;

                // Dat gaat  fout als niet de hele "periode" aangeboden wordt
                volume += candle.Volume;
#if SUPPORTBASEVOLUME
                baseVolume += candle.BaseVolume;
#endif
                if (candle.IsDuplicated)
                    candleNew.IsDuplicated = true; // Forward error flag
                firstCandle = false;
            }
            //else break; // the lower interval is not complete, stop? (see remarks)

            loop += sourceInterval.Duration;
        }
        // Keep old calculated volume
        if (volume > candleNew.Volume)
            candleNew.Volume = volume;
#if SUPPORTBASEVOLUME
        if (baseVolume > candleNew.BaseVolume)
            candleNew.BaseVolume = baseVolume;
#endif
        // Duplicated is indicating an error status
        // on the 1m is the data of the previous candle
        // on higher timeframes it indicates missing candles
        // BUT, if we calculate an old candle which does not have any lower timeframe candles it is set also...
        if (candleCount != 0)
            candleNew.IsDuplicated = true;



        // If there was some data add candle to the higher timeframe list if needed
        if (candleNew.Open != 0.0m && !candlesTargetTimeFrame.ContainsKey(candleNew.OpenTime) && candleCount != candleCountInSource)
        {
            candlesTargetTimeFrame.Add(candleNew.OpenTime, candleNew);
            UpdateCandleFetched(symbol, targetInterval);
        }

        //if (candleNew.Open == candleNew.Close && candleNew.Low == candleNew.Close && candleNew.High > candleNew.Open)
        //    GlobalData.AddTextToLogTab($"Reconstructed candle {candleNew.OhlcText(symbol, targetInterval, symbol.PriceDisplayFormat, true, true, true)}?????");


        if (GlobalData.Settings.General.DebugKLineReceive && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
            GlobalData.AddTextToLogTab($"Calc candle {candleNew.OhlcText(symbol, targetInterval, symbol.PriceDisplayFormat, true, true, true)}");

        // remark about the break and the -1 comparison:
        // remark: 1 missing candle will alway's trigger problems!
        // remark: not only for this interval but for the higher timeframes as well.....
        // TODO: Make decision: Use incomplete data OR incorrect data (including missing candles)

        // I Say: a missing 1m candle should not trigger missing 1D candles (I would opt for slight different data?)
        // Could also be dangerous, we should dive into this problem more, why are they missing, error of simply not there?

        //GlobalData.Logger.Info(candleNew.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true, true));
        return candleNew;
    }



    
    public static async Task<CryptoCandle> Process1mCandleAsync(CryptoSymbol symbol, DateTime openTime, decimal open, decimal high, decimal low, decimal close, 
        decimal baseVolume, decimal quoteVolume, bool duplicated = false)
    {
        //Monitor.Enter(symbol.CandleList);
        await symbol.CandleLock.WaitAsync();
        try
        {
            //GlobalData.AddTextToLogTab($"Process1mCandle {symbol.Name} Candle 1m {openTime.ToLocalTime()} start processing");
            // Last known price (and the price ticker will adjust)
            if (!GlobalData.BackTest)
            {
                symbol.LastPrice = close;
                symbol.AskPrice = close;
                symbol.BidPrice = close;
            }

            // Process the single 1m candle
            CryptoCandle? candle = CreateCandle(symbol, GlobalData.IntervalList[0], openTime, open, high, low, close, baseVolume, quoteVolume, duplicated);
            // Update administration of the last processed candle
            UpdateCandleFetched(symbol, GlobalData.IntervalList[0]);


            // Calculate the higher timeframes
            long candle1mCloseTime = candle!.OpenTime + 60;
            foreach (CryptoInterval interval in GlobalData.IntervalList)
            {
                if (interval.ConstructFrom != null && candle1mCloseTime % interval.Duration == 0)
                {
                    var (outside, targetStart) = IntervalTools.StartOfIntervalCandle3(candle.OpenTime, interval.ConstructFrom.Duration, interval.Duration);
                    if (!outside) //  && targetStart + interval.Duration <= candle!.OpenTime
                    {
                        // Calculate the candle using the candles from the lower timeframes
                        CalculateCandleForInterval(symbol, interval.ConstructFrom, interval, targetStart); //candle.OpenTime ? candle1mCloseTime
                        // Update administration of the last processed candle
                        UpdateCandleFetched(symbol, interval);
                    }
                }
            }

            return candle;
        }
        finally
        {
            //Monitor.Exit(symbol.CandleList);
            symbol.CandleLock.Release();
        }
    }

    static public void BulkAddMissingCandles(CryptoSymbol symbol, CryptoInterval interval)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        SortedList<long, CryptoCandle> candleList = symbolInterval.CandleList;

        if (candleList.Count != 0)
        {
            CryptoCandle stickOld = candleList.Values.First();
            //GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Debug missing candle " + CandleTools.GetUnixDate(stickOld.OpenTime).ToLocalTime());
            long unixTime = stickOld.OpenTime;
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
#if SUPPORTBASEVOLUME
                        BaseVolume = 0,
#endif
                        Volume = 0,
                        IsDuplicated = true // retry fetch
                    };
                    candleList.Add(candle.OpenTime, candle);
                    if (GlobalData.Settings.General.DebugKLineReceive && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
                        GlobalData.AddTextToLogTab($"Debug calculating candle {candle.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true)} (duplicated)");
                }
                stickOld = candle;
                unixTime += interval.Duration;
            }
        }
    }


    static public void BulkCalculateCandles(CryptoSymbol symbol, CryptoInterval sourceInterval, CryptoInterval targetInterval, long fetchEndUnix)
    {
        //GlobalData.AddTextToLogTab($"{symbol.Name} BulkCalculateCandles {sourceInterval.Name} {targetInterval.Name}");
        CryptoSymbolInterval symbolSourceInterval = symbol.GetSymbolInterval(sourceInterval.IntervalPeriod);
        SortedList<long, CryptoCandle> candleSourceInterval = symbolSourceInterval.CandleList;
        if (candleSourceInterval.Values.Any())
        {
            DateTime firstCandleDateDebug;
            DateTime lastCandleDateDebug;
            DateTime fetchEndUnixDate = CandleTools.GetUnixDate(fetchEndUnix);

            CryptoCandle firstCandle = candleSourceInterval.Values.First();
            var (firstOutside, firstCandleDate) = IntervalTools.StartOfIntervalCandle3(firstCandle.OpenTime, sourceInterval.Duration, targetInterval.Duration);
            firstCandleDateDebug = CandleTools.GetUnixDate(firstCandleDate);
            if (firstOutside || firstCandleDate < firstCandle.OpenTime) // Has candles outside and will not be complete and will be flagged as error
            {
                firstCandleDate += targetInterval.Duration;
                firstCandleDateDebug = CandleTools.GetUnixDate(firstCandleDate);
            }

            CryptoCandle lastCandle = candleSourceInterval.Values.Last();
            var (lastOutside, lastCandleDate) = IntervalTools.StartOfIntervalCandle3(lastCandle.OpenTime, sourceInterval.Duration, targetInterval.Duration);
            lastCandleDateDebug = CandleTools.GetUnixDate(lastCandleDate);
            if (lastOutside || lastCandleDate + targetInterval.Duration > fetchEndUnix) // Has candles outside and will not be complete and will be flagged as error (also future candle)
            {
                lastCandleDate -= targetInterval.Duration;
                lastCandleDateDebug = CandleTools.GetUnixDate(lastCandleDate);
            }

            // Bulk calculate all higher interval candles (ranging from the firstLowerCandle to the last candle)
            long loop = firstCandleDate;
#if DEBUG
            DateTime loopDebug = CandleTools.GetUnixDate(loop);
#endif
            while (loop <= lastCandleDate)
            {
                CryptoCandle? candle = CandleTools.CalculateCandleForInterval(symbol, sourceInterval, targetInterval, loop);
                if (candle!.IsDuplicated)
                    candle = CandleTools.CalculateCandleForInterval(symbol, sourceInterval, targetInterval, loop);

                loop += targetInterval.Duration;
#if DEBUG
                loopDebug = CandleTools.GetUnixDate(loop);
#endif

                //if (GlobalData.Settings.General.DebugKLineReceive && (GlobalData.Settings.General.DebugSymbol == symbol.Name || GlobalData.Settings.General.DebugSymbol == ""))
                //    GlobalData.AddTextToLogTab($"Debug calculating candle {candle?.OhlcText(symbol, targetInterval, symbol.PriceDisplayFormat, true, true, true)}");
            }

            CandleTools.UpdateCandleFetched(symbol, targetInterval);
        }
    }


    static public void UpdateCandleFetched(CryptoSymbol symbol, CryptoInterval interval)
    {
        var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.LastCandleSynchronized.HasValue)
        {
            var candles = symbolInterval.CandleList;
            if (candles.Count != 0)
            {
                while (candles.TryGetValue((long)symbolInterval.LastCandleSynchronized, out CryptoCandle? candle) && !candle.IsDuplicated)
                    symbolInterval.LastCandleSynchronized += interval.Duration;
            }
        }
    }


    static public async Task CleanCandleDataAsync(CryptoSymbol symbol, long? lastCandle1mCloseTime)
    {
        // We nemen aardig wat geheugen in beslag door alles in het geheugen te berekenen, probeer in 
        // ieder geval de CandleData te clearen. Vanaf x candles terug tot de eerste de beste die null is.

        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            if (lastCandle1mCloseTime == null || lastCandle1mCloseTime % interval.Duration == 0)
            {
                //Monitor.Enter(symbol.CandleList);
                await symbol.CandleLock.WaitAsync();
                try
                {
                    // Remove old indicator data
                    SortedList<long, CryptoCandle> candles = symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList;
                    for (int i = candles.Count - 62; i > 0; i--)
                    {
                        CryptoCandle c = candles.Values[i];
                        if (c.CandleData != null)
                            c.CandleData = null;
                        else break;
                    }


                    // Remove old candles
                    long startFetchUnix = CandleIndicatorData.GetCandleFetchStart(symbol, interval, DateTime.UtcNow);
                    //DateTime startFetchUnixDate = CandleTools.GetUnixDate(startFetchUnix);
                    while (candles.Values.Any())
                    {
                        CryptoCandle c = candles.Values[0];
                        if (c.OpenTime < startFetchUnix)
                        {
                            candles.Remove(c.OpenTime);
                            //GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} candle {c.DateLocal} removed");

                        }
                        else break;
                    }
                }
                finally
                {
                    //Monitor.Exit(symbol.CandleList);
                    symbol.CandleLock.Release();
                }
            }
        }
    }

}