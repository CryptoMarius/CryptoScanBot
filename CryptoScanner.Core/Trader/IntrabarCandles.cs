using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Trader;

/// <summary>
/// Supplies the finer candles that sit inside one base-interval candle, so paper trading can
/// establish the ORDER in which price levels were touched instead of guessing.
///
/// A candle gives four numbers and no path between them. When a single candle touches two of a
/// position's levels — say the entry limit and the take profit — it cannot say which came first,
/// and the two possible sequences produce opposite outcomes: entry then take profit is a closed
/// winner, take profit then entry leaves the position open at a loss. At a 1m base interval that
/// ambiguity is rare; at 15m it is common enough to move the result of a whole run.
///
/// The finest available resolution is loaded in ONE query rather than descending the ConstructFrom
/// chain step by step. Candle is a WITHOUT ROWID table keyed on (SymbolId, IntervalId, OpenTime),
/// so reading a window is a B-tree seek plus a short sequential scan — the seek dominates, and 15
/// rows cost practically the same as 3. Descending stepwise would pay that seek two or three times
/// over to avoid rows that were never the expensive part.
/// </summary>
public static class IntrabarCandles
{
    /// <summary>
    /// Loads the candles of the finest interval that COMPLETELY covers the given base candle.
    /// Completeness is required: a window with a gap in it hides price action, and acting on a
    /// partial view is worse than falling back to a coarser but complete one. Returns false when
    /// no finer interval has full coverage — the caller then keeps its existing behaviour.
    /// </summary>
    public static bool TryLoad(CryptoSymbol symbol, uint baseDuration, CandleTime openTime,
        out List<CryptoCandle> candles, out uint finerDuration)
    {
        candles = [];
        finerDuration = 0;
        if (baseDuration <= 1)
            return false;

        // Candidates: every interval strictly finer than the base that divides it evenly, finest
        // first. The even division keeps the sub-candles aligned to the base candle's boundaries;
        // 2m inside a 15m candle would straddle them, so it is not a candidate.
        List<CryptoInterval> candidates = [];
        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            if (interval.Duration < baseDuration && baseDuration % interval.Duration == 0)
                candidates.Add(interval);
        }
        if (candidates.Count == 0)
            return false;
        candidates.Sort((a, b) => a.Duration.CompareTo(b.Duration));

        CandleTime windowEnd = openTime + baseDuration;
        using var db = new CandleDatabase(symbol.Exchange);
        db.Open();

        foreach (CryptoInterval interval in candidates)
        {
            // LoadCandlesInRange takes an inclusive upper bound, so stop one candle short of the
            // next base candle's open time.
            List<CryptoCandle> found = CandleDatabase.LoadCandlesInRange(db.Connection, symbol, interval,
                openTime.Minutes, (windowEnd - interval.Duration).Minutes);

            int expected = (int)(baseDuration / interval.Duration);
            if (found.Count == expected)
            {
                candles = found;
                finerDuration = interval.Duration;
                return true;
            }
        }

        return false;
    }
}
