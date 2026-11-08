using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Core;

/// <summary>
/// Why a chart has no candles to draw, in words the user can act on. Shared by the Avalonia chart
/// window and the Photino chart page, so both explain an empty chart the same way. The chart draws
/// from memory only (CandleList), and the scanner releases the candles of a symbol it has no use
/// for, so "no candles" is a normal state with several distinct causes.
/// </summary>
public static class ChartEmptyReason
{
    /// <summary>The heading above the explanation.</summary>
    public const string Title = "Nothing to draw here";

    /// <summary>
    /// The reason and a note on the window the chart draws from; both null when there are candles.
    /// </summary>
    /// <param name="positionWindow">The stretch of a position window (chart opened from a position),
    /// null for the live chart. In a position window the candles come from candles.db.</param>
    public static (string? Reason, string? Window) Explain(CryptoSymbol symbol, CryptoInterval interval, int candleCount,
        (CandleTime DrawFrom, CandleTime DrawTo)? positionWindow)
    {
        if (candleCount > 0)
            return (null, null);

        if (positionWindow != null)
        {
            // In a position window the candles come from candles.db, not from memory, so none of
            // the reasons below apply. Empty here means the file does not reach back that far —
            // the cleanup keeps the standard window per interval plus the lifetime of every pivot
            // zone, and this position falls outside both.
            return (
                $"candles.db holds no {interval.Name} candles for {symbol.Name} around this position. "
                + "The cleanup keeps recent history plus whatever a pivot zone still needs, and this "
                + "stretch is older than both.",
                $"Looked for {interval.Name} candles between "
                + $"{positionWindow.Value.DrawFrom.ToDateTime().ToLocalTime():dd-MM-yyyy HH:mm} and "
                + $"{positionWindow.Value.DrawTo.ToDateTime().ToLocalTime():dd-MM-yyyy HH:mm}.");
        }

        // Does the symbol have nothing at all, or only nothing on THIS interval? The two have
        // different causes: released candles versus an interval not built up yet.
        int total = 0;
        foreach (var si in symbol.Data.SymbolIntervalList)
            total += si.CandleList.Count;

        string reason;
        if (total > 0)
            reason = $"{symbol.Name} has no {interval.Name} candles in memory yet, "
                + "while other intervals do. The scanner builds this one up from the interval below it.";
        else if (symbol.QuoteData == null || !symbol.QuoteData.FetchCandles)
            reason = $"Candles for the quote coin {symbol.Quote} are switched off, "
                + $"so the scanner never fetches any for {symbol.Name}.";
        else if (symbol.Status != 1)
            reason = $"{symbol.Name} is no longer trading on {symbol.Exchange?.Name}, "
                + "so the scanner released its candles.";
        else if (!symbol.EnoughVolume() && !symbol.IsTrading())
            reason = $"{symbol.Name} is below the minimum volume for {symbol.Quote} "
                + $"({symbol.Volume:N0} against {symbol.QuoteData.MinimalVolume:N0}), "
                + "so the scanner released its candles to save memory.";
        else
            reason = $"The scanner is holding no candles for {symbol.Name} — it may still be "
                + "starting up, or this symbol was only just added.";

        // The window itself is worth naming whatever the cause: it is also the answer to
        // "why does my position from last week not show up", which is the same story one
        // step further along.
        CandleTime start = CandleTools.GetCandleFetchStart(symbol, interval, GlobalData.Clock.UtcNow);
        string window = $"The chart only draws what the scanner keeps in memory: "
            + $"{interval.Name} candles from {start.ToDateTime().ToLocalTime():dd-MM-yyyy HH:mm} onwards. "
            + "Anything older stays in candles.db and is not drawn here.";

        return (reason, window);
    }
}
