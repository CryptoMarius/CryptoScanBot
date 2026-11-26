using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// Funding during a replay. The settlements of the symbols of the run are read once from the candle
/// database (fetched with the candles, EmulatorCandleSync) and booked on the open positions as the
/// clock passes them (FundingTools.Book).
/// <para>
/// Booked in the serial part of the minute loop, BEFORE phase A of the minute in which the settlement
/// falls: a position that is closed on the candle that closes at the settlement moment still pays it,
/// as on the exchange, where the close order goes out a moment after the candle closed; a position
/// opened on that candle is not charged yet. Nothing else touches the positions at that point.
/// </para>
/// </summary>
internal sealed class FundingReplay
{
    private readonly CryptoScanner.Core.Model.CryptoExchange exchange;
    private readonly List<(DateTime Time, CryptoSymbol Symbol, CryptoFundingRate Rate)> settlements = [];
    private int next;
    private int booked;
    private decimal total;

    public int SymbolsWithFunding { get; }
    public int SymbolsWithout { get; }
    public int SettlementCount => settlements.Count;


    private FundingReplay(CryptoScanner.Core.Model.CryptoExchange exchange, List<CryptoSymbol> symbols, DateTime from, DateTime to)
    {
        this.exchange = exchange;

        using var db = new CandleDatabase(exchange);
        db.Open();
        CandleDatabase.EnsureFundingTable(db.Connection);
        foreach (CryptoSymbol symbol in symbols)
        {
            if (symbol.IsBarometerSymbol())
                continue;
            List<CryptoFundingRate> rates = CandleDatabase.LoadFunding(db.Connection, symbol, from, to);
            if (rates.Count == 0)
            {
                SymbolsWithout++;
                continue;
            }
            SymbolsWithFunding++;
            foreach (CryptoFundingRate rate in rates)
                settlements.Add((rate.FundingTime, symbol, rate));
        }

        // Time first, then the symbol name: the same order on every run
        settlements.Sort((a, b) =>
        {
            int c = a.Time.CompareTo(b.Time);
            return c != 0 ? c : string.CompareOrdinal(a.Symbol.Name, b.Symbol.Name);
        });
    }


    /// <summary>
    /// The funding of this run, or null when the exchange has no funding history (spot, or an exchange
    /// whose history is not fetched). The window is the replay window; earlier settlements are skipped.
    /// </summary>
    public static FundingReplay? Create(CryptoScanner.Core.Model.CryptoExchange exchange, List<CryptoSymbol> symbols, CandleTime replayFrom, CandleTime replayTo)
    {
        if (exchange.GetApiInstance().Funding == null)
        {
            GlobalData.AddTextToLogTab($"Funding: {exchange.Name} has no funding history, no funding is booked in this run");
            return null;
        }

        // A settlement exactly on the first minute belongs to positions that cannot exist yet
        DateTime from = replayFrom.ToDateTime().AddMilliseconds(1);
        DateTime to = replayTo.ToDateTime().AddDays(1);
        FundingReplay replay = new(exchange, symbols, from, to);

        GlobalData.AddTextToLogTab($"Funding: {replay.SettlementCount} settlement(s) for {replay.SymbolsWithFunding} coin(s)"
            + (replay.SymbolsWithout > 0 ? $", {replay.SymbolsWithout} coin(s) without any (fetch the candles again to get them)" : ""));
        if (replay.SymbolsWithFunding == 0)
            GlobalData.AddErrorToLogTab("Funding: none of the coins of this run has funding in the candle database; "
                + "this run books no funding. Fetch the candles again to get it.");
        return replay;
    }


    /// <summary>
    /// Book every settlement up to and including <paramref name="upTo"/> on the open positions of its
    /// symbol. Called at the start of every replayed minute with the close time of that minute.
    /// </summary>
    public void BookUpTo(CandleTime upTo)
    {
        DateTime limit = upTo.ToDateTime();
        while (next < settlements.Count && settlements[next].Time <= limit)
        {
            var (_, symbol, rate) = settlements[next];
            next++;
            foreach (CryptoPosition position in PositionTools.PositionsOf(exchange, symbol))
            {
                decimal amount = FundingTools.Book(position, rate);
                if (amount != 0m)
                {
                    booked++;
                    total += amount;
                }
            }
        }
    }


    /// <summary>One line for the log at the end of the run.</summary>
    public string SummaryText => $"Funding: {booked} booking(s) on open positions, total {total:N2} (received positive, paid negative)";
}
