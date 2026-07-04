using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Bre;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

namespace CryptoScanner.ViewModels.Chart;

/// <summary>
/// "Buddy Reversion Engine (BRE) - Ultimate Master Cockpit" indicator (clone of the TradingView
/// Pine script). The construction, computed by <see cref="BreBandsHelper.ComputeBands"/> so the
/// chart and the bre signal always agree:
///   - Outer bands (gray plateaus): Donchian middle ± halfRange * (OuterMult / 2.5), computed over
///     the PREVIOUS BandLength candles.
///   - DIDO basis: the blue EMA(DidoLength) middle line. The trend coloured cloud lines and the
///     background fill from the Pine script are intentionally not drawn (removed on request).
///   - WGHM trend line: HMA(HmaLength), only drawn when the trend filter is enabled.
///   - White percentage labels at the exact candles where the bre long/short alert fires
///     (band break + stacking rule + all enabled filters).
/// </summary>
public class BreBands
{
    // Colours, translated from the Pine color.new(..., transparency) values.
    // Pine transparency is "percent transparent", so alpha = 255 * (100 - transparency) / 100.
    private static readonly OxyColor OuterBandColor = OxyColor.FromArgb(178, 0xb2, 0xb5, 0xbe); // #b2b5be, 30% transparent
    private static readonly OxyColor BasisColor = OxyColor.FromArgb(127, 0x29, 0x62, 0xff);     // #2962ff, 50% transparent
    private static readonly OxyColor HmaUpColor = OxyColors.Green;
    private static readonly OxyColor HmaDownColor = OxyColors.Red;

    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval, CandleTime minDate, CandleTime maxDate, string group)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return;

        var candles = symbolInterval.CandleList.Values.ToList();
        var settings = GlobalData.Settings.Signal.Bre;

        // Bands + filter series, computed by the shared helper so the chart and the bre signal
        // stay identical. Index-aligned with the candle list.
        BreBandValue[] bands = BreBandsHelper.ComputeBands(candles);

        // Outer Donchian bands (the gray plateaus from the dashboard).
        var outerUp = new LineSeries { Title = "bre.upper", Color = OuterBandColor, StrokeThickness = 2, YAxisKey = "price", Tag = group };
        var outerDown = new LineSeries { Title = "bre.lower", Color = OuterBandColor, StrokeThickness = 2, YAxisKey = "price", Tag = group };

        // DIDO basis (middle) line.
        var basisLine = new LineSeries { Title = "bre.basis", Color = BasisColor, StrokeThickness = 2, YAxisKey = "price", Tag = group };

        // WGHM trend line (only when the trend filter is enabled), split per trend colour.
        var hmaGreen = new LineSeries { Title = "bre.wghm", Color = HmaUpColor, StrokeThickness = 1, YAxisKey = "price", Tag = group };
        var hmaRed = new LineSeries { Title = "bre.wghm", Color = HmaDownColor, StrokeThickness = 1, YAxisKey = "price", Tag = group };

        // Break point used to interrupt a line/area series so trend segments do not connect.
        var breakPoint = new DataPoint(double.NaN, double.NaN);

        for (int i = 0; i < candles.Count; i++)
        {
            var candle = candles[i];
            CandleTime openTime = CandleTime.AlignFromDateTime(candle.Date, interval.Duration);
            if (openTime < minDate || openTime > maxDate)
                continue;

            double x = openTime.Minutes;
            double close = (double)candle.Close;

            ref BreBandValue value = ref bands[i];

            if (value.HasBands)
            {
                outerUp.Points.Add(new DataPoint(x, value.Upper));
                outerDown.Points.Add(new DataPoint(x, value.Lower));
            }

            if (value.DidoBasis.HasValue)
                basisLine.Points.Add(new DataPoint(x, value.DidoBasis.Value));

            if (settings.UseTrendFilter && value.Hma.HasValue)
            {
                if (close > value.Hma.Value)
                {
                    hmaGreen.Points.Add(new DataPoint(x, value.Hma.Value));
                    hmaRed.Points.Add(breakPoint);
                }
                else
                {
                    hmaRed.Points.Add(new DataPoint(x, value.Hma.Value));
                    hmaGreen.Points.Add(breakPoint);
                }
            }

            // Break labels: the exact long/short signal condition (band break + stacking rule +
            // all enabled filters), evaluated by the shared helper so chart and alert stay in sync.
            if (BreBandsHelper.IsShortBreak(candles, bands, i, out double pctShort, out _, out _))
                AddLabel(chart, x, (double)candle.High, pctShort, VerticalAlignment.Bottom, group);
            if (BreBandsHelper.IsLongBreak(candles, bands, i, out double pctLong, out _, out _))
                AddLabel(chart, x, (double)candle.Low, pctLong, VerticalAlignment.Top, group);
        }

        // Add the outer band lines first, then the basis on top.
        chart.Series.Add(outerUp);
        chart.Series.Add(outerDown);
        chart.Series.Add(basisLine);
        if (settings.UseTrendFilter)
        {
            chart.Series.Add(hmaGreen);
            chart.Series.Add(hmaRed);
        }
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
            // (same style as the BabaBands labels).
            Background = OxyColors.Undefined,
            Stroke = OxyColors.Transparent,
            StrokeThickness = 0,
            FontSize = 9,
            YAxisKey = "price",
            Tag = group,
        });
    }
}
