using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Indicators;

using OxyPlot;
using OxyPlot.Series;

using Skender.Stock.Indicators;

namespace CryptoScanner.ViewModels.Chart;

public class NweBb
{
    // Look back this many bars for the "prior extension" confirmation.
    // Matches the strategy's effective window in SignalNweBbBase (bars.Length - 6 .. ^1 = 5 bars).
    private const int ExtensionLookback = 5;

    internal static void Draw(PlotModel chart, CryptoSymbol symbol, CryptoInterval interval,
        CandleTime minDate, CandleTime maxDate, string group)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.CandleList.Count < 5)
            return;

        // Repainting NWE on the full candle list — same variant as the signal class
        var nweIndicator = new NweIndicator(
            bandwidth: GlobalData.Settings.Signal.Nwe.BandWidth,
            multiplier: GlobalData.Settings.Signal.Nwe.Multiplication,
            smoothRepainting: true);
        var nweResults = nweIndicator.Calculate(symbolInterval.CandleList);

        // BB via Skender on the full candle list
        var bbResults = (List<BollingerBandsResult>)symbolInterval.CandleList.Values.GetBollingerBands(
            lookbackPeriods: GlobalData.Settings.General.SettingsBb.Length,
            standardDeviations: GlobalData.Settings.General.SettingsBb.Deviation);

        // Build a BB lookup keyed by CandleTime
        var bbByTime = new Dictionary<CandleTime, BollingerBandsResult>(bbResults.Count);
        foreach (var bb in bbResults)
        {
            CandleTime ct = CandleTime.AlignFromDateTime(bb.Date, interval.Duration);
            bbByTime[ct] = bb;
        }

        // BB-width minimum — strategy sources this from Stobb settings, mirror that here.
        double bbWidthMin = GlobalData.Settings.Signal.Stobb.BBMinPercentage;

        var seriesLong = new ScatterSeries
        {
            Title = "nwe.bb ↑",
            MarkerSize = 7,
            MarkerFill = OxyColor.FromArgb(220, 0, 210, 100),
            MarkerType = MarkerType.Triangle,
            YAxisKey = "price",
            Tag = group,
            TrackerFormatString = "{0}\n{Tag}",
        };

        var seriesShort = new ScatterSeries
        {
            Title = "nwe.bb ↓",
            MarkerSize = 7,
            MarkerFill = OxyColor.FromArgb(220, 220, 60, 60),
            MarkerType = MarkerType.Diamond,
            YAxisKey = "price",
            Tag = group,
            TrackerFormatString = "{0}\n{Tag}",
        };

        // Iterate the NWE results (oldest first), detect crossings, emit visible markers
        for (int i = 2; i < nweResults.Count; i++)
        {
            var cur = nweResults[i];
            var prev = nweResults[i - 1];
            var prev2 = nweResults[i - 2];

            if (cur.Upper == null || cur.Lower == null
                || prev.Upper == null || prev.Lower == null
                || prev2.Upper == null || prev2.Lower == null)
                continue;

            if (!bbByTime.TryGetValue(cur.OpenTime, out var curBb)
                || !bbByTime.TryGetValue(prev.OpenTime, out var prevBb)
                || !bbByTime.TryGetValue(prev2.OpenTime, out var prev2Bb)
                || curBb.UpperBand == null || curBb.LowerBand == null
                || prevBb.UpperBand == null || prevBb.LowerBand == null
                || prev2Bb.UpperBand == null || prev2Bb.LowerBand == null
                || curBb.Sma == null)
                continue;

            if (!symbolInterval.CandleList.TryGetValue(cur.OpenTime, out var curCandle))
                continue;

            double curNweUp = (double)cur.Upper.Value;
            double curNweLow = (double)cur.Lower.Value;
            double prevNweUp = (double)prev.Upper.Value;
            double prevNweLow = (double)prev.Lower.Value;
            double curBbUp = curBb.UpperBand.Value;
            double curBbLow = curBb.LowerBand.Value;
            double prevBbUp = prevBb.UpperBand.Value;
            double prevBbLow = prevBb.LowerBand.Value;
            double prev2BbUp = prev2Bb.UpperBand.Value;
            double prev2BbLow = prev2Bb.LowerBand.Value;
            double curSma = curBb.Sma.Value;
            double curClose = (double)curCandle.Close;

            // Only emit markers for visible bars
            if (cur.OpenTime < minDate || cur.OpenTime > maxDate)
                continue;

            // BB-width filter — must match the strategy (Stobb.BBMinPercentage).
            double bbWidthPct = curSma != 0.0 ? ((curBbUp - curBbLow) / curSma) * 100.0 : 0.0;
            if (bbWidthPct < bbWidthMin)
                continue;

            // Short: NWE upper crosses BB upper from outside (above) to inside; BB upper rising 2 bars.
            // Strategy also requires close > sma20 (current candle still in upper half).
            if (prevNweUp >= prevBbUp
                && curNweUp < curBbUp
                && curBbUp > prevBbUp && prevBbUp > prev2BbUp
                && curClose > curSma
                && HadUpperExtension(nweResults, bbByTime, symbolInterval, i))
            {
                seriesShort.Points.Add(new ScatterPoint(
                    curCandle.OpenTime.Minutes,
                    (double)curCandle.High * 1.003,
                    double.NaN,
                    double.NaN,
                    tag: $"nwe.bb ↓ nwe={curNweUp:N6} bb={curBbUp:N6}"));
            }

            // Long: NWE lower crosses BB lower from outside (below) to inside; BB lower falling 2 bars.
            // Strategy also requires close < sma20 (current candle still in lower half).
            if (prevNweLow <= prevBbLow
                && curNweLow > curBbLow
                && curBbLow < prevBbLow && prevBbLow < prev2BbLow
                && curClose < curSma
                && HadLowerExtension(nweResults, bbByTime, symbolInterval, i))
            {
                seriesLong.Points.Add(new ScatterPoint(
                    curCandle.OpenTime.Minutes,
                    (double)curCandle.Low * 0.997,
                    double.NaN,
                    double.NaN,
                    tag: $"nwe.bb ↑ nwe={curNweLow:N6} bb={curBbLow:N6}"));
            }
        }

        chart.Series.Add(seriesLong);
        chart.Series.Add(seriesShort);
    }


    // Returns true when any of the preceding ExtensionLookback bars had close > BB upper
    private static bool HadUpperExtension(
        List<NweIndicator.NweResult> nweResults,
        Dictionary<CandleTime, BollingerBandsResult> bbByTime,
        CryptoSymbolInterval symbolInterval,
        int currentIdx)
    {
        int start = Math.Max(0, currentIdx - ExtensionLookback);
        for (int j = start; j < currentIdx; j++)
        {
            var r = nweResults[j];
            if (!bbByTime.TryGetValue(r.OpenTime, out var bb) || bb.UpperBand == null) continue;
            if (!symbolInterval.CandleList.TryGetValue(r.OpenTime, out var c)) continue;

            if ((double)c.Close > bb.UpperBand.Value)
                return true;
        }
        return false;
    }


    // Returns true when any of the preceding ExtensionLookback bars had close < BB lower
    private static bool HadLowerExtension(
        List<NweIndicator.NweResult> nweResults,
        Dictionary<CandleTime, BollingerBandsResult> bbByTime,
        CryptoSymbolInterval symbolInterval,
        int currentIdx)
    {
        int start = Math.Max(0, currentIdx - ExtensionLookback);
        for (int j = start; j < currentIdx; j++)
        {
            var r = nweResults[j];
            if (!bbByTime.TryGetValue(r.OpenTime, out var bb) || bb.LowerBand == null) continue;
            if (!symbolInterval.CandleList.TryGetValue(r.OpenTime, out var c)) continue;

            if ((double)c.Close < bb.LowerBand.Value)
                return true;
        }
        return false;
    }
}
