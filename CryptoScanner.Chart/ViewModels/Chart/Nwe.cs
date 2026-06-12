using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Indicators;

using OxyPlot;
using OxyPlot.Series;

namespace CryptoScanner.ViewModels.Chart;

public class Nwe
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
            YAxisKey = "price",
            Tag = group,
        };

        var seriesMiddle = new LineSeries
        {
            Title = "n middle",
            MarkerSize = 1,
            MarkerFill = OxyColors.Gray,
            Color = OxyColors.Gray,
            YAxisKey = "price",
            Tag = group,
        };
        var seriesLow = new LineSeries
        {
            Title = "n low",
            MarkerSize = 1,
            MarkerFill = OxyColors.DarkGray,
            Color = OxyColors.DarkGray,
            YAxisKey = "price",
            Tag = group,
        };

        var seriesBuy = new ScatterSeries
        {
            Title = "n buy",
            MarkerSize = 3,
            MarkerFill = OxyColors.White,
            MarkerType = MarkerType.Square,
            MarkerStrokeThickness = 1.5,
            YAxisKey = "price",
            Tag = group,
        };

        var seriesSell = new ScatterSeries
        {
            Title = "n sell",
            MarkerSize = 3,
            MarkerFill = OxyColors.White,
            MarkerType = MarkerType.Square,
            MarkerStrokeThickness = 1.5,
            YAxisKey = "price",
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
            bandwidth: GlobalData.Settings.Signal.Nwe.BandWidth,
            multiplier: GlobalData.Settings.Signal.Nwe.Multiplication,
            smoothRepainting: smoothRepainting
           );
        var result = nwe.Calculate(candles);

        foreach (var res in result)
        {
            if (candles.TryGetValue(res.OpenTime, out CryptoCandle candleLast) &&
                candles.TryGetValue(res.OpenTime - interval.Duration, out CryptoCandle candlePrev))
            {
                CandleTime openTime = CandleTime.AlignFromDateTime(candleLast.Date, interval.Duration);
                if (openTime >= minDate && openTime <= maxDate)
                {
                    if (res.Lower == null || res.Center == null || res.Upper == null)
                        continue;
                    decimal lowerband = res.Lower.Value;
                    decimal nwevalue = res.Center.Value;
                    decimal upperband = res.Upper.Value;

                    seriesLow.Points.Add(new DataPoint(candleLast.OpenTime.Minutes, (double)lowerband));
                    seriesMiddle.Points.Add(new DataPoint(candleLast.OpenTime.Minutes, (double)nwevalue));
                    seriesHigh.Points.Add(new DataPoint(candleLast.OpenTime.Minutes, (double)upperband));

                    // buy alert, candle sticking pearsing trough the band
                    if (candlePrev.Close > lowerband && candleLast.Close <= lowerband)
                    {
                        nwevalue = candleLast.Low * 0.995m;
                        seriesBuy.Points.Add(new ScatterPoint(candleLast.OpenTime.Minutes, (double)nwevalue));
                    }


                    // sell alert, Candle sticking pearsing trough the band
                    if (candlePrev.Close < upperband && candleLast.Close >= upperband)
                    {
                        nwevalue = candleLast.High * 1.005m;
                        seriesSell.Points.Add(new ScatterPoint(candleLast.OpenTime.Minutes, (double)nwevalue));
                    }

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