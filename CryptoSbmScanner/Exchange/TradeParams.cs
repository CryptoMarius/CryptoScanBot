using CryptoSbmScanner.Enums;

namespace CryptoSbmScanner.Exchange;

public class TradeParams
{
    // standaard buy of sell
    public CryptoOrderSide Side { get; set; }
    public CryptoOrderType OrderType { get; set; }
    public long OrderId { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public decimal QuoteQuantity { get; set; }
    public DateTime CreateTime { get; set; }

    // OCO gerelateerd
    public decimal? StopPrice { get; set; }
    public decimal? LimitPrice { get; set; }
    public long? Order2Id { get; set; }
    //public long? OrderListId { get; set; }
}
