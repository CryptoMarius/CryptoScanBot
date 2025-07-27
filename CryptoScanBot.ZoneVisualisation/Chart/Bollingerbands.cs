using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

using OxyPlot;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanBot.ZoneVisualisation.Chart;

public class Bollingerbands
{
    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval, long minDate, long maxDate)
    {
        var seriesHigh = new LineSeries { Title = "bb.upper", MarkerSize = 1, MarkerFill = OxyColors.Blue, Color = OxyColors.Blue };
        var seriesMiddle = new LineSeries { Title = "bb.middle", MarkerSize = 1, MarkerFill = OxyColors.Blue, Color = OxyColors.Blue };
        var seriesLow = new LineSeries { Title = "bb.lower", MarkerSize = 1, MarkerFill = OxyColors.Blue, Color = OxyColors.Blue };

        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return;

        List<BollingerBandsResult> bollingerBandsList = (List<BollingerBandsResult>)symbolInterval.CandleList.Values.GetBollingerBands(
            lookbackPeriods: GlobalData.Settings.General.SettingsBb.Length,
            standardDeviations: GlobalData.Settings.General.SettingsBb.Deviation);


        foreach (var bb in bollingerBandsList)
        {
            long openTime = CandleTools.GetUnixTime(bb.Date, interval.Duration);

            double? upperBand = bb.UpperBand;
            double? middleBand = bb.Sma;
            double? lowerBand = bb.LowerBand;


            if (lowerBand.HasValue)
                seriesLow.Points.Add(new DataPoint(openTime, lowerBand.Value));
            if (upperBand.HasValue)
                seriesHigh.Points.Add(new DataPoint(openTime, upperBand.Value));
            if (middleBand.HasValue)
                seriesMiddle.Points.Add(new DataPoint(openTime, middleBand.Value));
        }

        chart.Series.Add(seriesLow);
        chart.Series.Add(seriesHigh);
        chart.Series.Add(seriesMiddle);
    }
}