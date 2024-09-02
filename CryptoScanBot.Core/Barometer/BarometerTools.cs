using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Barometer;

public class BarometerTools
{
    private static readonly object LockObject = new();
    private delegate bool CalcBarometerMethod(CryptoQuoteData quoteData, SortedList<string, CryptoSymbol> symbols, CryptoInterval interval, long unixCandleLast, out decimal barometerPerc);


    static public void InitBarometerSymbols()
    {
        // Check all the (internal) barometer symbols
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (quoteData.FetchCandles)
            {
                CheckBarometerSymbolPrecence(Constants.SymbolNameBarometerPrice, quoteData);
            }
        }
    }

    private static CryptoSymbol? CheckBarometerSymbolPrecence(string baseName, CryptoQuoteData quoteData)
    {
        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName, out Model.CryptoExchange? exchange))
        {
            if (!exchange.SymbolListName.TryGetValue(baseName + quoteData.Name, out CryptoSymbol? symbol))
            {
                symbol = new CryptoSymbol
                {
                    Exchange = exchange,
                    ExchangeId = exchange.Id,
                    Base = baseName, //De "munt"
                    Quote = quoteData.Name, //USDT, BTC etc.
                    Volume = 0,
                    Status = 1,
                };
                symbol.Name = symbol.Base + symbol.Quote;

                CryptoDatabase databaseThread = new();
                try
                {
                    databaseThread.Open();
                    var transaction = databaseThread.BeginTransaction();
                    try
                    {
                        databaseThread.Connection.Insert(symbol, transaction);
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
                finally
                {
                    databaseThread.Close();
                }

                GlobalData.AddSymbol(symbol);
            }
            symbol.Status = 1;
            return symbol;
        }
        return null;
    }
    

    private static void CalculateBarometerInternal(CryptoSymbol bmSymbol, CryptoInterval interval, CryptoQuoteData quoteData, CalcBarometerMethod calcBarometerMethod, bool priceBarometer)
    {
        //if (priceBarometer)
        //    GlobalData.AddTextToLogTab($"Calculating price barometer chart {quoteData.Name} {interval.Name}");
        //else
        //    GlobalData.AddTextToLogTab($"Calculating volume barometer chart {quoteData.Name} {interval.Name}");

        CryptoSymbolInterval symbolInterval = bmSymbol.GetSymbolInterval(interval.IntervalPeriod);
        SortedList<long, CryptoCandle> candles = symbolInterval.CandleList;

        // Remove old candles from the barometer symbol (< 24 hours, 1440 candles)
        if (!GlobalData.BackTest)
        {
            long startFetchUnix = CandleIndicatorData.GetCandleFetchStart(bmSymbol, interval, DateTime.UtcNow);
            while (candles.Values.Count > 0)
            {
                CryptoCandle c = candles.Values[0];
                if (c.OpenTime < startFetchUnix)
                    candles.Remove(c.OpenTime);
                else break;
            }
        }


        long periodStart, periodStop;

        BarometerData? barometerData = GlobalData.ActiveAccount!.Data.GetBarometer(quoteData.Name, interval.IntervalPeriod);

        if (GlobalData.BackTest)
        {
            if (GlobalData.BackTestCandle == null)
                return;

            // Just 1 is okay
            periodStart = GlobalData.BackTestCandle!.OpenTime;
            periodStop = GlobalData.BackTestCandle!.OpenTime;
        }
        else
        {
            // Begin van de candle in interval X, bereken het laatste interval opnieuw (bewust)
            if (symbolInterval.LastCandleSynchronized.HasValue)
                periodStart = (long)symbolInterval.LastCandleSynchronized;
            else
            {
                // Geef deze alvast een waarde
                if (candles.Count > 0)
                    periodStart = candles.Values.First().OpenTime;
                else
                    periodStart = CandleTools.GetUnixTime(DateTime.UtcNow.AddDays(-2), 60); // GlobalData.GetCurrentDateTime(position.TradeAccount) oeps

                symbolInterval.LastCandleSynchronized = periodStart;
            }

            // De laatste candle die we moeten berekenen. Mogelijk 1 te hoog, wat "valse" waarden kan geven?
            // Dat kan opgelost worden door de laatst aangekomen candle mee te geven (vanuit de 1m stream)
            periodStop = CandleTools.GetUnixTime(DateTime.UtcNow, 60); // GlobalData.GetCurrentDateTime(position.TradeAccount) oeps
        }
        //DateTime periodStartDebug = CandleTools.GetUnixDate(periodStart);
        //DateTime periodStopDebug = CandleTools.GetUnixDate(periodStop);


        // De opgegeven periode per minuut itereren
        while (periodStart <= periodStop)
        {
            //periodStartDebug = CandleTools.GetUnixDate(periodStart);

            // Bereken de 1e waarde (alleen candle aanmaken als er candles bestaan voor beide intervallen)
            if (calcBarometerMethod(quoteData, bmSymbol.Exchange.SymbolListName, interval, periodStart, out decimal BarometerPerc))
            {
                // De candle aanmaken of bijwerken
                if (!candles.TryGetValue(periodStart, out CryptoCandle? candle))
                {
                    candle = new CryptoCandle
                    {
                        OpenTime = periodStart,
                    };
                    candles.Add(candle.OpenTime, candle);
                }

                // Alle waarden invullen
                candle.Open = BarometerPerc;
                candle.High = BarometerPerc;
                candle.Low = BarometerPerc;
                candle.Close = BarometerPerc;

                
                // Administratie bijwerken
                if (priceBarometer)
                {
                    barometerData.PriceDateTime = periodStart;
                    barometerData.PriceBarometer = BarometerPerc;
                }
                else
                {
                    barometerData.VolumeDateTime = periodStart;
                    barometerData.VolumeBarometer = BarometerPerc;
                }

                // Willen we dat hier wel bijwerken, zie ook opmerking hierboven
                if (periodStart > symbolInterval.LastCandleSynchronized)
                    symbolInterval.LastCandleSynchronized = periodStart;
            }

            // Naar de volgende 1m candle 
            periodStart += 60;
        }
    }

    /// <summary>
    /// Deze routine maakt barometer per 1m (ondanks dat we met de IntervalPeriod suggereren dat we het in een bepaald interval doen)
    /// </summary>
    private static void CalculateBarometerIntervals(CryptoSymbol symbol, CryptoQuoteData quoteData, CalcBarometerMethod calcBarometerMethod, bool pricebarometer)
    {

        // Herbereken de candles in de andere intervallen (voor de 15m, 30m, 1h, 4h en 1d)
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            if (interval.IntervalPeriod == CryptoIntervalPeriod.interval15m ||
                interval.IntervalPeriod == CryptoIntervalPeriod.interval30m ||
                interval.IntervalPeriod == CryptoIntervalPeriod.interval1h ||
                interval.IntervalPeriod == CryptoIntervalPeriod.interval4h ||
                interval.IntervalPeriod >= CryptoIntervalPeriod.interval1d)
            {
                //GlobalData.AddTextToLogTab("Calculating barometer chart " + bmSymbol.Name + " " + interval.Name);
                CalculateBarometerInternal(symbol, interval, quoteData, calcBarometerMethod, pricebarometer);
            }
        }
    }

    // Separate call because of emulator (calculate only 1 quote)
    public void CalculatePriceBarometerForQuote(CryptoQuoteData quoteData)
    {
        CryptoSymbol? symbol = CheckBarometerSymbolPrecence(Constants.SymbolNameBarometerPrice, quoteData);
        if (symbol != null)
        {
            CalculateBarometerIntervals(symbol, quoteData, BarometerPrice.CalculatePriceBarometer, true);
        }
    }


    public void CalculatePriceBarometerForAllQuotes()
    {
        // Bereken de (prijs en volume) barometers voor de aangevinkte basismunten
        //GlobalData.AddTextToLogTab("Calculating barometer for all quotes");
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values.ToList())
        {
            if (quoteData.FetchCandles)
                CalculatePriceBarometerForQuote(quoteData);
        }
    }


    public void ExecuteAsync()
    {
        try
        {
            if (Monitor.TryEnter(LockObject))
            {
                try
                {
                    CalculatePriceBarometerForAllQuotes();
                }
                finally
                {
                    Monitor.Exit(LockObject);
                }

                // Nu de barometer uitgerekend is mag het aantal 1m candles naar beneden
                CandleIndicatorData.SetInitialCandleCountFetch(24 * 60 * 60 + 10 * 60); // 10 extra, maar dat is een quick en dirty fix voor iets anders
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("");
            GlobalData.AddTextToLogTab(error.ToString());
        }
    }
}
