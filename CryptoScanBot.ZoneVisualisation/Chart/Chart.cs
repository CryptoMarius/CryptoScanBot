using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;

using System.Globalization;

namespace CryptoScanBot.ZoneVisualisation.Chart;


public class Chart
{
    private static string? _priceFormat;
    private static CryptoInterval? _interval;

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

    public static PlotModel Create(CryptoSymbol symbol, CryptoInterval interval, out LineAnnotation horizontalLine, out LineAnnotation verticalLine)
    {
        _interval = interval;
        _priceFormat = symbol.PriceDisplayFormat;

        PlotModel chart = new()
        {
            Subtitle = " ",
            TitleFont = Const.OxyFontName,
            TextColor = OxyColors.White,
            SubtitleFont = Const.OxyFontName,
            SubtitleColor = OxyColors.White,
            SubtitleFontWeight = FontWeights.Bold,
        };

        // x-axis
        chart.Axes.Add(new LinearAxis
        {
            Font = Const.OxyFontName,
            FontSize = Const.OxyFontSize,
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

            MajorStep = (24 * 60 * 60 / interval.Duration) * interval.Duration,
            MinorStep = (24 * 60 * 60 / interval.Duration) * interval.Duration / 6,
        });


        // y-axis
        chart.Axes.Add(new LinearAxis
        {
            LabelFormatter = LabelFormatterY,
            Font = Const.OxyFontName,
            FontSize = Const.OxyFontSize,
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


        // crosshair x
        verticalLine = new LineAnnotation { Type = LineAnnotationType.Vertical, Color = OxyColors.DarkGray, LineStyle = LineStyle.Dash, StrokeThickness = 0.25, X = 0 };
        chart.Annotations.Add(verticalLine);

        // crosshair y
        horizontalLine = new LineAnnotation { Type = LineAnnotationType.Horizontal, Color = OxyColors.DarkGray, LineStyle = LineStyle.Dash, StrokeThickness = 0.25, Y = 0 };
        chart.Annotations.Add(horizontalLine);


        // dunno?
        //long unix = CandleTools.GetUnixTime(new DateTime(2024, 11, 09, 00, 00, 00, DateTimeKind.Utc), 60);
        //if (data.SymbolInterval.CandleList.TryGetValue(unix, out var candle))
        //{
        //    OxyColor boxColor = OxyColors.BlueViolet;
        //    var rectangle = new RectangleAnnotation
        //    {
        //        MinimumX = candle.OpenTime - _interval?.Duration / 2, 
        //        MinimumY = (double)candle.Low - 20000,
        //        MaximumX = candle.OpenTime + _interval?.Duration / 2,
        //        MaximumY = (double)candle.High + 20000,
        //        Fill = OxyColor.FromArgb(128, boxColor.R, boxColor.G, boxColor.B),
        //        Stroke = OxyColor.FromArgb(128 + 64 + 32 + 16 + 8 + 4 + 2, boxColor.R, boxColor.G, boxColor.B),
        //        StrokeThickness = 0.25, 
        //    };
        //    chart.Annotations.Add(rectangle);
        //}

        return chart;
    }

}
