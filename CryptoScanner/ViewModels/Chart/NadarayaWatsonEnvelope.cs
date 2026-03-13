using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Indicator;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanner.ViewModels.Chart;

public class NadarayaWatsonEnvelope
{
    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval,
        CandleTime minDate, CandleTime maxDate, bool smoothRepainting, string group)
    {
        //TODO: Honour Min & Max Date!

        var seriesHigh = new LineSeries
        {
            Title = "n high",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkGray,
            Color = OxyColors.DarkGray,
            Tag = group,
        };

        var seriesMiddle = new LineSeries
        {
            Title = "n middle",
            MarkerSize = 1,
            MarkerFill = OxyColors.Gray,
            Color = OxyColors.Gray,
            Tag = group,
        };
        var seriesLow = new LineSeries
        {
            Title = "n low",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkGray,
            Color = OxyColors.DarkGray,
            Tag = group,
        };

        var seriesBuy = new ScatterSeries
        {
            Title = "n buy",
            MarkerSize = 3,
            MarkerFill = OxyColors.White,
            MarkerType = MarkerType.Square,
            MarkerStrokeThickness = 1.5,
            Tag = group,
        };

        var seriesSell = new ScatterSeries
        {
            Title = "n sell",
            MarkerSize = 3,
            MarkerFill = OxyColors.White,
            MarkerType = MarkerType.Square,
            MarkerStrokeThickness = 1.5,
            Tag = group,
        };

        // Iterate the last 500 candles
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return;

        CryptoCandleList candles = [];
        foreach (var c in symbolInterval.CandleList.Values)
        {
            if (c.OpenTime >= minDate && c.OpenTime <= maxDate)
            {
                candles.Add(c.OpenTime, c);
            }
        }

        NweIndicator nwe = new(
            bandwidth: (double)GlobalData.Settings.Signal.Nwe.BandWidth,
            multiplier: GlobalData.Settings.Signal.Nwe.Multiplication,
            smoothRepainting: smoothRepainting
           );
        var result = nwe.Calculate(candles);

        CandleTime offsett = candles.Values.Last().OpenTime; // - max * interval.Duration;


        for (int i = 0; i < candles.Count; i++)
        {
            if (candles.TryGetValue(offsett - (i + 0) * interval.Duration, out CryptoCandle candleLast) &&
                candles.TryGetValue(offsett - (i + 1) * interval.Duration, out CryptoCandle candlePrev))
            {
                CandleTime openTime = CandleTime.AlignFromDateTime(candleLast.Date, interval.Duration);
                if (openTime >= minDate && openTime <= maxDate)
                {
                    var res = result[i];
                    if (res.Lower == null)
                        continue;
                    decimal lowerband = res.Lower.Value;
                    decimal nwevalue = res.Center.Value;
                    decimal upperband = res.Upper.Value;

                    seriesLow.Points.Add(new DataPoint(candleLast.OpenTime.Minutes, (double)lowerband));
                    seriesMiddle.Points.Add(new DataPoint(candleLast.OpenTime.Minutes, (double)nwevalue));
                    seriesHigh.Points.Add(new DataPoint(candleLast.OpenTime.Minutes, (double)upperband));

                    // buy alert
                    // Candle outside the band
                    //if (candleLast!.Open <= lowerband && candleLast.Close <= lowerband)
                    //{
                    //    nwevalue = candleLast.Low * 0.995m;
                    //    seriesBuy.Points.Add(new ScatterPoint(candleLast.OpenTime, (double)nwevalue));
                    //}
                    // Candle sticking pearsing trough the band
                    if (candlePrev.Close > lowerband && candleLast.Close <= lowerband)
                    {
                        nwevalue = candleLast.Low * 0.995m;
                        seriesBuy.Points.Add(new ScatterPoint(candleLast.OpenTime.Minutes, (double)nwevalue));
                    }

                    // sell alert
                    // Candle outside the band
                    //if (candleLast!.Open >= upperband && candleLast.Close >= upperband)
                    //{
                    //    nwevalue = candleLast.High * 1.005m;
                    //    seriesSell.Points.Add(new ScatterPoint(candleLast.OpenTime, (double)nwevalue));
                    //}
                    // Candle sticking pearsing trough the band
                    if (candlePrev.Close < upperband && candleLast.Close >= upperband)
                    {
                        nwevalue = candleLast.High * 1.005m;
                        seriesSell.Points.Add(new ScatterPoint(candleLast.OpenTime.Minutes, (double)nwevalue));
                    }


                    //if (i > 0)
                    //{
                    //    if (smaList20[i - 0].Sma != null && smaList20[i - 1].Sma != null)
                    //    {
                    //        double nweLast = (double)nwe[i - 0];
                    //        double nwePrev = (double)nwe[i - 1];

                    //        double smaLast = (double)smaList20[i - 0].Sma!.Value;
                    //        double smaPrev = (double)smaList20[i - 1].Sma!.Value;

                    //        if (// buy alert when the nwe.lower crosses the sma20 upwards
                    //            (nwePrev - (double)sae < smaPrev! && nweLast - (double)sae >= smaLast) ||
                    //            // sell alert when the nwe.upper crosses the sma20 downwards
                    //            (nwePrev + (double)sae > smaPrev! && nweLast + (double)sae <= smaLast))
                    //        {
                    //            seriesBuy.Points.Add(new ScatterPoint(candleLast.OpenTime, (double)smaLast));
                    //        }
                    //    }
                    //}
                }
            }
        }

        chart.Series.Add(seriesLow);
        chart.Series.Add(seriesMiddle);
        chart.Series.Add(seriesHigh);
        chart.Series.Add(seriesBuy);
        chart.Series.Add(seriesSell);
    }

}