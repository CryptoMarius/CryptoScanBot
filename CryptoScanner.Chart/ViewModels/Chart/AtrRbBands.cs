using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.ViewModels.Chart;

/// <summary>
/// "Mean Reversion Bands" indicator (Bollinger basis + a fast ATR term), kept in sync with the atrrb
/// signal via GlobalData.Settings.Signal.AtrRb:
///   basis = SMA(Length)
///   band  = Mult * stdev(Length) + AtrMult * ATR(AtrLength)
///   upper = basis + band,  lower = basis - band
/// A percentage label (the signal's StopLossAtrFactor * ATR%) is printed when a wick or close breaks a
/// band, marking where the long/short alert can fire.
/// </summary>
public class AtrRbBands
{
    private static readonly OxyColor BandLineColor = OxyColor.FromArgb(255, 0, 150, 136); // teal
    private static readonly OxyColor BandFillColor = OxyColor.FromArgb(18, 0, 150, 136);  // teal, faint
    private static readonly OxyColor BasisColor = OxyColor.FromArgb(140, 128, 128, 128);  // gray

    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval,
        List<CryptoCandle> candles, CandleTime minDate, CandleTime maxDate, string group)
    {
        if (candles.Count == 0)
            return;

        var atrrb = GlobalData.Settings.Signal.AtrRb;

        // Bollinger (basis + stdev band) and the fast ATR, indexed by date so we can join on the candle.
        var bbByDate = new Dictionary<DateTime, BollingerBandsResult>();
        foreach (var bb in candles.GetBollingerBands(atrrb.Length, atrrb.Mult))
            bbByDate[bb.Date] = bb;

        // Fast ATR (band shape) and the slow ATR over the band Length (used for the SL% label, so it
        // stays stable through a rally — same as the signal's StopLossPercent).
        var atrByDate = new Dictionary<DateTime, double>();
        foreach (var atr in candles.GetAtr(atrrb.AtrLength))
        {
            if (atr.Atr.HasValue)
                atrByDate[atr.Date] = atr.Atr.Value;
        }

        var slAtrByDate = new Dictionary<DateTime, double>();
        foreach (var atr in candles.GetAtr(atrrb.Length))
        {
            if (atr.Atr.HasValue)
                slAtrByDate[atr.Date] = atr.Atr.Value;
        }

        var bandFill = new AreaSeries { Title = "atrrb.fill", Fill = BandFillColor, Color = OxyColors.Transparent, StrokeThickness = 0, YAxisKey = "price", Tag = group };
        var upperLine = new LineSeries { Title = "atrrb.upper", Color = BandLineColor, StrokeThickness = 2, YAxisKey = "price", Tag = group };
        var lowerLine = new LineSeries { Title = "atrrb.lower", Color = BandLineColor, StrokeThickness = 2, YAxisKey = "price", Tag = group };
        var basisLine = new LineSeries { Title = "atrrb.basis", Color = BasisColor, StrokeThickness = 1, YAxisKey = "price", Tag = group };

        for (int i = 0; i < candles.Count; i++)
        {
            var candle = candles[i];
            CandleTime openTime = CandleTime.AlignFromDateTime(candle.Date, interval.Duration);
            if (openTime < minDate || openTime > maxDate)
                continue;

            if (!bbByDate.TryGetValue(candle.Date, out var bb) || !atrByDate.TryGetValue(candle.Date, out double atr))
                continue;
            if (!bb.Sma.HasValue || !bb.UpperBand.HasValue || !bb.LowerBand.HasValue)
                continue;

            double x = openTime.Minutes;
            double close = (double)candle.Close;
            double high = (double)candle.High;
            double low = (double)candle.Low;

            double pad = atrrb.AtrMult * atr;
            double upper = bb.UpperBand.Value + pad;
            double lower = bb.LowerBand.Value - pad;

            bandFill.Points.Add(new DataPoint(x, upper));
            bandFill.Points2.Add(new DataPoint(x, lower));
            upperLine.Points.Add(new DataPoint(x, upper));
            lowerLine.Points.Add(new DataPoint(x, lower));
            basisLine.Points.Add(new DataPoint(x, bb.Sma.Value));

            // Break label = the SL distance the signal applies: StopLossAtrFactor * ATR(Length)%
            // (slow ATR over the band Length, so it stays stable through a volatile rally).
            double slAtr = slAtrByDate.TryGetValue(candle.Date, out double sa) ? sa : atr;
            double slPct = atrrb.StopLossAtrFactor * (slAtr / close * 100);

            if (high > upper || close > upper)
                AddLabel(chart, x, high, slPct, VerticalAlignment.Bottom, group);
            if (low < lower || close < lower)
                AddLabel(chart, x, low, slPct, VerticalAlignment.Top, group);
        }

        chart.Series.Add(bandFill);
        chart.Series.Add(upperLine);
        chart.Series.Add(lowerLine);
        chart.Series.Add(basisLine);
    }

    private static void AddLabel(PlotModel chart, double x, double y, double pct, VerticalAlignment vAlign, string group)
    {
        // Extra gap so the label clears the wick (a bit more than before).
        const double gapPixels = 30;
        double offsetY = vAlign == VerticalAlignment.Bottom ? -gapPixels : gapPixels;

        chart.Annotations.Add(new TextAnnotation
        {
            Text = pct.ToString("0.##") + "%",
            TextPosition = new DataPoint(x, y),
            Offset = new ScreenVector(0, offsetY),
            TextHorizontalAlignment = HorizontalAlignment.Center,
            TextVerticalAlignment = vAlign,
            TextColor = OxyColors.White,
            // No background rectangle / border — plain white text so it doesn't block the candles.
            Background = OxyColors.Undefined,
            Stroke = OxyColors.Transparent,
            StrokeThickness = 0,
            FontSize = 9,
            YAxisKey = "price",
            Tag = group,
        });
    }
}
