using CryptoScanner.Core.Context;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// Materialises a candle window for the emulator. Reads from the per-exchange candles.db
/// in the emulator's data folder (set via <c>--folder</c>). The split is up to the caller:
/// candles before the replay-start are typically pushed into the symbol's CandleList so
/// indicators warm up correctly, candles inside the replay range are popped one-by-one by
/// the TickRunner.
///
/// REST fallback for missing candles is intentionally not implemented here — that belongs
/// in a separate pre-flight step that fills the candles.db before the run begins, so the
/// engine itself stays I/O-free.
/// </summary>
public static class CandleSource
{
    /// <summary>
    /// Loads all candles for the given symbol+interval whose <see cref="CandleTime.Minutes"/>
    /// fall inside [<paramref name="from"/>, <paramref name="to"/>] (both inclusive).
    /// Returned list is ascending by OpenTime. Empty if no candles exist in the range.
    /// </summary>
    public static List<CryptoCandle> Load(CryptoSymbol symbol, CryptoInterval interval,
        CandleTime from, CandleTime to)
    {
        if (to.Minutes < from.Minutes)
            return [];

        using var db = new CandleDatabase(symbol.Exchange);
        db.Open();
        return CandleDatabase.LoadCandlesInRange(db.Connection, symbol, interval, from.Minutes, to.Minutes);
    }

    // LoadSplit (warmup prefix plus replay window in one pass) was removed on 31-10-2026: nothing
    // called it any more, the warmup and the chunked replay each load their own window.
}
