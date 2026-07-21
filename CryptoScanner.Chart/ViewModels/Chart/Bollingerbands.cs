using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.Chart.ViewModels.Chart;

public class Bollingerbands
{
    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval, List<CryptoCandle> candles, CandleTime minDate, CandleTime maxDate, string group)
    {
        // YAxisKey = "price" pins these series to the price axis so OxyPlot can resolve
        // the axis via key lookup during the very first render pass — before PlotModel.Update
        // populates the Series.XAxis/YAxis properties. Without this, GetClippingRect throws NRE.
        // Same fix idiom as the zone annotations (SmcZones/DlzZones/FvgZones).
        var seriesHigh = new LineSeries
        {
            Title = "bb.upper",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkBlue,
            Color = OxyColors.DarkBlue,
            YAxisKey = "price",
            Tag = group,
        };
        var seriesMiddle = new LineSeries
        {
            Title = "bb.middle",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkBlue,
            Color = OxyColors.DarkBlue,
            YAxisKey = "price",
            Tag = group,
        };
        var seriesLow = new LineSeries
        {
            Title = "bb.lower",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkBlue,
            Color = OxyColors.DarkBlue,
            YAxisKey = "price",
            Tag = group,
        };

        if (candles.Count == 0)
            return;

        IReadOnlyList<BollingerBandsResult> bollingerBandsList = candles.AsQuotes().ToBollingerBands(
            lookbackPeriods: GlobalData.Settings.General.SettingsBb.Length,
            standardDeviations: GlobalData.Settings.General.SettingsBb.Deviation).ToList();


        foreach (var bb in bollingerBandsList)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(bb.Timestamp, interval.Duration);
            if (openTime >= minDate && openTime <= maxDate)
            {
                double? upperBand = bb.UpperBand;
                double? middleBand = bb.Sma;
                double? lowerBand = bb.LowerBand;

                if (lowerBand.HasValue)
                    seriesLow.Points.Add(new DataPoint(openTime.Minutes, lowerBand.Value));
                if (upperBand.HasValue)
                    seriesHigh.Points.Add(new DataPoint(openTime.Minutes, upperBand.Value));
                if (middleBand.HasValue)
                    seriesMiddle.Points.Add(new DataPoint(openTime.Minutes, middleBand.Value));
            }
        }

        chart.Series.Add(seriesLow);
        chart.Series.Add(seriesHigh);
        chart.Series.Add(seriesMiddle);
    }

    ///// <summary>
    ///// Draws Bollinger %B and the band width into the shared oscillator sub-panel (Y-axis key "stoch",
    ///// fixed 0..100), so it sits next to Stoch/RSI/Lux which use the same scale.
    /////   - %B (price position within the bands) is scaled ×100 and drawn as a steady pink line.
    /////   - Band width (= (upper-lower)/middle, i.e. volatility) is scaled ×100 and drawn per segment:
    /////     RED while the bands are widening (width rising) and GREEN while they are narrowing (falling).
    ///// Two width series with DataPoint.Undefined breaks give the per-segment colour without one series
    ///// per point. %B can leave the 0..100 band when price trades outside the bands; those points clip.
    ///// </summary>
    //internal static void DrawPercentWidth(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval,
    //    List<CryptoCandle> candles, CandleTime minDate, CandleTime maxDate, string group, string axisKey = "stoch")
    //{
    //    if (candles.Count == 0)
    //        return;

    //    IReadOnlyList<BollingerBandsResult> bollingerBandsList = candles.AsQuotes().GetBollingerBands(
    //        lookbackPeriods: GlobalData.Settings.General.SettingsBb.Length,
    //        standardDeviations: GlobalData.Settings.General.SettingsBb.Deviation);

    //    //// %B — pink (steady colour)
    //    //var seriesPercentB = new LineSeries
    //    //{
    //    //    Title = "BB %B",
    //    //    Color = OxyColors.HotPink,
    //    //    StrokeThickness = 1,
    //    //    YAxisKey = axisKey,
    //    //    Tag = group,
    //    //};

    //    // Band width — red while widening, green while narrowing. Split over two series.
    //    var seriesWidthUp = new LineSeries
    //    {
    //        Title = "BB width (widening)",
    //        Color = OxyColors.Red,
    //        StrokeThickness = 1.2,
    //        YAxisKey = axisKey,
    //        Tag = group,
    //    };
    //    var seriesWidthDown = new LineSeries
    //    {
    //        Title = "BB width (narrowing)",
    //        Color = OxyColors.LimeGreen,
    //        StrokeThickness = 1.2,
    //        YAxisKey = axisKey,
    //        Tag = group,
    //    };

    //    double prevX = 0;
    //    double prevWidth = 0;
    //    bool havePrev = false;

    //    foreach (var bb in bollingerBandsList)
    //    {
    //        CandleTime openTime = CandleTime.AlignFromDateTime(bb.Date, interval.Duration);
    //        if (openTime < minDate || openTime > maxDate)
    //            continue;

    //        double x = openTime.Minutes;

    //        //if (bb.PercentB.HasValue)
    //        //    seriesPercentB.Points.Add(new DataPoint(x, bb.PercentB.Value * 100.0));

    //        if (bb.UpperBand.HasValue && bb.LowerBand.HasValue)
    //        {
    //            var width = 100 * (bb.UpperBand.Value / bb.LowerBand.Value - 1);
    //            //double width = bb.Width.Value * 100.0;
    //            if (havePrev)
    //            {
    //                // Colour the segment by its direction: rising width = widening = red.
    //                var target = width >= prevWidth ? seriesWidthUp : seriesWidthDown;
    //                target.Points.Add(new DataPoint(prevX, prevWidth));
    //                target.Points.Add(new DataPoint(x, width));
    //                target.Points.Add(DataPoint.Undefined); // break so disjoint segments don't connect
    //            }
    //            prevX = x;
    //            prevWidth = width;
    //            havePrev = true;
    //        }
    //    }

    //    chart.Series.Add(seriesWidthUp);
    //    chart.Series.Add(seriesWidthDown);
    //    //chart.Series.Add(seriesPercentB);
    //}
}