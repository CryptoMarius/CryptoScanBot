namespace CryptoScanner.Core.Model;

/// <summary>
/// The periods that were requested from the exchange for one (symbol, interval), kept as a sorted
/// list of non-overlapping ranges that are merged as soon as they touch.
///
/// <para>
/// It exists because the candles cannot answer the question themselves. The zone engine decides
/// whether it still has to fetch by walking the series and looking for the first missing candle, but
/// an exchange that only produces a candle for a minute in which something was traded leaves holes
/// that never fill. The walk finds the same hole on every recalculation and downloads the same
/// history again - measured on Bitvavo Spot 19-08-2026: 255 requests, 203,471 candles, 7% of them new.
/// What the candles cannot say, this list says: this period was requested at least once, so a candle
/// missing inside it is missing at the exchange as well.
/// </para>
///
/// <para>
/// A LIST, not one period, because the requests do not arrive in order: a DLZ recalculation first
/// pulls the deep history of its own interval and then zooms in around every dominant pivot, each
/// zoom asking for its own window somewhere in the past. With a single period every zoom would throw
/// away what the previous one established, and nothing would be remembered at all. Ranges that do not
/// touch stay separate, so two windows never add up to a claim about the gap between them.
/// </para>
///
/// <para>
/// Deliberately NOT persisted: after a restart it is empty and the whole history is checked once
/// more, which is the escape hatch if it is ever wrong. Whoever removes candles from the store
/// shortens it - see CandleDatabase.CleanCandlesForSymbol.
/// </para>
/// </summary>
public class HistoryAskedRanges
{
    private readonly List<(CandleTime From, CandleTime To)> ranges = [];

    /// <summary>Number of separate periods remembered (for tests and diagnostics).</summary>
    public int Count
    {
        get
        {
            lock (ranges)
                return ranges.Count;
        }
    }


    /// <summary>
    /// True when one single remembered period covers the whole of [from..to], so asking the exchange
    /// again cannot turn up anything: what is missing inside it is missing at the exchange too.
    /// </summary>
    public bool WasAsked(CandleTime from, CandleTime to)
    {
        lock (ranges)
        {
            foreach (var range in ranges)
            {
                if (from >= range.From && to <= range.To)
                    return true;
            }
            return false;
        }
    }


    /// <summary>
    /// The moment from which a search still has to look. Everything between <paramref name="from"/>
    /// and the returned moment was requested before, so examining it again cannot turn up anything.
    /// Returns <paramref name="from"/> unchanged when nothing is remembered about that moment.
    /// <para>
    /// This is what makes it usable hour after hour: the period the zone engine asks for slides
    /// forward with the clock, so it never fits inside what is remembered completely. Only its tail
    /// is new, and this says where that tail begins.
    /// </para>
    /// </summary>
    public CandleTime SkipAsked(CandleTime from)
    {
        lock (ranges)
        {
            foreach (var range in ranges)
            {
                if (from >= range.From && from <= range.To)
                    return range.To;
            }
            return from;
        }
    }


    /// <summary>
    /// Remember that [from..to] was requested from the exchange. Merged with the periods it touches
    /// or overlaps; a period that connects to nothing is added as a separate one, because the stretch
    /// in between was never requested.
    /// </summary>
    public void Remember(CandleTime from, CandleTime to)
    {
        if (to < from)
            return;

        lock (ranges)
        {
            int index = 0;
            while (index < ranges.Count)
            {
                var range = ranges[index];

                // Entirely before the new period and not touching it: keep it and move on.
                if (range.To < from)
                {
                    index++;
                    continue;
                }

                // Entirely after the new period and not touching it: this is where the new one goes.
                if (range.From > to)
                    break;

                // Touching or overlapping: swallow it and widen the new period.
                if (range.From < from)
                    from = range.From;
                if (range.To > to)
                    to = range.To;
                ranges.RemoveAt(index);
            }

            ranges.Insert(index, (from, to));
        }
    }


    /// <summary>
    /// Candles up to and including <paramref name="newestRemoved"/> were removed from the store, so
    /// everything up to that moment is unknown again and has to be fetched when it is needed - which
    /// is exactly what has to happen once the candles are gone. What is remembered can only ever get
    /// shorter this way, never longer, so "never fetched again" cannot happen.
    /// </summary>
    public void ForgetUpTo(CandleTime newestRemoved, uint duration)
    {
        CandleTime resumeFrom = newestRemoved + duration;

        lock (ranges)
        {
            int index = 0;
            while (index < ranges.Count)
            {
                var range = ranges[index];
                if (range.To < resumeFrom)
                {
                    ranges.RemoveAt(index);
                    continue;
                }

                if (range.From < resumeFrom)
                    ranges[index] = (resumeFrom, range.To);
                index++;
            }
        }
    }


    /// <summary>Nothing is known about what was asked any more (the candles were thrown away).</summary>
    public void Clear()
    {
        lock (ranges)
            ranges.Clear();
    }


    /// <summary>The remembered periods, oldest first. For tests and diagnostics.</summary>
    public List<(CandleTime From, CandleTime To)> ToList()
    {
        lock (ranges)
            return [.. ranges];
    }
}
