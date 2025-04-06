using CryptoScanBot.Core.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Model;

[Table("Exchange")]
public class CryptoExchange
{
    [Key]
    public int Id { get; set; }
    public required string Name { get; set; }
    public bool IsSupported { get; set; }

    // Datum dat de laatste keer de exchange informatie is opgehaald
    public DateTime? LastTimeFetched { get; set; }

    public decimal FeeRate { get; set; }

    public CryptoExchangeType ExchangeType { get; set; }
    public CryptoTradingType TradingType { get; set; }

    // Coins indexed on id
    [Computed]
    public SortedList<int, CryptoSymbol> SymbolListId { get; } = [];

    // Coins indexed on name
    [Computed]
    public SortedList<string, CryptoSymbol> SymbolListName { get; } = [];
    
    [Computed]
    public CryptoExchangeData Data { get; } = new();

    /// <summary>
    /// Clear symbol information (after change of exchange)
    /// </summary>
    public void Clear()
    {
        SymbolListId.Clear();
        SymbolListName.Clear();
    }

}