using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.CandlePattern.Signal;

/// <summary>
/// Fires on any of the candlestick reversal shapes named in the settings. Long and short share
/// everything; the side only decides how the shape is read, which is the point - a hammer and a
/// hanging man are the same candle.
/// <para>
/// No indicators. This exists to answer one question: does reacting to these shapes work as well as
/// any other reasonable strategy? Adding a filter first would leave that unanswered, so anything
/// extra belongs in a later variation, not in the measurement.
/// </para>
/// </summary>
public class CandlePatternBase : SignalCreateBase
{
    public override bool IndicatorsOkay(MyData data)
        => data != null && data.Candle.OpenTime != 0;


    /// <summary>The stop-loss distance handed to the trader, see CandlePatternStrategySettings.StopBeyondPattern.</summary>
    protected decimal? SlPercentage;
    public override decimal? OverrideSlPercentage => SlPercentage;

    /// <summary>The take-profit distance handed to the trader, see CandlePatternStrategySettings.RiskReward.</summary>
    protected decimal? TpPercentage;
    public override decimal? OverrideProfitPercentage => TpPercentage;


    public override bool IsSignal()
    {
        ExtraText = "";
        SlPercentage = null;
        TpPercentage = null;
        CandlePatternStrategySettings settings = CandlePatternPlugin.Settings;

        // Nothing ticked in the settings. Said out loud, because a strategy that produces nothing is
        // the most expensive thing to diagnose in this codebase.
        if (settings.Patterns.Count == 0)
        {
            ExtraText = "no pattern selected";
            return false;
        }

        // Before the shape, because it is by far the most selective of the two: most candles are
        // nowhere near a zone, and the shape test needs two extra candle lookups.
        if (!this.InsideARequiredZone(settings.RequireZone, settings.ZoneTolerancePercentage,
                out string zoneText))
        {
            ExtraText = zoneText;
            return false;
        }

        if (!GetPrevCandle(CandleLast!, out MyData? previous))
            return false;

        // Only the three-candle shapes read this far back, and asking for a candle that is not there
        // would drop every signal at the start of a run. Missing simply means those shapes cannot
        // match, which CandlePatternHelper already handles - a two-candle shape in the same list
        // still fires.
        GetPrevCandle(previous, out MyData? before);

        // Three methods reads up to three candles further back than that. Only fetched when it is in
        // the list, so the other shapes do not pay for the extra lookups.
        List<CryptoCandle>? earlier = null;
        if (before != null && settings.Patterns.Exists(p => p.Equals(nameof(CryptoCandlePattern.ThreeMethods),
                StringComparison.OrdinalIgnoreCase)))
        {
            earlier = CandlesNewestFirst(before.Candle.OpenTime - Interval.Duration,
                CandlePatternHelper.ThreeMethodsMaxInside - 1);
        }

        if (!CandlePatternHelper.MatchesAny(settings.Patterns, SignalSide, CandleLast!.Candle,
                previous!.Candle, before?.Candle, settings.Shape,
                nameof(settings.Patterns), out CryptoCandlePattern matched, earlier))
        {
            ExtraText = $"no {string.Join("/", settings.Patterns)}";
            return false;
        }

        // A continuation follows the move that is already going, so there is nothing to reverse and
        // the preceding move against the trade does not apply to it.
        if (!CandlePatternHelper.IsContinuation(matched) && !PrecededByAMoveTheOtherWay(settings, previous))
            return false;

        if (!SizeTrendAndStop(settings, matched, previous.Candle, before?.Candle, earlier))
            return false;

        ExtraText = zoneText.Length > 0 ? $"{matched} {zoneText}" : $"{matched}";
        return true;
    }


    /// <summary>
    /// Up to <paramref name="count"/> candles of the signal interval, newest first, starting at the
    /// candle that opens at <paramref name="openTime"/> and walking back. Stops early at a gap or at
    /// the start of the list, so the caller has to check how many came back. Reads the candle list
    /// directly: no indicator row is needed, and a missing candle is not an error here.
    /// </summary>
    private List<CryptoCandle> CandlesNewestFirst(CandleTime openTime, int count)
    {
        List<CryptoCandle> candles = new(Math.Max(count, 0));
        CandleTime time = openTime;
        while (candles.Count < count && SymbolInterval.CandleList.TryGetValue(time, out CryptoCandle candle))
        {
            candles.Add(candle);
            if (time < Interval.Duration)
                break;
            time -= Interval.Duration;
        }
        return candles;
    }


    /// <summary>
    /// The checks that need the candles before the pattern, done only once a shape matched: the size
    /// of the decisive candle(s) against the average body before them, the close against the trend
    /// EMA, and the stop-loss beyond the pattern with the take profit at RiskReward times its distance.
    /// </summary>
    private bool SizeTrendAndStop(CandlePatternStrategySettings settings, CryptoCandlePattern matched,
        in CryptoCandle previous, CryptoCandle? before, List<CryptoCandle>? earlier)
    {
        if (settings.MinSizeFactor <= 0 && settings.TrendEmaLength <= 0 && !settings.StopBeyondPattern)
            return true;

        CryptoCandle last = CandleLast!.Candle;
        int length = CandlePatternHelper.PatternLength(matched, SignalSide, last, previous, before,
            settings.Shape, earlier);
        if (length <= 0)
            return false;

        int needed = length;
        if (settings.MinSizeFactor > 0)
            needed = Math.Max(needed, length + CandlePatternStrategySettings.AverageBodyCandles);
        if (settings.TrendEmaLength > 0)
            needed = Math.Max(needed, TrendEmaWindow(settings.TrendEmaLength));
        List<CryptoCandle> history = CandlesNewestFirst(last.OpenTime, needed);
        if (history.Count < length)
        {
            ExtraText = $"{matched} not enough candles";
            return false;
        }

        if (settings.MinSizeFactor > 0)
        {
            foreach (int index in CandlePatternHelper.DecisiveCandles(matched, length))
            {
                if (!LargeEnough(history, index, settings.MinSizeFactor, out string reason))
                {
                    ExtraText = $"{matched} {reason}";
                    return false;
                }
            }
        }

        if (settings.TrendEmaLength > 0)
        {
            int emaLength = settings.TrendEmaLength;
            if (history.Count < 3 * emaLength)
            {
                ExtraText = $"{matched} not enough candles for the trend EMA";
                return false;
            }

            decimal ema = Ema(history, emaLength);
            bool withTrend = SignalSide == CryptoTradeSide.Long ? last.Close > ema : last.Close < ema;
            if (!withTrend)
            {
                ExtraText = $"{matched} close not {(SignalSide == CryptoTradeSide.Long ? "above" : "below")} EMA{emaLength}";
                return false;
            }
        }

        if (settings.StopBeyondPattern)
        {
            if (last.Close <= 0)
                return false;

            decimal stop = CandlePatternHelper.PatternStop(matched, SignalSide, history, length);
            decimal distance = 100m * (SignalSide == CryptoTradeSide.Long ? last.Close - stop : stop - last.Close)
                / last.Close;
            if (distance <= 0)
            {
                ExtraText = $"{matched} close not beyond the pattern stop";
                return false;
            }

            SlPercentage = Math.Round(distance, 2);
            if (settings.RiskReward > 0)
                TpPercentage = Math.Round(distance * settings.RiskReward, 2);
        }

        return true;
    }


    /// <summary>
    /// How many candles the trend EMA is computed over: six times its length, seeded with the plain
    /// average of the oldest ones. The weight left on the seed after the other five lengths is about
    /// (1 - 2 / (n + 1))^(5n), well under 0.1%, so the value matches an EMA over the whole history.
    /// </summary>
    private static int TrendEmaWindow(int length) => 6 * length;


    /// <summary>
    /// The EMA of the closes at the newest candle of <paramref name="newestFirst"/>, over everything
    /// in the list: seeded with the simple average of the oldest <paramref name="length"/> closes and
    /// carried forward from there.
    /// </summary>
    private static decimal Ema(List<CryptoCandle> newestFirst, int length)
    {
        int oldest = newestFirst.Count - 1;
        decimal sum = 0m;
        for (int i = oldest; i > oldest - length; i--)
            sum += newestFirst[i].Close;
        decimal ema = sum / length;

        decimal k = 2m / (length + 1);
        for (int i = oldest - length; i >= 0; i--)
            ema += (newestFirst[i].Close - ema) * k;
        return ema;
    }


    /// <summary>
    /// Whether the body of the candle at <paramref name="index"/> (newest first) is at least
    /// <paramref name="factor"/> times the average body of the candles before it.
    /// </summary>
    private static bool LargeEnough(List<CryptoCandle> newestFirst, int index, decimal factor, out string reason)
    {
        int count = CandlePatternStrategySettings.AverageBodyCandles;
        if (newestFirst.Count < index + 1 + count)
        {
            reason = "not enough candles for the average body";
            return false;
        }

        decimal sum = 0m;
        for (int i = index + 1; i <= index + count; i++)
            sum += Math.Abs(newestFirst[i].Close - newestFirst[i].Open);
        decimal average = sum / count;

        CryptoCandle candle = newestFirst[index];
        decimal body = Math.Abs(candle.Close - candle.Open);
        if (average <= 0 || body < factor * average)
        {
            reason = $"candle too small ({(average > 0 ? body / average : 0m):N2}x average body)";
            return false;
        }

        reason = "";
        return true;
    }


    /// <summary>
    /// Whether price actually moved against the trade over the candles before the pattern. Measured
    /// from the close BEFORE those candles up to the close of the candle BEFORE the last one, so the
    /// reversal candle itself is not counted as part of the move it is supposed to reverse. Taking
    /// the last close instead would measure the NET move including the reversal, and the stronger the
    /// reversal the sooner it would be rejected.
    /// <para>
    /// For the two-candle shapes (engulfing, harami, piercing line, tweezer) that candle before the
    /// last one is still the first candle of the pattern, so its own move is counted. That is a
    /// deliberate limitation: keeping the reversal candle out is what the measurement was about.
    /// </para>
    /// </summary>
    private bool PrecededByAMoveTheOtherWay(CandlePatternStrategySettings settings, MyData? previous)
    {
        if (settings.PrecedingCandles <= 0)
            return true;

        // The caller already fetched this one with GetPrevCandle, so null cannot happen here; a
        // signal without the candle the move is measured to simply does not fire.
        if (previous == null)
        {
            ExtraText = "not enough candles for the preceding move";
            return false;
        }

        MyData? walk = previous;
        for (int step = 0; step < settings.PrecedingCandles; step++)
        {
            if (!GetPrevCandle(walk, out walk))
            {
                ExtraText = "not enough candles for the preceding move";
                return false;
            }
        }

        decimal from = walk!.Candle.Close;
        decimal to = previous.Candle.Close;
        if (from <= 0)
            return false;

        // Positive when price moved AGAINST the trade over those candles: down before a long, up
        // before a short. That is what makes it a reversal rather than a continuation. The reversal
        // candle's own close stays out of it, so a pattern that recovers the whole move in one candle
        // still qualifies.
        decimal moved = 100m * (SignalSide == CryptoTradeSide.Long ? from - to : to - from) / from;
        if (moved < settings.PrecedingPercentage || moved <= 0)
        {
            ExtraText = $"price did not run against the trade first ({moved:N2}%)";
            return false;
        }

        return true;
    }
}


public class CandlePatternLong : CandlePatternBase
{
}


public class CandlePatternShort : CandlePatternBase
{
}
