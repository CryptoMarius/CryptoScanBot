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

    public bool BackTest { get; set; }
    public bool IsInvalid { get; set; }

    public DateTime OpenDate { get; set; }

    // Einde van de candle (voor sorteren in web)
    public DateTime CloseDate { get; set; }

    // Valid until.. Used by the startup query
    public DateTime ExpirationDate { get; set; }

    [Computed]
    public string DisplayText { get { return Symbol.Name + " " + Interval.Name + " signal=" + OpenDate.ToLocalTime() + " " + SideText + " " + StrategyText; } }

    // This is a not null field which is not used anymore
    public string? EventText { get; set; }

    // Optional per-signal SL/TP price set by strategies that compute their own levels
    // (e.g. swing-anchored). Not persisted: when set, copied to the resulting position
    // at creation time via PositionTools.AddSignalProperties.
    [Computed]
    public decimal? SlPrice { get; set; }
    [Computed]
    public decimal? TpPrice { get; set; }

    [Computed]
    public double? PriceDiff { get { if (Symbol.LastPrice.HasValue) return (double)(100 * (Symbol.LastPrice / SignalPrice - 1)); else return 0; } }

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


