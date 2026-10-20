using CryptoScanner.Analyzers.Chart;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

namespace CryptoScanner.Analyzers.VwapMr.Chart;

/// <summary>
/// The day VWAP with its two stdev bands, and a label on every candle where the signal would fire.
/// The lines are broken at midnight UTC, where the VWAP starts again.
/// </summary>
public class VwapMrChartOverlay : IChartOverlay
{
    public string Label => "VWAP MR Bands";
    public string GroupKey => "vwapmr";
#pragma warning disable CS0067 // Required by IChartOverlay; raised externally when needed
    public event Action? RequestRedraw;
#pragma warning restore CS0067

    public const string KeyUpper = "vwapmrUpper";
    public const string KeyLower = "vwapmrLower";
    public const string KeyVwap = "vwapmrVwap";

    public IReadOnlyList<ChartOverlayStyleDefinition> StyleDefinitions { get; } =
    [
        new() { Key = KeyUpper, Label = "Upper band", Color = "#FFE53935", LineWidth = 2 },
        new() { Key = KeyLower, Label = "Lower band", Color = "#FF43A047", LineWidth = 2 },
        new() { Key = KeyVwap, Label = "VWAP", Color = "#FF1E88E5", LineWidth = 2 },
    ];

    private static readonly OxyColor UpperColor = OxyColor.FromArgb(255, 229, 57, 53);  // red
    private static readonly OxyColor LowerColor = OxyColor.FromArgb(255, 67, 160, 71);  // green
    private static readonly OxyColor VwapColor = OxyColor.FromArgb(255, 30, 136, 229);  // blue

    /// <summary>
    /// The same test as VwapMrBase.IsSignal, on the chart's own band array: a touch on this candle
    /// or the one before, each against its own band, and (when asked for) the reversal candle.
    /// </summary>
    private static void SignalAt(List<CryptoCandle> candles, VwapMrBandCalculator.BandValue[] bands, int i,
        out bool isLong, out bool isShort)
    {
        isLong = false;
        isShort = false;
        if (!bands[i].HasValue)
            return;

        var settings = VwapMrPlugin.Settings;
        CryptoCandle c = candles[i];
        bool hasPrev = i > 0 && bands[i - 1].HasValue;

        bool touchLower = (double)c.Low <= bands[i].Lower || (hasPrev && (double)candles[i - 1].Low <= bands[i - 1].Lower);
        bool touchUpper = (double)c.High >= bands[i].Upper || (hasPrev && (double)candles[i - 1].High >= bands[i - 1].Upper);

        isLong = touchLower && (!settings.RequireReversalCandle || c.Close > c.Open);
        isShort = touchUpper && (!settings.RequireReversalCandle || c.Close < c.Open);
    }

    public void Draw(object plotModel, CryptoSymbol symbol, CryptoInterval interval,
                     List<CryptoCandle> candles, CandleTime minDate, CandleTime maxDate, string group)
    {
        var chart = (PlotModel)plotModel;
        if (candles.Count == 0)
            return;

        var bands = VwapMrBandCalculator.ComputeBands(candles);

        LineSeries MakeLine(string title, string key, OxyColor fallback) => new()
        {
            Title = title,
            Color = ChartStyleOxy.ColorFor(key, fallback),
            StrokeThickness = ChartStyleOxy.ThicknessFor(key, 2),
            LineStyle = ChartStyleOxy.LineStyleFor(key, LineStyle.Solid),
            YAxisKey = "price",
            Tag = group,
        };
        var upperLine = MakeLine("vwapmr.upper", KeyUpper, UpperColor);
        var lowerLine = MakeLine("vwapmr.lower", KeyLower, LowerColor);
        var vwapLine = MakeLine("vwapmr.vwap", KeyVwap, VwapColor);

        long previousDay = long.MinValue;
        for (int i = 0; i < candles.Count; i++)
        {
            var candle = candles[i];
            CandleTime openTime = CandleTime.AlignFromDateTime(candle.Date, interval.Duration);
            if (openTime < minDate || openTime > maxDate)
                continue;
            if (!bands[i].HasValue)
                continue;

            // A break in the lines where a new day starts, so the jump of the VWAP is not drawn as
            // a slope.
            long day = candle.Timestamp.Ticks / TimeSpan.TicksPerDay;
            if (previousDay != long.MinValue && day != previousDay)
            {
                upperLine.Points.Add(DataPoint.Undefined);
                lowerLine.Points.Add(DataPoint.Undefined);
                vwapLine.Points.Add(DataPoint.Undefined);
            }
            previousDay = day;

            double x = openTime.Minutes;
            upperLine.Points.Add(new DataPoint(x, bands[i].Upper));
            lowerLine.Points.Add(new DataPoint(x, bands[i].Lower));
            vwapLine.Points.Add(new DataPoint(x, bands[i].Vwap));

            SignalAt(candles, bands, i, out bool isLong, out bool isShort);
            if (isShort)
                AddLabel(chart, x, (double)candle.High, "short", VerticalAlignment.Bottom, group);
            if (isLong)
                AddLabel(chart, x, (double)candle.Low, "long", VerticalAlignment.Top, group);
        }

        chart.Series.Add(upperLine);
        chart.Series.Add(lowerLine);
        chart.Series.Add(vwapLine);
    }

    public IReadOnlyList<ChartOverlaySeries> GetSeries(CryptoSymbol symbol, CryptoInterval interval, List<CryptoCandle> candles)
    {
        if (candles.Count == 0)
            return [];

        var bands = VwapMrBandCalculator.ComputeBands(candles);

        var upper = ((IChartOverlay)this).SeriesFor(KeyUpper);
        var lower = ((IChartOverlay)this).SeriesFor(KeyLower);
        var vwap = ((IChartOverlay)this).SeriesFor(KeyVwap);

        for (int i = 0; i < candles.Count; i++)
        {
            if (!bands[i].HasValue)
                continue;

            long time = CandleTime.AlignFromDateTime(candles[i].Date, interval.Duration).ToUnixSeconds();
            upper.Points.Add(new ChartOverlayPoint { Time = time, Value = bands[i].Upper });
            lower.Points.Add(new ChartOverlayPoint { Time = time, Value = bands[i].Lower });
            vwap.Points.Add(new ChartOverlayPoint { Time = time, Value = bands[i].Vwap });
        }

        return [upper, lower, vwap];
    }

    public IReadOnlyList<ChartOverlayLabel> GetLabels(CryptoSymbol symbol, CryptoInterval interval, List<CryptoCandle> candles)
    {
        if (candles.Count == 0)
            return [];

        // Same conditions as Draw, so the web chart shows the labels at the very same candles.
        var bands = VwapMrBandCalculator.ComputeBands(candles);
        var labels = new List<ChartOverlayLabel>();
        for (int i = 0; i < candles.Count; i++)
        {
            SignalAt(candles, bands, i, out bool isLong, out bool isShort);
            if (!isLong && !isShort)
                continue;

            long time = CandleTime.AlignFromDateTime(candles[i].Date, interval.Duration).ToUnixSeconds();
            if (isShort)
                labels.Add(new ChartOverlayLabel { Time = time, Above = true, Price = (double)candles[i].High, Text = "short" });
            if (isLong)
                labels.Add(new ChartOverlayLabel { Time = time, Above = false, Price = (double)candles[i].Low, Text = "long" });
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
