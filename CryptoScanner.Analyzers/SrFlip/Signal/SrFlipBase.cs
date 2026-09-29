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
    private static readonly ConcurrentDictionary<(string Symbol, CryptoIntervalPeriod Interval, int History), (CandleTime Time, SupportResistanceResult? Result, List<CryptoCandle> Candles)> Cache = new();

    // The volume of the entry candle is compared with the average of this many candles before it
    private const int VolumeLength = 20;

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
    private SupportResistanceResult? ScanFor(CandleTime openTime, int history, out List<CryptoCandle> window)
    {
        var key = (Symbol.Name, Interval.IntervalPeriod, history);
        if (Cache.TryGetValue(key, out var cached) && cached.Time == openTime)
        {
            window = cached.Candles;
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

        window = candles;
        SupportResistanceResult? result = candles.Count >= 60 ? SupportResistance.Scan(candles) : null;
        Cache[key] = (openTime, result, candles);
        return result;
    }


    /// <summary>The candle closed in the trade direction: green for a long, red for a short.</summary>
    private static bool Turns(CryptoCandle candle, int side)
        => side == 1 ? candle.Close > candle.Open : candle.Close < candle.Open;


    /// <summary>The candle traded at least factor times the average volume of the candles before it; a factor of zero is off.</summary>
    private static bool VolumeUp(IReadOnlyList<CryptoCandle> candles, int index, decimal factor)
    {
        if (factor <= 0)
            return true;
        if (index < VolumeLength)
            return false;
        decimal sum = 0;
        for (int i = index - VolumeLength; i < index; i++)
            sum += candles[i].Volume;
        decimal average = sum / VolumeLength;
        return average > 0 && candles[index].Volume >= factor * average;
    }


    /// <summary>
    /// The first candle after the retest that confirms the flip the way a trader waits for it: it closes
    /// in the trade direction and beyond the retest candle's high (long) or low (short), on enough
    /// volume. Null when none did within the window, or a close went back through the level first.
    /// </summary>
    private static int? FirstConfirmation(IReadOnlyList<CryptoCandle> candles, SupportResistanceEvent flip, SrFlipSettings settings)
    {
        SupportResistance.Parameters p = SupportResistance.Default;
        int side = flip.Side == CryptoTradeSide.Long ? 1 : -1;
        // The flip's stop sits StopAtr ATR beyond the level, so the ATR of the retest candle follows from it
        double atr = Math.Abs(flip.Level - flip.Stop) / p.StopAtr;
        CryptoCandle retest = candles[flip.Index];
        int last = Math.Min(candles.Count - 1, flip.Index + settings.ConfirmationCandles);
        for (int j = flip.Index + 1; j <= last; j++)
        {
            CryptoCandle candle = candles[j];
            if ((flip.Level - (double)candle.Close) * side > p.RetestAtr * atr)
                return null;
            bool beyond = side == 1 ? candle.Close > retest.High : candle.Close < retest.Low;
            if (Turns(candle, side) && beyond && VolumeUp(candles, j, settings.VolumeFactor))
                return j;
        }
        return null;
    }


    /// <summary>
    /// The candle a trade on this flip is entered on: the confirmation candle, or with WaitForConfirmation
    /// off the retest candle itself (on enough volume). Null when the flip never got an entry. The chart
    /// draws the flip there too, so the arrow sits where the strategy would act.
    /// </summary>
    public static int? EntryIndex(IReadOnlyList<CryptoCandle> candles, SupportResistanceEvent flip, SrFlipSettings settings)
    {
        if (settings.WaitForConfirmation)
            return FirstConfirmation(candles, flip, settings);
        return VolumeUp(candles, flip.Index, settings.VolumeFactor) ? flip.Index : null;
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

        SupportResistanceResult? result = ScanFor(CandleLast.Candle.OpenTime, Math.Max(60, settings.HistoryCandles), out List<CryptoCandle> candles);
        if (result == null)
        {
            ExtraText = $"not enough candles ({candles.Count})";
            return false;
        }

        int last = candles.Count - 1;
        foreach (SupportResistanceEvent e in result.Events)
        {
            if (e.Type != SupportResistanceEventType.Flip || e.Side != SignalSide)
                continue;
            // Without confirmation the retest candle is the entry; with it, the first confirming candle
            // after the retest, so the retest itself lies up to ConfirmationCandles back
            if (settings.WaitForConfirmation)
            {
                if (e.Index >= last || last - e.Index > settings.ConfirmationCandles)
                    continue;
            }
            else if (e.Index != last)
                continue;
            if (e.Kind == SupportResistanceKind.Horizontal && (!settings.UseHorizontal || e.Touches < settings.MinimumTouches))
                continue;
            if (e.Kind == SupportResistanceKind.Sloped && !settings.UseSloped)
                continue;

            if (EntryIndex(candles, e, settings) != last)
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
            ExtraText = settings.WaitForConfirmation ? $"flip on {what}, confirmed {last - e.Index} candle(s) after the retest" : $"flip on {what}";
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
