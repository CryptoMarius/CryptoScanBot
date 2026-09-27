using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Trend;

/// <summary>Horizontal level or sloped line.</summary>
public enum SupportResistanceKind
{
    Horizontal,
    Sloped,
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

    // Walk state: which side of the level the price is on (+1 above, -1 under), and a running break
    internal int State;
    internal int? BreakIndex;
    internal int BreakSide;
}


/// <summary>
/// A sloped support/resistance line through two pivots of the same kind: a falling line through
/// two highs (resistance) or a rising line through two lows (support). Straight in log price, so the
/// slope is a percentage per candle.
/// </summary>
public sealed class SupportResistanceLine
{
    public int Index1 { get; internal set; }
    public int Index2 { get; internal set; }
    public double Price1 { get; internal set; }
    public double Price2 { get; internal set; }

    /// <summary>True for a falling line through two highs, false for a rising line through two lows.</summary>
    public bool IsResistance { get; internal set; }

    internal double LogPrice1;
    internal double LogPrice2;
    internal int? BreakIndex;
    internal int BreakSide;
    internal bool Done;

    /// <summary>The price of the line at candle <paramref name="index"/>.</summary>
    public double ValueAt(int index)
        => Math.Exp(LogPrice1 + (LogPrice2 - LogPrice1) * (index - Index1) / (Index2 - Index1));
}


/// <summary>One breakout or flip, at the close of candle <see cref="Index"/>. Touches is the number of
/// pivots in a horizontal level, and 2 for a sloped line (the two pivots it runs through).</summary>
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
        public double StopAtr { get; init; } = 0.5;
    }

    public static Parameters Default { get; } = new();

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
    {
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
        foreach (Pivot q in Pivots(high, low, p))
        {
            if (q.Confirm <= last && q.Index >= last - p.LevelLookback)
                prices.Add(q.Price);
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
    /// How much room there is between <paramref name="price"/> and the nearest level on the side of
    /// <paramref name="side"/> (under the price for a short, above it for a long), in ATRs of the
    /// last candle. Null when there is no such level (or no ATR yet): nothing is in the way.
    /// </summary>
    public static double? RoomToNextLevel(IReadOnlyList<CryptoCandle> candles, double price, CryptoTradeSide side,
        out double level, Parameters? parameters = null)
    {
        level = double.NaN;
        var levels = CurrentLevels(candles, out double atr, parameters);
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

        void Add(SupportResistanceKind kind, SupportResistanceEventType type, int side, int i, double level, double stop, int touches = 2)
            => result.Events.Add(new SupportResistanceEvent(kind, type, side == 1 ? CryptoTradeSide.Long : CryptoTradeSide.Short,
                i, candles[i].OpenTime, level, stop, touches));

        void RebuildLevels(int i)
        {
            List<double> prices = [];
            foreach (Pivot q in confirmed)
            {
                if (q.Index >= i - p.LevelLookback)
                    prices.Add(q.Price);
            }
            prices.Sort();

            double tolerance = p.ToleranceAtr * atr[i];
            List<List<double>> clusters = [];
            foreach (double price in prices)
            {
                if (clusters.Count > 0 && price - clusters[^1][^1] <= tolerance)
                    clusters[^1].Add(price);
                else
                    clusters.Add([price]);
            }

            List<SupportResistanceLevel> fresh = [];
            foreach (List<double> cluster in clusters)
            {
                if (cluster.Count < 2)
                    continue;
                double sum = 0;
                foreach (double x in cluster)
                    sum += x;
                double price = sum / cluster.Count;

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
                    fresh.Add(keep);
                }
                else
                {
                    fresh.Add(new SupportResistanceLevel
                    {
                        Price = price,
                        Touches = cluster.Count,
                        State = close[i] > price ? 1 : -1,
                    });
                }
            }
            levels = fresh;
        }

        void RebuildLines()
        {
            Pivot? high1 = null, high2 = null, low1 = null, low2 = null;
            foreach (Pivot q in confirmed)
            {
                if (q.Kind == 1)
                {
                    high1 = high2;
                    high2 = q;
                }
                else
                {
                    low1 = low2;
                    low2 = q;
                }
            }

            List<(Pivot a, Pivot b, bool resistance)> candidates = [];
            if (high1 != null && high2!.Value.Price < high1.Value.Price && high2.Value.Index - high1.Value.Index >= p.MinLineSpan)
                candidates.Add((high1.Value, high2.Value, true));
            if (low1 != null && low2!.Value.Price > low1.Value.Price && low2.Value.Index - low1.Value.Index >= p.MinLineSpan)
                candidates.Add((low1.Value, low2.Value, false));

            List<SupportResistanceLine> fresh = [];
            foreach (var (a, b, resistance) in candidates)
            {
                if (usedLines.Contains((a.Index, b.Index, resistance)))
                    continue;
                SupportResistanceLine? existing = lines.Find(l => l.Index1 == a.Index && l.Index2 == b.Index && l.IsResistance == resistance);
                fresh.Add(existing ?? new SupportResistanceLine
                {
                    Index1 = a.Index,
                    Index2 = b.Index,
                    Price1 = a.Price,
                    Price2 = b.Price,
                    LogPrice1 = Math.Log(a.Price),
                    LogPrice2 = Math.Log(b.Price),
                    IsResistance = resistance,
                });
            }
            // A line in a running break keeps its retest window
            foreach (SupportResistanceLine line in lines)
            {
                if (line.BreakIndex != null && !fresh.Contains(line))
                    fresh.Add(line);
            }
            lines = fresh;
        }

        for (int i = 0; i < n; i++)
        {
            if (double.IsNaN(atr[i]))
                continue;

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
                RebuildLines();
            }

            double brk = p.BreakAtr * atr[i];
            double ret = p.RetestAtr * atr[i];

            foreach (SupportResistanceLevel level in levels)
            {
                double price = level.Price;
                if (level.BreakIndex == null)
                {
                    if (level.State == -1 && close[i] > price + brk)
                    {
                        level.BreakIndex = i;
                        level.BreakSide = 1;
                        Add(SupportResistanceKind.Horizontal, SupportResistanceEventType.Breakout, 1, i, price, price - p.StopAtr * atr[i], level.Touches);
                    }
                    else if (level.State == 1 && close[i] < price - brk)
                    {
                        level.BreakIndex = i;
                        level.BreakSide = -1;
                        Add(SupportResistanceKind.Horizontal, SupportResistanceEventType.Breakout, -1, i, price, price + p.StopAtr * atr[i], level.Touches);
                    }
                    else
                        level.State = close[i] > price ? 1 : -1;
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
                        }
                        else if (side == 1 && low[i] <= price + ret && close[i] > price)
                        {
                            Add(SupportResistanceKind.Horizontal, SupportResistanceEventType.Flip, 1, i, price, price - p.StopAtr * atr[i], level.Touches);
                            level.BreakIndex = null;
                            level.State = 1;
                        }
                        else if (side == -1 && high[i] >= price - ret && close[i] < price)
                        {
                            Add(SupportResistanceKind.Horizontal, SupportResistanceEventType.Flip, -1, i, price, price + p.StopAtr * atr[i], level.Touches);
                            level.BreakIndex = null;
                            level.State = -1;
                        }
                    }
                }
            }

            foreach (SupportResistanceLine line in lines)
            {
                if (i <= line.Index2)
                    continue;
                double value = line.ValueAt(i);
                if (line.BreakIndex == null)
                {
                    if (line.IsResistance && close[i] > value + brk)
                    {
                        line.BreakIndex = i;
                        line.BreakSide = 1;
                        Add(SupportResistanceKind.Sloped, SupportResistanceEventType.Breakout, 1, i, value, value - p.StopAtr * atr[i]);
                    }
                    else if (!line.IsResistance && close[i] < value - brk)
                    {
                        line.BreakIndex = i;
                        line.BreakSide = -1;
                        Add(SupportResistanceKind.Sloped, SupportResistanceEventType.Breakout, -1, i, value, value + p.StopAtr * atr[i]);
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
                        else if (side == 1 && low[i] <= value + ret && close[i] > value)
                        {
                            Add(SupportResistanceKind.Sloped, SupportResistanceEventType.Flip, 1, i, value, value - p.StopAtr * atr[i]);
                            line.Done = true;
                        }
                        else if (side == -1 && high[i] >= value - ret && close[i] < value)
                        {
                            Add(SupportResistanceKind.Sloped, SupportResistanceEventType.Flip, -1, i, value, value + p.StopAtr * atr[i]);
                            line.Done = true;
                        }
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
