using System.Text;
using CryptoScanBot.Core.Enums;
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
    /// Verwerk een definitieve 1m candle 
    /// Uitgangspunt hierbij is dat de data ingelezen is en we niet naar de database hoeven
    /// </summary>
    static public CryptoCandle HandleFinalCandleData(CryptoSymbol symbol, CryptoInterval interval,
        DateTime openTime, decimal open, decimal high, decimal low, decimal close, decimal volume, bool isDuplicated)
    {
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        SortedList<long, CryptoCandle> candles = symbolPeriod.CandleList;

        long candleOpenUnix = GetUnixTime(openTime, 60);
        //DateTime candleOpenDate = CandleTools.GetUnixDate(candleOpenUnix); //ter debug want een unix date is onleesbaar

        // Altijd de candle maken als deze niet bestaat
        if (!candles.TryGetValue(candleOpenUnix, out CryptoCandle candle))
        {
            candle = new CryptoCandle
            {
#if SQLDATABASE
                ExchangeId = symbol.ExchangeId,
                SymbolId = symbol.Id,
	            IntervalId = interval.Id,
#endif
                //Symbol = symbol,
                //Interval = interval
            };
            candles.Add(candleOpenUnix, candle);
        }

        // Dit is de "echte data" (krijgen we ook data van niet volledige candles??)
        candle.OpenTime = candleOpenUnix;
        candle.Open = open;
        candle.High = high;
        candle.Low = low;
        candle.Close = close;
        candle.Volume = volume;
        candle.IsDuplicated = isDuplicated;

        return candle;
    }


    static public CryptoCandle CalculateCandleForInterval(CryptoInterval higherInterval, CryptoInterval lowerInterval, CryptoSymbol symbol, long candleLowerTimeFrameCloseTime)
    {
        // Voeg een candle toe aan een hogere tijd interval (indien het berekend kan worden uit het lagere timeframe)
        // candleLowerTimeFrameCloseTime is het tijdstip dat de candle van het lagere timeframe afgesloten is.
        // Vanwege het volume moeten de candles allemaal berekend worden.
        // (eventueel ontbrekende candles zijn een probleem)

        // Het "hogere" timeframe & de open tijd van het hogere interval
        CryptoSymbolInterval higherSymbolPeriod = symbol.GetSymbolInterval(higherInterval.IntervalPeriod);
        SortedList<long, CryptoCandle> candlesHigherTimeFrame = higherSymbolPeriod.CandleList;
        long candleHigherTimeFrameOpenTime = candleLowerTimeFrameCloseTime - higherInterval.Duration;


        // Het "lagere" timeframe en de start van de te itereren periode (is start van de higher candle?)
        CryptoSymbolInterval lowerSymbolPeriod = symbol.GetSymbolInterval(lowerInterval.IntervalPeriod);
        SortedList<long, CryptoCandle> candlesLowerTimeFrame = lowerSymbolPeriod.CandleList;
        long candleCountInLowerTimeFrame = higherInterval.Duration / lowerInterval.Duration;
        long candleLowerTimeFrameStart = candleLowerTimeFrameCloseTime - candleCountInLowerTimeFrame * lowerInterval.Duration;

#if DEBUG
        // controles (beetje overbodig als je de input kent, maar kan ook geen kwaad)
        if (candleCountInLowerTimeFrame * lowerInterval.Duration != higherInterval.Duration)
            throw new Exception("Probleem met de definitie van de intervallen");

        if (higherInterval.Duration % lowerInterval.Duration > 0)
            throw new Exception("Probleem met de definitie van de intervallen");

        // ter debug want die unix date zijn onleesbaar
        DateTime candleHigherTimeFrameDate = GetUnixDate(candleHigherTimeFrameOpenTime);
        DateTime candleLowerTimeFrameStartDate = GetUnixDate(candleLowerTimeFrameStart);
        DateTime candleLowerTimeFrameEindeDate = GetUnixDate(candleLowerTimeFrameCloseTime);
#endif


        // Maak alvast een candle (wordt later pas toegevoegd indien okay)
#pragma warning disable CS0219 // Variable is assigned but its value is never used
        bool IsChanged = false; // nodig  voor SQL database
#pragma warning restore CS0219 // Variable is assigned but its value is never used
        if (!candlesHigherTimeFrame.TryGetValue(candleHigherTimeFrameOpenTime, out CryptoCandle candleNew))
        {
            IsChanged = true;
            candleNew = new CryptoCandle()
            {
#if SQLDATABASE
                ExchangeId = symbol.ExchangeId,
                SymbolId = symbol.Id,
                IntervalId = higherInterval.Id,
#endif
                OpenTime = candleHigherTimeFrameOpenTime,
                Open = -1,
                High = decimal.MinValue,
                Low = decimal.MaxValue,
                Close = -1
            };
        }
        decimal LastVolume = candleNew.Volume;
        candleNew.Volume = 0;



        // De nieuwe candle bestaat uit x van de vorige
        int candleCount = (int)(higherInterval.Duration / lowerInterval.Duration);
        long candleOpenUnixLoop = candleLowerTimeFrameStart;
        DateTime candleOpenDateLoop = GetUnixDate(candleOpenUnixLoop); //ter debug want een unix date is onleesbaar

        // Itereer over de bron candles van start tot einde en 
        while (candleOpenUnixLoop < candleLowerTimeFrameCloseTime)
        {
            if (candlesLowerTimeFrame.TryGetValue(candleOpenUnixLoop, out CryptoCandle candle))
            {
                candleCount--;

                // De open bijwerken
                if (candleOpenUnixLoop == candleHigherTimeFrameOpenTime)
                {
                    if (candleNew.Open != candle.Open)
                    {
                        IsChanged = true;
                        candleNew.Open = candle.Open;
                    }
                }

                // De close bijwerken
                if (candleCount == 0)
                {
                    if (candleNew.Close != candle.Close)
                    {
                        IsChanged = true;
                        candleNew.Close = candle.Close;
                    }
                }

                // High en low bijwerken
                if (candle.High > candleNew.High)
                {
                    IsChanged = true;
                    candleNew.High = candle.High;
                }
                if (candle.Low < candleNew.Low)
                {
                    IsChanged = true;
                    candleNew.Low = candle.Low;
                }

                // Dat gaat  fout als niet de hele "periode" aangeboden wordt
                candleNew.Volume += candle.Volume;
            }
            else break; // het lagere interval is niet compleet, stop maar!

            candleOpenUnixLoop += lowerInterval.Duration;
        }

        if (LastVolume != candleNew.Volume)
            IsChanged = true;


        // Onvolledige candles willen we niet bewaren(dat geeft alleen problemen)
        if (candleNew.Open != -1 && candleNew.Close != -1 && candleCount == 0)
        {
#if SQLDATABASE
            if (candleNew.Id > 0)
            {
                //Optimalisatie: Vaak wordt een candle helemaal niet aangepast, valt bijvoorbeeld tussen eerdere high en lows.
                //deze hoeft dan niet naar de database geschreven te worden wat zeer waarschijnlijk een beetje tijd scheelt....
                if (IsChanged)
                {
                    CandleTools.UpdateCandleFetched(symbol, higherInterval);
                    GlobalData.TaskSaveCandles.AddToQueue(candleNew);
                }
            }
            else
            {
                if (!candlesHigherTimeFrame.ContainsKey(candleNew.OpenTime))
                {
                    candlesHigherTimeFrame.Add(candleNew.OpenTime, candleNew);
                    CandleTools.UpdateCandleFetched(symbol, higherInterval);
                    GlobalData.TaskSaveCandles.AddToQueue(candleNew);
                }
            }
#else
            if (!candlesHigherTimeFrame.ContainsKey(candleNew.OpenTime))
            {
                candlesHigherTimeFrame.Add(candleNew.OpenTime, candleNew);
                UpdateCandleFetched(symbol, higherInterval);
            }
#endif
        }

        //GlobalData.Logger.Info(candleNew.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, true, true));
        return candleNew;
    }

    // Move?
    public static void Process1mCandle(CryptoSymbol symbol, DateTime openTime, decimal open, decimal high, decimal low, decimal close, decimal volume)
    {
        Monitor.Enter(symbol.CandleList);
        try
        {
            // Laatste bekende prijs (priceticker vult aan)
            symbol.LastPrice = close;

            // Process the single 1m candle
            CryptoCandle candle = HandleFinalCandleData(symbol, GlobalData.IntervalList[0], openTime, open, high, low, close, volume, false);
            UpdateCandleFetched(symbol, GlobalData.IntervalList[0]);
#if SQLDATABASE
            GlobalData.TaskSaveCandles.AddToQueue(candle);
#endif
#if SHOWTIMING

            GlobalData.Logger.Info($"ticker(1m):" + candle.OhlcText(symbol, GlobalData.IntervalList[0], symbol.PriceDisplayFormat, true, false, true));
#endif


            // Calculate higher timeframes
            //long candle1mOpenTime = candle.OpenTime;
            long candle1mCloseTime = candle.OpenTime + 60;
            foreach (CryptoInterval interval in GlobalData.IntervalList)
            {
                if (interval.ConstructFrom != null && candle1mCloseTime % interval.Duration == 0)
                {
                    CryptoCandle candleNew = CalculateCandleForInterval(interval, interval.ConstructFrom, symbol, candle1mCloseTime);
                    UpdateCandleFetched(symbol, interval);
#if SHOWTIMING

                    GlobalData.Logger.Info($"ticker({interval.Name}):" + candleNew.OhlcText(symbol, interval, symbol.PriceDisplayFormat, true, false, true));
#endif

                }
            }

            // Aanbieden voor analyse (dit gebeurd zowel in de ticker als ProcessCandles)
            if (GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
                GlobalData.ThreadMonitorCandle.AddToQueue(symbol, candle);
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
    //#if SQLDATABASE
    //                        ExchangeId = stickOld.ExchangeId,
    //                        SymbolId = stickOld.SymbolId,
    //                        IntervalId = interval.Id,
    //#endif
    //                        //Symbol = stickOld.Symbol,
    //                        //Interval = interval,
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
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        SortedList<long, CryptoCandle> candles = symbolInterval.CandleList;

        if (candles.Count != 0)
        {
            // Wacht met een waarde geven totdat de fetch candles zijn werk heeft gedaan
            if (symbolInterval.LastCandleSynchronized != null)
            {
                // Het moet een waarde hebben om in de database te zetten
                if (!symbolInterval.LastCandleSynchronized.HasValue)
                {
                    symbolInterval.LastCandleSynchronized = candles.Values.First().OpenTime;
                    // dat zou niet eens kunnen gebeuren, maar voila..
                    symbolInterval.LastCandleSynchronized -= symbolInterval.LastCandleSynchronized % interval.Duration;
                }

                while (candles.TryGetValue((long)symbolInterval.LastCandleSynchronized, out CryptoCandle candle) && !candle.IsDuplicated)
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