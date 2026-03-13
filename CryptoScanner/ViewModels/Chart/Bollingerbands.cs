using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.ViewModels.Chart;

public class Bollingerbands
{
    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval, CandleTime minDate, CandleTime maxDate, string group)
    {
        var seriesHigh = new LineSeries
        {
            Title = "bb.upper",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkBlue,
            Color = OxyColors.Blue,
            Tag = group,
        };
        var seriesMiddle = new LineSeries
        {
            Title = "bb.middle",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkBlue,
            Color = OxyColors.DarkBlue,
            Tag = group,
        };
        var seriesLow = new LineSeries
        {
            Title = "bb.lower",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkBlue,
            Color = OxyColors.Blue,
            Tag = group,
        };

        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return;

        List<BollingerBandsResult> bollingerBandsList = (List<BollingerBandsResult>)symbolInterval.CandleList.Values.GetBollingerBands(
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