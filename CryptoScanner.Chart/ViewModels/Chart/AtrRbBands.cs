using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.ViewModels.Chart;

/// <summary>
/// "AtrRb Bands & Ribbon" indicator (clone of the TradingView Pine script).
/// It is a Keltner-style construction: an EMA basis with two ATR based band sets.
///   - Macro outer bands : basis +/- ATR * outerMult  (the wide green cloud)
///   - Inner ribbon       : basis +/- ATR * innerMult  (trend coloured: green up / red down)
///   - Basis              : EMA(len)
///   - Overextension labels: percentage deviation when high/low breaks the macro bands,
///     filtered to the highest/lowest point within a 5 candle window to avoid label spam.
/// </summary>
public class AtrRbBands
{
    // Parameters come from GlobalData.Settings.Signal.AtrRb (read per draw, below) so the chart and
    // the "atrrb" signal stay in sync with the user's configured values.

    // Colours, translated from the Pine color.new(..., transparency) values.
    // Pine transparency is "percent transparent", so alpha = 255 * (100 - transparency) / 100.
    private static readonly OxyColor MacroLineColor = OxyColors.Gray; // gray (outer macro band lines)
    private static readonly OxyColor MacroFillColor = OxyColor.FromArgb(15, 0, 128, 0);     // green, 94% transparent
    private static readonly OxyColor BasisColor = OxyColor.FromArgb(153, 0, 0, 255);        // blue, 40% transparent
    private static readonly OxyColor RibbonUpColor = OxyColor.FromArgb(178, 0, 255, 170);   // #00ffaa, 30% transparent
    private static readonly OxyColor RibbonDownColor = OxyColor.FromArgb(178, 255, 59, 59); // #ff3b3b, 30% transparent
    private static readonly OxyColor RibbonUpFill = OxyColor.FromArgb(38, 0, 255, 170);     // ribbon shading, 85% transparent
    private static readonly OxyColor RibbonDownFill = OxyColor.FromArgb(38, 255, 59, 59);

    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval, CandleTime minDate, CandleTime maxDate, string group)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return;

        var candles = symbolInterval.CandleList.Values.ToList();

        // Read the configured band parameters (same source as the atrrb signal → chart stays in sync).
        var atrrb = GlobalData.Settings.Signal.AtrRb;
        int Len = atrrb.Length;
        double OuterMult = atrrb.OuterMult;
        double InnerMult = atrrb.InnerMult;
        int BreakLookback = atrrb.BreakLookback;

        // EMA basis, ATR and Bollinger bands are computed by Skender; the result lists are aligned
        // 1:1 with the candle list, so we index them positionally instead of joining on date.
        IReadOnlyList<IQuote> quotes = candles.AsQuotes();
        IReadOnlyList<EmaResult> emaResults = quotes.ToEma(Len);
        IReadOnlyList<AtrResult> atrResults = quotes.ToAtr(Len);

        // Bollinger-band width per candle (BollingerBandsPercentage = 100 * (upper/lower - 1)),
        // computed with the same BB settings as the signal pipeline. Used to gate the break labels
        // exactly like the atrrb signal's CheckBollingerBandsWidth, so chart labels match the alert.
        IReadOnlyList<BollingerBandsResult> bbResults = quotes.ToBollingerBands(
            lookbackPeriods: GlobalData.Settings.General.SettingsBb.Length,
            standardDeviations: GlobalData.Settings.General.SettingsBb.Deviation);

        // Macro outer cloud (fill behind everything) and its two bounding lines.
        var macroFill = new AreaSeries
        {
            Title = "atrrb.macro.fill",
            Fill = MacroFillColor,
            Color = OxyColors.Transparent,
            StrokeThickness = 0,
            YAxisKey = "price",
            Tag = group,
        };
        var macroUp = new LineSeries { Title = "atrrb.macro.up", Color = MacroLineColor, StrokeThickness = 1, YAxisKey = "price", Tag = group };
        var macroDown = new LineSeries { Title = "atrrb.macro.down", Color = MacroLineColor, StrokeThickness = 1, YAxisKey = "price", Tag = group };

        // Inner ribbon shading (trend coloured body between inner_up and inner_down).
        var ribbonFillUp = new AreaSeries { Title = "atrrb.ribbon.fill.up", Fill = RibbonUpFill, Color = OxyColors.Transparent, StrokeThickness = 0, YAxisKey = "price", Tag = group };
        var ribbonFillDown = new AreaSeries { Title = "atrrb.ribbon.fill.down", Fill = RibbonDownFill, Color = OxyColors.Transparent, StrokeThickness = 0, YAxisKey = "price", Tag = group };

        // Inner ribbon lines, split per trend so each segment keeps its own colour.
        var ribbonUpGreen = new LineSeries { Title = "atrrb.ribbon.up", Color = RibbonUpColor, StrokeThickness = 1, YAxisKey = "price", Tag = group };
        var ribbonUpRed = new LineSeries { Title = "atrrb.ribbon.up", Color = RibbonDownColor, StrokeThickness = 1, YAxisKey = "price", Tag = group };
        var ribbonDownGreen = new LineSeries { Title = "atrrb.ribbon.down", Color = RibbonUpColor, StrokeThickness = 1, YAxisKey = "price", Tag = group };
        var ribbonDownRed = new LineSeries { Title = "atrrb.ribbon.down", Color = RibbonDownColor, StrokeThickness = 1, YAxisKey = "price", Tag = group };

        // Basis (middle) line.
        var basisLine = new LineSeries { Title = "atrrb.basis", Color = BasisColor, StrokeThickness = 2, YAxisKey = "price", Tag = group };

        // Break point used to interrupt a line/area series so trend segments do not connect.
        var breakPoint = new DataPoint(double.NaN, double.NaN);

        for (int i = 0; i < candles.Count; i++)
        {
            var candle = candles[i];
            CandleTime openTime = CandleTime.AlignFromDateTime(candle.Date, interval.Duration);
            if (openTime < minDate || openTime > maxDate)
                continue;

            double? basisN = emaResults[i].Ema;
            double? atrN = atrResults[i].Atr;
            if (!basisN.HasValue || !atrN.HasValue)
                continue;
            double basis = basisN.Value;
            double atr = atrN.Value;

            double x = openTime.Minutes;
            double close = (double)candle.Close;
            double high = (double)candle.High;
            double low = (double)candle.Low;

            double outerUp = basis + atr * OuterMult;
            double outerDown = basis - atr * OuterMult;
            double innerUp = basis + atr * InnerMult;
            double innerDown = basis - atr * InnerMult;
            bool isUptrend = close > basis;

            macroFill.Points.Add(new DataPoint(x, outerUp));
            macroFill.Points2.Add(new DataPoint(x, outerDown));
            macroUp.Points.Add(new DataPoint(x, outerUp));
            macroDown.Points.Add(new DataPoint(x, outerDown));
            basisLine.Points.Add(new DataPoint(x, basis));

            // Trend coloured ribbon: add the real value on the matching trend, a break on the other.
            if (isUptrend)
            {
                ribbonUpGreen.Points.Add(new DataPoint(x, innerUp));
                ribbonDownGreen.Points.Add(new DataPoint(x, innerDown));
                ribbonUpRed.Points.Add(breakPoint);
                ribbonDownRed.Points.Add(breakPoint);

                ribbonFillUp.Points.Add(new DataPoint(x, innerUp));
                ribbonFillUp.Points2.Add(new DataPoint(x, innerDown));
                ribbonFillDown.Points.Add(breakPoint);
                ribbonFillDown.Points2.Add(breakPoint);
            }
            else
            {
                ribbonUpRed.Points.Add(new DataPoint(x, innerUp));
                ribbonDownRed.Points.Add(new DataPoint(x, innerDown));
                ribbonUpGreen.Points.Add(breakPoint);
                ribbonDownGreen.Points.Add(breakPoint);

                ribbonFillDown.Points.Add(new DataPoint(x, innerUp));
                ribbonFillDown.Points2.Add(new DataPoint(x, innerDown));
                ribbonFillUp.Points.Add(breakPoint);
                ribbonFillUp.Points2.Add(breakPoint);
            }

            // Overextension labels: price breaks the macro band and is the extreme of a 5 candle window.
            // Label value = the stop-loss distance the atrrb signal applies: StopLossAtrFactor * ATR%,
            // so the chart prints exactly the percentage used as the SL (chart and alert stay in sync).
            // Same number for an up- or down-break; it does NOT depend on how far the wick extended.
            double slPct = atrrb.StopLossAtrFactor * (atr / close * 100);

            // BB-width gate: only flag a break when the BB width is within range, exactly like the
            // atrrb signal. A break on a candle whose BB width is out of range gets no label.
            var bb = bbResults[i];
            bool bbWidthOk = bb.UpperBand.HasValue && bb.LowerBand.HasValue && bb.LowerBand.Value != 0
                && BbWidthOk(100 * (bb.UpperBand.Value / bb.LowerBand.Value - 1), atrrb.BBMinPercentage, atrrb.BBMaxPercentage);

            if (bbWidthOk && high > outerUp && IsHighestHigh(candles, i, BreakLookback))
            {
                // vAlign Bottom = the label's bottom sits on the High, so the text extends UPWARD,
                // above the candle instead of over it.
                AddLabel(chart, x, high, slPct, VerticalAlignment.Bottom, group);
            }
            if (bbWidthOk && low < outerDown && IsLowestLow(candles, i, BreakLookback))
            {
                // vAlign Top = the label's top sits on the Low, so the text extends DOWNWARD, below
                // the candle instead of over it.
                AddLabel(chart, x, low, slPct, VerticalAlignment.Top, group);
            }
        }

        // Add background fills first, then lines, then the basis on top.
        chart.Series.Add(macroFill);
        chart.Series.Add(ribbonFillUp);
        chart.Series.Add(ribbonFillDown);
        chart.Series.Add(macroUp);
        chart.Series.Add(macroDown);
        chart.Series.Add(ribbonUpGreen);
        chart.Series.Add(ribbonUpRed);
        chart.Series.Add(ribbonDownGreen);
        chart.Series.Add(ribbonDownRed);
        chart.Series.Add(basisLine);
    }

    private static void AddLabel(PlotModel chart, double x, double y, double pct, VerticalAlignment vAlign, string group)
    {
        // Extra gap so the label clears the wick. Screen coordinates: Y increases downward, so a
        // label above the High (vAlign Bottom) is nudged UP (negative), one below the Low (vAlign Top)
        // DOWN (positive). Bump the pixel value to push the labels further away from the price action.
        const double gapPixels = 20;
        double offsetY = vAlign == VerticalAlignment.Bottom ? -gapPixels : gapPixels;

        chart.Annotations.Add(new TextAnnotation
        {
            Text = pct.ToString("0.##") + "%",
            TextPosition = new DataPoint(x, y),
            Offset = new ScreenVector(0, offsetY),
            TextHorizontalAlignment = HorizontalAlignment.Center,
            TextVerticalAlignment = vAlign,
            TextColor = OxyColors.Black,
            Background = OxyColors.White,
            FontSize = 9,
            YAxisKey = "price",
            Tag = group,
        });
    }

    // Mirrors BollingerBandsHelper.CheckBollingerBandsWidth: a bound of 0 disables that side, so the
    // width must be > min (when min > 0) and < max (when max > 0).
    private static bool BbWidthOk(double bbPct, double min, double max)
    {
        if (min > 0 && bbPct <= min)
            return false;
        if (max > 0 && bbPct >= max)
            return false;
        return true;
    }

    // True when candle[index] has the highest High within the trailing BreakLookback window (matches ta.highest).
    private static bool IsHighestHigh(List<CryptoCandle> candles, int index, int lookback)
    {
        decimal value = candles[index].High;
        for (int j = Math.Max(0, index - lookback + 1); j <= index; j++)
        {
            if (candles[j].High > value)
                return false;
        }
        return true;
    }

    // True when candle[index] has the lowest Low within the trailing BreakLookback window (matches ta.lowest).
    private static bool IsLowestLow(List<CryptoCandle> candles, int index, int lookback)
    {
        decimal value = candles[index].Low;
        for (int j = Math.Max(0, index - lookback + 1); j <= index; j++)
        {
            if (candles[j].Low < value)
                return false;
        }
        return true;
    }
}
