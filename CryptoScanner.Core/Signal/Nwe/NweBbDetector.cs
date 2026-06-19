//using System.Linq;

//using CryptoScanner.Core.Core;
//using CryptoScanner.Core.Enums;
//using CryptoScanner.Core.Model;
//using CryptoScanner.Core.Signal.Indicators;

//using Skender.Stock.Indicators;

//namespace CryptoScanner.Core.Signal.Nwe;

///// <summary>
///// Stand-alone recompute of the NWE × BB crossover signals from a plain candle list, mirroring the
///// rules in <see cref="SignalNweBbLong"/> / <see cref="SignalNweBbShort"/>.
/////
///// The live strategies decide walk-forward and persist a <see cref="CryptoSignal"/>; the chart can
///// normally only show those stored records. In the emulator (or any history where the NWE×BB strategy
///// wasn't active) no such records exist, so this lets the chart redraw the markers by running the same
///// algorithm over the visible candles.
/////
///// Inputs are reproduced exactly as the live pipeline computes them:
/////   - NWE: the repainting variant (same as <c>SignalNweBbBase.TryBuildHistory</c> and the chart NWE display).
/////   - BB:  Skender GetBollingerBands with SettingsBb.Length/Deviation; BbUpper/BbLower as Sma ± 0.5·(Upper−Lower)
/////          and width% as 100·(Upper/Lower−1), identical to IndicatorData's CandleData values.
/////
///// KEEP IN SYNC with the two strategy classes if their entry rules change.
///// </summary>
//public static class NweBbDetector
//{
//    /// <summary>A detected crossover marker: the candle's open time, the side, and the close price.</summary>
//    public readonly record struct Marker(CandleTime OpenTime, CryptoTradeSide Side, decimal Price);

//    private readonly record struct Bar(
//        CandleTime OpenTime, decimal Close, decimal Sma, double WidthPercentage,
//        decimal NweUpper, decimal NweLower, decimal BbUpper, decimal BbLower);

//    public static List<Marker> Detect(IReadOnlyList<CryptoCandle> candles)
//    {
//        var markers = new List<Marker>();
//        if (candles == null || candles.Count < 6)
//            return markers;

//        // Repainting NWE over the whole (windowed) candle list — matches TryBuildHistory + chart display.
//        var candleList = new CryptoCandleList();
//        foreach (var c in candles)
//            candleList.TryAdd(c.OpenTime, c);

//        var nwe = new NweIndicator(
//            bandwidth: GlobalData.Settings.Signal.Nwe.BandWidth,
//            multiplier: GlobalData.Settings.Signal.Nwe.Multiplication,
//            smoothRepainting: true);
//        var nweByTime = nwe.Calculate(candleList)
//            .Where(r => r.Upper.HasValue && r.Lower.HasValue)
//            .ToDictionary(r => r.OpenTime);

//        // BB with the same settings the live indicator cache uses; aligned 1:1 to candles by index.
//        var bbList = (List<BollingerBandsResult>)candles.GetBollingerBands(
//            lookbackPeriods: GlobalData.Settings.General.SettingsBb.Length,
//            standardDeviations: GlobalData.Settings.General.SettingsBb.Deviation);

//        // Contiguous list of valid bars (both NWE and BB present), oldest-first — mirrors the collected
//        // `bars` the strategy walks back over (current = last collected).
//        var bars = new List<Bar>(candles.Count);
//        for (int i = 0; i < candles.Count; i++)
//        {
//            var c = candles[i];
//            var bb = bbList[i];
//            if (bb.Sma == null || bb.UpperBand == null || bb.LowerBand == null)
//                continue;
//            if (!nweByTime.TryGetValue(c.OpenTime, out var n))
//                continue;

//            double sma = bb.Sma.Value;
//            double dev = 0.5 * (bb.UpperBand.Value - bb.LowerBand.Value);
//            double widthPct = bb.LowerBand.Value != 0 ? 100.0 * (bb.UpperBand.Value / bb.LowerBand.Value - 1.0) : 0.0;

//            bars.Add(new Bar(
//                c.OpenTime,
//                c.Close,
//                (decimal)sma,
//                widthPct,
//                n.Upper!.Value,
//                n.Lower!.Value,
//                (decimal)(sma + dev),
//                (decimal)(sma - dev)));
//        }

//        double bbMin = GlobalData.Settings.Signal.Stobb.BBMinPercentage;

//        for (int j = 2; j < bars.Count; j++)
//        {
//            var current = bars[j];
//            var prev = bars[j - 1];
//            var prev2 = bars[j - 2];

//            // Shared pre-filter: BB width within (BBMinPercentage, 100) — matches CheckBollingerBandsWidth.
//            if ((bbMin > 0 && current.WidthPercentage <= bbMin) || current.WidthPercentage >= 100.0)
//                continue;

//            int lookbackStart = Math.Max(0, j - 5); // the 5 bars before current

//            // ---- Long (mirror SignalNweBbLong) ----
//            if (current.Close < current.Sma
//                && prev.NweLower <= prev.BbLower
//                && current.NweLower > current.BbLower
//                && current.BbLower < prev.BbLower && prev.BbLower < prev2.BbLower)
//            {
//                for (int i = lookbackStart; i < j; i++)
//                {
//                    if (bars[i].Close < bars[i].BbLower)
//                    {
//                        markers.Add(new Marker(current.OpenTime, CryptoTradeSide.Long, current.Close));
//                        break;
//                    }
//                }
//            }

//            // ---- Short (mirror SignalNweBbShort) ----
//            if (current.Close > current.Sma
//                && prev.NweUpper >= prev.BbUpper
//                && current.NweUpper < current.BbUpper
//                && current.BbUpper > prev.BbUpper && prev.BbUpper > prev2.BbUpper)
//            {
//                for (int i = lookbackStart; i < j; i++)
//                {
//                    if (bars[i].Close > bars[i].BbUpper)
//                    {
//                        markers.Add(new Marker(current.OpenTime, CryptoTradeSide.Short, current.Close));
//                        break;
//                    }
//                }
//            }
//        }

//        return markers;
//    }
//}
