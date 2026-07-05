using CryptoScanner.Core.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Model;

[Table("Exchange")]
public class CryptoExchange
{
    [Key]
    public int Id { get; set; }
    public required string Name { get; set; } = string.Empty;
    public bool IsSupported { get; set; } = true;

    // Datum dat de laatste keer de exchange informatie is opgehaald
    public DateTime? LastTimeFetched { get; set; }

    public decimal FeeRate { get; set; } = 0.1m;

    public CryptoExchangeType ExchangeType { get; set; }
    public CryptoTradingType TradingType { get; set; }

    // Last candle time up to which zone invalidation (break/touch checks for DLZ/FVG/SMC)
    // and position hit checks have been performed. On scanner restart this tells us how far
    // back we need to replay candles to catch up. Null = full historical scan required.
    public CandleTime? LastZoneCheckTime { get; set; }

    // Coins indexed on id
    [Computed]
    public SortedList<int, CryptoSymbol> SymbolListId { get; } = [];

    // Scanner mapping index
    [Computed]
    public SortedList<string, CryptoSymbol> SymbolListName { get; } = [];

    // Exchange Mapping index
    [Computed]
    public SortedList<string, CryptoSymbol> SymbolListExchangeName { get; } = [];

    [Computed]
    public CryptoExchangeData Data { get; } = new();

    /// <summary>
    /// Clear symbol information (after change of exchange)
    /// </summary>
    public void Clear()
    {
        SymbolListId.Clear();
        SymbolListName.Clear();
        SymbolListExchangeName.Clear();
    }

}