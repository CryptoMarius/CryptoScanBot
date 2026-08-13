using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

namespace CryptoScanner.Chart.ViewModels.Chart;

public class FibRetracement
{

    private static List<(decimal value, decimal percent, OxyColor color)> RetracementX(CryptoTradeSide side, decimal low, decimal high)
    {
        List<(decimal value, OxyColor color)> levels = [
            (0.000m, OxyColors.Gray),
            (0.236m, OxyColors.White),
            (0.382m, OxyColors.White),
            (0.500m, OxyColors.Yellow),
            (0.618m, OxyColors.Yellow),
            (0.786m, OxyColors.White),
            (1.000m, OxyColors.Gray)
         ];

        List<(decimal, decimal, OxyColor color)> retracements = [];
        foreach (var (value, color) in levels)
        {
            decimal incr = (high - low) * value;
            if (side == CryptoTradeSide.Long)
                retracements.Add((high - incr, value, color));
            else
                retracements.Add((low + incr, value, color));
        }
        return retracements;
    }

    public static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval, ZigZagIndicator indicator, string group)
    {
        //// Mhh, fib levels proberen te zetten
        //// !!! Dit lijkt alvast niet te werken!!!!
        //// eerst maar eens iets verder uitdenken
        if (indicator.LastSwingHigh != null && indicator.LastSwingLow != null)
        {
            // Place a Fibonacci grid from low to high in an uptrend and high to low in a downtrend
            List<(decimal value, decimal percent, OxyColor color)> fibRetracement;

            //ZigZagResult first;
            ZigZagResult last;
            CryptoTradeSide side;
            if (indicator.LastSwingHigh.Candle.OpenTime > indicator.LastSwingLow.Candle.OpenTime)
            {
                //first = indicator.LastSwingLow;
                side = CryptoTradeSide.Long;
                last = indicator.LastSwingHigh;
                //fibRetracement = Retracement1(indicator.LastSwingHigh.Value, indicator.LastSwingLow.Value);
            }
            else
            {
                //first = indicator.LastSwingHigh;
                side = CryptoTradeSide.Short;
                last = indicator.LastSwingLow;
                //fibRetracement = Retracement2(indicator.LastSwingHigh.Value, indicator.LastSwingLow.Value);
            }
            fibRetracement = RetracementX(side, (decimal)indicator.LastSwingLow.Value, (decimal)indicator.LastSwingHigh.Value);

            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            CandleTime start = last.Candle.OpenTime + interval.Duration;
            var lastCandle = symbolInterval.CandleList.Values.Last();
            CandleTime stop = lastCandle.OpenTime + 10 * interval.Duration;

            foreach (var (value, percent, color) in fibRetracement)
            {
                var fibLevel = new LineSeries
                {
                    Title = "fib",
                    Color = color,
                    LineStyle = LineStyle.Dot,
                    Font = Const.OxyFontName,
                    YAxisKey = "price",
                    Tag = group,
                };
                fibLevel.Points.Add(new DataPoint(start.Minutes, (double)value));
                fibLevel.Points.Add(new DataPoint(stop.Minutes, (double)value));
                chart.Series.Add(fibLevel);

                chart.Annotations.Add(new TextAnnotation
                {
                    TextColor = OxyColors.White,
                    TextPosition = new DataPoint((stop + interval.Duration * 4).Minutes, (double)value),
                    TextVerticalAlignment = VerticalAlignment.Middle,
                    Text = $"{percent:N3}%",
                    Font = Const.OxyFontName,
                    //FontSize = OxyFontSize,
                    //FontWeight = FontWeights.Bold,
                    Tag = group,
                });
            }
        }
    }

}
