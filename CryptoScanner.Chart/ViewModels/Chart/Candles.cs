using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanner.Chart.ViewModels.Chart;

public class Candles
{

    public static CandleTime Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval,
        List<CryptoCandle> candles, CandleTime minDate, CandleTime maxDate, string group)
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
            YAxisKey = "price",
            Tag = group,
        };

        if (candles.Count > 0)
        {
            CryptoCandle last = default;
            foreach (var c in candles)
            {
                if (c.OpenTime >= minDate && c.OpenTime <= maxDate)
                {
                    try
                    {
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
            // Shared with the Photino chart, see CandleTools.BuildRunningCandles for the reasoning.
            foreach (CryptoCandle c in CandleTools.BuildRunningCandles(symbol, symbolInterval.Interval,
                last.OpenTime, minDate, maxDate))
            {
                var curHighLow = new HighLowItem(c.OpenTime.Minutes, (double)c.High, (double)c.Low, (double)c.Open, (double)c.Close);
                candleSerie.Items.Add(curHighLow);
            }
        }
        chart.Series.Add(candleSerie);

        return lastCandleTime;
    }


}
