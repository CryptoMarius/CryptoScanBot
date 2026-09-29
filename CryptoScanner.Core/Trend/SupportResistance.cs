using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Trend;

/// <summary>Horizontal level or sloped line.</summary>
public enum SupportResistanceKind
{
    Horizontal,
    Sloped,
}

/// <summary>Where the tops and bottoms (pivots) of the levels and lines come from.</summary>
public enum SupportResistancePivots
{
    /// <summary>
    /// The scanner's own ZigZag: secondary, on the wicks (high/low), swing points after Lance Beggs.
    /// The same pivots the chart draws and trend and DLZ work with. The default since 28-09-2026:
    /// Marius did not want two kinds of pivots in the scanner.
    /// </summary>
    ZigZag,

    /// <summary>
    /// A simple fractal: the highest high (lowest low) of PivotLeft/PivotRight candles on either side,
    /// LinePivot for the sloped lines. Only here so the port can still be held to the offline
    /// measurement it was made from (sr_flip_scan.py), which is written that way to run fast.
    /// </summary>
    Fractal,
}


/// <summary>What happened at a level or line.</summary>
public enum SupportResistanceEventType
{
    /// <summary>A close more than BreakAtr * ATR beyond it, straight from the other side: the candle
    /// before was still under (or above) the level. A price that creeps through - a close just
    /// beyond first - only changes the side the level is on, as in the measurement.</summary>
    Breakout,

    /// <summary>After a breakout, a retest from the other side that held: resistance became support
    /// (long) or support became resistance (short).</summary>
    Flip,
}


/// <summary>A horizontal support/resistance level: pivots that lie close together.</summary>
public sealed class SupportResistanceLevel
{
    /// <summary>The average price of the pivots in the level.</summary>
    public double Price { get; internal set; }

    /// <summary>How many pivots (highs and lows) make up the level.</summary>
    public int Touches { get; internal set; }

    /// <summary>
    /// The bottom and top of the level as a zone: the lowest and highest pivot in it, at least
    /// <see cref="SupportResistance.ZoneHeightAtr"/> * ATR apart (around <see cref="Price"/>), the
    /// way traders draw their zones. Only for drawing; the breakouts and flips work with <see cref="Price"/>.
    /// </summary>
    public double Low { get; internal set; }
    public double High { get; internal set; }

    /// <summary>The candle of the oldest pivot in the level, where its zone starts.</summary>
    public int FirstIndex { get; internal set; }

    // Walk state: which side of the level the price is on (+1 above, -1 under), and a running break
    internal int State;
    internal int? BreakIndex;
    internal int BreakSide;

    // Closer to the theory (28-09-2026): the price ran far enough away after the breakout, how often
    // the level flipped and was crossed, and whether it is done (a chop zone or flipped twice)
    internal bool Departed;
    internal int Flips;
    internal int Crossings;
    internal bool Retired;
}


/// <summary>
/// A sloped support/resistance line through two clear pivots of the same kind: a falling line
/// through two tops (resistance) or a rising line through two bottoms (support). Straight in log price,
/// so the slope is a percentage per candle.
/// <para>
/// Since 28-09-2026 drawn the way a trader draws one (Marius found the old ones "merkwaardig"): clear
/// tops and bottoms only (LinePivot candles on either side), no close beyond the line between its
/// two points or since, counted from a third touch, and gone at the first close beyond it.
/// </para>
/// </summary>
public sealed class SupportResistanceLine
{
    public int Index1 { get; internal set; }
    public int Index2 { get; internal set; }
    public double Price1 { get; internal set; }
    public double Price2 { get; internal set; }

    /// <summary>True for a falling line through two highs, false for a rising line through two lows.</summary>
    public bool IsResistance { get; internal set; }

    /// <summary>The two pivots plus every later touch (a wick within RetestAtr, LineTouchGap apart).</summary>
    public int Touches { get; internal set; } = 2;

    /// <summary>A line counts - is drawn, can break - from its third touch.</summary>
    public bool IsConfirmed => Touches >= 3;

    /// <summary>The candle of the breakout while the retest window runs, null otherwise.</summary>
    public int? BrokenAt => BreakIndex;

    internal double LogPrice1;
    internal double LogPrice2;
    internal int? BreakIndex;
    internal int BreakSide;
    internal bool Done;
    internal int LastTouch;
    internal int Start;
    internal bool Departed;

    /// <summary>The price of the line at candle <paramref name="index"/>.</summary>
    public double ValueAt(int index)
        => Math.Exp(LogPrice1 + (LogPrice2 - LogPrice1) * (index - Index1) / (Index2 - Index1));
}


/// <summary>One breakout or flip, at the close of candle <see cref="Index"/>. Touches is the number of
/// pivots in a horizontal level, and 2 for a sloped line (the two pivots it runs through).</summary>
/// <summary>
/// A block drawn on the chart: one or more levels whose zones overlap, merged into one
/// (see <see cref="SupportResistance.Zones"/>). Only for drawing.
/// </summary>
public readonly record struct SupportResistanceZone(double Low, double High, double Price, int Touches, int FirstIndex);


public readonly record struct SupportResistanceEvent(
    SupportResistanceKind Kind,
    SupportResistanceEventType Type,
    CryptoTradeSide Side,
    int Index,
    CandleTime Time,
    double Level,
    double Stop,
    int Touches);


/// <summary>What a scan found: every event on the way, and the levels and lines as they stand at the end.</summary>
public sealed class SupportResistanceResult
{
    public List<SupportResistanceEvent> Events { get; } = [];
    public List<SupportResistanceLevel> Levels { get; internal set; } = [];
    public List<SupportResistanceLine> Lines { get; internal set; } = [];
}


/// <summary>
/// Support and resistance, horizontal and sloped, and the breakouts and flips on them (open point 46).
/// <para>
/// A building block, not a strategy: measured offline on 27-09-2026 (CryptoScanBot.tools,
/// studies/sr-flip) a flip on its own earned nothing, but the levels mattered as context - a short
/// that opened just above a support did worse, in both runs and both halves of the period. This class
/// finds them; the chart draws them and a trader filter can ask for them.
/// </para>
/// <para>
/// A literal port of sr_flip_scan.scan() from that study, down to the order of the operations, and
/// SupportResistanceTests holds it to the events the Python version wrote for two candle files. Change
/// one, change the other. Everything is causal: a pivot counts from the candle that confirms it
/// (PivotRight candles later) and every decision at candle i uses nothing after i, so the scanner and
/// the emulator see exactly what a live scanner would have seen.
/// </para>
/// </summary>
public static class SupportResistance
{
    /// <summary>The settings of the measurement; the defaults are the values that were measured.</summary>
    public sealed record Parameters
    {
        public int PivotLeft { get; init; } = 5;
        public int PivotRight { get; init; } = 5;
        public int AtrLength { get; init; } = 14;
        public int LevelLookback { get; init; } = 300;
        public double ToleranceAtr { get; init; } = 0.25;
        public double BreakAtr { get; init; } = 0.5;
        public double RetestAtr { get; init; } = 0.25;
        public int RetestWindow { get; init; } = 30;
        public int MinLineSpan { get; init; } = 10;
        public int LinePivot { get; init; } = 10;
        public int LineTouchGap { get; init; } = 5;
        public SupportResistancePivots PivotSource { get; init; } = SupportResistancePivots.ZigZag;

        // Closer to the theory of the flip (28-09-2026, Marius: "wel heel veel flips"): the price has
        // to run this many ATR away after the breakout before a return counts as the retest; a level
        // needs this many pivots before it breaks and flips; a level that flipped RetireFlips times or
        // was crossed RetireCrossings times is done.
        public double DepartureAtr { get; init; } = 1.0;
        public int MinEventTouches { get; init; } = 3;
        public int RetireFlips { get; init; } = 2;
        public int RetireCrossings { get; init; } = 4;
        public double StopAtr { get; init; } = 0.5;
    }

    public static Parameters Default { get; } = new();

    /// <summary>
    /// The minimum height of a level's zone in ATRs (28-09-2026). The pivots of a level lie within
    /// ToleranceAtr of each other, which gave blocks of a quarter ATR - much thinner than the zones
    /// traders draw (about one ATR on a BTC-EUR 8h chart). Only for drawing; a candidate setting.
    /// </summary>
    public const double ZoneHeightAtr = 1.0;

    private readonly record struct Pivot(int Confirm, int Index, double Price, int Kind);


    /// <summary>Average true range, Wilder's smoothing, seeded with a plain average. NaN before the seed.</summary>
    internal static double[] Atr(double[] high, double[] low, double[] close, int length)
    {
        int n = close.Length;
        double[] tr = new double[n];
        for (int i = 0; i < n; i++)
        {
            double prev = i == 0 ? close[0] : close[i - 1];
            tr[i] = Math.Max(high[i] - low[i], Math.Max(Math.Abs(high[i] - prev), Math.Abs(low[i] - prev)));
        }

        double[] result = new double[n];
        Array.Fill(result, double.NaN);
        if (n < length)
            return result;

        double sum = 0;
        for (int i = 0; i < length; i++)
            sum += tr[i];
        result[length - 1] = sum / length;
        for (int i = length; i < n; i++)
            result[i] = (result[i - 1] * (length - 1) + tr[i]) / length;
        return result;
    }


    /// <summary>Fractal pivots, in the order they get confirmed.</summary>
    private static List<Pivot> Pivots(double[] high, double[] low, Parameters p)
        => Pivots(high, low, p.PivotLeft, p.PivotRight);

    /// <summary>
    /// The final points of the ZigZag, oldest first, as pivots known at candle <paramref name="confirm"/>.
    /// <para>
    /// Left out: every dummy (the provisional right-hand edge, not a swing) and the LAST real point.
    /// That one is not final yet: as long as no swing of the other kind follows it, a new extreme
    /// moves it (ZigZagIndicator reuses the point for the new candle). A top counts from the moment
    /// a bottom follows it, and the other way round (Marius, 28-09-2026).
    /// </para>
    /// </summary>
    private static List<Pivot> ZigZagPivots(ZigZagIndicator zigZag, Dictionary<CandleTime, int> indexByTime, int confirm)
    {
        List<Pivot> result = new(zigZag.ZigZagList.Count);
        foreach (ZigZagResult point in zigZag.ZigZagList)
        {
            if (point.Dummy || !indexByTime.TryGetValue(point.Candle.OpenTime, out int index))
                continue;
            result.Add(new Pivot(confirm, index, point.Value, point.PointType == 'H' ? +1 : -1));
        }
        if (result.Count > 0)
            result.RemoveAt(result.Count - 1);
        return result;
    }


    /// <summary>Fractal pivots with <paramref name="left"/>/<paramref name="right"/> candles on either side.</summary>
    private static List<Pivot> Pivots(double[] high, double[] low, int left, int right)
    {
        Parameters p = Default with { PivotLeft = left, PivotRight = right };
        List<Pivot> result = [];
        int n = high.Length;
        for (int i = p.PivotLeft; i < n - p.PivotRight; i++)
        {
            double maxHigh = double.MinValue, minLow = double.MaxValue;
            int countHigh = 0, countLow = 0;
            for (int j = i - p.PivotLeft; j <= i + p.PivotRight; j++)
            {
                maxHigh = Math.Max(maxHigh, high[j]);
                minLow = Math.Min(minLow, low[j]);
            }
            for (int j = i - p.PivotLeft; j <= i + p.PivotRight; j++)
            {
                if (high[j] == high[i])
                    countHigh++;
                if (low[j] == low[i])
                    countLow++;
            }
            if (high[i] == maxHigh && countHigh == 1)
                result.Add(new Pivot(i + p.PivotRight, i, high[i], +1));
            if (low[i] == minLow && countLow == 1)
                result.Add(new Pivot(i + p.PivotRight, i, low[i], -1));
        }

        // The Python tuple sort: confirm, index, price, kind
        result.Sort((a, b) =>
        {
            int c = a.Confirm.CompareTo(b.Confirm);
            if (c != 0)
                return c;
            c = a.Index.CompareTo(b.Index);
            if (c != 0)
                return c;
            c = a.Price.CompareTo(b.Price);
            if (c != 0)
                return c;
            return a.Kind.CompareTo(b.Kind);
        });
        return result;
    }


    /// <summary>
    /// The horizontal levels as they stand at the close of the LAST candle: pivots confirmed by then,
    /// of the last LevelLookback candles, clustered within ToleranceAtr times the ATR of that candle,
    /// at least two per level. A port of levels_at() in sr_context_positions.py, the measurement the
    /// short filter comes from (SupportResistanceTests holds the two together). Unlike the levels of
    /// <see cref="Scan"/> these are rebuilt for this very candle, not at the last new pivot.
    /// </summary>
    public static List<(double Price, int Touches)> CurrentLevels(IReadOnlyList<CryptoCandle> candles, out double atr,
        Parameters? parameters = null)
    {
        Parameters p = parameters ?? Default;
        int n = candles.Count;
        List<(double, int)> result = [];
        atr = double.NaN;
        if (n == 0)
            return result;

        double[] high = new double[n], low = new double[n], close = new double[n];
        for (int i = 0; i < n; i++)
        {
            high[i] = (double)candles[i].High;
            low[i] = (double)candles[i].Low;
            close[i] = (double)candles[i].Close;
        }
        int last = n - 1;
        atr = Atr(high, low, close, p.AtrLength)[last];
        if (double.IsNaN(atr))
            return result;

        List<double> prices = [];
        if (p.PivotSource == SupportResistancePivots.ZigZag)
        {
            // The ZigZag as it stands after the last candle: everything it shows is known by then
            var zigZag = new ZigZagIndicator(TrendType.Secondary, useHighLow: true);
            Dictionary<CandleTime, int> indexByTime = new(n);
            for (int i = 0; i < n; i++)
            {
                indexByTime[candles[i].OpenTime] = i;
                zigZag.Calculate(candles[i], batchProcess: true);
            }
            foreach (Pivot q in ZigZagPivots(zigZag, indexByTime, last))
            {
                if (q.Index >= last - p.LevelLookback)
                    prices.Add(q.Price);
            }
        }
        else
        {
            foreach (Pivot q in Pivots(high, low, p))
            {
                if (q.Confirm <= last && q.Index >= last - p.LevelLookback)
                    prices.Add(q.Price);
            }
        }
        prices.Sort();

        double tolerance = p.ToleranceAtr * atr;
        List<List<double>> clusters = [];
        foreach (double price in prices)
        {
            if (clusters.Count > 0 && price - clusters[^1][^1] <= tolerance)
                clusters[^1].Add(price);
            else
                clusters.Add([price]);
        }
        foreach (List<double> cluster in clusters)
        {
            if (cluster.Count < 2)
                continue;
            double sum = 0;
            foreach (double x in cluster)
                sum += x;
            result.Add((sum / cluster.Count, cluster.Count));
        }
        return result;
    }


    /// <summary>
    /// The zones of <paramref name="levels"/> for the chart, with overlapping zones merged into one
    /// block (28-09-2026): with a height of one ATR, levels about one ATR apart otherwise make a
    /// stack of blocks that touch or overlap. A merged block runs from the lowest bottom to the
    /// highest top, adds up the touches, starts at the oldest pivot, and its price is the average
    /// of the levels weighted by their touches. Sorted from low to high.
    /// </summary>
    public static List<SupportResistanceZone> Zones(IEnumerable<SupportResistanceLevel> levels)
    {
        List<SupportResistanceZone> result = [];
        double weighted = 0;
        foreach (SupportResistanceLevel level in levels.OrderBy(l => l.Low))
        {
            if (result.Count > 0 && level.Low <= result[^1].High)
            {
                SupportResistanceZone zone = result[^1];
                int touches = zone.Touches + level.Touches;
                weighted += level.Price * level.Touches;
                result[^1] = new SupportResistanceZone(zone.Low, Math.Max(zone.High, level.High),
                    weighted / touches, touches, Math.Min(zone.FirstIndex, level.FirstIndex));
            }
            else
            {
                weighted = level.Price * level.Touches;
                result.Add(new SupportResistanceZone(level.Low, level.High, level.Price, level.Touches, level.FirstIndex));
            }
        }
        return result;
    }


    /// <summary>
    /// How much room there is between <paramref name="price"/> and the nearest level on the side of
    /// <paramref name="side"/> (under the price for a short, above it for a long), in ATRs of the
    /// last candle. Null when there is no such level (or no ATR yet): nothing is in the way.
    /// </summary>
    public static double? RoomToNextLevel(IReadOnlyList<CryptoCandle> candles, double price, CryptoTradeSide side,
        out double level, Parameters? parameters = null)
    {
        var levels = CurrentLevels(candles, out double atr, parameters);
        return RoomToNextLevel(levels, atr, price, side, out level);
    }


    /// <summary>The ATR of the last candle, the unit of <see cref="RoomToNextLevel(IReadOnlyList{CryptoCandle}, double, CryptoTradeSide, out double, Parameters?)"/>.</summary>
    public static double LastAtr(IReadOnlyList<CryptoCandle> candles, Parameters? parameters = null)
    {
        Parameters p = parameters ?? Default;
        int n = candles.Count;
        if (n == 0)
            return double.NaN;
        double[] high = new double[n], low = new double[n], close = new double[n];
        for (int i = 0; i < n; i++)
        {
            high[i] = (double)candles[i].High;
            low[i] = (double)candles[i].Low;
            close[i] = (double)candles[i].Close;
        }
        return Atr(high, low, close, p.AtrLength)[n - 1];
    }


    /// <summary>The same room, from levels and an ATR worked out before (the short filter caches them per 1h candle).</summary>
    public static double? RoomToNextLevel(List<(double Price, int Touches)> levels, double atr, double price,
        CryptoTradeSide side, out double level)
    {
        level = double.NaN;
        if (double.IsNaN(atr) || atr <= 0)
            return null;

        double? best = null;
        foreach (var (levelPrice, _) in levels)
        {
            double distance = side == CryptoTradeSide.Short ? price - levelPrice : levelPrice - price;
            if (distance > 0 && (best == null || distance < best))
            {
                best = distance;
                level = levelPrice;
            }
        }
        return best == null ? null : best.Value / atr;
    }


    /// <summary>Walk the candles in order and report every breakout and flip, plus the levels and
    /// lines as they stand after the last candle.</summary>
    public static SupportResistanceResult Scan(IReadOnlyList<CryptoCandle> candles, Parameters? parameters = null)
    {
        Parameters p = parameters ?? Default;
        int n = candles.Count;
        double[] high = new double[n], low = new double[n], close = new double[n];
        for (int i = 0; i < n; i++)
        {
            high[i] = (double)candles[i].High;
            low[i] = (double)candles[i].Low;
            close[i] = (double)candles[i].Close;
        }

        SupportResistanceResult result = new();
        double[] atr = Atr(high, low, close, p.AtrLength);
        List<Pivot> pivots = Pivots(high, low, p);

        List<Pivot> confirmed = [];
        int next = 0;
        List<SupportResistanceLevel> levels = [];
        List<SupportResistanceLine> lines = [];
        HashSet<(int, int, bool)> usedLines = [];
        List<Pivot> linePivots = Pivots(high, low, p.LinePivot, p.LinePivot);
        List<Pivot> lineConfirmed = [];
        int nextLine = 0;

        // The ZigZag source: fed one candle at a time, so at candle i it shows exactly what a live
        // scanner would have seen then. Its last points can still move (see
        // ZigZagIndicator.MutableTailLength); a changed top or bottom counts as a new one.
        bool useZigZag = p.PivotSource == SupportResistancePivots.ZigZag;
        var zigZag = new ZigZagIndicator(TrendType.Secondary, useHighLow: true);
        Dictionary<CandleTime, int> indexByTime = new(n);
        for (int i = 0; i < n; i++)
            indexByTime[candles[i].OpenTime] = i;
        (int Count, CandleTime Time1, double Value1, CandleTime Time2, double Value2) signature = default;
        (int Index, double Price) lastTop = (-1, 0), lastBottom = (-1, 0);

        void Add(SupportResistanceKind kind, SupportResistanceEventType type, int side, int i, double level, double stop, int touches = 2)
            => result.Events.Add(new SupportResistanceEvent(kind, type, side == 1 ? CryptoTradeSide.Long : CryptoTradeSide.Short,
                i, candles[i].OpenTime, level, stop, touches));

        void RebuildLevels(int i)
        {
            // The pivot index travels along with the price, for the zone of the level
            List<(double Price, int Index)> prices = [];
            foreach (Pivot q in confirmed)
            {
                if (q.Index >= i - p.LevelLookback)
                    prices.Add((q.Price, q.Index));
            }
            prices.Sort((a, b) => a.Price.CompareTo(b.Price));

            double tolerance = p.ToleranceAtr * atr[i];
            List<List<(double Price, int Index)>> clusters = [];
            foreach (var pivot in prices)
            {
                if (clusters.Count > 0 && pivot.Price - clusters[^1][^1].Price <= tolerance)
                    clusters[^1].Add(pivot);
                else
                    clusters.Add([pivot]);
            }

            List<SupportResistanceLevel> fresh = [];
            foreach (var cluster in clusters)
            {
                if (cluster.Count < 2)
                    continue;
                double sum = 0;
                int firstIndex = int.MaxValue;
                foreach (var x in cluster)
                {
                    sum += x.Price;
                    firstIndex = Math.Min(firstIndex, x.Index);
                }
                double price = sum / cluster.Count;

                // Sorted on price, so the first and last pivot are the bottom and top of the zone
                double zoneHalf = ZoneHeightAtr * atr[i] / 2;
                double zoneLow = Math.Min(cluster[0].Price, price - zoneHalf);
                double zoneHigh = Math.Max(cluster[^1].Price, price + zoneHalf);

                // One cluster per level, see the Python version
                SupportResistanceLevel? keep = null;
                foreach (SupportResistanceLevel level in levels)
                {
                    if (Math.Abs(level.Price - price) <= tolerance && !fresh.Contains(level))
                    {
                        keep = level;
                        break;
                    }
                }
                if (keep != null)
                {
                    keep.Price = price;
                    keep.Touches = cluster.Count;
                    keep.Low = zoneLow;
                    keep.High = zoneHigh;
                    keep.FirstIndex = firstIndex;
                    fresh.Add(keep);
                }
                else
                {
                    fresh.Add(new SupportResistanceLevel
                    {
                        Price = price,
                        Touches = cluster.Count,
                        Low = zoneLow,
                        High = zoneHigh,
                        FirstIndex = firstIndex,
                        State = close[i] > price ? 1 : -1,
                    });
                }
            }
            levels = fresh;
        }

        // On a new clear top (bottom) b: the line back to the most recent HIGHER top (LOWER bottom)
        // from which no close went beyond the line up to candle i. Null when there is no such top.
        SupportResistanceLine? NewLine(int i, Pivot b)
        {
            bool resistance = b.Kind == 1;
            for (int k = lineConfirmed.Count - 2; k >= 0; k--)
            {
                Pivot a = lineConfirmed[k];
                if (a.Kind != b.Kind || b.Index - a.Index < p.MinLineSpan)
                    continue;
                if (b.Index - a.Index > p.LevelLookback)
                    break;
                if ((resistance && a.Price <= b.Price) || (!resistance && a.Price >= b.Price))
                    continue;
                if (usedLines.Contains((a.Index, b.Index, resistance)))
                    continue;

                var line = new SupportResistanceLine
                {
                    Index1 = a.Index,
                    Index2 = b.Index,
                    Price1 = a.Price,
                    Price2 = b.Price,
                    LogPrice1 = Math.Log(a.Price),
                    LogPrice2 = Math.Log(b.Price),
                    IsResistance = resistance,
                    LastTouch = b.Index,
                    Start = i,
                };
                bool ok = true;
                for (int j = a.Index + 1; j <= i; j++)
                {
                    double value = line.ValueAt(j);
                    if ((resistance && close[j] > value) || (!resistance && close[j] < value))
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok)
                    return line;
            }
            return null;
        }

        void AddLine(int i, Pivot b)
        {
            SupportResistanceLine? fresh = NewLine(i, b);
            if (fresh != null)
            {
                foreach (SupportResistanceLine old in lines)
                {
                    if (old.IsResistance == fresh.IsResistance && old.BreakIndex == null)
                    {
                        old.Done = true;
                        usedLines.Add((old.Index1, old.Index2, old.IsResistance));
                    }
                }
                lines.RemoveAll(l => l.Done);
                lines.Add(fresh);
            }
        }

        for (int i = 0; i < n; i++)
        {
            if (useZigZag)
                zigZag.Calculate(candles[i], batchProcess: true);

            if (double.IsNaN(atr[i]))
                continue;

            if (useZigZag)
            {
                // Only when the ZigZag changed: a new point, or one of the last two moved
                var list = zigZag.ZigZagList;
                int count = list.Count;
                var now = (count,
                    count > 0 ? list[^1].Candle.OpenTime : default, count > 0 ? list[^1].Value : 0,
                    count > 1 ? list[^2].Candle.OpenTime : default, count > 1 ? list[^2].Value : 0);
                if (now != signature)
                {
                    signature = now;
                    confirmed = ZigZagPivots(zigZag, indexByTime, i);
                    RebuildLevels(i);

                    // A new (or moved) top gives a new resistance line, a new bottom a new support line
                    int top = confirmed.FindLastIndex(q => q.Kind == 1);
                    if (top >= 0 && (confirmed[top].Index, confirmed[top].Price) != lastTop)
                    {
                        lastTop = (confirmed[top].Index, confirmed[top].Price);
                        lineConfirmed = confirmed.GetRange(0, top + 1);
                        AddLine(i, confirmed[top]);
                    }
                    int bottom = confirmed.FindLastIndex(q => q.Kind == -1);
                    if (bottom >= 0 && (confirmed[bottom].Index, confirmed[bottom].Price) != lastBottom)
                    {
                        lastBottom = (confirmed[bottom].Index, confirmed[bottom].Price);
                        lineConfirmed = confirmed.GetRange(0, bottom + 1);
                        AddLine(i, confirmed[bottom]);
                    }
                }
            }
            else
            {
                bool changed = false;
                // <= and not ==: a pivot confirmed during the ATR warm-up must not block every later one
                while (next < pivots.Count && pivots[next].Confirm <= i)
                {
                    confirmed.Add(pivots[next]);
                    next++;
                    changed = true;
                }
                if (changed)
                {
                    RebuildLevels(i);
                    //RebuildLines();
                }

                // A new clear top gives a new resistance line, a new clear bottom a new support line. It
                // replaces the line of the same role unless that one is in a running break.
                while (nextLine < linePivots.Count && linePivots[nextLine].Confirm <= i)
                {
                    Pivot b = linePivots[nextLine];
                    lineConfirmed.Add(b);
                    nextLine++;
                    AddLine(i, b);
                }
            }

            double brk = p.BreakAtr * atr[i];
            double ret = p.RetestAtr * atr[i];

            foreach (SupportResistanceLevel level in levels)
            {
                if (level.Retired)
                    continue;
                double price = level.Price;
                if (level.BreakIndex == null)
                {
                    if (level.State == -1 && close[i] > price + brk)
                    {
                        level.Crossings++;
                        if (level.Touches >= p.MinEventTouches)
                        {
                            level.BreakIndex = i;
                            level.BreakSide = 1;
                            level.Departed = high[i] >= price + p.DepartureAtr * atr[i];
                            Add(SupportResistanceKind.Horizontal, SupportResistanceEventType.Breakout, 1, i, price, price - p.StopAtr * atr[i], level.Touches);
                        }
                        else
                            level.State = 1;
                    }
                    else if (level.State == 1 && close[i] < price - brk)
                    {
                        level.Crossings++;
                        if (level.Touches >= p.MinEventTouches)
                        {
                            level.BreakIndex = i;
                            level.BreakSide = -1;
                            level.Departed = low[i] <= price - p.DepartureAtr * atr[i];
                            Add(SupportResistanceKind.Horizontal, SupportResistanceEventType.Breakout, -1, i, price, price + p.StopAtr * atr[i], level.Touches);
                        }
                        else
                            level.State = -1;
                    }
                    else
                    {
                        int state = close[i] > price ? 1 : -1;
                        if (state != level.State)
                            level.Crossings++;
                        level.State = state;
                    }
                }
                else
                {
                    int side = level.BreakSide;
                    if (i - level.BreakIndex.Value > p.RetestWindow)
                    {
                        level.BreakIndex = null;
                        level.State = side;
                    }
                    else if (i > level.BreakIndex.Value)
                    {
                        if ((side == 1 && close[i] < price - ret) || (side == -1 && close[i] > price + ret))
                        {
                            level.BreakIndex = null;
                            level.State = -side;
                            level.Crossings++;
                        }
                        else if (level.Departed && side == 1 && low[i] <= price + ret && close[i] > price)
                        {
                            Add(SupportResistanceKind.Horizontal, SupportResistanceEventType.Flip, 1, i, price, price - p.StopAtr * atr[i], level.Touches);
                            level.BreakIndex = null;
                            level.State = 1;
                            level.Flips++;
                        }
                        else if (level.Departed && side == -1 && high[i] >= price - ret && close[i] < price)
                        {
                            Add(SupportResistanceKind.Horizontal, SupportResistanceEventType.Flip, -1, i, price, price + p.StopAtr * atr[i], level.Touches);
                            level.BreakIndex = null;
                            level.State = -1;
                            level.Flips++;
                        }
                        else if ((side == 1 && high[i] >= price + p.DepartureAtr * atr[i]) || (side == -1 && low[i] <= price - p.DepartureAtr * atr[i]))
                            level.Departed = true;
                    }
                }
                if (level.Flips >= p.RetireFlips || level.Crossings >= p.RetireCrossings)
                    level.Retired = true;
            }

            foreach (SupportResistanceLine line in lines)
            {
                if (i <= line.Start)
                    continue;
                double value = line.ValueAt(i);
                if (line.BreakIndex == null)
                {
                    bool crossed = (line.IsResistance && close[i] > value) || (!line.IsResistance && close[i] < value);
                    if (crossed)
                    {
                        // Gone at the first close beyond it; a breakout when it was a real line and the
                        // close went far enough
                        if (line.Touches >= 3 && Math.Abs(close[i] - value) > brk)
                        {
                            int side = line.IsResistance ? 1 : -1;
                            line.BreakIndex = i;
                            line.BreakSide = side;
                            line.Departed = side == 1 ? high[i] >= value + p.DepartureAtr * atr[i] : low[i] <= value - p.DepartureAtr * atr[i];
                            Add(SupportResistanceKind.Sloped, SupportResistanceEventType.Breakout, side, i, value,
                                value - side * p.StopAtr * atr[i], line.Touches);
                        }
                        else
                            line.Done = true;
                    }
                    else if (((line.IsResistance && high[i] >= value - ret) || (!line.IsResistance && low[i] <= value + ret))
                        && i - line.LastTouch >= p.LineTouchGap)
                    {
                        line.Touches++;
                        line.LastTouch = i;
                    }
                }
                else
                {
                    int side = line.BreakSide;
                    if (i - line.BreakIndex.Value > p.RetestWindow || line.Done)
                    {
                        line.Done = true;
                        continue;
                    }
                    if (i > line.BreakIndex.Value)
                    {
                        if ((side == 1 && close[i] < value - ret) || (side == -1 && close[i] > value + ret))
                            line.Done = true;
                        else if (line.Departed && side == 1 && low[i] <= value + ret && close[i] > value)
                        {
                            Add(SupportResistanceKind.Sloped, SupportResistanceEventType.Flip, 1, i, value, value - p.StopAtr * atr[i], line.Touches);
                            line.Done = true;
                        }
                        else if (line.Departed && side == -1 && high[i] >= value - ret && close[i] < value)
                        {
                            Add(SupportResistanceKind.Sloped, SupportResistanceEventType.Flip, -1, i, value, value + p.StopAtr * atr[i], line.Touches);
                            line.Done = true;
                        }
                        else if ((side == 1 && high[i] >= value + p.DepartureAtr * atr[i]) || (side == -1 && low[i] <= value - p.DepartureAtr * atr[i]))
                            line.Departed = true;
                    }
                }
            }
            foreach (SupportResistanceLine line in lines)
            {
                if (line.Done)
                    usedLines.Add((line.Index1, line.Index2, line.IsResistance));
            }
            lines.RemoveAll(l => l.Done);
        }

        result.Levels = levels;
        result.Lines = lines;
        return result;
    }
}
