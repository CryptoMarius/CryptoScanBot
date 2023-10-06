using CryptoSbmScanner.Enums;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Model;

[Table("SymbolInterval")]
public class CryptoSymbolInterval
{
    [Key]
    public int Id { get; set; }
    public int ExchangeId { get; set; }
    public int SymbolId { get; set; }
    public int IntervalId { get; set; }
    [Computed]
    public virtual CryptoInterval Interval { get; set; }

    public CryptoIntervalPeriod IntervalPeriod { get; set; }

    [Computed]
    public bool IsChanged { get; set; }

    // De laatste datum dat de candles aansluiten c.q. zijn gesynchroniseerd met de exchange
    private long? _Date;
    public long? LastCandleSynchronized
	{
        get => _Date; set
        {
            IsChanged = true;
            _Date = value;
        }
    }

    // De laatst berekende trend
    public CryptoTrendIndicator TrendIndicator { get; set; }
    public DateTime? TrendInfoDate { get; set; }

    [Computed]
    public CryptoSignal Signal { get; set; }

    [Computed]
    public DateTime? LastStobbOrdSbmDate { get; set; }

    // De candles voor dit interval
    [Computed]
    public SortedList<long, CryptoCandle> CandleList { get; set; } = new();
}
