//using CryptoScanBot.Core.Core;
//using CryptoScanBot.Core.Model;

//using OxyPlot;
//using OxyPlot.Series;

//using Skender.Stock.Indicators;

//namespace CryptoScanBot.ZoneVisualisation.Chart;

//public class NadarayaWatsonEnvelopeNonRepainting
//{
//    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval, long minDate, long maxDate)
//    {
//        //TODO: Honour Min & Max Date!

//        var seriesHigh = new LineSeries { Title = "n high", MarkerSize = 1, MarkerFill = OxyColors.DarkGray, Color = OxyColors.DarkGray };
//        var seriesMiddle = new LineSeries { Title = "n middle", MarkerSize = 1, MarkerFill = OxyColors.Gray, Color = OxyColors.Gray };
//        var seriesLow = new LineSeries { Title = "n low", MarkerSize = 1, MarkerFill = OxyColors.DarkGray, Color = OxyColors.DarkGray };
//        var seriesBuy = new ScatterSeries { Title = "n buy", MarkerSize = 3, MarkerFill = OxyColors.White, MarkerType = MarkerType.Square, MarkerStrokeThickness = 1.5 };
//        var seriesSell = new ScatterSeries { Title = "n sell", MarkerSize = 3, MarkerFill = OxyColors.White, MarkerType = MarkerType.Square, MarkerStrokeThickness = 1.5 };

//        // configuration:
//        decimal h = GlobalData.Settings.Signal.Nwe.BandWidth;
//        decimal mult = GlobalData.Settings.Signal.Nwe.Multiplication;


//        // Iterate the last 500 candles
//        int maxlen = 500;
//        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
//        if (symbolInterval.CandleList.Count == 0)
//            return;

//        CryptoCandleList candles = [];
//        foreach (var c in symbolInterval.CandleList.Values)
//        {
//            if (c.OpenTime >= minDate && c.OpenTime <= maxDate)
//            {
//                candles.Add(c.OpenTime, c);
//            }
//        }

//        decimal den = 0;
//        List<decimal> coefs = [];
//        for (int i = 0; i < 499; i++)
//        {
//            decimal w = (decimal)Math.Exp(-(Math.Pow(i, 2)) / (double)(h * h * 2));
//            coefs.Add(w);
//            den += w;
//        }

//        long offsett = candles.Values.Last().OpenTime; // - max * interval.Duration;

//        decimal outx = 0;
//        for (int i = 0; i < 499; i++)
//        {
//            if (candles.TryGetValue(offsett - i * interval.Duration, out CryptoCandle? candlei))
//                outx += candlei.Close * coefs[i];
//        }
//        outx /= den;
//        decimal mae = ta.sma(math.abs(src - outx), 499) * mult;

//        int n = candles.Count;
//        int max = Math.Min(maxlen, n - 1);


//        for (int i = 0; i < max; i++)
//        {
//            if (candles.TryGetValue(offsett - (i + 0) * interval.Duration, out CryptoCandle? candleLast) &&
//                candles.TryGetValue(offsett - (i + 1) * interval.Duration, out CryptoCandle? candlePrev))
//            {
//                long openTime = CandleTools.GetUnixTime(candleLast.Date, interval.Duration);
//                if (openTime >= minDate && openTime <= maxDate)
//                {

//                    decimal nwevalue = nwe[i];
//                    decimal upperband = nwevalue + sae;
//                    decimal lowerband = nwevalue - sae;

//                    seriesLow.Points.Add(new DataPoint(candleLast.OpenTime, (double)lowerband));
//                    seriesMiddle.Points.Add(new DataPoint(candleLast.OpenTime, (double)nwevalue));
//                    seriesHigh.Points.Add(new DataPoint(candleLast.OpenTime, (double)upperband));

//                    // buy alert
//                    // Candle outside the band
//                    //if (candleLast!.Open <= lowerband && candleLast.Close <= lowerband)
//                    //{
//                    //    nwevalue = candleLast.Low * 0.995m;
//                    //    seriesBuy.Points.Add(new ScatterPoint(candleLast.OpenTime, (double)nwevalue));
//                    //}
//                    // Candle sticking pearsing trough the band
//                    if (candlePrev!.Close > lowerband && candleLast.Close <= lowerband)
//                    {
//                        nwevalue = candleLast.Low * 0.995m;
//                        seriesBuy.Points.Add(new ScatterPoint(candleLast.OpenTime, (double)nwevalue));
//                    }

//                    // sell alert
//                    // Candle outside the band
//                    //if (candleLast!.Open >= upperband && candleLast.Close >= upperband)
//                    //{
//                    //    nwevalue = candleLast.High * 1.005m;
//                    //    seriesSell.Points.Add(new ScatterPoint(candleLast.OpenTime, (double)nwevalue));
//                    //}
//                    // Candle sticking pearsing trough the band
//                    if (candlePrev!.Close < upperband && candleLast.Close >= upperband)
//                    {
//                        nwevalue = candleLast.High * 1.005m;
//                        seriesSell.Points.Add(new ScatterPoint(candleLast.OpenTime, (double)nwevalue));
//                    }


//                    if (i > 0)
//                    {
//                        if (smaList20[i - 0].Sma != null && smaList20[i - 1].Sma != null)
//                        {
//                            double nweLast = (double)nwe[i - 0];
//                            double nwePrev = (double)nwe[i - 1];

//                            double smaLast = (double)smaList20[i - 0].Sma!.Value;
//                            double smaPrev = (double)smaList20[i - 1].Sma!.Value;

//                            if (// buy alert when the nwe.lower crosses the sma20 upwards
//                                (nwePrev - (double)sae < smaPrev! && nweLast - (double)sae >= smaLast) ||
//                                // sell alert when the nwe.upper crosses the sma20 downwards
//                                (nwePrev + (double)sae > smaPrev! && nweLast + (double)sae <= smaLast))
//                            {
//                                seriesBuy.Points.Add(new ScatterPoint(candleLast.OpenTime, (double)smaLast));
//                            }
//                        }
//                    }
//                }
//            }
//        }

//        chart.Series.Add(seriesLow);        
//        chart.Series.Add(seriesMiddle);
//        chart.Series.Add(seriesHigh);
//        chart.Series.Add(seriesBuy);
//        chart.Series.Add(seriesSell);
//    }
//}
