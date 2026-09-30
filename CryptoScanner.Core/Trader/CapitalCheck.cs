namespace CryptoScanner.Core.Trader;

/// <summary>
/// Afterwards, from the positions alone: did the money tied up in open positions ever exceed what
/// the account had at that moment?
/// <para>
/// This asks the question the run result is judged on, and it asks it WITHOUT the reservation model
/// in PaperAssets - that is the point. Both leaks found on 30-09-2026 sat inside that model (a short
/// that freed money, a DCA ladder nobody held back), and a check built on the same model inherits its
/// blind spots. Run 1765 stood at a peak of 30.266 on 20.000 of capital and nothing compared the two.
/// </para>
/// <para>
/// A position counts with its Invested (entry plus the DCA levels that filled) from the moment it
/// opened until it closed, and its profit joins the capital when it closes. Invested is at most what
/// the entry check had to approve (entry plus the whole ladder), so a peak above the capital means
/// the checks let through more than there was - it cannot be a false alarm from counting too much.
/// </para>
/// </summary>
public static class CapitalCheck
{
    public readonly record struct Position(string Quote, DateTime Open, DateTime? Close, decimal Invested, decimal Profit);

    public readonly record struct Breach(string Quote, DateTime Moment, decimal Committed, decimal Available);


    /// <summary>
    /// The worst moment per quote coin at which the open positions held more than the start capital
    /// plus the profit realised so far; quote coins that stayed within it are not listed. One percent
    /// of the available money is tolerated, for the same reason as PaperAssets.WarnWhenOvercommitted.
    /// </summary>
    public static List<Breach> Find(IEnumerable<Position> positions, decimal startCapitalPerQuote)
    {
        List<Breach> breaches = [];
        foreach (var quote in positions.Where(p => p.Invested > 0).GroupBy(p => p.Quote))
        {
            // Closes before opens at the same moment: money freed in a minute can be reused in it
            var events = new List<(DateTime Moment, int Order, decimal InvestedChange, decimal ProfitChange)>();
            foreach (Position p in quote)
            {
                events.Add((p.Open, 1, p.Invested, 0m));
                if (p.Close.HasValue)
                    events.Add((p.Close.Value, 0, -p.Invested, p.Profit));
            }

            decimal committed = 0m;
            decimal realised = 0m;
            Breach? worst = null;
            foreach (var e in events.OrderBy(e => e.Moment).ThenBy(e => e.Order))
            {
                committed += e.InvestedChange;
                realised += e.ProfitChange;
                decimal available = startCapitalPerQuote + realised;
                decimal tolerance = Math.Max(1m, available / 100m);
                if (committed > available + tolerance
                    && (worst == null || committed - available > worst.Value.Committed - worst.Value.Available))
                    worst = new Breach(quote.Key, e.Moment, committed, available);
            }
            if (worst != null)
                breaches.Add(worst.Value);
        }
        return breaches;
    }
}
