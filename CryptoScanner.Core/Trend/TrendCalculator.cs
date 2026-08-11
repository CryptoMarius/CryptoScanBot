using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

using System.Diagnostics;
using System.Text;

namespace CryptoScanner.Core.Trend;


/// <summary>
/// Builds the ZigZag indicator ONCE per (symbol, interval, trend-type) and feeds it to BOTH
/// trend interpretations (Dow theory + BOS/CHoCH). The expensive part is the candle ingestion
/// inside <see cref="TrendTools.AddCandlesToIndicatorsAsync"/>; doing it once instead of twice
/// halves the trend-calculation cost.
///
/// The two interpretations stay specialised in their own files:
///   <see cref="TrendInterval.InterpretZigZagPoints"/> (Dow theory, count > 1 damping)
///   <see cref="TrendIntervalBos.InterpretZigZagPoints"/> (single-break BOS/CHoCH)
/// Their resulting trends typically differ — Dow flips slower, BOS flips on a single break —
/// so each gets its own <see cref="CryptoTrendData"/> slot.
/// </summary>
public class TrendCalculator
{
    /// <summary>
    /// Resolve the [minDate, maxDate] window for this interval's candle list.
    /// Identical to the helpers that used to live in TrendInterval and TrendIntervalBos.
    /// </summary>
    private static bool ResolveStartAndEndDate(CryptoSymbol symbol, CryptoInterval interval,
        CryptoCandleList candleList, ref CandleTime minDate, ref CandleTime maxDate)
    {
        // start time
        if (minDate == 0)
        {
            if (!candleList.TryGetFirstCandle(out var candle))
                return false;
            if (maxDate > 0)
            {
                minDate = maxDate - 5000 * interval.Duration;
                if (minDate < candle.OpenTime)
                    minDate = candle.OpenTime;
            }
            else
            {
                minDate = candle.OpenTime;
            }
        }
        else
            minDate = IntervalTools.StartOfIntervalCandle(minDate, interval.Duration);

        // end time
        if (maxDate == 0)
        {
            CryptoCandle lastCandle = symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList.LastCandle;
            if (lastCandle.OpenTime == 0)
                return false;
            maxDate = lastCandle.OpenTime;
        }
        else
            maxDate = IntervalTools.StartOfIntervalCandle(maxDate, interval.Duration);

        if (!candleList.ContainsKey(maxDate))
            maxDate -= interval.Duration;

        return true;
    }


    /// <summary>
    /// Write LastPivot / PrevPivot bookkeeping into a trend-data slot. Identical for Dow
    /// and BOS — both want to see the most recent confirmed swing point.
    /// </summary>
    private static void WritePivotData(ZigZagIndicator indicator, CryptoTrendData target)
    {
        if (indicator.ZigZagList.Count > 0)
        {
            var lastPivot = indicator.ZigZagList[^1];
            target.LastPivotType = lastPivot.PointType;
            target.LastPivotValue = lastPivot.Value;
            target.LastPivotTime = lastPivot.Candle.OpenTime;

            if (indicator.ZigZagList.Count > 1)
            {
                var prevPivot = indicator.ZigZagList[^2];
                target.PrevPivotType = prevPivot.PointType;
                target.PrevPivotValue = prevPivot.Value;
                target.PrevPivotTime = prevPivot.Candle.OpenTime;
            }
            else
            {
                target.PrevPivotType = null;
                target.PrevPivotValue = null;
                target.PrevPivotTime = null;
            }
        }
    }


    /// <summary>
    /// Calculate both the Dow-theory trend (<paramref name="dowTrendData"/>) and the BOS/CHoCH
    /// trend (<paramref name="bosTrendData"/>) for this symbol/interval, sharing one ZigZag pass.
    /// </summary>
    public static async Task CalculateBothAsync(CryptoSymbol symbol, CryptoInterval interval,
        CryptoCandleList candleList, CryptoTrendData dowTrendData, CryptoTrendData bosTrendData,
        SettingsZigZag trendSettings, StringBuilder? log = null)
    {
        //return;

        // Profiling: time the whole call (any exit path) — see PipelineProfiler. This is the
        // per-stale-interval unit of work inside MarketTrend.CalculateMarketTrendAsync's foreach loop.
        long profCalcBothStart = Stopwatch.GetTimestamp();
        try
        {
            log?.AppendLine("");
            log?.AppendLine("----");
            log?.AppendLine($"{symbol.Name} Interval {interval.Name} [Dow + BOS]");
            log?.AppendLine("");

            // No candles → reset both slots and bail. Mirrors the old behaviour of the two
            // separate CalculateAsync methods.
            if (candleList.Count == 0)
            {
                dowTrendData.Reset();
                bosTrendData.Reset();
                return;
            }

            CandleTime minDate = CandleTime.MinValue;
            CandleTime maxDate = CandleTime.MinValue;
            if (!ResolveStartAndEndDate(symbol, interval, candleList, ref minDate, ref maxDate))
            {
                log?.AppendLine($"{symbol.Name} {interval.Name} (date period problem)");
                return;
            }

            // Emulator only: bound the ZigZag window to the same span the live scanner retains
            // (GetCandleFetchStart). The live scanner trims its CandleList to that window via
            // CleanCandleDataAsync, so it never builds the trend over more than ~500 candles per interval
            // (deliberately sized for the trend calculation). The emulator disables that cleanup, so its
            // CandleList grows unbounded and the trend was being rebuilt over the entire multi-week
            // history on every recompute — the single dominant run cost, AND over far more history than
            // live ever sees. Clamping minDate up makes the emulator trend both fast and faithful to live.
            // Gated on IsEmulatorMode so the live path is provably untouched (no edge case where live's
            // list happens to hold more than this window).
            if (GlobalData.IsEmulatorMode)
            {
                // Align the window on THIS interval instead of on the minute. GetCandleFetchStart
                // counts back from a minute-aligned "now", so the cold build started on a different
                // candle depending on which minute the first trend calculation happened to land on —
                // and that first candle decides where the ZigZag puts its first pivot, which it then
                // carries for the rest of the run (it is only fed incrementally afterwards). For any
                // interval FINER than the replay's base interval that minute differs between runs,
                // which is exactly the set of intervals whose trend drifted apart: 1m/2m/3m on a 5m
                // run, plus 5m/10m on a 15m run.
                CandleTime windowStart = CandleTools.GetCandleFetchStart(symbol, interval, maxDate.ToDateTime());
                windowStart = new CandleTime(windowStart.Minutes - (windowStart.Minutes % interval.Duration));
                if (minDate < windowStart)
                    minDate = windowStart;
            }

            // Reuse the cached ZigZag indicator across calls instead of rebuilding it from the full
            // window every time. ZigZagIndicator.Calculate() is already incremental per candle — the
            // instance just needs to survive between calls (cached on CryptoSymbolInterval, keyed by
            // (TrendType, UseHighLow)). Cold (re)build happens once, on first use for this key or after
            // the cache was cleared (e.g. emulator run start) — identical to today's behaviour. Every
            // call after that feeds only the candles since the last call instead of replaying
            // [minDate, maxDate] again, which is the dominant cost MarketTrend pays per stale interval.
            CryptoSymbolInterval symbolIntervalForCache = symbol.GetSymbolInterval(interval.IntervalPeriod);
            var zigZagCacheKey = (trendSettings.TrendType, trendSettings.UseHighLow);
            ZigZagIndicator? indicator;
            if (!symbolIntervalForCache.ZigZagIndicators.TryGetValue(zigZagCacheKey, out indicator)
                || indicator.LastFedCandleTime == null)
            {
                indicator = new(trendSettings.TrendType, trendSettings.UseHighLow, 1.0m);
                symbolIntervalForCache.ZigZagIndicators[zigZagCacheKey] = indicator;
                CandleTime? lastFed = await TrendTools.AddCandlesToIndicatorsAsync(indicator, symbol, interval, minDate, maxDate);
                if (lastFed != null)
                    indicator.LastFedCandleTime = lastFed;
            }
            else if (indicator.LastFedCandleTime!.Value < maxDate)
            {
                CandleTime feedFrom = indicator.LastFedCandleTime.Value + interval.Duration;
                CandleTime? lastFed = await TrendTools.AddCandlesToIndicatorsAsync(indicator, symbol, interval, feedFrom, maxDate);
                if (lastFed != null)
                    indicator.LastFedCandleTime = lastFed;
            }
            // else: already up to date (e.g. a second stale-check on the same minute) — reuse as is.

            // --- Dow theory interpretation -------------------------------------------------
            long profDowStart = Stopwatch.GetTimestamp();
            CryptoTrendIndicator dowIndicator = TrendInterval.InterpretZigZagPoints(indicator, log);
            PipelineProfiler.RecordTrendDow(Stopwatch.GetTimestamp() - profDowStart);
            dowTrendData.PrevTrend = dowTrendData.Trend;
            dowTrendData.PrevTime = dowTrendData.Time;
            dowTrendData.Trend = dowIndicator;
            dowTrendData.Time = maxDate;
            WritePivotData(indicator, dowTrendData);

            // --- BOS/CHoCH interpretation --------------------------------------------------
            long profBosStart = Stopwatch.GetTimestamp();
            CryptoTrendIndicator bosIndicator = TrendIntervalBos.InterpretZigZagPoints(indicator, log,
                out var structureEvents);
            PipelineProfiler.RecordTrendBos(Stopwatch.GetTimestamp() - profBosStart);
            bosTrendData.PrevTrend = bosTrendData.Trend;
            bosTrendData.PrevTime = bosTrendData.Time;
            bosTrendData.Trend = bosIndicator;
            bosTrendData.Time = maxDate;
            bosTrendData.StructureEvents.Clear();
            bosTrendData.StructureEvents.AddRange(structureEvents);
            WritePivotData(indicator, bosTrendData);

            if (GlobalData.Settings.General.DebugTrendCalculation)
            {
                string text = $"{symbol.Name} {interval.Name} [Dow+BOS] candles={candleList.Count} " +
                    $"calculated at {dowTrendData.Time?.ToDateTime()} " +
                    $"zigzagcount={indicator.ZigZagList.Count} dow={dowTrendData.Trend} bos={bosTrendData.Trend}";
                log?.AppendLine(text);
                ScannerLog.Logger.Debug("TrendCalculator.CalculateBoth " + text);
            }
        }
        finally
        {
            PipelineProfiler.RecordTrendCalcBoth(Stopwatch.GetTimestamp() - profCalcBothStart);
        }
    }
}
