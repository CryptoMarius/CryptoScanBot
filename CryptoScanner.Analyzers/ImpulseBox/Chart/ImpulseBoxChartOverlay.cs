using CryptoScanner.Analyzers.Chart;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

namespace CryptoScanner.Analyzers.ImpulseBox.Chart;

/// <summary>
/// The boxes the detector found (top and bottom from their first candle to the candle they broke or
/// expired on), the start of the impulse as a dashed line over the same stretch, and a label on every
/// candle where an enabled trade fires.
/// </summary>
public class ImpulseBoxChartOverlay : IChartOverlay
{
    public string Label => "Impulse box";
    public string GroupKey => "impulsebox";
#pragma warning disable CS0067 // Required by IChartOverlay; raised externally when needed
    public event Action? RequestRedraw;
#pragma warning restore CS0067

    public const string KeyBoxUp = "impulseboxBoxUp";
    public const string KeyBoxDown = "impulseboxBoxDown";
    public const string KeyStart = "impulseboxStart";

    public IReadOnlyList<ChartOverlayStyleDefinition> StyleDefinitions { get; } =
    [
        new() { Key = KeyBoxUp, Label = "Box after an impulse up", Color = "#FF43A047", LineWidth = 2 },
        new() { Key = KeyBoxDown, Label = "Box after an impulse down", Color = "#FFE53935", LineWidth = 2 },
        new() { Key = KeyStart, Label = "Impulse start", Color = "#FF9E9E9E", LineWidth = 1, LineStyle = 2 },
    ];

    private static readonly OxyColor BoxUpColor = OxyColor.FromArgb(255, 67, 160, 71);    // green
    private static readonly OxyColor BoxDownColor = OxyColor.FromArgb(255, 229, 57, 53);  // red
    private static readonly OxyColor StartColor = OxyColor.FromArgb(255, 158, 158, 158);  // grey

    /// <summary>The label text of an event when its kind is enabled, otherwise null.</summary>
    private static string? LabelFor(ImpulseBoxEvent e)
    {
        var settings = ImpulseBoxPlugin.Settings;
        string side = e.IsLong ? "long" : "short";
        return e.Kind switch
        {
            ImpulseBoxEventKind.Counter when settings.TradeCounter => "counter " + side,
            ImpulseBoxEventKind.FakeBreak when settings.TradeFakeBreak => "fake " + side,
            ImpulseBoxEventKind.Continuation when settings.TradeContinuation => "cont " + side,
            _ => null,
        };
    }

    public void Draw(object plotModel, CryptoSymbol symbol, CryptoInterval interval,
                     List<CryptoCandle> candles, CandleTime minDate, CandleTime maxDate, string group)
    {
        var chart = (PlotModel)plotModel;
        if (candles.Count == 0)
            return;

        var (events, spans) = ImpulseBoxDetector.Compute(candles, ImpulseBoxPlugin.Settings);

        LineSeries MakeLine(string title, string key, OxyColor fallback, LineStyle style, int width) => new()
        {
            Title = title,
            Color = ChartStyleOxy.ColorFor(key, fallback),
            StrokeThickness = ChartStyleOxy.ThicknessFor(key, width),
            LineStyle = ChartStyleOxy.LineStyleFor(key, style),
            YAxisKey = "price",
            Tag = group,
        };
        var boxUp = MakeLine("impulsebox.up", KeyBoxUp, BoxUpColor, LineStyle.Solid, 2);
        var boxDown = MakeLine("impulsebox.down", KeyBoxDown, BoxDownColor, LineStyle.Solid, 2);
        var start = MakeLine("impulsebox.start", KeyStart, StartColor, LineStyle.Dash, 1);

        double X(int i) => CandleTime.AlignFromDateTime(candles[i].Date, interval.Duration).Minutes;
        bool Visible(int i)
        {
            CandleTime t = CandleTime.AlignFromDateTime(candles[i].Date, interval.Duration);
            return t >= minDate && t <= maxDate;
        }

        foreach (var span in spans)
        {
            if (!Visible(span.FirstIndex) && !Visible(span.LastIndex))
                continue;
            var line = span.Direction == 1 ? boxUp : boxDown;
            double x0 = X(span.FirstIndex);
            double x1 = X(span.LastIndex);
            // Top, bottom and start each as their own stretch, broken by an undefined point
            line.Points.Add(new DataPoint(x0, span.High));
            line.Points.Add(new DataPoint(x1, span.High));
            line.Points.Add(DataPoint.Undefined);
            line.Points.Add(new DataPoint(x0, span.Low));
            line.Points.Add(new DataPoint(x1, span.Low));
            line.Points.Add(DataPoint.Undefined);
            start.Points.Add(new DataPoint(x0, span.ImpulseStart));
            start.Points.Add(new DataPoint(x1, span.ImpulseStart));
            start.Points.Add(DataPoint.Undefined);
        }

        for (int i = 0; i < candles.Count; i++)
        {
            if (events[i] == null || !Visible(i))
                continue;
            foreach (var e in events[i]!)
            {
                string? text = LabelFor(e);
                if (text == null)
                    continue;
                if (e.IsLong)
                    AddLabel(chart, X(i), (double)candles[i].Low, text, VerticalAlignment.Top, group);
                else
                    AddLabel(chart, X(i), (double)candles[i].High, text, VerticalAlignment.Bottom, group);
            }
        }

        chart.Series.Add(boxUp);
        chart.Series.Add(boxDown);
        chart.Series.Add(start);
    }

    public IReadOnlyList<ChartOverlaySeries> GetSeries(CryptoSymbol symbol, CryptoInterval interval, List<CryptoCandle> candles)
    {
        if (candles.Count == 0)
            return [];

        var (_, spans) = ImpulseBoxDetector.Compute(candles, ImpulseBoxPlugin.Settings);

        var boxUp = ((IChartOverlay)this).SeriesFor(KeyBoxUp);
        var boxDown = ((IChartOverlay)this).SeriesFor(KeyBoxDown);
        var start = ((IChartOverlay)this).SeriesFor(KeyStart);

        long T(int i) => CandleTime.AlignFromDateTime(candles[i].Date, interval.Duration).ToUnixSeconds();

        // Every level is a flat stretch closed by a NaN. The Photino chart turns each stretch into a
        // line of its own, so the top, the bottom and the start of a box are never joined by a
        // slanted stroke, and stretches may overlap in time.
        foreach (var span in spans)
        {
            var box = span.Direction == 1 ? boxUp : boxDown;
            long from = T(span.FirstIndex);
            long to = T(span.LastIndex);
            AddStretch(box.Points, from, to, span.High);
            AddStretch(box.Points, from, to, span.Low);
            AddStretch(start.Points, from, to, span.ImpulseStart);
        }

        return [boxUp, boxDown, start];
    }

    private static void AddStretch(List<ChartOverlayPoint> points, long from, long to, double value)
    {
        points.Add(new ChartOverlayPoint { Time = from, Value = value });
        if (to > from)
            points.Add(new ChartOverlayPoint { Time = to, Value = value });
        points.Add(new ChartOverlayPoint { Time = to, Value = double.NaN });
    }

    public IReadOnlyList<ChartOverlayLabel> GetLabels(CryptoSymbol symbol, CryptoInterval interval, List<CryptoCandle> candles)
    {
        if (candles.Count == 0)
            return [];

        // Same events as Draw, so the web chart shows the labels at the very same candles
        var (events, _) = ImpulseBoxDetector.Compute(candles, ImpulseBoxPlugin.Settings);
        var labels = new List<ChartOverlayLabel>();
        for (int i = 0; i < candles.Count; i++)
        {
            if (events[i] == null)
                continue;
            long time = CandleTime.AlignFromDateTime(candles[i].Date, interval.Duration).ToUnixSeconds();
            foreach (var e in events[i]!)
            {
                string? text = LabelFor(e);
                if (text == null)
                    continue;
                if (e.IsLong)
                    labels.Add(new ChartOverlayLabel { Time = time, Above = false, Price = (double)candles[i].Low, Text = text });
                else
                    labels.Add(new ChartOverlayLabel { Time = time, Above = true, Price = (double)candles[i].High, Text = text });
            }
        }
        return labels;
    }

    private static void AddLabel(PlotModel chart, double x, double y, string text, VerticalAlignment vAlign, string group)
    {
        const double gapPixels = 20;
        double offsetY = vAlign == VerticalAlignment.Bottom ? -gapPixels : gapPixels;

        chart.Annotations.Add(new TextAnnotation
        {
            Text = text,
            TextPosition = new DataPoint(x, y),
            Offset = new ScreenVector(0, offsetY),
            TextHorizontalAlignment = HorizontalAlignment.Center,
            TextVerticalAlignment = vAlign,
            TextColor = OxyColors.White,
            Background = OxyColors.Undefined,
            Stroke = OxyColors.Transparent,
            StrokeThickness = 0,
            FontSize = 9,
            YAxisKey = "price",
            Tag = group,
        });
    }
}
