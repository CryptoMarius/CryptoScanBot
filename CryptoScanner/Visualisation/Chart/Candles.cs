using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanner.Visualisation.Chart;

public class Candles
{

    public static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval, long minDate, long maxDate)
    {

        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        var candleSerie = new CandleStickSeries
        {
            Title = "Candles",
            //DecreasingColor = OxyColors.Red,
            DecreasingColor = OxyColors.DarkOrange,
            Color = OxyColors.Black,
            //IncreasingColor = OxyColors.LightGreen,
            IncreasingColor = OxyColors.DarkGreen,
            //TrackerFormatString = "Time: {0}\nHigh: {1}\nLow: {2}\nOpen: {3}\nClose: {4}"
            //The default format string for CandleStickSeries is "{0}\n{1}: {2}\nHigh: {3:0.###}\nLow: {4:0.###}\nOpen: {5:0.###}\nClose: {6:0.###}"
            //TrackerFormatString = "{0}\n {1}\n {2}\nOpen: {5:0.###}\nHigh: {3:0.###}\nLow: {4:0.###}\nClose: {6:0.###} {DateX}"
            //TrackerFormatString = "Open: {5}\nHigh: {3}\nLow: {4}\nClose: {6}\n{Description}"
            TrackerFormatString = "Open: {5}\nHigh: {3}\nLow: {4}\nClose: {6}"
        };

        if (symbolInterval.CandleList.Count > 0)
        {
            CryptoCandle? last = null;
            foreach (var c in symbolInterval.CandleList.Values)
            {
                if (c.OpenTime >= minDate && c.OpenTime <= maxDate)
                {
                    try
                    {
                        //var curHighLow = new MyHighLowItem(c.Time.ToString(), c.OpenTime, (double)c.High, (double)c.Low, (double)c.Open, (double)c.Close); //OhlcvItem
                        var curHighLow = new HighLowItem(c.OpenTime, (double)c.High, (double)c.Low, (double)c.Open, (double)c.Close); //OhlcvItem
                        candleSerie.Items.Add(curHighLow);
                        last = c;
                    }
                    catch (Exception error)
                    {
                        // daytimesaving problemo?
                        //
                        ScannerLog.Logger.Info($"Error {error}");
                    }
                }
            }



            // Build the last candle(s) from scratch using the 1m candles
            if (last != null)
            {
                long loopHighInterval = last.OpenTime + symbolInterval.Interval.Duration;
                CryptoSymbolInterval symbolInterval1m = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
                while (symbolInterval1m.CandleList.TryGetValue(loopHighInterval, out CryptoCandle? _))
                {
                    long loop1m = loopHighInterval;
                    long loop1mMax = loopHighInterval + symbolInterval.Interval.Duration;
                    CryptoCandle newCandle = new()
                    {
                        Low = decimal.MaxValue,
                        High = decimal.MinValue
                    };
                    while (loop1m < loop1mMax && symbolInterval1m.CandleList.TryGetValue(loop1m, out CryptoCandle? c))
                    {
                        if (newCandle.OpenTime == 0)
                        {
                            newCandle.OpenTime = c.OpenTime;
                            newCandle.Open = c.Open;
                        }
                        if (c.Low < newCandle.Low)
                            newCandle.Low = c.Low;
                        if (c.High > newCandle.High)
                            newCandle.High = c.High;
                        newCandle.Close = c.Close;
                        loop1m += symbolInterval1m.Interval.Duration;
                    }
                    if (newCandle.OpenTime > 0 && newCandle.OpenTime >= minDate && newCandle.OpenTime <= maxDate)
                    {
                        var c = newCandle;
                        var curHighLow = new HighLowItem(newCandle.OpenTime, (double)c.High, (double)c.Low, (double)c.Open, (double)c.Close);
                        candleSerie.Items.Add(curHighLow);
                    }

                    loopHighInterval += symbolInterval.Interval.Duration;
                }
            }
        }
        chart.Series.Add(candleSerie);
    }


}
