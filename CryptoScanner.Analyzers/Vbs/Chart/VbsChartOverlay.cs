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

        var settings = VbsPlugin.Settings;

        // Volume-weighted VWAP bands (basis/upper/lower), computed by the shared helper so the chart and
        // the signal stay identical. Index-aligned with the candle list below.
        var bands = VbsBandsHelper.ComputeBands(candles);

        // RSI confluence for the break labels (same gate as SignalVbsLong/Short): when the RSI filter
        // is enabled, only label a lower-band break when RSI is oversold and an upper-band break when
        // RSI is overbought. Thresholds come from the general RSI settings (Indicators tab).
        var rsiSettings = GlobalData.Settings.General.SettingsRsi;
        IReadOnlyList<RsiResult>? rsiList = null;
        if (settings.UseRsiFilter)
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

            // Break label = the SL distance the signal applies: the ACS% (average candle size).
            // Acs is stored on BandValue so the chart and signal always agree.
            double slPct = bands[i].Acs;

            // Take-profit distance the signal would hand to the trader: RiskRewardRatio * SL-distance.
            // Only shown when the take-profit is enabled, so the label matches what actually gets placed.
            double? tpPct = settings.RiskRewardRatio * slPct;

            // Same pass criteria as the signal: short needs rsi >= Overbought, long needs rsi <= Oversold.
            // With the RSI filter disabled every break is labeled, as before.
            double? rsi = rsiList?[i].Rsi;
            bool rsiOverbought = rsiList == null || (rsi.HasValue && rsi.Value >= rsiSettings.Overbought);
            bool rsiOversold = rsiList == null || (rsi.HasValue && rsi.Value <= rsiSettings.Oversold);

            if ((high > upper || close > upper) && rsiOverbought)
                AddLabel(chart, x, high, slPct, tpPct, VerticalAlignment.Bottom, group);
            if ((low < lower || close < lower) && rsiOversold)
                AddLabel(chart, x, low, slPct, tpPct, VerticalAlignment.Top, group);
        }

        //chart.Series.Add(bandFill);
        chart.Series.Add(upperLine);
        chart.Series.Add(lowerLine);
        chart.Series.Add(basisLine);
    }

    public IReadOnlyList<ChartOverlaySeries> GetSeries(CryptoSymbol symbol, CryptoInterval interval, List<CryptoCandle> candles)
    {
        if (candles.Count == 0)
            return [];

        var bands = VbsBandsHelper.ComputeBands(candles);

        var upper = new ChartOverlaySeries { Key = "vbsUpper", Label = "VBS upper", Color = "#009688", LineWidth = 2 };
        var lower = new ChartOverlaySeries { Key = "vbsLower", Label = "VBS lower", Color = "#009688", LineWidth = 2 };
        var basis = new ChartOverlaySeries { Key = "vbsBasis", Label = "VBS basis", Color = "#9e9e9e", LineStyle = 2 };

        for (int i = 0; i < candles.Count; i++)
        {
            if (!bands[i].HasValue)
                continue;

            long time = CandleTime.AlignFromDateTime(candles[i].Date, interval.Duration).ToUnixSeconds();
            upper.Points.Add(new ChartOverlayPoint { Time = time, Value = bands[i].Upper });
            lower.Points.Add(new ChartOverlayPoint { Time = time, Value = bands[i].Lower });
            basis.Points.Add(new ChartOverlayPoint { Time = time, Value = bands[i].Basis });
        }

        return [upper, lower, basis];
    }

    public IReadOnlyList<ChartOverlayLabel> GetLabels(CryptoSymbol symbol, CryptoInterval interval, List<CryptoCandle> candles)
    {
        if (candles.Count == 0)
            return [];

        // Same conditions as Draw, so the web chart shows the labels at the very same candles
        var settings = VbsPlugin.Settings;
        var bands = VbsBandsHelper.ComputeBands(candles);

        var rsiSettings = GlobalData.Settings.General.SettingsRsi;
        IReadOnlyList<RsiResult>? rsiList = null;
        if (settings.UseRsiFilter)
            rsiList = candles.AsQuotes().ToRsi(rsiSettings.Length);

        var labels = new List<ChartOverlayLabel>();

        for (int i = 0; i < candles.Count; i++)
        {
            if (!bands[i].HasValue)
                continue;

            var candle = candles[i];
            double close = (double)candle.Close;
            double high = (double)candle.High;
            double low = (double)candle.Low;
            double upper = bands[i].Upper;
            double lower = bands[i].Lower;

            double slPct = bands[i].Acs;
            double tpPct = settings.RiskRewardRatio * slPct;

            double? rsi = rsiList?[i].Rsi;
            bool rsiOverbought = rsiList == null || (rsi.HasValue && rsi.Value >= rsiSettings.Overbought);
            bool rsiOversold = rsiList == null || (rsi.HasValue && rsi.Value <= rsiSettings.Oversold);

            bool upperBreak = (high > upper || close > upper) && rsiOverbought;
            bool lowerBreak = (low < lower || close < lower) && rsiOversold;
            if (!upperBreak && !lowerBreak)
                continue;

            // Two lines with the take-profit under the stop-loss. A marker holds one line of text,
            // so they are emitted separately and the renderer stacks them outward from the candle:
            // above the bar the first one ends up lowest, below the bar the first one ends up
            // highest. Emitting them in the right order per side keeps TP under SL either way.
            long time = CandleTime.AlignFromDateTime(candle.Date, interval.Duration).ToUnixSeconds();
            double anchor = upperBreak ? high : low;
            var stopLoss = new ChartOverlayLabel
            {
                Time = time,
                Above = upperBreak,
                Price = anchor,
                Text = "SL " + slPct.ToString("0.##") + "%",
            };
            var takeProfit = new ChartOverlayLabel
            {
                Time = time,
                Above = upperBreak,
                Price = anchor,
                Text = "TP " + tpPct.ToString("0.##") + "%",
            };

            if (upperBreak)
            {
                labels.Add(takeProfit);
                labels.Add(stopLoss);
            }
            else
            {
                labels.Add(stopLoss);
                labels.Add(takeProfit);
            }
        }

        return labels;
    }

    private static void AddLabel(PlotModel chart, double x, double y, double slPct, double? tpPct, VerticalAlignment vAlign, string group)
    {
        // Extra gap so the label clears the wick (a bit more than before).
        const double gapPixels = 30;
        double offsetY = vAlign == VerticalAlignment.Bottom ? -gapPixels : gapPixels;

        // Stop-loss on the first line; the take-profit (when enabled) on a second line. Words spelled out.
        string text = "Stop-loss " + slPct.ToString("0.##") + "%";
        if (tpPct.HasValue)
            text += "\nTake-profit " + tpPct.Value.ToString("0.##") + "%";

        chart.Annotations.Add(new TextAnnotation
        {
            Text = text,
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
