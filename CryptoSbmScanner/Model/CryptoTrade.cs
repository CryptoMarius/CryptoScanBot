using CryptoSbmScanner.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Model;

[Table("Trade")]
public class CryptoTrade
{
    [Key]
    public int Id { get; set; }

    public DateTime TradeTime { get; set; }

    public int TradeAccountId { get; set; }
    [Computed]
    public virtual CryptoTradeAccount TradeAccount { get; set; }

    public int ExchangeId { get; set; }
    [Computed]
    public virtual CryptoExchange Exchange { get; set; }

    public int SymbolId { get; set; }
    [Computed]
    public virtual CryptoSymbol Symbol { get; set; }

    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public decimal QuoteQuantity { get; set; }
    

    public decimal Commission { get; set; }
    public string CommissionAsset { get; set; }

    public long TradeId { get; set; }
    public long OrderId { get; set; }

    public CryptoOrderSide Side { get; set; }
}
