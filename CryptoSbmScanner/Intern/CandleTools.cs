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
    static public CryptoCandle HandleFinalCandleData(CryptoSymbol symbol, CryptoInterval interval, DateTime openTime, decimal open, decimal high, decimal low, decimal close, decimal volume)
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
                Symbol = symbol,
                Interval = interval
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

        //bool IsChanged = false;
        if (!candles.TryGetValue(candleOpenUnix, out CryptoCandle candleNew))
        {
            //IsChanged = true;
            candleNew = new CryptoCandle()
            {
                Symbol = symbol,
                Interval = interval,
                OpenTime = candleOpenUnix,

                Open = -1,
                High = decimal.MinValue,
                Low = decimal.MaxValue,
                Close = -1
            };
        }

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
                        candleNew.Open = candle.Open;
                }

                // De close bijwerken
                if (candleCount == 0)
                {
                    if (candleNew.Close != candle.Close)
                        candleNew.Close = candle.Close;
                }

                // High en low bijwerken
                if (candle.High > candleNew.High)
                    candleNew.High = candle.High;
                if (candle.Low < candleNew.Low)
                    candleNew.Low = candle.Low;

                // Dat gaat  fout als niet de hele "periode" aangeboden wordt
                candleNew.Volume += candle.Volume;
            }
            else break; // het lagere interval is niet compleet, stop maar!

            candleOpenUnixLoop += constructFrom.Duration;


            // Onvolledige candles willen we niet bewaren(dat geeft alleen problemen)
            if (candleNew.Open != -1 && candleNew.Close != -1 && candleCount == 0)
            {
                if (!candles.ContainsKey(candleNew.OpenTime))
                {
                    candles.Add(candleNew.OpenTime, candleNew);
                    UpdateCandleFetched(symbol, interval);
                }
            }
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
                if (!candles.TryGetValue(nextCandleUnix, out _))
                {
                    CryptoCandle stickNew = new()
                    {
                        Symbol = stickOld.Symbol,
                        Interval = interval,
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

                while (candles.ContainsKey((long)symbolInterval.LastCandleSynchronized))
                    symbolInterval.LastCandleSynchronized += interval.Duration;
            }
        }
    }

}
