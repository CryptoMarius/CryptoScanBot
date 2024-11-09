using CryptoScanBot.Core.Enums;
using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Model;

[Table("Trade")]
public class CryptoTrade
{
    [Key]
    public int Id { get; set; }

    public DateTime TradeTime { get; set; }

    public int TradeAccountId { get; set; }
    [Computed]
    public required CryptoAccount? TradeAccount { get; set; }

    public int ExchangeId { get; set; }
    [Computed]
    public required CryptoExchange? Exchange { get; set; }

    public int SymbolId { get; set; }
    [Computed]
    public required CryptoSymbol? Symbol { get; set; }

    public decimal Price { get; set; }
    public decimal Quantity { get; set; } // De gevulde quantity
    public decimal QuoteQuantity { get; set; } // Filled quantity * price


    public decimal Commission { get; set; }
    public string CommissionAsset { get; set; } = "";

    public string TradeId { get; set; } = "";
    public string OrderId { get; set; } = "";
}
