using CryptoScanBot.Core.Experimental;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;


using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace CryptoShowTrend;

public class CryptoCharting
{
    public static void DrawCandleSerie(PlotModel chart, CryptoSymbolInterval symbolInterval, long unix)
    {
        var candleSerie = new CandleStickSeries //CandleStickAndVolumeSeries -> obsolete, mwhee
        {
            Title = "Candles",
            IncreasingColor = OxyColors.LightGreen,
            DecreasingColor = OxyColors.DarkOrange,
            //TrackerFormatString = "Date: {0}\nHigh: {1}\nLow: {2}\nOpen: {3}\nClose: {4}" ???
        };


        foreach (var c in symbolInterval.CandleList.Values)
        {
            if (c.OpenTime > unix)
            {
                //if (c.Date == new DateTime(2024, 11, 01, 12, 0, 0, DateTimeKind.Utc))
                //    symbolInterval = symbolInterval; // debug 

                var curDate = DateTimeAxis.ToDouble(c.Date);
                var curHighLow = new HighLowItem(curDate, (double)c.High, (double)c.Low, (double)c.Open, (double)c.Close); //OhlcvItem
                candleSerie.Items.Add(curHighLow);
            }
        }

        chart.Series.Add(candleSerie);
    }



    public static void DrawZigZagSerie(CryptoSymbol symbol, PlotModel chart, ZigZagIndicator9 indicator, long unix)
    {
        var seriesZigZag = new LineSeries { Title = "ZigZag", Color = OxyColors.White };
        var seriesHigh = new ScatterSeries { Title = "Markers high", MarkerSize = 4, MarkerFill = OxyColors.Red, MarkerType = MarkerType.Circle, };
        var seriesLow = new ScatterSeries { Title = "Markers low", MarkerSize = 4, MarkerFill = OxyColors.Yellow, MarkerType = MarkerType.Circle, };
        foreach (var zigzag in indicator.ZigZagList)
        {
            if (zigzag.Candle!.OpenTime > unix)
            {
                if (zigzag.PointType == 'L')
                    seriesLow.Points.Add(new ScatterPoint(DateTimeAxis.ToDouble(zigzag.Candle!.Date), (double)zigzag.Value));
                else
                    seriesHigh.Points.Add(new ScatterPoint(DateTimeAxis.ToDouble(zigzag.Candle!.Date), (double)zigzag.Value));
                seriesZigZag.Points.Add(new DataPoint(DateTimeAxis.ToDouble(zigzag.Candle!.Date), (double)zigzag.Value));
            }
        }

        chart.Series.Add(seriesLow);
        chart.Series.Add(seriesHigh);
        chart.Series.Add(seriesZigZag);

        //string format = symbol.PriceDisplayFormat[1..];
        //string text = "Date: {yyyy-MM-dd HH:mm}\nPrice: {$:0.00}";
        //text = text.Replace("$", format);
        //seriesLow.TrackerFormatString = text;
        //seriesHigh.TrackerFormatString = text;
        //seriesZigZag.TrackerFormatString = text;
    }

    public static void DrawLiqBoxes(PlotModel chart, CryptoZoneData data, long unix, CryptoCandle lastCandle)
    {
        // Liquidity levels
        var lastZigZag = data.Indicator.ZigZagList.Last();
        foreach (var zigzag in data.Indicator.ZigZagList)
        {
            // Laatste zigZag is niet relevant (niet bevestigd?)
            if (zigzag == lastZigZag || !zigzag.Dominant)
                continue;

            if (zigzag.Candle!.OpenTime > unix && zigzag.Dominant)
            {
                OxyColor color;
                if (zigzag.PointType == 'L')
                    color = OxyColors.Green;
                else
                    color = OxyColors.OrangeRed;

                DateTime dateLast;
                if (zigzag.InvalidOn != null)
                    dateLast = zigzag.InvalidOn.Date.ToLocalTime();
                else
                    dateLast = lastCandle.Date.ToLocalTime();
                dateLast.AddSeconds(-data.Interval.Duration);

                // Create a rectangle annotation
                var rectangle = new RectangleAnnotation
                {
                    MinimumX = DateTimeAxis.ToDouble(zigzag.Candle.Date),  // X-coordinate of the lower-left corner
                    MinimumY = (double)zigzag.Bottom,  // Y-coordinate of the lower-left corner
                    MaximumX = DateTimeAxis.ToDouble(dateLast),  // X-coordinate of the upper-right corner
                    MaximumY = (double)zigzag.Top,  // Y-coordinate of the upper-right corner
                                                    //Fill = Color.LightGray,  // Rectangle fill color
                    Fill = OxyColor.FromArgb(128, color.R, color.G, color.B),
                    Stroke = OxyColor.FromArgb(128 + 64 + 32 + 16 + 8 + 4 + 2, color.R, color.G, color.B), // rectangle
                    StrokeThickness = 0,          // Border thickness
                    Text = zigzag.Percentage.ToString("N2")
                };
                chart.Annotations.Add(rectangle);
            }
            //chart.Annotations.TrackerFormatString = "X: {2:0.00}\nY: {4:0.00}";
        }
    }



    public static void CreateChart(out PlotModel chart, CryptoSymbol symbol)
    {
        chart = new PlotModel
        {
            TextColor = OxyColors.White,
        };

        chart.Axes.Clear();
        chart.Series.Clear();
        chart.Annotations.Clear();

        chart.Axes.Add(new DateTimeAxis
        {
            //Angle = 0,
            Position = AxisPosition.Bottom,
            StringFormat = "dd",
            TextColor = OxyColors.LightGray,

            AxislineStyle = LineStyle.Automatic,
            AxislineColor = OxyColors.White,
            AxislineThickness = 1,

            TicklineColor = OxyColors.White,

            //Titlema= 10, // Voeg wat marge toe voor leesbaarheid
            MinorIntervalType = DateTimeIntervalType.Auto,
            IntervalType = DateTimeIntervalType.Auto,
            //IntervalLength = 24,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dash,
            MinorStep = (double)4 / 24, // 4 hours, 6 ticks
            MajorStep = 1, // day's
            MinimumPadding = 0.1, // Voeg wat padding toe om te voorkomen dat de labels tegen de rand komen
            MaximumPadding = 0.1,
        });


        chart.Axes.Add(new LinearAxis 
        { 
            Position = AxisPosition.Right,
            MinorTickSize = (double)symbol.PriceTickSize * 2,
            MajorTickSize = (double)symbol.PriceTickSize * 10,
            TickStyle = OxyPlot.Axes.TickStyle.Inside,
            TicklineColor = OxyColors.Yellow,
            MinorTicklineColor = OxyColors.Yellow,
            TextColor = OxyColors.LightGray,
            MajorGridlineStyle = LineStyle.Solid, 
            MinorGridlineStyle = LineStyle.Solid,

            AxislineStyle = LineStyle.Automatic,
            AxislineColor = OxyColors.White,
            AxislineThickness = 1,

            MinimumPadding = 0.1, // Voeg wat padding toe om te voorkomen dat de labels tegen de rand komen
            MaximumPadding = 0.1,
        });
    }

}
