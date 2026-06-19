//using CryptoScanner.Core.Enums;
//using CryptoScanner.Core.Model;
//using CryptoScanner.Core.Signal.Nwe;

//using OxyPlot;
//using OxyPlot.Series;

//namespace CryptoScanner.ViewModels.Chart;

//public class NweBb
//{
//    /// <summary>
//    /// Draws markers for the NWE × BB crossover signals. Rather than rely on stored CryptoSignal
//    /// records (which don't exist in the emulator, or whenever the strategy wasn't active over this
//    /// history), the markers are recomputed on the fly from the visible candles via
//    /// <see cref="NweBbDetector"/>, which runs the same algorithm as the live strategy. The candle list
//    /// is the bounded window list (incl. indicator warmup); markers outside [minDate, maxDate] are skipped.
//    /// </summary>
//    internal static void Draw(PlotModel chart, IReadOnlyList<CryptoCandle> candles,
//        CandleTime minDate, CandleTime maxDate, string group)
//    {
//        var seriesLong = new ScatterSeries
//        {
//            Title = "nwe.bb ↑",
//            MarkerSize = 7,
//            MarkerFill = OxyColor.FromArgb(220, 0, 210, 100),
//            MarkerType = MarkerType.Triangle,
//            YAxisKey = "price",
//            Tag = group,
//            TrackerFormatString = "{0}\n{Tag}",
//        };

//        var seriesShort = new ScatterSeries
//        {
//            Title = "nwe.bb ↓",
//            MarkerSize = 7,
//            MarkerFill = OxyColor.FromArgb(220, 220, 60, 60),
//            MarkerType = MarkerType.Diamond,
//            YAxisKey = "price",
//            Tag = group,
//            TrackerFormatString = "{0}\n{Tag}",
//        };

//        foreach (var marker in NweBbDetector.Detect(candles))
//        {
//            if (marker.OpenTime < minDate || marker.OpenTime > maxDate)
//                continue;

//            if (marker.Side == CryptoTradeSide.Long)
//            {
//                seriesLong.Points.Add(new ScatterPoint(
//                    marker.OpenTime.Minutes,
//                    (double)(0.997m * marker.Price),
//                    double.NaN,
//                    double.NaN,
//                    tag: "nwe.bb ↑"));
//            }
//            else
//            {
//                seriesShort.Points.Add(new ScatterPoint(
//                    marker.OpenTime.Minutes,
//                    (double)(1.003m * marker.Price),
//                    double.NaN,
//                    double.NaN,
//                    tag: "nwe.bb ↓"));
//            }
//        }

//        chart.Series.Add(seriesLong);
//        chart.Series.Add(seriesShort);
//    }
//}
