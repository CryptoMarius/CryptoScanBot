using CryptoScanBot.Core.Enums;
using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Model;

[Table("[Order]")]
public class CryptoOrder
{
    [Key]
    public int Id { get; set; }

    public DateTime CreateTime { get; set; }
    public DateTime UpdateTime { get; set; }

    public int TradeAccountId { get; set; }
    [Computed]
    public virtual required CryptoAccount TradeAccount { get; set; }

    public int ExchangeId { get; set; }
    [Computed]
    public virtual required CryptoExchange Exchange { get; set; }

    public int SymbolId { get; set; }
    [Computed]
    public virtual required CryptoSymbol Symbol { get; set; }

    public string OrderId { get; set; }
    public CryptoOrderSide Side { get; set; }
    public CryptoOrderType Type { get; set; }
    public CryptoOrderStatus Status { get; set; }

    public decimal? Price { get; set; }
    public decimal Quantity { get; set; }
    public decimal QuoteQuantity { get; set; }

    public decimal? AveragePrice { get; set; }
    public decimal? QuantityFilled { get; set; }
    public decimal? QuoteQuantityFilled { get; set; }

    public decimal? Commission { get; set; }
    public string? CommissionAsset { get; set; }


}
