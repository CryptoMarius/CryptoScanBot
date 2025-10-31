//using Skender.Stock.Indicators;
//using CryptoScanBot.Core.Model;

//// MQL5 Market – “BBMA Oma Ally Labels” indicator
//// Deze lijkt niet zo compleet te zijn..

//namespace CryptoScanBot.TestStuff.BBMA.MQL5Market
//{
//    //// Simpele candle / quote representatie
//    //public class Candle
//    //{
//    //    public DateTime Time { get; set; }
//    //    public decimal Open { get; set; }
//    //    public decimal High { get; set; }
//    //    public decimal Low { get; set; }
//    //    public decimal Close { get; set; }
//    //    public decimal Volume { get; set; }

//    //    public Candle() { }

//    //    public Candle(DateTime time, decimal o, decimal h, decimal l, decimal c, decimal v = 0)
//    //    {
//    //        Time = time;
//    //        Open = o;
//    //        High = h;
//    //        Low = l;
//    //        Close = c;
//    //        Volume = v;
//    //    }
//    //}

//    // Resultaat van BBMA-analyse per candle
//    public class BBMAResult
//    {
//        public DateTime Time { get; set; }
//        public bool IsExtreme { get; set; }
//        public bool IsCSAK { get; set; }    // strong continuation candle (bearish/bullish)
//        public bool IsCSD { get; set; }     // candle setup/demand?
//        public bool IsMHV { get; set; }     // MA High/Low Violation
//        public bool IsReEntry { get; set; } // re-entry signal
//    }

//    /// <summary>
//    /// Wrapper die Skender.Stock.Indicators gebruikt om indicatorreeksen te berekenen.
//    /// Houdt geen globale state; elke call is functioneel.
//    /// </summary>
//    public interface IIndicatorProvider
//    {
//        // retourneren series voor eenvoudige gebruik
//        IEnumerable<SmaResult> GetSma(IEnumerable<Quote> quotes, int period);
//        IEnumerable<EmaResult> GetEma(IEnumerable<Quote> quotes, int period);
//        IEnumerable<BollingerBandsResult> GetBollingerBands(IEnumerable<Quote> quotes, int period, decimal sd);
//        // Indien je meer indicatoren nodig hebt, voeg hier toe
//    }

//    public class SkenderIndicatorProvider : IIndicatorProvider
//    {
//        public IEnumerable<SmaResult> GetSma(IEnumerable<Quote> quotes, int period)
//        {
//            return quotes.GetSma(period);
//        }

//        public IEnumerable<EmaResult> GetEma(IEnumerable<Quote> quotes, int period)
//        {
//            return quotes.GetEma(period);
//        }

//        public IEnumerable<BollingerBandsResult> GetBollingerBands(IEnumerable<Quote> quotes, int period, decimal sd)
//        {
//            return quotes.GetBollingerBands(period, (double)sd); // Skender signature often uses double for sd
//        }
//    }

//    // Helper to convert our Candle -> Skender Quote
//    public static class QuoteConverter
//    {
//        public static IEnumerable<Quote> ToQuotes(IEnumerable<CryptoCandle> candles)
//        {
//            return candles.Select(c => new Quote
//            {
//                Date = c.Date,
//                Open = c.Open,
//                High = c.High,
//                Low = c.Low,
//                Close = c.Close,
//                Volume = c.Volume
//            });
//        }


//        /// <summary>
//        /// BBMA-analyzer: detecteert Extreme, CSAK, CSD, MHV, Re-entry op basis van candle-series.
//        /// Objectgeoriënteerd: instelbare parameters via constructor.
//        /// </summary>
//        public class BBMAAnalyzer
//        {
//            private readonly IIndicatorProvider _indicators;
//            public int MaFast { get; }
//            public int MaMedium { get; }
//            public int MaSlow { get; }

//            public BBMAAnalyzer(IIndicatorProvider indicatorProvider,
//                                int maFast = 5, int maMedium = 21, int maSlow = 55)
//            {
//                _indicators = indicatorProvider ?? throw new ArgumentNullException(nameof(indicatorProvider));
//                MaFast = maFast;
//                MaMedium = maMedium;
//                MaSlow = maSlow;
//            }

//            /// <summary>
//            /// Analyseer candle-reeks en retourneer BBMAResult per candle (nulls mogelijk in eerste records).
//            /// </summary>
//            public List<BBMAResult> Analyze(CryptoCandleList candles)
//            {
//                var quotes = ToQuotes(candles.Values).ToList();

//                // Bereken indicatorkernen
//                var smaFast = _indicators.GetSma(quotes, MaFast).ToList();
//                var smaMed = _indicators.GetSma(quotes, MaMedium).ToList();
//                var smaSlow = _indicators.GetSma(quotes, MaSlow).ToList();
//                var bb = _indicators.GetBollingerBands(quotes, 20, 2.0m).ToList();

//                // Map results per index/time
//                var results = new List<BBMAResult>();

//                foreach (var c in candles.Values)
//                {
//                    var r = new BBMAResult { Time = c.Date };

//                    // fetch indicator values (null-safe)
//                    decimal? maFastVal = GetIndicatorValue(smaFast, i, x => (decimal)x.Sma);
//                    decimal? maMedVal = GetIndicatorValue(smaMed, i, x => (decimal)x.Sma);
//                    decimal? maSlowVal = GetIndicatorValue(smaSlow, i, x => (decimal)x.Sma);
//                    var bbVal = SafeGet(bb, i);

//                    // Extreme: close buiten Bollingerbanden (boven upper of onder lower)
//                    if (bbVal != null)
//                    {
//                        if (c.Close > (decimal)bbVal.UpperBand) r.IsExtreme = true;
//                        else if (c.Close < (decimal)bbVal.LowerBand) r.IsExtreme = true;
//                    }

//                    // CSAK (voorbeeld-interpretatie):
//                    // CSAK = "Continuation Strong" - lange body in richting trend en sluit buiten fast MA, often large body.
//                    // Hier: body > 0.6 * range & close on same side of MAfast & direction strong
//                    decimal body = Math.Abs(c.Close - c.Open);
//                    decimal range = c.High - c.Low;
//                    bool sizeableBody = range > 0 ? (body / range) >= 0.6m : false;

//                    if (maFastVal.HasValue)
//                    {
//                        // bullish CSAK
//                        if (c.Close > c.Open && c.Close > maFastVal && sizeableBody)
//                            r.IsCSAK = true;
//                        // bearish CSAK
//                        if (c.Close < c.Open && c.Close < maFastVal && sizeableBody)
//                            r.IsCSAK = true;
//                    }

//                    // CSD (voorbeeld): small body near MA with wick structure (setup)
//                    bool smallBody = range > 0 ? (body / range) <= 0.3m : false;
//                    if (maMedVal.HasValue)
//                    {
//                        // CSD bullish setup: close > maMed but small body touching maMed area
//                        decimal distanceToMa = Math.Abs(c.Close - maMedVal.Value);
//                        decimal relDist = maMedVal.Value > 0 ? distanceToMa / maMedVal.Value : 0;
//                        if (smallBody && relDist < 0.002m) // 0.2% proximity heuristic
//                            r.IsCSD = true;
//                    }

//                    // MHV: MA High/Low Violation (prijs breekt MA level aan high/low kant)
//                    // Simpel: candle high > maSlow (bullish violation) of low < maSlow (bearish violation)
//                    if (maSlowVal.HasValue)
//                    {
//                        if (c.High > maSlowVal) r.IsMHV = true;
//                        if (c.Low < maSlowVal) r.IsMHV = true;
//                    }

//                    // ReEntry: pullback towards MAFast after extreme or after initial move
//                    // Heuristisch: price crosses back inside bb or touches MAfast after being extreme/CSAK
//                    if (i >= 1)
//                    {
//                        var prev = candles[i - 1].Value;
//                        // if previous was extreme and current closes nearer to maFast than prev
//                        if (r.IsExtreme == false && (/* previous extreme check */ false))
//                        {
//                            // placeholder - advanced logic could keep state of prior signals
//                        }

//                        // Simpeler re-entry: close within small distance of MA fast and direction matches
//                        if (maFastVal.HasValue)
//                        {
//                            decimal d = Math.Abs(c.Close - maFastVal.Value);
//                            if (maFastVal.Value > 0 && (d / maFastVal.Value) < 0.0025m && sizeableBody == false)
//                            {
//                                r.IsReEntry = true;
//                            }
//                        }
//                    }

//                    results.Add(r);
//                }

//                return results;
//            }

//            // helpers
//            private static T SafeGet<T>(IList<T> list, int idx) where T : class
//            {
//                if (idx < 0 || idx >= list.Count) return null;
//                return list[idx];
//            }

//            private static decimal? GetIndicatorValue<T>(IList<T> list, int idx, Func<T, decimal?> selector) where T : class
//            {
//                if (idx < 0 || idx >= list.Count) return null;
//                var item = list[idx];
//                if (item == null) return null;
//                return selector(item);
//            }
//        }

//    }

//}
