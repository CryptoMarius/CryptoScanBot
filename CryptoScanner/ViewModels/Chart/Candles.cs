using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanner.ViewModels.Chart;

public class Candles
{

    public static CandleTime Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval, 
        CandleTime minDate, CandleTime maxDate, string group)
    {
        CandleTime lastCandleTime = CandleTime.MinValue;
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
            TrackerFormatString = "Open: {5}\nHigh: {3}\nLow: {4}\nClose: {6}",
            Tag = group,
        };

        if (symbolInterval.CandleList.Count > 0)
        {
            CryptoCandle last = default;
            foreach (var c in symbolInterval.CandleList.Values)
            {
                if (c.OpenTime >= minDate && c.OpenTime <= maxDate)
                {
                    try
                    {
                        //var curHighLow = new MyHighLowItem(c.Time.ToString(), c.OpenTime, (double)c.High, (double)c.Low, (double)c.Open, (double)c.Close); //OhlcvItem
                        var curHighLow = new HighLowItem(c.OpenTime.Minutes, (double)c.High, (double)c.Low, (double)c.Open, (double)c.Close); //OhlcvItem
                        candleSerie.Items.Add(curHighLow);
                        last = c;

                        lastCandleTime = c.OpenTime;
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
            if (last.OpenTime != 0)
            {
                CandleTime loopHighInterval = last.OpenTime + symbolInterval.Interval.Duration;
                CryptoSymbolInterval symbolInterval1m = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
                while (symbolInterval1m.CandleList.TryGetValue(loopHighInterval, out CryptoCandle _))
                {
                    CandleTime loop1m = loopHighInterval;
                    CandleTime loop1mMax = loopHighInterval + symbolInterval.Interval.Duration;

                    CandleTime openTime = new(0);
                    decimal open = 0, high = 0, low = 0, close = 0;
                    while (loop1m < loop1mMax && symbolInterval1m.CandleList.TryGetValue(loop1m, out CryptoCandle c))
                    {
                        if (openTime == 0)
                        {
                            openTime = c!.OpenTime;
                            open = c.Open;
                            low = c.Open;
                            high = c.Open;
                        }
                        if (c.Low < low)
                            low = c.Low;
                        if (c.High > high)
                            high = c.High;
                        close = c.Close;
                        loop1m += symbolInterval1m.Interval.Duration;
                    }
                    if (openTime > 0 && openTime >= minDate && openTime <= maxDate)
                    {
                        CryptoCandle newCandle = new()
                        {
                            TickDecimals = symbol.PriceDecimals,
                            OpenTime = openTime,
                            Open = open,
                            High = high,
                            Low = low,
                            Close = close,
                            Volume = 0,
                        };

                        var c = newCandle;
                        var curHighLow = new HighLowItem(newCandle.OpenTime.Minutes, (double)c.High, (double)c.Low, (double)c.Open, (double)c.Close);
                        candleSerie.Items.Add(curHighLow);
                    }
                    loopHighInterval += symbolInterval.Interval.Duration;
                }
            }
        }
        chart.Series.Add(candleSerie);

        return lastCandleTime;
    }


}
