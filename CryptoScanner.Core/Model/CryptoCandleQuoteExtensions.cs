using Skender.Stock.Indicators;

namespace CryptoScanner.Core.Model;

/// <summary>
/// Bridges CryptoCandle to Skender.Stock.Indicators v3. The v3 indicator methods take a NON-generic
/// <c>IEnumerable&lt;IQuote&gt;</c> (the generic <c>&lt;TQuote&gt;</c> overloads were removed). CryptoCandle
/// is a <c>struct</c>, and <c>IEnumerable&lt;T&gt;</c> covariance only applies to reference types, so a
/// <c>List&lt;CryptoCandle&gt;</c> is NOT an <c>IEnumerable&lt;IQuote&gt;</c> — it has to be boxed.
/// <see cref="AsQuotes"/> boxes the window ONCE so a whole batch of GetXxx() calls can reuse the result
/// instead of boxing per call.
/// </summary>
public static class CryptoCandleQuoteExtensions
{
    public static IReadOnlyList<IQuote> AsQuotes(this IEnumerable<CryptoCandle> candles)
    {
        var list = candles is IReadOnlyCollection<CryptoCandle> c ? new List<IQuote>(c.Count) : [];
        foreach (CryptoCandle candle in candles)
            list.Add(candle); // boxes the struct to IQuote
        return list;
    }
}
