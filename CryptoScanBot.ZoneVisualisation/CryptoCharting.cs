using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;
using CryptoScanBot.Core.Zones;


using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

using System.Globalization;

namespace CryptoScanBot.ZoneVisualisation;

public class CryptoCharting
{
    private const int OxyFontSize = 14;
    static private CryptoInterval? _interval;

    public static List<(decimal value, decimal percent, OxyColor color)> RetracementX(CryptoTradeSide side, decimal low, decimal high)
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

    public static void TrySomethingWithFib(PlotModel chart, CryptoZoneData data)
    {
        var indicator = data.IndicatorFib;
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
            fibRetracement = RetracementX(side, indicator.LastSwingLow.Value, indicator.LastSwingHigh.Value);

            long start = last.Candle.OpenTime + data.Interval.Duration;
            var lastCandle = data.SymbolInterval.CandleList.Values.Last();
            long stop = lastCandle.OpenTime + 10 * data.Interval.Duration;


            foreach (var (value, percent, color) in fibRetracement)
            {
                var fibLevel = new LineSeries { Title = "fib", Color = color, LineStyle = LineStyle.Dot, FontSize = OxyFontSize };
                fibLevel.Points.Add(new DataPoint(start, (double)value));
                fibLevel.Points.Add(new DataPoint(stop, (double)value));
                chart.Series.Add(fibLevel);

                chart.Annotations.Add(new TextAnnotation { 
                    TextPosition = new DataPoint(stop + data.Interval.Duration * 4, (double)value), 
                    TextVerticalAlignment = VerticalAlignment.Middle,
                    Text = $"{percent:N3}" });
            }
        }
    }

    public static void DrawCandleSerie(PlotModel chart, CryptoZoneData data, CryptoZoneSession session)
    {

        CryptoSymbolInterval symbolInterval = data.Symbol.GetSymbolInterval(session.ActiveInterval);

        var candleSerie = new CandleStickSeries //CandleStickAndVolumeSeries -> obsolete, x...
        {
            Title = "Candles",
            IncreasingColor = OxyColors.LightGreen,
            DecreasingColor = OxyColors.DarkOrange,
            //TrackerFormatString = "Date: {0}\nHigh: {1}\nLow: {2}\nOpen: {3}\nClose: {4}" ???
        };

        foreach (var c in symbolInterval.CandleList.Values)
        {
            if (c.OpenTime >= session.MinUnix && c.OpenTime <= session.MaxUnix)
            {
                try
                {
                    var curHighLow = new HighLowItem(c.OpenTime, (double)c.High, (double)c.Low, (double)c.Open, (double)c.Close); //OhlcvItem
                    candleSerie.Items.Add(curHighLow);
                }
                catch (Exception error)
                {
                    // daytimesaving problemo?
                    //
                    ScannerLog.Logger.Info($"Error {error}");
                }
            }
        }

        chart.Series.Add(candleSerie);
    }


    public static void DrawZigZagSerie(PlotModel chart, CryptoZoneData data, CryptoZoneSession session)
    {
        var seriesZigZag = new LineSeries { Title = "ZigZag", Color = OxyColors.White };
        var seriesHigh = new ScatterSeries { Title = "Markers high", MarkerSize = 4, MarkerFill = OxyColors.Red, MarkerType = MarkerType.Circle, };
        var seriesLow = new ScatterSeries { Title = "Markers low", MarkerSize = 4, MarkerFill = OxyColors.Yellow, MarkerType = MarkerType.Circle, };
        var seriesDummyHigh = new ScatterSeries { Title = "Markers dummy", MarkerSize = 4, MarkerFill = OxyColors.Red, MarkerType = MarkerType.Square, };
        var seriesDummyLow = new ScatterSeries { Title = "Markers dummy", MarkerSize = 4, MarkerFill = OxyColors.Yellow, MarkerType = MarkerType.Square, };
        foreach (var zigzag in data.Indicator.ZigZagList)
        {
            if (zigzag.Candle!.OpenTime >= session.MinUnix && zigzag.Candle!.OpenTime <= session.MaxUnix)
            {
                ScatterSeries? series;
                if (zigzag.Dummy)
                {
                    if (zigzag.PointType == 'L')
                        series = seriesDummyLow;
                    else
                        series = seriesDummyHigh;
                }
                else
                {
                    if (zigzag.PointType == 'L')
                        series = seriesLow;
                    else
                        series = seriesHigh;
                }
                series?.Points.Add(new ScatterPoint(zigzag.Candle.OpenTime, (double)zigzag.Value));
                seriesZigZag.Points.Add(new DataPoint(zigzag.Candle.OpenTime, (double)zigzag.Value));
            }
        }

        chart.Series.Add(seriesLow);
        chart.Series.Add(seriesHigh);
        chart.Series.Add(seriesZigZag);
        chart.Series.Add(seriesDummyLow);
        chart.Series.Add(seriesDummyHigh);

        //string format = symbol.PriceDisplayFormat[1..];
        //string text = "Date: {yyyy-MM-dd HH:mm}\nPrice: {$:0.00}";
        //text = text.Replace("$", format);
        //seriesLow.TrackerFormatString = text;
        //seriesHigh.TrackerFormatString = text;
        //seriesZigZag.TrackerFormatString = text;
    }

    public static void DrawLiqBoxes(PlotModel chart, CryptoZoneData data, CryptoZoneSession session)
    {
        var lastZigZag = data.Indicator.ZigZagList.Last();
        foreach (var zigzag in data.Indicator.ZigZagList)
        {
            // Laatste zigZag is niet relevant (niet bevestigd?)
            if (zigzag == lastZigZag || !zigzag.Dominant)
                continue;

            if (zigzag.Candle!.OpenTime >= session.MinUnix && zigzag.Candle!.OpenTime <= session.MaxUnix && zigzag.Dominant)
            {
                OxyColor color;
                if (zigzag.PointType == 'L')
                    color = OxyColors.Green;
                else
                    color = OxyColors.OrangeRed;

                long dateLast;
                if (zigzag.InvalidOn != null)
                    dateLast = zigzag.InvalidOn.OpenTime;
                else
                    dateLast = session.MaxUnix;
                //dateLast -= data.Interval.Duration;

                // Create a rectangle annotation
                var rectangle = new RectangleAnnotation
                {
                    MinimumX = zigzag.Candle.OpenTime,  // X-coordinate of the lower-left corner
                    MinimumY = (double)zigzag.Bottom,  // Y-coordinate of the lower-left corner
                    MaximumX = dateLast,  // X-coordinate of the upper-right corner
                    MaximumY = (double)zigzag.Top,  // Y-coordinate of the upper-right corner
                                                    //Fill = Color.LightGray,  // Rectangle fill color
                    Fill = OxyColor.FromArgb(128, color.R, color.G, color.B),
                    Stroke = OxyColor.FromArgb(128 + 64 + 32 + 16 + 8 + 4 + 2, color.R, color.G, color.B), // rectangle
                    StrokeThickness = 0,          // Border thickness
                    Text = zigzag.Percentage.ToString("N2")
                };
                chart.Annotations.Add(rectangle);
            }
        }
    }


    private static string LabelFormatterX(double x)
    {
        string s;
        long unix = CandleTools.GetUnixTime((long)x, 0);
        DateTime date = CandleTools.GetUnixDate(unix); //.ToLocalTime();
        if (_interval?.IntervalPeriod <= CryptoIntervalPeriod.interval1h && date.Hour == 0)
            s = date.Day.ToString();
        else if (_interval?.IntervalPeriod <= CryptoIntervalPeriod.interval1d)
            s = date.Day.ToString();
        else
            s = "?";

        if (date.Day == 1)
        {
            string monthName = date.ToString("MMM", CultureInfo.InvariantCulture);
            s += "\r\n" + monthName;
        }

        //s += "\r\n" + date.Hour.ToString() + ":" + date.Minute.ToString(); 

        return s;
    }

    public static PlotModel CreateChart(CryptoZoneData data, out LineAnnotation horizontalLine, out LineAnnotation verticalLine)
    {
        _interval = data.Interval;

        PlotModel chart = new()
        {
            Subtitle = " ",
            TextColor = OxyColors.White,
            //SubtitleColor = OxyColors.LightGray
        };


        chart.Axes.Add(new LinearAxis
        {
            FontSize = OxyFontSize,
            //Font = chart.TitleFont,
            TextColor = OxyColors.White,
            Position = AxisPosition.Bottom,
            LabelFormatter = LabelFormatterX,

            MajorTickSize = 15,
            MinorTickSize = 5,
            TicklineColor = OxyColors.Gray,
            TickStyle = OxyPlot.Axes.TickStyle.Inside,

            AxislineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.Gray,
            AxislineThickness = 2,

            //MajorGridlineStyle = LineStyle.None,
            //MajorGridlineColor = OxyColors.Gray,

            //MinorGridlineStyle = LineStyle.None,
            //MinorGridlineColor = OxyColors.Gray,

            MajorStep = (24 * 60 * 60 / data.Interval.Duration) * data.Interval.Duration,
            MinorStep = (24 * 60 * 60 / data.Interval.Duration) * data.Interval.Duration / 6, 
        });
        

        chart.Axes.Add(new LinearAxis
        {
            FontSize = OxyFontSize,
            //Font = chart.TitleFont,
            TextColor = OxyColors.White,
            Position = AxisPosition.Right,

            MajorTickSize = 15,
            MinorTickSize = 5,
            TicklineColor = OxyColors.Gray,
            TickStyle = OxyPlot.Axes.TickStyle.Inside,

            AxislineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.Gray,
            AxislineThickness = 2,
        });

        // Something like a crosshair
        verticalLine = new LineAnnotation { Type = LineAnnotationType.Vertical, Color = OxyColors.DarkGray, LineStyle = LineStyle.Dash, StrokeThickness = 0.25, X = 0};
        chart.Annotations.Add(verticalLine);

        horizontalLine = new LineAnnotation { Type = LineAnnotationType.Horizontal, Color = OxyColors.DarkGray, LineStyle = LineStyle.Dash, StrokeThickness = 0.25, Y = 0};
        chart.Annotations.Add(horizontalLine);

        //long unix = CandleTools.GetUnixTime(new DateTime(2024, 11, 09, 00, 00, 00, DateTimeKind.Utc), 60);
        //if (data.SymbolInterval.CandleList.TryGetValue(unix, out var candle))
        //{
        //    OxyColor color = OxyColors.BlueViolet;
        //    var rectangle = new RectangleAnnotation
        //    {
        //        MinimumX = candle.OpenTime - _interval?.Duration / 2, 
        //        MinimumY = (double)candle.Low - 20000,
        //        MaximumX = candle.OpenTime + _interval?.Duration / 2,
        //        MaximumY = (double)candle.High + 20000,
        //        Fill = OxyColor.FromArgb(128, color.R, color.G, color.B),
        //        Stroke = OxyColor.FromArgb(128 + 64 + 32 + 16 + 8 + 4 + 2, color.R, color.G, color.B),
        //        StrokeThickness = 0.25, 
        //    };
        //    chart.Annotations.Add(rectangle);
        //}

        return chart;
    }

}
