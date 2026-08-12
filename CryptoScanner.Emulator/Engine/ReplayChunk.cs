using CryptoScanner.Core.Model;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// The boundaries of one replay chunk, as pure arithmetic so they can be reasoned about (and
/// tested) without a database, a symbol or a clock.
///
/// Three separate times matter here and mixing them up has bitten this replay more than once:
/// <list type="bullet">
///   <item><see cref="From"/> — the OPEN time of the first base candle of the chunk.</item>
///   <item><see cref="LastBaseOpen"/> — the OPEN time of the LAST base candle of the chunk. The
///         replay loop runs while openTime &lt;= this value.</item>
///   <item><see cref="End"/> — the CLOSE time of that last base candle, so the moment the chunk's
///         clock actually reaches. This is where the next chunk starts, and it is how far the
///         candles have to be loaded: everything finer than the base interval keeps running up to
///         this point, not up to <see cref="LastBaseOpen"/>.</item>
/// </list>
/// </summary>
public readonly record struct ReplayChunk(CandleTime From, CandleTime LastBaseOpen, CandleTime End)
{
    /// <summary>
    /// The chunk covering <paramref name="from"/> onwards. <paramref name="chunkMinutes"/> is 0 (or
    /// the span fits in one chunk) for a single-pass replay, in which case the whole range is one
    /// chunk. The last chunk is clipped to <paramref name="replayTo"/>.
    /// </summary>
    public static ReplayChunk Resolve(CandleTime from, CandleTime replayTo, uint chunkMinutes, uint baseDuration)
    {
        bool useChunks = chunkMinutes > 0 && replayTo.Minutes - from.Minutes > chunkMinutes;

        // The last bar OPENS one base interval before from + chunkMinutes, so the next chunk starts
        // exactly on from + chunkMinutes and the boundary never drifts with the base interval.
        //
        // The same subtraction caps the very last chunk: a base candle OPENING on replayTo closes a
        // base interval later, so clipping the open time at replayTo ran the replay past its own end
        // date - one minute on a 1m base, a quarter of an hour on a 15m one. Capping the CLOSE time
        // instead makes every base interval stop on exactly the same moment.
        uint lastOpenCap = replayTo.Minutes >= baseDuration ? replayTo.Minutes - baseDuration : from.Minutes;
        CandleTime lastBaseOpen = useChunks
            ? new CandleTime(Math.Min(from.Minutes + chunkMinutes - baseDuration, lastOpenCap))
            : new CandleTime(Math.Max(lastOpenCap, from.Minutes));

        return new ReplayChunk(from, lastBaseOpen, lastBaseOpen + baseDuration);
    }

    /// <summary>
    /// First candle of an interval this chunk needs: its own boundary at or before the chunk start.
    /// The candle STRADDLING the boundary opens before it and closes inside it, so loading from
    /// <see cref="From"/> would drop it — the previous chunk ended before its close time and never
    /// handed it over either.
    /// </summary>
    public CandleTime LoadFrom(uint intervalDuration)
        => new(From.Minutes - (From.Minutes % intervalDuration));

    /// <summary>
    /// Last candle open time to load. Deliberately <see cref="End"/> and not
    /// <see cref="LastBaseOpen"/>: the replay keeps running to the close of the final base candle,
    /// so every interval finer than the base interval still has candles closing in that stretch.
    /// Loading up to LastBaseOpen left them short by one base candle's worth — on a 5m run the 1m
    /// candles of the last four minutes were simply absent, so anything reading "the newest 1m
    /// candle" saw a stale one and stamped its work minutes into the past.
    /// </summary>
    public CandleTime LoadTo => End;

    /// <summary>Where the next chunk starts — the close of this chunk's last base candle.</summary>
    public CandleTime NextFrom => End;
}
