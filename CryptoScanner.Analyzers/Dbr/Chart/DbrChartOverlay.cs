using CryptoScanner.Analyzers.Dbr.Signal;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

namespace CryptoScanner.Analyzers.Dbr.Chart;

/// <summary>
/// "Donchian Breakout Reversion (DBR) - Ultimate Master Cockpit" indicator (clone of the TradingView
/// Pine script). The construction, computed by <see cref="DbrBandsHelper.ComputeBands"/> so the
/// chart and the dbr signal always agree:
///   - Outer bands (gray plateaus): Donchian middle +/- halfRange * (OuterMult / 2.5), computed over
///     the PREVIOUS BandLength candles.
///   - White percentage labels at the exact candles where the dbr long/short alert fires
///     (band break + stacking rule + all enabled filters).
/// </summary>
public class DbrChartOverlay : IChartOverlay
{
    public string Label => "DBR Bands";
    public string GroupKey => "dbr";
#pragma warning disable CS0067 // Required by IChartOverlay; raised externally when needed
    public event Action? RequestRedraw;
#pragma warning restore CS0067

    // Colours, translated from the Pine color.new(..., transparency) values.
    // Pine transparency is "percent transparent", so alpha = 255 * (100 - transparency) / 100.
    private static readonly OxyColor OuterBandColor = OxyColor.FromArgb(178, 0xb2, 0xb5, 0xbe); // #b2b5be, 30% transparent

    public void Draw(object plotModel, CryptoSymbol symbol, CryptoInterval interval,
                     List<CryptoCandle> candles, CandleTime minDate, CandleTime maxDate, string group)
    {
        var chart = (PlotModel)plotModel;
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return;

        var allCandles = symbolInterval.CandleList.Values.ToList();

        // Bands, computed by the shared helper so the chart and the dbr signal
        // stay identical. Index-aligned with the candle list.
        DbrBandValue[] bands = DbrBandsHelper.ComputeBands(allCandles);

        // Outer Donchian bands (the gray plateaus from the dashboard).
        var outerUp = new LineSeries { Title = "dbr.upper", Color = OuterBandColor, StrokeThickness = 2, YAxisKey = "price", Tag = group };
        var outerDown = new LineSeries { Title = "dbr.lower", Color = OuterBandColor, StrokeThickness = 2, YAxisKey = "price", Tag = group };

        for (int i = 0; i < allCandles.Count; i++)
        {
            var candle = allCandles[i];
            CandleTime openTime = CandleTime.AlignFromDateTime(candle.Date, interval.Duration);
            if (openTime < minDate || openTime > maxDate)
                continue;

            double x = openTime.Minutes;

            ref DbrBandValue value = ref bands[i];

            if (value.HasBands)
            {
                outerUp.Points.Add(new DataPoint(x, value.Upper));
                outerDown.Points.Add(new DataPoint(x, value.Lower));
            }

            // Break labels: the exact long/short signal condition (band break + stacking rule +
            // all enabled filters), evaluated by the shared helper so chart and alert stay in sync.
            if (DbrBandsHelper.IsShortBreak(allCandles, bands, i, out double pctShort, out _, out _))
                AddLabel(chart, x, (double)candle.High, pctShort, VerticalAlignment.Bottom, group);
            if (DbrBandsHelper.IsLongBreak(allCandles, bands, i, out double pctLong, out _, out _))
                AddLabel(chart, x, (double)candle.Low, pctLong, VerticalAlignment.Top, group);
        }

        chart.Series.Add(outerUp);
        chart.Series.Add(outerDown);
    }

    private static void AddLabel(PlotModel chart, double x, double y, double pct, VerticalAlignment vAlign, string group)
    {
        // Extra gap so the label clears the wick. Screen coordinates: Y increases downward, so a
        // label above the High (vAlign Bottom) is nudged UP (negative), one below the Low (vAlign Top)
        // DOWN (positive).
        const double gapPixels = 20;
        double offsetY = vAlign == VerticalAlignment.Bottom ? -gapPixels : gapPixels;

        chart.Annotations.Add(new TextAnnotation
        {
            Text = pct.ToString("0.##") + "%",
            TextPosition = new DataPoint(x, y),
            Offset = new ScreenVector(0, offsetY),
            TextHorizontalAlignment = HorizontalAlignment.Center,
            TextVerticalAlignment = vAlign,
            TextColor = OxyColors.White,
            // No background rectangle / border — plain white text so it doesn't block the candles
            Background = OxyColors.Undefined,
            Stroke = OxyColors.Transparent,
            StrokeThickness = 0,
            FontSize = 9,
            YAxisKey = "price",
            Tag = group,
        });
    }
}
