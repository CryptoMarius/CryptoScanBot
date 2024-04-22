using CryptoScanBot.Core.Enums;

namespace CryptoScanBot.Core.Exchange;

public class TradeParams
{
    public CryptoPartPurpose Purpose { get; set; }
    public CryptoOrderSide OrderSide { get; set; }
    public CryptoOrderType OrderType { get; set; }
    public string OrderId { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public decimal QuoteQuantity { get; set; }
    public DateTime CreateTime { get; set; }

    // OCO of StopLimit gerelateerd
    public decimal? StopPrice { get; set; }
    public decimal? LimitPrice { get; set; }
    public string Order2Id { get; set; }

    // if error
    public System.Net.HttpStatusCode? ResponseStatusCode { get; set; }
    public CryptoExchange.Net.Objects.Error? Error { get; internal set; }
}
