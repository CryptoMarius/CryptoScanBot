using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Emulator;

/// <summary>
/// Per-symbol queue of 1-minute candles waiting to be replayed. Populated by
/// <see cref="IndicatorWarmup.PrepareSymbol"/> with the candles inside the replay window;
/// the TickRunner pops one candle at a time, advances the clock, and feeds it to the
/// scanner pipeline. Single-symbol; multi-symbol replays compose multiple ReserveLists
/// behind a MergedFeed.
/// </summary>
public sealed class ReserveList
{
    private readonly Queue<CryptoCandle> _queue;

    public CryptoSymbol Symbol { get; }

    public ReserveList(CryptoSymbol symbol, IEnumerable<CryptoCandle> candles)
    {
        Symbol = symbol;
        _queue = new Queue<CryptoCandle>(candles);
    }

    public int RemainingCount => _queue.Count;

    public bool IsEmpty => _queue.Count == 0;

    /// <summary>Inspects the next candle without removing it. Returns false when the queue is empty.</summary>
    public bool TryPeek(out CryptoCandle candle)
    {
        if (_queue.Count == 0) { candle = default; return false; }
        candle = _queue.Peek();
        return true;
    }

    /// <summary>Removes and returns the next candle, false when the queue is empty.</summary>
    public bool TryPop(out CryptoCandle candle)
    {
        if (_queue.Count == 0) { candle = default; return false; }
        candle = _queue.Dequeue();
        return true;
    }
}
