using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.ViewModels.Chart;

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

        List<BollingerBandsResult> bollingerBandsList = (List<BollingerBandsResult>)candles.GetBollingerBands(
            lookbackPeriods: GlobalData.Settings.General.SettingsBb.Length,
            standardDeviations: GlobalData.Settings.General.SettingsBb.Deviation);


        foreach (var bb in bollingerBandsList)
        {
            CandleTime openTime = CandleTime.AlignFromDateTime(bb.Date, interval.Duration);
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
}