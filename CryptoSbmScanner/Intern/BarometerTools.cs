using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CryptoSbmScanner
{

    public class BarometerTools
    {

        private Object LockObject = new Object();
        private delegate bool CalcBarometerMethod(CryptoQuoteData quoteData, SortedList<string, CryptoSymbol> symbols, CryptoInterval interval, long unixCandleLast, out decimal barometerPerc);


        private CryptoSymbol CheckSymbolPrecence(string baseName, CryptoQuoteData quoteData)
        {
            CryptoExchange exchange;
            if (GlobalData.ExchangeListName.TryGetValue("Binance", out exchange))
            {
                CryptoSymbol symbol;
                if (!exchange.SymbolListName.TryGetValue(baseName + quoteData.Name, out symbol))
                {
                    //using (SqlConnection databaseThread = new SqlConnection(GlobalData.ConnectionString))
                    {
                        //databaseThread.Close();
                        //databaseThread.Open();

                        //using (SqlTransaction transaction = databaseThread.BeginTransaction())
                        {
                            symbol = new CryptoSymbol();
                            symbol.Base = baseName; //De "munt"
                            symbol.Quote = quoteData.Name; //USDT, BTC etc.
                            symbol.Name = symbol.Base + symbol.Quote;
                            symbol.Volume = 0;
                            symbol.Status = 1;
                            symbol.Exchange = exchange;
                            GlobalData.AddSymbol(symbol);
                        }
                    }
                }
                return symbol;
            }
            return null;
        }



        private bool CalculatePriceBarometer(CryptoQuoteData quoteData, SortedList<string, CryptoSymbol> symbols, CryptoInterval interval, long unixCandleLast, out decimal barometerPerc)
        {
            // Wat is de candle in het vorige interval
            long unixCandlePrev = unixCandleLast - interval.Duration;

            // Ter debug van de intervallen (unix datetime's zijn slecht leesbaar)
            //DateTime dateCandlePrev = CandleTools.GetUnixDate(unixCandlePrev);
            //DateTime dateCandleLast = CandleTools.GetUnixDate(unixCandleLast);

            decimal sumPerc = 0;
            int coinsMatching = 0;
            // De prijs en/of volume sommeren over alle munten
            foreach (var symbol in quoteData.SymbolList)
            {
                CryptoCandle candlePrev, candleLast;
                if (symbol.CandleList.TryGetValue(unixCandlePrev, out candlePrev) && symbol.CandleList.TryGetValue(unixCandleLast, out candleLast))
                {
                    decimal perc;
                    decimal diff = candleLast.Close - candlePrev.Close;
                    if (!candlePrev.Close.Equals(0))
                        perc = 100m * (diff / candlePrev.Close);
                    else perc = 0;

                    sumPerc += perc;
                    coinsMatching++;
                }
            }

            if (coinsMatching > 0)
            {
                decimal result = sumPerc / coinsMatching;
                barometerPerc = decimal.Round(result, 8);
            }
            else
                barometerPerc = -99m;

            // Met 1 munt krijgen we dus een true, mhhhh
            return coinsMatching > 0;
        }


        private void CalculateBarometerInternal(CryptoSymbol bmSymbol, CryptoInterval interval, CryptoQuoteData quoteData, CalcBarometerMethod calcBarometerMethod, bool pricebarometer)
        {
            CryptoSymbolInterval symbolInterval = bmSymbol.GetSymbolInterval(interval.IntervalPeriod);
            SortedList<long, CryptoCandle> candles = symbolInterval.CandleList;

            BarometerData barometerData = quoteData.BarometerList[(long)interval.IntervalPeriod];

            // Begin van de candle in interval X, bereken het laatste interval opnieuw (bewust)
            long periodStart;
            if (symbolInterval.LastCandleSynchronized.HasValue)
                periodStart = (long)symbolInterval.LastCandleSynchronized;
            else
            {
                // Geef deze alvast een waarde
                if (candles.Values.Any()) 
                    periodStart = candles.Values.First().OpenTime;
                else
                    periodStart = CandleTools.GetUnixTime(DateTime.UtcNow.AddDays(-2), 60);

                symbolInterval.LastCandleSynchronized = periodStart;
            }
            //DateTime periodStartDebug = CandleTools.GetUnixDate(periodStart);

            // De laatste candle die we moeten berekenen. Mogelijk 1 te hoog, wat "valse" waarden kan geven?
            // Dat kan opgelost worden door de laatst aangekomen candle mee te geven (vanuit de 1m stream)
            long periodStop = CandleTools.GetUnixTime(DateTime.UtcNow, 60);
            //DateTime periodStopDebug = CandleTools.GetUnixDate(periodStop);

            // De opgegeven periode per minuut itereren
            while (periodStart <= periodStop)
            {
                //periodStartDebug = CandleTools.GetUnixDate(periodStart);

                // Bereken de 1e waarde (alleen candle aanmaken als er candles bestaan voor beide intervallen)
                decimal BarometerPerc;
                if (calcBarometerMethod(quoteData, bmSymbol.Exchange.SymbolListName, interval, periodStart, out BarometerPerc))
                {
                    // De candle aanmaken of bijwerken
                    CryptoCandle candle;
                    if (!candles.TryGetValue(periodStart, out candle))
                    {
                        candle = new CryptoCandle();
                        candle.Symbol = bmSymbol;
                        candle.OpenTime = periodStart;
                        candle.Interval = interval;
                        candles.Add(candle.OpenTime, candle);
                    }

                    // Alle waarden invullen
                    candle.Open = BarometerPerc;
                    candle.High = BarometerPerc;
                    candle.Low = BarometerPerc;
                    candle.Close = BarometerPerc;

                    // Administratie bijwerken
                    if (pricebarometer)
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
                    {
                        //candleFetched.IsChanged = true;
                        symbolInterval.LastCandleSynchronized = periodStart;
                    }
                }

                // Naar de volgende 1m candle 
                periodStart += 60;
            }
        }

        /// <summary>
        /// Deze routine maakt barometer per 1m (ondanks dat we met de IntervalPeriod suggereren dat we het in een bepaald interval doen)
        /// </summary>
        private void CalculateBarometerIntervals(CryptoSymbol symbol, CryptoQuoteData quoteData, CalcBarometerMethod calcBarometerMethod, bool pricebarometer)
        {

            // Herbereken de candles in de andere intervallen (voor de 1h, 4h en 1d)
            foreach (CryptoInterval interval in GlobalData.IntervalList)
            {
                if ((interval.IntervalPeriod == CryptoIntervalPeriod.interval1h) || 
                    (interval.IntervalPeriod == CryptoIntervalPeriod.interval4h) ||
                    (interval.IntervalPeriod == CryptoIntervalPeriod.interval1d))
                {
                    //GlobalData.AddTextToLogTab("Calculating barometer chart " + bmSymbol.Name + " " + interval.Name);
                    CalculateBarometerInternal(symbol, interval, quoteData, calcBarometerMethod, pricebarometer);
                }
            }
        }


        public void ExecuteInternal()
        {
            // Bereken de (prijs en volume) barometers voor de aangevinkte basismunten

            //GlobalData.AddTextToLogTab("Calculating barometer charts start");
            foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
            {
                if (quoteData.FetchCandles)
                {
                    Monitor.Enter(quoteData.SymbolList);
                    try
                    {
                        //GlobalData.AddTextToLogTab(string.Format("Calculating barometer charts start for {0}", quoteData.Name));

                        // Controleer of de prijs barometer symbol bestaat en berekenen
                        //GlobalData.AddTextToLogTab("Calculating price barometer chart " + baseCoin");
                        CryptoSymbol symbol = CheckSymbolPrecence(Constants.SymbolNameBarometerPrice, quoteData);
                        if (symbol != null)
                        {
                            Monitor.Enter(symbol.CandleList);
                            try
                            {
                                CalculateBarometerIntervals(symbol, quoteData, CalculatePriceBarometer, true);
                            }
                            finally
                            {
                                Monitor.Exit(symbol.CandleList);
                            }
                        }

                        // Ik weet niet wat ik met de volume barometer kan (of moet aanvangen, laat maar achterwege)
                        // Controleer of de volume barometer symbol bestaat en berekenen (CPU gaat behoorlijk omhoog)
                        //GlobalData.AddTextToLogTab("Calculating volume barometer chart " + baseCoin");
                        //symbol = CheckSymbolPrecence(Constants.SymbolNameBarometerVolume, quoteData);
                        //if (symbol != null)
                        //{
                        //    Monitor.Enter(symbol.CandleList);
                        //    try
                        //    {
                        //        CalculateBarometerIntervals(symbol, quoteData, CalculateVolumeBarometer, false);
                        //    }
                        //    finally
                        //    {
                        //        Monitor.Exit(symbol.CandleList);
                        //    }
                        //}
                    }
                    finally
                    {
                        Monitor.Exit(quoteData.SymbolList);
                    }
                }
            }

            // Nu de barometer uitgerekend is mag het aantal 1m candles naar beneden
            CandleIndicatorData.InitialCandleCountFetch = (24 * 60 * 60) + (1 * 60);
        }


        public void Execute()
        {
            try
            {
                if (Monitor.TryEnter(LockObject))
                {
                    try
                    {
                        ExecuteInternal();
                    }
                    finally
                    {
                        Monitor.Exit(LockObject);
                    }
                } 
                else
                    GlobalData.AddTextToLogTab("");
            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab("");
                GlobalData.AddTextToLogTab(error.ToString());
            }
        }
    }
}
