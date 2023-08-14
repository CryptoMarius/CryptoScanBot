using System.Text;

using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Intern;


public static class CandleTools
{
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


    static public void CalculateCandleForInterval(CryptoInterval interval, CryptoInterval constructFrom, CryptoSymbol symbol, long openTime)
    {
        // De aangeboden candle is een willekeurige (1m) candle die toegevoegd is 
        // Het idee is dat we enkel de laatste candle moeten herbereken.

        // Optimalisatie: Het heeft geen zin om een candle te berekenen als niet alle candles 
        // aanwezig zijn in het lagere interval. Maar hoe beredeneer je dat?


        // Besloten om alle candles in het interval opnieuw toe te passen zodat de volume goed loopt.
        // (en op basis van de andere intervallen is dit niet zo moeilijk, wel veel van hetzelfde)

        //Voeg een candle toe aan een hogere tijd interval (eventueel uit db laden)
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        SortedList<long, CryptoCandle> candles = symbolPeriod.CandleList;


        // De unix tijd "afgekapt" naar het gewenste interval
        long candleOpenUnix = openTime - openTime % interval.Duration;
        //DateTime candleOpenDate = GetUnixDate(candleOpenUnix); //ter debug want een unix date is onleesbaar

#pragma warning disable CS0219 // Variable is assigned but its value is never used
        bool IsChanged = false; // nodig  voor SQL database
#pragma warning restore CS0219 // Variable is assigned but its value is never used
        if (!candles.TryGetValue(candleOpenUnix, out CryptoCandle candleNew))
        {
            IsChanged = true;
            candleNew = new CryptoCandle()
            {
#if SQLDATABASE
                ExchangeId = symbol.ExchangeId,
                SymbolId = symbol.Id,
                IntervalId = interval.Id,
#endif
                //Symbol = symbol,
                //Interval = interval,
                OpenTime = candleOpenUnix,

                Open = -1,
                High = decimal.MinValue,
                Low = decimal.MaxValue,
                Close = -1
            };
        }
        decimal LastVolume = candleNew.Volume;
        //Hier de volume op 0 zetten
        candleNew.Volume = 0;

        //Itereer over de bron candles van start tot einde en 
        CryptoSymbolInterval symbolPeriodConstruct = symbol.GetSymbolInterval(constructFrom.IntervalPeriod);
        SortedList<long, CryptoCandle> constructFromCandles = symbolPeriodConstruct.CandleList;

        // De unix tijd "afgekapt" naar het gewenste interval
        long candleOpenUnixStart = candleOpenUnix;
        //DateTime candleOpenDateStart = CandleTools.GetUnixDate(candleOpenUnixStart); //ter debug want een unix date is onleesbaar

        // Naar de volgende candle (1 hoger)
        long candleOpenUnixEinde = candleOpenUnix + interval.Duration;
        //DateTime candleOpenDateEinde = CandleTools.GetUnixDate(candleOpenUnixEinde); //ter debug want een unix date is onleesbaar

        // De nieuwe candle bestaat uit x van de vorige
        int candleCount = (int)(interval.Duration / constructFrom.Duration);
        long candleOpenUnixLoop = candleOpenUnixStart;
        //DateTime candleOpenDateLoop = CandleTools.GetUnixDate(candleOpenUnixLoop); //ter debug want een unix date is onleesbaar

        while (candleOpenUnixLoop < candleOpenUnixEinde)
        {
            if (constructFromCandles.TryGetValue(candleOpenUnixLoop, out CryptoCandle candle))
            {
                candleCount--;

                // De open bijwerken
                if (candleOpenUnixLoop == candleOpenUnix)
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

            candleOpenUnixLoop += constructFrom.Duration;
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
                    CandleTools.UpdateCandleFetched(symbol, interval);
                    GlobalData.TaskSaveCandles.AddToQueue(candleNew);
                }
            }
            else
            {
                if (!candles.ContainsKey(candleNew.OpenTime))
                {
                    candles.Add(candleNew.OpenTime, candleNew);
                    CandleTools.UpdateCandleFetched(symbol, interval);
                    GlobalData.TaskSaveCandles.AddToQueue(candleNew);
                }
            }
#else
            if (!candles.ContainsKey(candleNew.OpenTime))
            {
                candles.Add(candleNew.OpenTime, candleNew);
                UpdateCandleFetched(symbol, interval);
            }
#endif
        }
    }


    /// <summary>
    /// Voeg de ontbrekende candles to aan de lijst (een nadeel van de stream)
    /// </summary>
    static public void AddMissingSticks(SortedList<long, CryptoCandle> candles, long candleUnixTime, CryptoInterval interval)
    {
        // De eventueel ontbrekende candles maken (dat kan een hele reeks zijn)
        // Die zetten we op de close van laatste candle (wat moeten we anders?)
        // (niet bedoeld om ontbrekende <tussenliggende> candles in te voegen)

        if (candles.Count > 0)
        {
            CryptoCandle stickOld = candles.Values.Last();

            long currentCandleUnix = candleUnixTime;
            //DateTime currentCandleDate = GetUnixDate(currentCandleUnix);

            //De verwachte datum van de volgende candle in deze reeks
            long nextCandleUnix = stickOld.OpenTime + interval.Duration;
            //DateTime nextCandleDate = GetUnixDate(nextCandleUnix); //debug

            while (nextCandleUnix < currentCandleUnix)
            {
                if (!candles.ContainsKey(nextCandleUnix))
                {
                    CryptoCandle stickNew = new()
                    {
#if SQLDATABASE
                        ExchangeId = stickOld.ExchangeId,
                        SymbolId = stickOld.SymbolId,
                        IntervalId = interval.Id,
#endif
                        //Symbol = stickOld.Symbol,
                        //Interval = interval,
                        OpenTime = nextCandleUnix,
                        Open = stickOld.Close,
                        Close = stickOld.Close,
                        Low = stickOld.Close,
                        High = stickOld.Close,
                        Volume = 0
                    };
                    candles.Add(nextCandleUnix, stickNew);
                }

                // De gegevens voor de volgende candle (bevat dezelfde gegevens)
                nextCandleUnix += interval.Duration;
                //nextCandleDate = GetUnixDate(nextCandleUnix); //debug
            }
        }

    }


    static public void UpdateCandleFetched(CryptoSymbol symbol, CryptoInterval interval)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        SortedList<long, CryptoCandle> candles = symbolInterval.CandleList;

        if (candles.Any())
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
            System.IO.Directory.CreateDirectory(filename);
            System.IO.File.WriteAllText(filename + "symbols.csv", csv.ToString());

        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
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

            Monitor.Enter(candleList);
            try
            {
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
            }
            finally
            {
                Monitor.Exit(candleList);
            }
            string filename = GlobalData.GetBaseDir();
            filename = filename + @"\data\" + exchange.Name + @"\Candles\" + symbol.Name + @"\"; // + interval.Name + @"\";
            System.IO.Directory.CreateDirectory(filename);
            System.IO.File.WriteAllText(filename + symbol.Name + "-" + interval.Name + ".csv", csv.ToString());

        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
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