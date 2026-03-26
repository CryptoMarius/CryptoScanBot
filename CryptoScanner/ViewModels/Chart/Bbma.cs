using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.ViewModels.Chart;

public class Bbma
{
    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval, CandleTime minDate, CandleTime maxDate, string group)
    {
        var seriesWma5High = new LineSeries
        {
            Title = "wma5high",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkRed,
            Color = OxyColors.DarkRed,
            Tag = group,
        };
        var seriesWma10High = new LineSeries
        {
            Title = "wma10high",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkRed,
            Color = OxyColors.DarkRed,
            Tag = group,
        };

        var seriesWma5Low = new LineSeries
        {
            Title = "wma5low",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkGreen,
            Color = OxyColors.DarkGreen,
            Tag = group,
        };
        var seriesWma10Low = new LineSeries
        {
            Title = "wma10low",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkGreen,
            Color = OxyColors.DarkGreen,
            Tag = group,
        };

        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return;

        var candles = symbolInterval.CandleList.Values;
        List<WmaResult> wmaList05Low = (List<WmaResult>)candles.Use(CandlePart.Low).GetWma(05);
        List<WmaResult> wmaList05High = (List<WmaResult>)candles.Use(CandlePart.High).GetWma(05);
        List<WmaResult> wmaList10Low = (List<WmaResult>)candles.Use(CandlePart.Low).GetWma(10);
        List<WmaResult> wmaList10High = (List<WmaResult>)candles.Use(CandlePart.High).GetWma(10);

        foreach (var wma in wmaList05Low)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(wma.Date, interval.Duration);
            if (openTime >= minDate && openTime <= maxDate)
            {
                if (wma.Wma.HasValue)
                    seriesWma5Low.Points.Add(new DataPoint(openTime.Minutes, wma.Wma.Value));
            }
        }
        foreach (var wma in wmaList05High)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(wma.Date, interval.Duration);
            if (openTime >= minDate && openTime <= maxDate)
            {
                if (wma.Wma.HasValue)
                    seriesWma5High.Points.Add(new DataPoint(openTime.Minutes, wma.Wma.Value));
            }
        }

        foreach (var wma in wmaList10Low)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(wma.Date, interval.Duration);
            if (openTime >= minDate && openTime <= maxDate)
            {
                if (wma.Wma.HasValue)
                    seriesWma10Low.Points.Add(new DataPoint(openTime.Minutes, wma.Wma.Value));
            }
        }
        foreach (var wma in wmaList10High)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(wma.Date, interval.Duration);
            if (openTime >= minDate && openTime <= maxDate)
            {
                if (wma.Wma.HasValue)
                    seriesWma10High.Points.Add(new DataPoint(openTime.Minutes, wma.Wma.Value));
            }
        }
        chart.Series.Add(seriesWma5Low);
        chart.Series.Add(seriesWma10Low);
        chart.Series.Add(seriesWma5High);
        chart.Series.Add(seriesWma10High);


        var seriesEma50 = new LineSeries
        {
            Title = "ema50",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkOrange,
            Color = OxyColors.DarkOrange,
            Tag = group,
        };


        List<EmaResult> emaList = (List<EmaResult>)candles.Use(CandlePart.Close).GetEma(50);

        foreach (var ema in emaList)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(ema.Date, interval.Duration);
            if (openTime >= minDate && openTime <= maxDate)
            {
                if (ema.Ema.HasValue)
                    seriesEma50.Points.Add(new DataPoint(openTime.Minutes, ema.Ema.Value));
            }
        }
        chart.Series.Add(seriesEma50);
    }
}