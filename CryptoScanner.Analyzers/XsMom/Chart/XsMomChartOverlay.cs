using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using OxyPlot;
using OxyPlot.Annotations;

namespace CryptoScanner.Analyzers.XsMom.Chart;

/// <summary>
/// A label on every candle that closes at a rebalance moment while the coin is in the selection:
/// "long 97/100" or "short 3/100" (its place in the ranking). The same ranking the strategy uses
/// (XsMomRanking, cached per rebalance moment).
/// </summary>
public class XsMomChartOverlay : IChartOverlay
{
    public string Label => "Momentum ranking";
    public string GroupKey => "xsmom";
#pragma warning disable CS0067 // Required by IChartOverlay; raised externally when needed
    public event Action? RequestRedraw;
#pragma warning restore CS0067

    public IReadOnlyList<ChartOverlayStyleDefinition> StyleDefinitions { get; } = [];


    private static IEnumerable<(int Index, XsMomPick Pick)> PicksOn(CryptoSymbol symbol, CryptoInterval interval, List<CryptoCandle> candles)
    {
        XsMomSettings settings = XsMomPlugin.Settings;
        for (int i = 0; i < candles.Count; i++)
        {
            CandleTime closeTime = candles[i].OpenTime + interval.Duration;
            if (!XsMomRanking.IsRebalanceMoment(closeTime, settings))
                continue;
            XsMomSelection? selection = XsMomRanking.GetSelection(symbol, closeTime, settings);
            if (selection != null && selection.TryGet(symbol.Name, out XsMomPick pick))
                yield return (i, pick);
        }
    }


    private static string TextOf(XsMomPick pick)
        => $"{(pick.Side == CryptoTradeSide.Long ? "long" : "short")} {pick.Rank + 1}/{pick.Count}";


    public void Draw(object plotModel, CryptoSymbol symbol, CryptoInterval interval,
                     List<CryptoCandle> candles, CandleTime minDate, CandleTime maxDate, string group)
    {
        var chart = (PlotModel)plotModel;
        foreach (var (i, pick) in PicksOn(symbol, interval, candles))
        {
            CandleTime openTime = candles[i].OpenTime;
            if (openTime < minDate || openTime > maxDate)
                continue;
            bool isLong = pick.Side == CryptoTradeSide.Long;
            double y = isLong ? (double)candles[i].Low : (double)candles[i].High;
            VerticalAlignment vAlign = isLong ? VerticalAlignment.Top : VerticalAlignment.Bottom;
            chart.Annotations.Add(new TextAnnotation
            {
                Text = TextOf(pick),
                TextPosition = new DataPoint(openTime.Minutes, y),
                Offset = new ScreenVector(0, isLong ? 20 : -20),
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

    public IReadOnlyList<ChartOverlaySeries> GetSeries(CryptoSymbol symbol, CryptoInterval interval, List<CryptoCandle> candles) => [];

    public IReadOnlyList<ChartOverlayLabel> GetLabels(CryptoSymbol symbol, CryptoInterval interval, List<CryptoCandle> candles)
    {
        // The same labels as Draw, for the web chart
        List<ChartOverlayLabel> labels = [];
        foreach (var (i, pick) in PicksOn(symbol, interval, candles))
        {
            bool isLong = pick.Side == CryptoTradeSide.Long;
            labels.Add(new ChartOverlayLabel
            {
                Time = candles[i].OpenTime.ToUnixSeconds(),
                Above = !isLong,
                Price = isLong ? (double)candles[i].Low : (double)candles[i].High,
                Text = TextOf(pick),
            });
        }
        return labels;
    }
}
