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

        List<BollingerBandsResult> bollingerBandsList = (List<BollingerBandsResult>)candles.GetBollingerBands(
            lookbackPeriods: GlobalData.Settings.General.SettingsBb.Length, standardDeviations: GlobalData.Settings.General.SettingsBb.Deviation);

        // Filled band between WMA5-High and WMA10-High — dark red background.
        // Filled band between WMA5-Low and WMA10-Low — dark green background.
        // Both inserted at index 0 so they render behind candles and all other series.
        var seriesBandHigh = new AreaSeries
        {
            Title = "wma high band",
            Fill = OxyColor.FromArgb(120, 139, 0, 0),   // semi-transparent dark red
            Color = OxyColors.Transparent,
            StrokeThickness = 0,
            Tag = group,
        };
        var seriesBandLow = new AreaSeries
        {
            Title = "wma low band",
            Fill = OxyColor.FromArgb(120, 0, 100, 0),   // semi-transparent dark green
            Color = OxyColors.Transparent,
            StrokeThickness = 0,
            Tag = group,
        };

        // Scatter signals for extreme conditions (wma crosses outside Bollinger Band).
        // Extreme-A: only wma5 outside the band. Magic extreme: wma10 also outside the band.
        var seriesExtremeAHigh = new ScatterSeries
        {
            Title = "extreme-A high",
            MarkerSize = 4,
            MarkerFill = OxyColors.Red,
            MarkerType = MarkerType.Triangle,
            Tag = group,
        };
        var seriesMagicExtremeHigh = new ScatterSeries
        {
            Title = "magic extreme high",
            MarkerSize = 4,
            MarkerFill = OxyColors.OrangeRed,
            MarkerType = MarkerType.Triangle,
            Tag = group,
        };
        var seriesExtremeALow = new ScatterSeries
        {
            Title = "extreme-A low",
            MarkerSize = 4,
            MarkerFill = OxyColors.Yellow,
            MarkerType = MarkerType.Triangle,
            Tag = group,
        };
        var seriesMagicExtremeLow = new ScatterSeries
        {
            Title = "magic extreme low",
            MarkerSize = 4,
            MarkerFill = OxyColors.White,
            MarkerType = MarkerType.Triangle,
            Tag = group,
        };

        foreach (var (wma5, wma10, bb) in Enumerable.Zip(wmaList05High, wmaList10High, bollingerBandsList))
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(wma5.Date, interval.Duration);
            if (openTime >= minDate && openTime <= maxDate)
            {
                if (wma5.Wma.HasValue && wma10.Wma.HasValue)
                {
                    seriesBandHigh.Points.Add(new DataPoint(openTime.Minutes, wma5.Wma.Value));
                    seriesBandHigh.Points2.Add(new DataPoint(openTime.Minutes, wma10.Wma.Value));
                }
                if (wma5.Wma.HasValue)
                {
                    seriesWma5High.Points.Add(new DataPoint(openTime.Minutes, wma5.Wma.Value));
                    // Extreme-A: wma5 high above BB upper band
                    if (bb.UpperBand.HasValue && wma5.Wma.Value > bb.UpperBand.Value)
                        seriesExtremeAHigh.Points.Add(new ScatterPoint(openTime.Minutes, 1.005 * wma5.Wma.Value));
                }
                if (wma10.Wma.HasValue)
                {
                    seriesWma10High.Points.Add(new DataPoint(openTime.Minutes, wma10.Wma.Value));
                    // Magic extreme: wma10 high also above BB upper band
                    if (bb.UpperBand.HasValue && wma10.Wma.Value > bb.UpperBand.Value)
                        seriesMagicExtremeHigh.Points.Add(new ScatterPoint(openTime.Minutes, 1.005 * wma10.Wma.Value));
                }
            }
        }

        foreach (var (wma5, wma10, bb) in Enumerable.Zip(wmaList05Low, wmaList10Low, bollingerBandsList))
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(wma5.Date, interval.Duration);
            if (openTime >= minDate && openTime <= maxDate)
            {
                if (wma5.Wma.HasValue && wma10.Wma.HasValue)
                {
                    seriesBandLow.Points.Add(new DataPoint(openTime.Minutes, wma5.Wma.Value));
                    seriesBandLow.Points2.Add(new DataPoint(openTime.Minutes, wma10.Wma.Value));
                }
                if (wma5.Wma.HasValue)
                {
                    seriesWma5Low.Points.Add(new DataPoint(openTime.Minutes, wma5.Wma.Value));
                    // Extreme-A: wma5 low below BB lower band
                    if (bb.LowerBand.HasValue && wma5.Wma.Value < bb.LowerBand.Value)
                        seriesExtremeALow.Points.Add(new ScatterPoint(openTime.Minutes, 0.995 * wma5.Wma.Value));
                }
                if (wma10.Wma.HasValue)
                {
                    seriesWma10Low.Points.Add(new DataPoint(openTime.Minutes, wma10.Wma.Value));
                    // Magic extreme: wma10 low also below BB lower band
                    if (bb.LowerBand.HasValue && wma10.Wma.Value < bb.LowerBand.Value)
                        seriesMagicExtremeLow.Points.Add(new ScatterPoint(openTime.Minutes, 0.995 * wma10.Wma.Value));
                }
            }
        }

        chart.Series.Insert(0, seriesBandLow);
        chart.Series.Insert(0, seriesBandHigh);
        chart.Series.Add(seriesWma5Low);
        chart.Series.Add(seriesWma10Low);
        chart.Series.Add(seriesWma5High);
        chart.Series.Add(seriesWma10High);
        chart.Series.Add(seriesExtremeAHigh);
        chart.Series.Add(seriesMagicExtremeHigh);
        chart.Series.Add(seriesExtremeALow);
        chart.Series.Add(seriesMagicExtremeLow);


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
