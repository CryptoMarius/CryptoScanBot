using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Series;

using Skender.Stock.Indicators;
using CryptoScanner.Analyzers.Chart;

namespace CryptoScanner.Analyzers.AtrRb.Chart;

/// <summary>
/// "AtrRb Bands &amp; Ribbon" indicator (clone of the TradingView Pine script).
/// It is a Keltner-style construction: an EMA basis with two ATR based band sets.
/// </summary>
public class AtrRbChartOverlay : IChartOverlay
{
    public string Label => "ATR Reversal Bands";
    public string GroupKey => "atrrb";
#pragma warning disable CS0067 // Required by IChartOverlay; raised externally when needed
    public event Action? RequestRedraw;
#pragma warning restore CS0067

    public const string KeyUpper = "atrRbUpper";
    public const string KeyLower = "atrRbLower";
    public const string KeyBasis = "atrRbBasis";

    /// <summary>
    /// What this overlay draws and how it looks by default. The colour screen builds its section from
    /// this, so the entries exist exactly as long as the plugin is registered: a strategy that is not
    /// in the build takes its colours with it instead of leaving them behind on screen, and a new one
    /// arrives with its own instead of in the fallback grey. GetSeries reads the same list, so the
    /// chart and the screen cannot drift apart.
    /// </summary>
    public IReadOnlyList<ChartOverlayStyleDefinition> StyleDefinitions { get; } =
    [
        new() { Key = KeyUpper, Label = "Upper band", Color = "#90a4ae" },
        new() { Key = KeyLower, Label = "Lower band", Color = "#90a4ae" },
        new() { Key = KeyBasis, Label = "Basis", Color = "#42a5f5", LineWidth = 2, LineStyle = 2 },
    ];

    private static readonly OxyColor MacroLineColor = OxyColors.Gray;
    private static readonly OxyColor MacroFillColor = OxyColor.FromArgb(15, 0, 128, 0);
    private static readonly OxyColor BasisColor = OxyColor.FromArgb(153, 0, 0, 255);
    private static readonly OxyColor RibbonUpColor = OxyColor.FromArgb(178, 0, 255, 170);
    private static readonly OxyColor RibbonDownColor = OxyColor.FromArgb(178, 255, 59, 59);
    //private static readonly OxyColor RibbonUpFill = OxyColor.FromArgb(38, 0, 255, 170);
    //private static readonly OxyColor RibbonDownFill = OxyColor.FromArgb(38, 255, 59, 59);

    public IReadOnlyList<ChartOverlaySeries> GetSeries(CryptoSymbol symbol, CryptoInterval interval, List<CryptoCandle> candles)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return [];

        var allCandles = symbolInterval.CandleList.Values.ToList();

        var atrrb = AtrRbPlugin.Settings;
        IReadOnlyList<IQuote> quotes = allCandles.AsQuotes();
        IReadOnlyList<EmaResult> emaResults = quotes.ToEma(atrrb.Length);
        IReadOnlyList<AtrResult> atrResults = quotes.ToAtr(atrrb.Length);

        var upper = ((IChartOverlay)this).SeriesFor(KeyUpper);
        var lower = ((IChartOverlay)this).SeriesFor(KeyLower);
        var basis = ((IChartOverlay)this).SeriesFor(KeyBasis);

        for (int i = 0; i < allCandles.Count; i++)
        {
            double? basisValue = emaResults[i].Ema;
            double? atrValue = atrResults[i].Atr;
            if (!basisValue.HasValue || !atrValue.HasValue)
                continue;

            long time = CandleTime.AlignFromDateTime(allCandles[i].Date, interval.Duration).ToUnixSeconds();
            upper.Points.Add(new ChartOverlayPoint { Time = time, Value = basisValue.Value + atrValue.Value * atrrb.OuterMult });
            lower.Points.Add(new ChartOverlayPoint { Time = time, Value = basisValue.Value - atrValue.Value * atrrb.OuterMult });
            basis.Points.Add(new ChartOverlayPoint { Time = time, Value = basisValue.Value });
        }

        return [upper, lower, basis];
    }

    public void Draw(object plotModel, CryptoSymbol symbol, CryptoInterval interval,
                     List<CryptoCandle> candles, CandleTime minDate, CandleTime maxDate, string group)
    {
        var chart = (PlotModel)plotModel;
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return;

        var allCandles = symbolInterval.CandleList.Values.ToList();

        var atrrb = AtrRbPlugin.Settings;
        int Len = atrrb.Length;
        double OuterMult = atrrb.OuterMult;
        int BreakLookback = atrrb.BreakLookback;

        IReadOnlyList<IQuote> quotes = allCandles.AsQuotes();
        IReadOnlyList<EmaResult> emaResults = quotes.ToEma(Len);
        IReadOnlyList<AtrResult> atrResults = quotes.ToAtr(Len);

        IReadOnlyList<BollingerBandsResult> bbResults = quotes.ToBollingerBands(
            lookbackPeriods: GlobalData.Settings.General.SettingsBb.Length,
            standardDeviations: GlobalData.Settings.General.SettingsBb.Deviation);

        // The user's colours, with what this overlay declared as the fallback - so a scanner where
        // nobody opened the colour screen draws exactly what it drew before. See ChartStyleOxy.
        var macroUp = new LineSeries
        {
            Title = "atrrb.macro.up",
            Color = ChartStyleOxy.ColorFor(KeyUpper, MacroLineColor),
            StrokeThickness = ChartStyleOxy.ThicknessFor(KeyUpper, 1),
            LineStyle = ChartStyleOxy.LineStyleFor(KeyUpper, LineStyle.Solid),
            YAxisKey = "price",
            Tag = group,
        };
        var macroDown = new LineSeries
        {
            Title = "atrrb.macro.down",
            Color = ChartStyleOxy.ColorFor(KeyLower, MacroLineColor),
            StrokeThickness = ChartStyleOxy.ThicknessFor(KeyLower, 1),
            LineStyle = ChartStyleOxy.LineStyleFor(KeyLower, LineStyle.Solid),
            YAxisKey = "price",
            Tag = group,
        };
        var basisLine = new LineSeries
        {
            Title = "atrrb.basis",
            Color = ChartStyleOxy.ColorFor(KeyBasis, BasisColor),
            StrokeThickness = ChartStyleOxy.ThicknessFor(KeyBasis, 2),
            LineStyle = ChartStyleOxy.LineStyleFor(KeyBasis, LineStyle.Solid),
            YAxisKey = "price",
            Tag = group,
        };

        for (int i = 0; i < allCandles.Count; i++)
        {
            var candle = allCandles[i];
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

            macroUp.Points.Add(new DataPoint(x, outerUp));
            macroDown.Points.Add(new DataPoint(x, outerDown));
            basisLine.Points.Add(new DataPoint(x, basis));

            double slPct = atrrb.StopLossAtrFactor * (atr / close * 100);

            var bb = bbResults[i];
            bool bbWidthOk = bb.UpperBand.HasValue && bb.LowerBand.HasValue && bb.LowerBand.Value != 0
                && BbWidthOk(100 * (bb.UpperBand.Value / bb.LowerBand.Value - 1), atrrb.BBMinPercentage, atrrb.BBMaxPercentage);

            if (bbWidthOk && high > outerUp && IsHighestHigh(allCandles, i, BreakLookback))
                AddLabel(chart, x, high, slPct, VerticalAlignment.Bottom, group);
            if (bbWidthOk && low < outerDown && IsLowestLow(allCandles, i, BreakLookback))
                AddLabel(chart, x, low, slPct, VerticalAlignment.Top, group);
        }

        chart.Series.Add(macroUp);
        chart.Series.Add(macroDown);
        chart.Series.Add(basisLine);
    }

    public IReadOnlyList<ChartOverlayLabel> GetLabels(CryptoSymbol symbol, CryptoInterval interval, List<CryptoCandle> candles)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count == 0)
            return [];

        // Same break conditions as Draw, so the labels land on the very same candles
        var allCandles = symbolInterval.CandleList.Values.ToList();
        var atrrb = AtrRbPlugin.Settings;

        IReadOnlyList<IQuote> quotes = allCandles.AsQuotes();
        IReadOnlyList<EmaResult> emaResults = quotes.ToEma(atrrb.Length);
        IReadOnlyList<AtrResult> atrResults = quotes.ToAtr(atrrb.Length);
        IReadOnlyList<BollingerBandsResult> bbResults = quotes.ToBollingerBands(
            lookbackPeriods: GlobalData.Settings.General.SettingsBb.Length,
            standardDeviations: GlobalData.Settings.General.SettingsBb.Deviation);

        var labels = new List<ChartOverlayLabel>();

        for (int i = 0; i < allCandles.Count; i++)
        {
            double? basisN = emaResults[i].Ema;
            double? atrN = atrResults[i].Atr;
            if (!basisN.HasValue || !atrN.HasValue)
                continue;

            var candle = allCandles[i];
            double close = (double)candle.Close;
            double high = (double)candle.High;
            double low = (double)candle.Low;

            double outerUp = basisN.Value + atrN.Value * atrrb.OuterMult;
            double outerDown = basisN.Value - atrN.Value * atrrb.OuterMult;
            double slPct = atrrb.StopLossAtrFactor * (atrN.Value / close * 100);

            var bb = bbResults[i];
            bool bbWidthOk = bb.UpperBand.HasValue && bb.LowerBand.HasValue && bb.LowerBand.Value != 0
                && BbWidthOk(100 * (bb.UpperBand.Value / bb.LowerBand.Value - 1), atrrb.BBMinPercentage, atrrb.BBMaxPercentage);
            if (!bbWidthOk)
                continue;

            bool upperBreak = high > outerUp && IsHighestHigh(allCandles, i, atrrb.BreakLookback);
            bool lowerBreak = low < outerDown && IsLowestLow(allCandles, i, atrrb.BreakLookback);
            if (!upperBreak && !lowerBreak)
                continue;

            labels.Add(new ChartOverlayLabel
            {
                Time = CandleTime.AlignFromDateTime(candle.Date, interval.Duration).ToUnixSeconds(),
                Above = upperBreak,
                Price = upperBreak ? high : low,
                Text = "SL " + slPct.ToString("0.##") + "%",
            });
        }

        return labels;
    }

    private static void AddLabel(PlotModel chart, double x, double y, double pct, VerticalAlignment vAlign, string group)
    {
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
            Background = OxyColors.Undefined,
            Stroke = OxyColors.Transparent,
            StrokeThickness = 0,
            FontSize = 9,
            YAxisKey = "price",
            Tag = group,
        });
    }

    private static bool BbWidthOk(double bbPct, double min, double max)
    {
        if (min > 0 && bbPct <= min)
            return false;
        if (max > 0 && bbPct >= max)
            return false;
        return true;
    }

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
