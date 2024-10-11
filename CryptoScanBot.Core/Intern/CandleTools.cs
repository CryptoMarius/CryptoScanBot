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


    /// <summary>
    /// Add the final candle in the right interval
    /// </summary>
    static public CryptoCandle HandleFinalCandleData(CryptoSymbol symbol, CryptoInterval interval,
        DateTime openTime, decimal open, decimal high, decimal low, decimal close, decimal volume, bool isDuplicated)
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
        candle.Volume = volume;
        candle.IsDuplicated = isDuplicated;

        return candle;
    }

    /// <summary>
    /// Calculate the candle using the candles from the lower timeframes
    /// </summary>
    static public CryptoCandle CalculateCandleForInterval(CryptoSymbol symbol, CryptoInterval higherInterval, CryptoInterval lowerInterval, long candleLowerTimeFrameCloseTime)
    {
        // The higher timeframe & the starttime of it
        CryptoSymbolInterval higherSymbolInterval = symbol.GetSymbolInterval(higherInterval.IntervalPeriod);
        SortedList<long, CryptoCandle> candlesHigherTimeFrame = higherSymbolInterval.CandleList;
        long candleHigherTimeFrameOpenTime = candleLowerTimeFrameCloseTime - higherInterval.Duration;


        // The lower timeframe & the starttime of the first of the range
        CryptoSymbolInterval lowerSymbolInterval = symbol.GetSymbolInterval(lowerInterval.IntervalPeriod);
        SortedList<long, CryptoCandle> candlesLowerTimeFrame = lowerSymbolInterval.CandleList;
        int candleCountInLowerTimeFrame = higherInterval.Duration / lowerInterval.Duration;
        long candleLowerTimeFrameStart = candleLowerTimeFrameCloseTime - candleCountInLowerTimeFrame * lowerInterval.Duration;

#if DEBUG
        // Checks, kint of overkill, butit wont hurt either
        if (candleCountInLowerTimeFrame * lowerInterval.Duration != higherInterval.Duration)
            throw new Exception("Probleem met de definitie van de intervallen");

        if (higherInterval.Duration % lowerInterval.Duration > 0)
            throw new Exception("Probleem met de definitie van de intervallen");

        DateTime candleHigherTimeFrameDate = GetUnixDate(candleHigherTimeFrameOpenTime); // debug 
        DateTime candleLowerTimeFrameStartDate = GetUnixDate(candleLowerTimeFrameStart); // debug 
        DateTime candleLowerTimeFrameEindeDate = GetUnixDate(candleLowerTimeFrameCloseTime); // debug 
#endif


        // Create the higher timeframe candle (it will be added later when its data is fully calculated)
        if (!candlesHigherTimeFrame.TryGetValue(candleHigherTimeFrameOpenTime, out CryptoCandle? candleNew))
        {
            candleNew = new CryptoCandle()
            {
                OpenTime = candleHigherTimeFrameOpenTime,
                Open = 0,
                High = decimal.MinValue,
                Low = decimal.MaxValue,
                Close = 0
            };
        }

        // reset volume && error status
        candleNew.Volume = 0;
        candleNew.IsDuplicated = false; // ? Should we rest this one?

        // The candle  in the higher timeframe contains x candles from the lower timeframe
        bool firstCandle = true;
        int candleCount = candleCountInLowerTimeFrame;
        long candleOpenUnixLoop = candleLowerTimeFrameStart;
        while (candleOpenUnixLoop < candleLowerTimeFrameCloseTime)
        {
            //DateTime candleOpenDateLoop = GetUnixDate(candleOpenUnixLoop); // debug
            if (candlesLowerTimeFrame.TryGetValue(candleOpenUnixLoop, out CryptoCandle? candle))
            {
                candleCount--;

                // Open is 
                //if (candleNew.Open == 0 || candleOpenUnixLoop == candleHigherTimeFrameOpenTime)
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
                candleNew.Volume += candle.Volume;

                if (candle.IsDuplicated) // && lowerInterval.IntervalPeriod != Enums.CryptoIntervalPeriod.interval1m???
                    candleNew.IsDuplicated = true; // Forward error flag
                firstCandle = false;
            }
            //else break; // the lower interval is not complete, stop? (see remarks)

            candleOpenUnixLoop += lowerInterval.Duration;
        }


        // Duplicated is some kind of error status
        // on the 1m is the data of the previous candle
        // on higher timeframes it indicates missing candles
        if (candleCount != 0)
            candleNew.IsDuplicated = true;


        // If there was some data add candle to the higher timeframe list if needed
        if (!candlesHigherTimeFrame.ContainsKey(candleNew.OpenTime) && candleCount != candleCountInLowerTimeFrame)
        {
            candlesHigherTimeFrame.Add(candleNew.OpenTime, candleNew);
            UpdateCandleFetched(symbol, higherInterval);
        }


        // remark about the break and the -1 comparison:
        // remark: 1 missing candle will alway's trigger problems!
        // remark: not only for this interval but for the higher timeframes as well.....
        // TODO: Make decision: Use incomplete data OR incorrect data (including missing candles)

        // I Say: a missing 1m candle should not trigger missing 1D candles (I would opt for slight different data?)
        // Could also be dangerous, we should dive into this problem more, why are they missing, error of simply not there?

        //GlobalData.Logger.Info(candleNew.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true, true));
        return candleNew;
    }



    
    public static CryptoCandle Process1mCandle(CryptoSymbol symbol, DateTime openTime, decimal open, decimal high, decimal low, decimal close, decimal volume, bool duplicated = false)
    {
        Monitor.Enter(symbol.CandleList);
        try
        {
            //GlobalData.AddTextToLogTab(String.Format("{0} Candle {1} start processing", topic, kline.Timestamp.ToLocalTime()));
            // Last known price (and the price ticker will adjust)
            if (!GlobalData.BackTest)
            {
                symbol.LastPrice = close;
                symbol.AskPrice = close;
                symbol.BidPrice = close;
            }

            // Process the single 1m candle
            CryptoCandle candle = HandleFinalCandleData(symbol, GlobalData.IntervalList[0], openTime, open, high, low, close, volume, duplicated);

            // Update administration of the last processed candle
            UpdateCandleFetched(symbol, GlobalData.IntervalList[0]);
#if SHOWTIMING
            GlobalData.AddTextToLogTab($"ticker(1m):" + candle.OhlcText(symbol, GlobalData.IntervalList[0], symbol.PriceDisplayFormat, true, false, true));
#endif


            // Calculate the higher timeframes
            long candle1mCloseTime = candle.OpenTime + 60;
            foreach (CryptoInterval interval in GlobalData.IntervalList)
            {
                if (interval.ConstructFrom != null && candle1mCloseTime % interval.Duration == 0)
                {
                    // Calculate the candle using the candles from the lower timeframes
                    CryptoCandle candleNew = CalculateCandleForInterval(symbol, interval, interval.ConstructFrom, candle1mCloseTime);
                    // Update administration of the last processed candle
                    UpdateCandleFetched(symbol, interval);
#if SHOWTIMING
                    GlobalData.AddTextToLogTab($"ticker({interval.Name}):" + candleNew.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, false, true));
#endif
                }
            }

            return candle;
        }
        finally
        {
            Monitor.Exit(symbol.CandleList);
        }
    }

    //    /// <summary>
    //    /// Voeg de ontbrekende candles to aan de lijst (een nadeel van de stream)
    //    /// </summary>
    //    static public void AddMissingSticks(SortedList<long, CryptoCandle> candles, long candleUnixTime, CryptoInterval interval)
    //    {
    //        // De eventueel ontbrekende candles maken (dat kan een hele reeks zijn)
    //        // Die zetten we op de close van laatste candle (wat moeten we anders?)
    //        // (niet bedoeld om ontbrekende <tussenliggende> candles in te voegen)

    //        if (candles.Count > 0)
    //        {
    //            CryptoCandle stickOld = candles.Values.Last();

    //            long currentCandleUnix = candleUnixTime;
    //            //DateTime currentCandleDate = GetUnixDate(currentCandleUnix);

    //            //De verwachte datum van de volgende candle in deze reeks
    //            long nextCandleUnix = stickOld.OpenTime + interval.Duration;
    //            //DateTime nextCandleDate = GetUnixDate(nextCandleUnix); //debug

    //            while (nextCandleUnix < currentCandleUnix)
    //            {
    //                if (!candles.ContainsKey(nextCandleUnix))
    //                {
    //                    CryptoCandle stickNew = new()
    //                    {
    //                        OpenTime = nextCandleUnix,
    //                        Open = stickOld.Close,
    //                        Close = stickOld.Close,
    //                        Low = stickOld.Close,
    //                        High = stickOld.Close,
    //                        Volume = 0
    //                    };
    //                    candles.Add(nextCandleUnix, stickNew);
    //                }

    //                // De gegevens voor de volgende candle (bevat dezelfde gegevens)
    //                nextCandleUnix += interval.Duration;
    //                //nextCandleDate = GetUnixDate(nextCandleUnix); //debug
    //            }
    //        }

    //    }


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

    static private void ExportSymbolsToExcel(Model.CryptoExchange exchange)
    {
        try
        {
            var csv = new StringBuilder();
            var newLine = string.Format("{0};{1};{2}", "Exchange", "Symbol", "Volume");
            csv.AppendLine(newLine);

            for (int i = 0; i < exchange.SymbolListName.Values.Count; i++)
            {
                CryptoSymbol symbol = exchange.SymbolListName.Values[i];

                newLine = string.Format("{0};{1};{2}",
                symbol.Exchange.Name,
                symbol.Name,
                symbol.Volume.ToString());

                csv.AppendLine(newLine);
            }
            string filename = GlobalData.GetBaseDir();
            filename = filename + @"\data\" + exchange.Name + @"\";
            Directory.CreateDirectory(filename);
            File.WriteAllText(filename + "symbols.csv", csv.ToString());

        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
            //GlobalData.AddTextToLogTab(error.ToString());
        }
    }

    static private void ExportToExcel(Model.CryptoExchange exchange, CryptoSymbol symbol, CryptoInterval interval, SortedList<long, CryptoCandle> candleList)
    {
        //Deze is op dit moment specifiek voor de TradeView aanpak gemaakt (datum er ff uitgehaald en vervangen met unix 1000's)
        try

        {
            var csv = new StringBuilder();
            var newLine = string.Format("{0},{1},{2},{3},{4},{5},{6}", "Timestamp", "Symbol", "Open", "High", "Low", "Close", "Volume");
            csv.AppendLine(newLine);

            //Monitor.Enter(candleList);
            //try
            //{
            for (int i = 0; i < candleList.Count; i++)
            {
                CryptoCandle candle = candleList.Values[i];

                newLine = string.Format("{0}000,{1},{2},{3},{4},{5},{6}",
                candle.OpenTime.ToString(),
                //CandleTools.GetUnixDate(candle.OpenTime).ToString(),
                symbol.Name,
                //candle.Interval.ToString(),
                candle.Open.ToString(),
                candle.High.ToString(),
                candle.Low.ToString(),
                candle.Close.ToString(),
                //GetUnixDate(candle.CloseTime).ToString(),
                candle.Volume.ToString());
                //candle.Trades.ToString());

                csv.AppendLine(newLine);
            }
            //}
            //finally
            //{
            //    Monitor.Exit(candleList);
            //}
            string filename = GlobalData.GetBaseDir();
            filename = filename + @"\data\" + exchange.Name + @"\Candles\" + symbol.Name + @"\"; // + interval.Name + @"\";
            Directory.CreateDirectory(filename);
            File.WriteAllText(filename + symbol.Name + "-" + interval.Name + ".csv", csv.ToString());

        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
            //GlobalData.AddTextToLogTab(error.ToString());
        }
    }


    static public void ExportToExcelAll()
    {
        foreach (Model.CryptoExchange exchange in GlobalData.ExchangeListName.Values)
        {
            ExportSymbolsToExcel(exchange);

            foreach (var symbol in exchange.SymbolListName.Values)
            {
                foreach (CryptoInterval interval in GlobalData.IntervalList)
                {
                    CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
                    ExportToExcel(exchange, symbol, interval, symbolPeriod.CandleList);
                }
            }
        }
    }

}