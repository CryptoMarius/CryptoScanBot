using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Trend;

using System.Collections.Concurrent;

namespace CryptoScanner.Analyzers.SrFlip.Signal;

/// <summary>
/// A flip on the candle that just closed: a level or line that broke earlier was retested from the
/// other side and held. Long on a broken resistance that became support, short on a broken support
/// that became resistance.
/// <para>
/// The scan runs over the last HistoryCandles candles of the signal interval. Its result is kept per
/// symbol and interval for the candle it was made for, so the long and the short of the same candle
/// share one scan instead of doing it twice.
/// </para>
/// </summary>
public class SrFlipBase : SignalCreateBase
{
    private static readonly ConcurrentDictionary<(string Symbol, CryptoIntervalPeriod Interval, int History), (CandleTime Time, SupportResistanceResult? Result, int Count)> Cache = new();

    /// <summary>The stop-loss distance handed to the trader, see SrFlipSettings.StopBeyondLevel.</summary>
    protected decimal? SlPercentage;
    public override decimal? OverrideSlPercentage => SlPercentage;

    /// <summary>The take-profit distance handed to the trader, see SrFlipSettings.RiskRewardRatio.</summary>
    protected decimal? TpPercentage;
    public override decimal? OverrideProfitPercentage => TpPercentage;


    // Candles only, no indicators
    public override bool IndicatorsOkay(MyData data)
        => data != null && data.Candle.OpenTime != 0;


    /// <summary>The scan for the candle being evaluated, from the cache when the other side already made it.</summary>
    private SupportResistanceResult? ScanFor(CandleTime openTime, int history, out int count)
    {
        var key = (Symbol.Name, Interval.IntervalPeriod, history);
        if (Cache.TryGetValue(key, out var cached) && cached.Time == openTime)
        {
            count = cached.Count;
            return cached.Result;
        }

        // Walk back from the candle being evaluated; a gap ends the window
        List<CryptoCandle> candles = new(history);
        CandleTime time = openTime;
        while (candles.Count < history && SymbolInterval.CandleList.TryGetValue(time, out CryptoCandle candle))
        {
            candles.Add(candle);
            if (time < Interval.Duration)
                break;
            time -= Interval.Duration;
        }
        candles.Reverse();

        count = candles.Count;
        SupportResistanceResult? result = candles.Count >= 60 ? SupportResistance.Scan(candles) : null;
        Cache[key] = (openTime, result, count);
        return result;
    }


    public override bool IsSignal()
    {
        ExtraText = "";
        SlPercentage = null;
        TpPercentage = null;
        SrFlipSettings settings = SrFlipPlugin.Settings;
        if (!settings.UseHorizontal && !settings.UseSloped)
        {
            ExtraText = "neither horizontal levels nor sloped lines switched on";
            return false;
        }

        SupportResistanceResult? result = ScanFor(CandleLast.Candle.OpenTime, Math.Max(60, settings.HistoryCandles), out int count);
        if (result == null)
        {
            ExtraText = $"not enough candles ({count})";
            return false;
        }

        int last = count - 1;
        foreach (SupportResistanceEvent e in result.Events)
        {
            if (e.Index != last || e.Type != SupportResistanceEventType.Flip || e.Side != SignalSide)
                continue;
            if (e.Kind == SupportResistanceKind.Horizontal && (!settings.UseHorizontal || e.Touches < settings.MinimumTouches))
                continue;
            if (e.Kind == SupportResistanceKind.Sloped && !settings.UseSloped)
                continue;

            decimal close = CandleLast.Candle.Close;
            if (settings.StopBeyondLevel && close > 0)
            {
                decimal distance = Math.Abs(close - (decimal)e.Stop) / close * 100m;
                if (distance > 0)
                {
                    SlPercentage = Math.Round(distance, 2);
                    if (settings.RiskRewardRatio > 0)
                        TpPercentage = Math.Round(distance * settings.RiskRewardRatio, 2);
                }
            }

            string what = e.Kind == SupportResistanceKind.Horizontal
                ? $"horizontal level {e.Level.ToString(Symbol.PriceDisplayFormat)} ({e.Touches}x)"
                : $"sloped {(SignalSide == CryptoTradeSide.Long ? "resistance" : "support")} line at {e.Level.ToString(Symbol.PriceDisplayFormat)}";
            ExtraText = $"flip on {what}";
            if (SlPercentage != null)
                ExtraText += $" sl {SlPercentage.Value:N2}%";
            if (TpPercentage != null)
                ExtraText += $" tp {TpPercentage.Value:N2}%";
            return true;
        }

        ExtraText = "no flip on this candle";
        return false;
    }
}


public class SrFlipLong : SrFlipBase
{
}


public class SrFlipShort : SrFlipBase
{
}
