using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings.Strategy;

namespace CryptoScanner.Core.Signal.Helpers;

/// <summary>
/// The classic candlestick reversal patterns, measured RELATIVE to the candle's own range.
/// <para>
/// Relative is the whole point. The OHLC_Candlestick_Patterns package was measured against this
/// candle database on 29-08-2026 and its thresholds turned out to be absolute price amounts: on
/// BTCUSDT (~65 000) only 7 of its 74 patterns ever fired and an engulfing never once, while on
/// 1000PEPEUSDT (~0.01) a "long black candlestick" fired on 51% of ALL candles. It was reporting
/// the price of the coin, not the shape of the candle. Everything here is therefore a percentage of
/// the candle's own high-low range, which is scale-free by construction. The measurement is written
/// up in Tools/PatternScan/README.md.
/// </para>
/// <para>
/// Pure geometry on purpose: no trend, no indicators, no context. A hammer and a hanging man are the
/// same shape and only what came before tells them apart, so that belongs to the strategy using this
/// and not here - which is also what makes these testable against hand-drawn candles.
/// </para>
/// </summary>
public static class CandlePatternHelper
{
    /// <summary>Body height as a percentage of the candle's full range. A flat candle counts as 0.</summary>
    public static decimal BodyPercentage(in CryptoCandle candle)
    {
        decimal range = candle.High - candle.Low;
        if (range <= 0)
            return 0m;
        return 100m * Math.Abs(candle.Close - candle.Open) / range;
    }

    /// <summary>Wick above the body, as a percentage of the candle's full range.</summary>
    public static decimal UpperWickPercentage(in CryptoCandle candle)
    {
        decimal range = candle.High - candle.Low;
        if (range <= 0)
            return 0m;
        return 100m * (candle.High - Math.Max(candle.Open, candle.Close)) / range;
    }

    /// <summary>Wick below the body, as a percentage of the candle's full range.</summary>
    public static decimal LowerWickPercentage(in CryptoCandle candle)
    {
        decimal range = candle.High - candle.Low;
        if (range <= 0)
            return 0m;
        return 100m * (Math.Min(candle.Open, candle.Close) - candle.Low) / range;
    }

    public static bool IsBullish(in CryptoCandle candle) => candle.Close > candle.Open;

    public static bool IsBearish(in CryptoCandle candle) => candle.Close < candle.Open;


    /// <summary>
    /// Whether the candles ending at <paramref name="last"/> form the given pattern, read for the
    /// given side. <paramref name="previous"/> is the candle before it and <paramref name="before"/>
    /// the one before that; a pattern that needs fewer candles ignores what it does not use, and one
    /// that needs more than was supplied returns false. <paramref name="earlier"/> holds the candles
    /// before <paramref name="before"/>, newest first; only <see cref="CryptoCandlePattern.ThreeMethods"/>
    /// reads them.
    /// </summary>
    public static bool Matches(CryptoCandlePattern pattern, CryptoTradeSide side,
        in CryptoCandle last, CryptoCandle? previous, CryptoCandle? before, CandlePatternSettings settings,
        IReadOnlyList<CryptoCandle>? earlier = null)
        => pattern switch
        {
            CryptoCandlePattern.Hammer => IsHammer(last, settings),
            CryptoCandlePattern.InvertedHammer => IsInvertedHammer(last, settings),
            CryptoCandlePattern.Engulfing => previous is not null && IsEngulfing(last, previous.Value, side, settings),
            CryptoCandlePattern.Harami => previous is not null && IsHarami(last, previous.Value, side, settings),
            CryptoCandlePattern.PiercingLine => previous is not null && IsPiercingLine(last, previous.Value, side),
            CryptoCandlePattern.MorningStar => previous is not null && before is not null
                && IsMorningStar(last, previous.Value, before.Value, side, settings),
            CryptoCandlePattern.Tweezer => previous is not null && IsTweezer(last, previous.Value, side, settings),
            CryptoCandlePattern.ThreeMethods => previous is not null && before is not null
                && ThreeMethodsLength(side, last, previous.Value, before.Value, earlier, settings) > 0,
            _ => false,
        };


    /// <summary>
    /// How many candles the matched pattern spans, the last one included: one for the hammers, two
    /// for engulfing, harami, piercing line and tweezer, three for the morning star, and four to six
    /// for three methods. The caller uses it to find the candles a stop under the pattern and the
    /// size check have to look at. Zero when the candles do not form the pattern.
    /// </summary>
    public static int PatternLength(CryptoCandlePattern pattern, CryptoTradeSide side,
        in CryptoCandle last, CryptoCandle? previous, CryptoCandle? before, CandlePatternSettings settings,
        IReadOnlyList<CryptoCandle>? earlier = null)
    {
        if (pattern == CryptoCandlePattern.ThreeMethods)
        {
            return previous is not null && before is not null
                ? ThreeMethodsLength(side, last, previous.Value, before.Value, earlier, settings)
                : 0;
        }

        if (!Matches(pattern, side, last, previous, before, settings, earlier))
            return 0;

        return pattern switch
        {
            CryptoCandlePattern.Hammer or CryptoCandlePattern.InvertedHammer => 1,
            CryptoCandlePattern.MorningStar => 3,
            _ => 2,
        };
    }


    /// <summary>
    /// Whether the pattern continues the move that was already going instead of reversing it. The
    /// requirement that price first ran against the trade belongs to the reversals only.
    /// </summary>
    public static bool IsContinuation(CryptoCandlePattern pattern)
        => pattern == CryptoCandlePattern.ThreeMethods;


    /// <summary>
    /// The price a stop under the pattern goes to, on candles given newest first with at least
    /// <paramref name="length"/> of them: the lowest low of the pattern for a long, the highest high
    /// for a short. Three methods is the exception: the stop goes beyond the FIRST candle, the
    /// decisive one the small candles stayed inside of, not beyond the closing candle.
    /// </summary>
    public static decimal PatternStop(CryptoCandlePattern pattern, CryptoTradeSide side,
        IReadOnlyList<CryptoCandle> newestFirst, int length)
    {
        if (pattern == CryptoCandlePattern.ThreeMethods)
        {
            CryptoCandle first = newestFirst[length - 1];
            return side == CryptoTradeSide.Long ? first.Low : first.High;
        }

        decimal stop = side == CryptoTradeSide.Long ? decimal.MaxValue : decimal.MinValue;
        for (int i = 0; i < length; i++)
        {
            stop = side == CryptoTradeSide.Long
                ? Math.Min(stop, newestFirst[i].Low)
                : Math.Max(stop, newestFirst[i].High);
        }
        return stop;
    }


    /// <summary>
    /// The candles of the pattern whose body has to be large compared with the candles before it:
    /// the last one, and for three methods also the first one. Indexes into the newest-first list.
    /// </summary>
    public static int[] DecisiveCandles(CryptoCandlePattern pattern, int length)
        => pattern == CryptoCandlePattern.ThreeMethods ? [0, length - 1] : [0];


    /// <summary>
    /// Whether the candles form ANY of the named patterns, and which one did. The names are members
    /// of <see cref="CryptoCandlePattern"/>; the first one in the list that matches wins, so the
    /// declaration order of the setting decides what a candle forming two shapes is reported as.
    /// <para>
    /// An unknown name is a hard error rather than a silent miss. A typo would otherwise reject every
    /// signal and read exactly like "the strategy produced nothing", which is the most expensive
    /// thing to diagnose in this codebase. <paramref name="setting"/> names the setting the list came
    /// from, so the message points at the place that has to be corrected. Because the first match
    /// wins, a name behind one that already matched is not reached on that candle - the throw then
    /// comes on the first candle where none of the names before it fit.
    /// </para>
    /// </summary>
    public static bool MatchesAny(List<string> names, CryptoTradeSide side,
        in CryptoCandle last, CryptoCandle? previous, CryptoCandle? before, CandlePatternSettings settings,
        string setting, out CryptoCandlePattern matched, IReadOnlyList<CryptoCandle>? earlier = null)
    {
        matched = default;
        foreach (string name in names)
        {
            if (!Enum.TryParse(name, ignoreCase: true, out CryptoCandlePattern pattern))
            {
                throw new InvalidOperationException(
                    $"{setting} contains '{name}', which is not a CryptoCandlePattern");
            }

            if (Matches(pattern, side, last, previous, before, settings, earlier))
            {
                matched = pattern;
                return true;
            }
        }

        return false;
    }


    /// <summary>
    /// A small body with a long wick BELOW it and almost nothing above. Hammer and hanging man are
    /// the same shape, so the side plays no part here - the strategy decides which reading applies.
    /// The classic patterns allow either body colour, and so does this.
    /// </summary>
    private static bool IsHammer(in CryptoCandle candle, CandlePatternSettings settings)
        => BodyPercentage(candle) <= settings.MaxBodyPercentage
            && LowerWickPercentage(candle) >= settings.MinWickPercentage
            && UpperWickPercentage(candle) <= settings.MaxOppositeWickPercentage;

    /// <summary>The same shape upside down: the long wick is above. Inverted hammer / shooting star.</summary>
    private static bool IsInvertedHammer(in CryptoCandle candle, CandlePatternSettings settings)
        => BodyPercentage(candle) <= settings.MaxBodyPercentage
            && UpperWickPercentage(candle) >= settings.MinWickPercentage
            && LowerWickPercentage(candle) <= settings.MaxOppositeWickPercentage;

    /// <summary>
    /// This candle's body covers the whole body of the previous one and points the other way. Both
    /// bodies have to be worth something: two nearly flat candles technically engulf each other, and
    /// that is noise rather than a reversal.
    /// </summary>
    private static bool IsEngulfing(in CryptoCandle last, in CryptoCandle previous,
        CryptoTradeSide side, CandlePatternSettings settings)
    {
        if (BodyPercentage(last) < settings.MinBodyPercentage || BodyPercentage(previous) < settings.MinBodyPercentage)
            return false;

        decimal lastBottom = Math.Min(last.Open, last.Close);
        decimal lastTop = Math.Max(last.Open, last.Close);
        decimal prevBottom = Math.Min(previous.Open, previous.Close);
        decimal prevTop = Math.Max(previous.Open, previous.Close);
        if (lastBottom > prevBottom || lastTop < prevTop)
            return false;

        return side == CryptoTradeSide.Long
            ? IsBullish(last) && IsBearish(previous)
            : IsBearish(last) && IsBullish(previous);
    }

    /// <summary>The reverse of engulfing: the PREVIOUS body covers this one entirely.</summary>
    private static bool IsHarami(in CryptoCandle last, in CryptoCandle previous,
        CryptoTradeSide side, CandlePatternSettings settings)
    {
        if (BodyPercentage(previous) < settings.MinBodyPercentage)
            return false;

        decimal lastBottom = Math.Min(last.Open, last.Close);
        decimal lastTop = Math.Max(last.Open, last.Close);
        decimal prevBottom = Math.Min(previous.Open, previous.Close);
        decimal prevTop = Math.Max(previous.Open, previous.Close);
        if (lastBottom < prevBottom || lastTop > prevTop)
            return false;

        return side == CryptoTradeSide.Long
            ? IsBullish(last) && IsBearish(previous)
            : IsBearish(last) && IsBullish(previous);
    }

    /// <summary>
    /// Opens beyond the previous close and closes back past the middle of the previous body without
    /// covering it completely - covering it completely would be an engulfing. Piercing line going
    /// up, dark cloud cover going down.
    /// </summary>
    private static bool IsPiercingLine(in CryptoCandle last, in CryptoCandle previous, CryptoTradeSide side)
    {
        decimal middle = (previous.Open + previous.Close) / 2m;
        return side == CryptoTradeSide.Long
            ? IsBearish(previous) && IsBullish(last)
                && last.Open < previous.Close && last.Close > middle && last.Close < previous.Open
            : IsBullish(previous) && IsBearish(last)
                && last.Open > previous.Close && last.Close < middle && last.Close > previous.Open;
    }

    /// <summary>
    /// Three candles: a decisive one, a hesitant one, and a decisive one back the other way that
    /// recovers past the middle of the first. Morning star going up, evening star going down.
    /// </summary>
    private static bool IsMorningStar(in CryptoCandle last, in CryptoCandle previous, in CryptoCandle before,
        CryptoTradeSide side, CandlePatternSettings settings)
    {
        // The middle candle IS the hesitation, so it needs a small body; the two around it do not.
        if (BodyPercentage(previous) > settings.MaxBodyPercentage)
            return false;
        if (BodyPercentage(before) < settings.MinBodyPercentage || BodyPercentage(last) < settings.MinBodyPercentage)
            return false;

        decimal middle = (before.Open + before.Close) / 2m;
        return side == CryptoTradeSide.Long
            ? IsBearish(before) && IsBullish(last) && last.Close > middle
            : IsBullish(before) && IsBearish(last) && last.Close < middle;
    }

    /// <summary>
    /// Two candles that stop at the same price: equal lows for a bottom, equal highs for a top.
    /// "Equal" within a tolerance, again as a percentage of the range - an exact match between two
    /// decimal prices would essentially never happen.
    /// </summary>
    private static bool IsTweezer(in CryptoCandle last, in CryptoCandle previous,
        CryptoTradeSide side, CandlePatternSettings settings)
    {
        decimal range = Math.Max(last.High - last.Low, previous.High - previous.Low);
        if (range <= 0)
            return false;
        decimal tolerance = range * settings.TweezerTolerancePercentage / 100m;

        return side == CryptoTradeSide.Long
            ? IsBearish(previous) && IsBullish(last) && Math.Abs(last.Low - previous.Low) <= tolerance
            : IsBullish(previous) && IsBearish(last) && Math.Abs(last.High - previous.High) <= tolerance;
    }

    /// <summary>The fewest and the most small candles between the two decisive ones of three methods.</summary>
    public const int ThreeMethodsMinInside = 2;
    public const int ThreeMethodsMaxInside = 4;

    /// <summary>
    /// Three methods: a decisive candle with the trade, two to four small candles that stay inside its
    /// high and low with a smaller body each, and a candle with the trade that closes beyond the
    /// first one - above its high going up, below its low going down. The colour of the small candles
    /// is free. The fewest small candles that fit win, so the shortest reading is the one reported.
    /// Returns how many candles the pattern spans, last one included, or zero.
    /// </summary>
    private static int ThreeMethodsLength(CryptoTradeSide side, in CryptoCandle last, in CryptoCandle previous,
        in CryptoCandle before, IReadOnlyList<CryptoCandle>? earlier, CandlePatternSettings settings)
    {
        bool isLong = side == CryptoTradeSide.Long;
        if (isLong ? !IsBullish(last) : !IsBearish(last))
            return 0;

        // Newest first: the last candle, then the ones before it. Copied, because a local function
        // cannot capture an in-parameter.
        CryptoCandle c0 = last, c1 = previous, c2 = before;
        int available = 3 + (earlier?.Count ?? 0);
        CryptoCandle At(int index) => index switch
        {
            0 => c0,
            1 => c1,
            2 => c2,
            _ => earlier![index - 3],
        };

        for (int inside = ThreeMethodsMinInside; inside <= ThreeMethodsMaxInside; inside++)
        {
            int firstIndex = inside + 1;
            if (firstIndex >= available)
                break;

            CryptoCandle first = At(firstIndex);
            if (isLong ? !IsBullish(first) : !IsBearish(first))
                continue;
            if (BodyPercentage(first) < settings.MinBodyPercentage)
                continue;

            decimal firstBody = Math.Abs(first.Close - first.Open);
            bool contained = true;
            for (int i = 1; i <= inside && contained; i++)
            {
                CryptoCandle small = At(i);
                contained = small.High <= first.High && small.Low >= first.Low
                    && Math.Abs(small.Close - small.Open) < firstBody;
            }
            if (!contained)
                continue;

            if (isLong ? last.Close > first.High : last.Close < first.Low)
                return firstIndex + 1;
        }

        return 0;
    }
}


/// <summary>
/// The thresholds the shapes are measured against, every one a percentage of the candle's own range
/// so they mean the same thing on a coin at 65 000 and on one at 0.01.
/// </summary>
public class CandlePatternSettings
{
    /// <summary>A body at or under this counts as small (hammer, and the middle of a morning star).</summary>
    [SettingCaption("Small body max %", Unit = "of the candle range",
        Tooltip = "A body at or under this counts as small: the hammer's body, and the hesitant "
            + "middle candle of a morning star.")]
    public decimal MaxBodyPercentage { get; set; } = 30m;

    /// <summary>A body at or over this counts as decisive (engulfing, the outer morning-star candles).</summary>
    [SettingCaption("Decisive body min %", Unit = "of the candle range",
        Tooltip = "A body at or over this counts as decisive: both candles of an engulfing or a "
            + "harami, and the two outer candles of a morning star. Keeps two nearly flat candles "
            + "from engulfing each other.")]
    public decimal MinBodyPercentage { get; set; } = 40m;

    /// <summary>How long the dominant wick has to be for a hammer or an inverted hammer.</summary>
    [SettingCaption("Dominant wick min %", Unit = "of the candle range",
        Tooltip = "How long the long wick has to be for a hammer (below the body) or an inverted "
            + "hammer (above it).")]
    public decimal MinWickPercentage { get; set; } = 60m;

    /// <summary>And how short the wick at the other end has to stay.</summary>
    [SettingCaption("Opposite wick max %", Unit = "of the candle range",
        Tooltip = "And how short the wick at the other end has to stay for that same hammer.")]
    public decimal MaxOppositeWickPercentage { get; set; } = 10m;

    /// <summary>How far apart two lows or highs may be and still count as equal.</summary>
    [SettingCaption("Tweezer tolerance %", Unit = "of the candle range",
        Tooltip = "How far apart the two lows (or highs) of a tweezer may be and still count as "
            + "equal. An exact match between two decimal prices essentially never happens.")]
    public decimal TweezerTolerancePercentage { get; set; } = 5m;
}
