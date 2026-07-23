using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.Analyzers.Vbs.Chart;

/// <summary>
/// Volume-weighted VWAP bands
/// </summary>
public class VbsChartOverlay : IChartOverlay
{
    public string Label => "Vbs Bands";
    public string GroupKey => "vbs";
#pragma warning disable CS0067 // Required by IChartOverlay; raised externally when needed
    public event Action? RequestRedraw;
#pragma warning restore CS0067

    private static readonly OxyColor BandLineColor = OxyColor.FromArgb(255, 0, 150, 136); // teal
    //private static readonly OxyColor BandFillColor = OxyColor.FromArgb(18, 0, 150, 136);  // teal, faint
    private static readonly OxyColor BasisColor = OxyColor.FromArgb(140, 128, 128, 128);  // gray

    public void Draw(object plotModel, CryptoSymbol symbol, CryptoInterval interval,
                     List<CryptoCandle> candles, CandleTime minDate, CandleTime maxDate, string group)
    {
        var chart = (PlotModel)plotModel;
        if (candles.Count == 0)
            return;

        var vbs = VbsPlugin.Settings;

        // Volume-weighted VWAP bands (basis/upper/lower), computed by the shared helper so the chart and
        // the signal stay identical. Index-aligned with the candle list below.
        var bands = VbsBandsHelper.ComputeBands(candles);

        // Slow ATR over the band Length (used for the SL% label, so it stays stable through a rally —
        // same as the signal's StopLossPercent).
        var slAtrByDate = new Dictionary<DateTime, double>();
        foreach (var atr in candles.AsQuotes().ToAtr(vbs.Length))
        {
            if (atr.Atr.HasValue)
                slAtrByDate[atr.Timestamp] = atr.Atr.Value;
        }

        // RSI confluence for the break labels (same gate as SignalVbsLong/Short): when the RSI filter
        // is enabled, only label a lower-band break when RSI is oversold and an upper-band break when
        // RSI is overbought. Thresholds come from the general RSI settings (Indicators tab).
        var rsiSettings = GlobalData.Settings.General.SettingsRsi;
        IReadOnlyList<RsiResult>? rsiList = null;
        if (vbs.UseRsiFilter)
            rsiList = candles.AsQuotes().ToRsi(rsiSettings.Length);

        //var bandFill = new AreaSeries { Title = "vbs.fill", Fill = BandFillColor, Color = OxyColors.Transparent, StrokeThickness = 0, YAxisKey = "price", Tag = group };
        var upperLine = new LineSeries { Title = "vbs.upper", Color = BandLineColor, StrokeThickness = 2, YAxisKey = "price", Tag = group };
        var lowerLine = new LineSeries { Title = "vbs.lower", Color = BandLineColor, StrokeThickness = 2, YAxisKey = "price", Tag = group };
        var basisLine = new LineSeries { Title = "vbs.basis", Color = BasisColor, StrokeThickness = 1, YAxisKey = "price", Tag = group };

        for (int i = 0; i < candles.Count; i++)
        {
            var candle = candles[i];
            CandleTime openTime = CandleTime.AlignFromDateTime(candle.Date, interval.Duration);
            if (openTime < minDate || openTime > maxDate)
                continue;

            // bands is index-aligned with candles; skip the indicator warm-up.
            if (!bands[i].HasValue)
                continue;

            double x = openTime.Minutes;
            double close = (double)candle.Close;
            double high = (double)candle.High;
            double low = (double)candle.Low;

            double upper = bands[i].Upper;
            double lower = bands[i].Lower;

            //bandFill.Points.Add(new DataPoint(x, upper));
            //bandFill.Points2.Add(new DataPoint(x, lower));
            upperLine.Points.Add(new DataPoint(x, upper));
            lowerLine.Points.Add(new DataPoint(x, lower));
            basisLine.Points.Add(new DataPoint(x, bands[i].Basis));

            // Break label = the SL distance the signal applies: SLStdevFactor * vwStdev / band%.
            // vwStdev is stored on BandValue so the chart and signal always agree.
            double vwStdev = bands[i].VwStdev;
            double refBand = low < lower ? lower : upper;
            double slPct = refBand > 0 ? vbs.SLStdevFactor * vwStdev / refBand * 100.0 : 0;

            // Same pass criteria as the signal: short needs rsi >= Overbought, long needs rsi <= Oversold.
            // With the RSI filter disabled every break is labeled, as before.
            double? rsi = rsiList?[i].Rsi;
            bool rsiOverbought = rsiList == null || (rsi.HasValue && rsi.Value >= rsiSettings.Overbought);
            bool rsiOversold = rsiList == null || (rsi.HasValue && rsi.Value <= rsiSettings.Oversold);

            if ((high > upper || close > upper) && rsiOverbought)
                AddLabel(chart, x, high, slPct, VerticalAlignment.Bottom, group);
            if ((low < lower || close < lower) && rsiOversold)
                AddLabel(chart, x, low, slPct, VerticalAlignment.Top, group);
        }

        //chart.Series.Add(bandFill);
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
