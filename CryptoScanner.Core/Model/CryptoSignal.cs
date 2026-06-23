using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Model;

[Table("Signal")]
public partial class CryptoSignal : CryptoData2
{
    [Key]
    public int Id { get; set; }

    public int ExchangeId { get; set; }
    [Computed]
    public virtual required CryptoExchange Exchange { get; set; }

    public int SymbolId { get; set; }
    [Computed]
    public virtual required CryptoSymbol Symbol { get; set; }

    public int IntervalId { get; set; }
    [Computed]
    public virtual required CryptoInterval Interval { get; set; }

    [Computed]
    public virtual required CryptoCandle? Candle { get; set; }

    public bool IsInvalid { get; set; }

    // FK to EmulatorRun (null on live signals; populated by the emulator's TickRunner via
    // GlobalData.CurrentEmulatorRunId so each run's signals can be retrieved / compared).
    public int? EmulatorRunId { get; set; }

    public DateTime OpenDate { get; set; }

    // Einde van de candle (voor sorteren in web)
    public DateTime CloseDate { get; set; }

    // Valid until.. Used by the startup query
    public DateTime ExpirationDate { get; set; }

    [Computed]
    public string DisplayText { get { return Symbol.Name + " " + Interval.Name + " signal=" + OpenDate.ToLocalTime() + " " + SideText + " " + StrategyText; } }

    public string? EventText { get; set; }

    // Optional per-signal SL distance, a positive percentage from the entry, set by strategies
    // that compute their own level (e.g. baba). Persisted, and also copied to the resulting
    // position at creation time via PositionTools.AddSignalProperties. A percentage is
    // reference-independent (works for market orders and maps straight onto Altrady); the absolute
    // stop price is derived where needed.
    public decimal? SlPercentage { get; set; }

    // In-memory only: set when the strategy supplied an explicit entry price via OverrideSignalPrice
    // (so SignalPrice is a deliberate entry level, not just the signal candle's close). The trader then
    // enters at SignalPrice instead of the current market price.
    [Computed]
    public bool EntryPriceOverridden { get; set; }

    [Computed]
    public double? PriceDiff
    {
        get
        {
            if (Symbol.LastPrice.HasValue)
                return (double)(100 * (Symbol.LastPrice / SignalPrice - 1));
            else return 0;
        }
    }

    [Computed]
    public decimal MinEntry
    {
        get
        {
            decimal minEntryValue = 0;
            if (Symbol.LastPrice.HasValue)
                minEntryValue = Symbol.QuantityMinimum * (decimal)Symbol.LastPrice;

            if (Symbol.QuoteValueMinimum > 0 && Symbol.QuoteValueMinimum > minEntryValue)
                minEntryValue = Symbol.QuoteValueMinimum;

            return minEntryValue;
        }
    }
}


