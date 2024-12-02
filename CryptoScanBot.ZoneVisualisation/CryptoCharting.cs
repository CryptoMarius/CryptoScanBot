using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;
using CryptoScanBot.ZoneVisualisation.Zones;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

using System.Globalization;

namespace CryptoScanBot.ZoneVisualisation;

public class CryptoCharting
{
    private const int OxyFontSize = 14;
    private const string OxyFontName = "Arial";
    static private string? _priceFormat;
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

    public static void DrawFibRetracement(PlotModel chart, CryptoZoneData data)
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
                var fibLevel = new LineSeries { Title = "fib", Color = color, LineStyle = LineStyle.Dot, Font = OxyFontName };
                fibLevel.Points.Add(new DataPoint(start, (double)value));
                fibLevel.Points.Add(new DataPoint(stop, (double)value));
                chart.Series.Add(fibLevel);

                chart.Annotations.Add(new TextAnnotation {
                    TextColor = OxyColors.White,
                    TextPosition = new DataPoint(stop + data.Interval.Duration * 4, (double)value), 
                    TextVerticalAlignment = VerticalAlignment.Middle,
                    Text = $"{percent:N3}%",
                    Font = OxyFontName,
                    //FontSize = OxyFontSize,
                    //FontWeight = FontWeights.Bold,
                });
            }
        }
    }

    public static void DrawCandleSerie(PlotModel chart, CryptoZoneData data, CryptoZoneSession session)
    {

        CryptoSymbolInterval symbolInterval = data.Symbol.GetSymbolInterval(session.ActiveInterval);

        var candleSerie = new CandleStickSeries //CandleStickAndVolumeSeries -> obsolete, symbolData...
        {
            Title = "Candles",
            IncreasingColor = OxyColors.LightGreen,
            DecreasingColor = OxyColors.DarkOrange,
            //TrackerFormatString = "Date: {0}\nHigh: {1}\nLow: {2}\nOpen: {3}\nClose: {4}" ???
        };

        foreach (var c in symbolInterval.CandleList.Values)
        {
            if (c.OpenTime >= session.MinDate && c.OpenTime <= session.MaxDate)
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


    internal static void DrawPoints(PlotModel chart, CryptoZoneSession session, List<ZigZagResult> pivotList)
    {
        var seriesHigh = new ScatterSeries { Title = "p high", MarkerSize = 2, MarkerFill = OxyColors.Red, MarkerType = MarkerType.Square, };
        var seriesLow = new ScatterSeries { Title = "p low", MarkerSize = 2, MarkerFill = OxyColors.Yellow, MarkerType = MarkerType.Square, };
        foreach (var zigzag in pivotList)
        {
            if (zigzag.Candle!.OpenTime >= session.MinDate && zigzag.Candle!.OpenTime <= session.MaxDate)
            {
                decimal value;
                ScatterSeries? series;
                if (zigzag.PointType == 'L')
                {
                    series = seriesLow;
                    value = zigzag.Value * 0.995m;
                }
                else
                {
                    value = zigzag.Value * 1.005m;
                    series = seriesHigh;
                }
                series?.Points.Add(new ScatterPoint(zigzag.Candle.OpenTime, (double)value));
            }
        }

        chart.Series.Add(seriesLow);
        chart.Series.Add(seriesHigh);
    }


    internal static void DrawSignals(PlotModel chart, CryptoZoneSession session, List<CryptoSignal> signalList)
    {
        var seriesShort = new ScatterSeries { Title = "s high", MarkerSize = 2, MarkerFill = OxyColors.Red, MarkerType = MarkerType.Diamond, };
        var seriesLong = new ScatterSeries { Title = "s low", MarkerSize = 2, MarkerFill = OxyColors.Yellow, MarkerType = MarkerType.Diamond, };
        foreach (var signal in signalList)
        {
            if (signal.EventTime >= session.MinDate && signal.EventTime <= session.MaxDate)
            {
                decimal y;
                ScatterSeries? series;
                if (signal.Side == CryptoTradeSide.Long)
                {
                    series = seriesLong;
                    y = 0.99m * signal.SignalPrice;
                }
                else
                {
                    series = seriesShort;
                    y = 1.01m * signal.SignalPrice;
                }

                series?.Points.Add(new ScatterPoint(signal.EventTime, (double)y));
            }
        }

        chart.Series.Add(seriesLong);
        chart.Series.Add(seriesShort);
    }


    public static void DrawZigZag(PlotModel chart, CryptoZoneSession session, List<ZigZagResult> zigZagList, string caption, OxyColor color)
    {
        var seriesZigZag = new LineSeries { Title = caption, Color = color };
        var seriesHigh = new ScatterSeries { Title = "Markers high", MarkerSize = 4, MarkerFill = OxyColors.Red, MarkerType = MarkerType.Circle, };
        var seriesLow = new ScatterSeries { Title = "Markers low", MarkerSize = 4, MarkerFill = OxyColors.Yellow, MarkerType = MarkerType.Circle, };
        var seriesDummyHigh = new ScatterSeries { Title = "Markers dummy", MarkerSize = 5, MarkerFill = OxyColors.Red, MarkerType = MarkerType.Square, };
        var seriesDummyLow = new ScatterSeries { Title = "Markers dummy", MarkerSize = 5, MarkerFill = OxyColors.Yellow, MarkerType = MarkerType.Square, };
        foreach (var zigzag in zigZagList)
        {
            if (zigzag.Candle!.OpenTime >= session.MinDate && zigzag.Candle!.OpenTime <= session.MaxDate)
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
        //seriesLong.TrackerFormatString = text;
        //seriesShort.TrackerFormatString = text;
        //seriesZigZag.TrackerFormatString = text;
    }

    private static void DrawLiqBoxesInternal(PlotModel chart, CryptoZone zone, CryptoZoneSession session)
    {
        if (zone.OpenTime >= session.MinDate && zone.OpenTime <= session.MaxDate)
        {
            OxyColor color;
            if (zone.Side == CryptoTradeSide.Long)
                color = OxyColors.Green;
            else
                color = OxyColors.OrangeRed;

            long dateOpen;
            if (zone.OpenTime != null)
                dateOpen = (long)zone.OpenTime;
            else
                dateOpen = session.MinDate;

            long dateLast;
            if (zone.CloseTime != null)
                dateLast = (long)zone.CloseTime;
            else
                dateLast = session.MaxDate;

            // Create a rectangle annotation
            var rectangle = new RectangleAnnotation
            {
                MinimumX = dateOpen,  // X-coordinate of the lower-left corner
                MinimumY = (double)zone.Bottom,  // Y-coordinate of the lower-left corner
                MaximumX = dateLast,  // X-coordinate of the upper-right corner
                MaximumY = (double)zone.Top,  // Y-coordinate of the upper-right corner
                                              //Fill = Color.LightGray,  // Rectangle fill color
                Fill = OxyColor.FromArgb(128, color.R, color.G, color.B),
                Stroke = OxyColor.FromArgb(128 + 64 + 32 + 16 + 8 + 4 + 2, color.R, color.G, color.B), // rectangle
                StrokeThickness = 0,          // Border thickness
                Text = zone.Description,
            };
            chart.Annotations.Add(rectangle);
        }
    }


    public static void DrawLiqBoxes(PlotModel chart, CryptoZoneData data, CryptoZoneSession session)
    {
        var symbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(data.Symbol.Name);
        foreach (var zone in symbolData.ZoneListLong.Values)
            DrawLiqBoxesInternal(chart, zone, session);
        foreach (var zone in symbolData.ZoneListShort.Values)
            DrawLiqBoxesInternal(chart, zone, session);


        //var lastZigZag = data.Indicator.ZigZagList.Last();
        //foreach (var signal in data.Indicator.ZigZagList)
        //{
        //    // Laatste zigZag is niet relevant (niet bevestigd?)
        //    if (signal == lastZigZag || !signal.Dominant)
        //        continue;

        //    if (signal.Candle!.OpenTime >= session.MinDate && signal.Candle!.OpenTime <= session.MaxDate && signal.Dominant)
        //    {
        //        OxyColor color;
        //        if (signal.PointType == 'L')
        //            color = OxyColors.Green;
        //        else
        //            color = OxyColors.OrangeRed;

        //        long dateLast;
        //        if (signal.CloseTime != null)
        //            dateLast = (long)signal.CloseTime;
        //        else
        //            dateLast = session.MaxDate;
        //        //dateLast -= data.Interval.Duration;

        //        // Create a rectangle annotation
        //        var rectangle = new RectangleAnnotation
        //        {
        //            MinimumX = signal.Candle.OpenTime,  // X-coordinate of the lower-left corner
        //            MinimumY = (double)signal.Bottom,  // Y-coordinate of the lower-left corner
        //            MaximumX = dateLast,  // X-coordinate of the upper-right corner
        //            MaximumY = (double)signal.Top,  // Y-coordinate of the upper-right corner
        //                                            //Fill = Color.LightGray,  // Rectangle fill color
        //            Fill = OxyColor.FromArgb(128, color.R, color.G, color.B),
        //            Stroke = OxyColor.FromArgb(128 + 64 + 32 + 16 + 8 + 4 + 2, color.R, color.G, color.B), // rectangle
        //            StrokeThickness = 0,          // Border thickness
        //            Text = signal.Percentage.ToString("N2")
        //        };
        //        chart.Annotations.Add(rectangle);
        //    }
        //}
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

    private static string LabelFormatterY(double x)
    {
        string s = x.ToString(_priceFormat);
        return s;
    }

    public static PlotModel CreateChart(CryptoZoneData data, out LineAnnotation horizontalLine, out LineAnnotation verticalLine)
    {
        _interval = data.Interval;
        _priceFormat = data.Symbol.PriceDisplayFormat;

        PlotModel chart = new()
        {
            Subtitle = " ",
            TitleFont = OxyFontName,
            TextColor = OxyColors.White,
            SubtitleFont = OxyFontName,
            SubtitleColor = OxyColors.White,
            SubtitleFontWeight = FontWeights.Bold,
        };

        chart.Axes.Add(new LinearAxis
        {
            Font = OxyFontName,
            FontSize = OxyFontSize,
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
            LabelFormatter = LabelFormatterY,
            Font = OxyFontName,
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
