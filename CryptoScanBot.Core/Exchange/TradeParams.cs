using CryptoExchange.Net.Objects;
using CryptoScanBot.Core.Enums;
using System.Net;

namespace CryptoScanBot.Core.Exchange;

public class TradeParams
{
    public CryptoPartPurpose Purpose { get; set; }
    public CryptoOrderSide OrderSide { get; set; }
    public CryptoOrderType OrderType { get; set; }
    public string? OrderId { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public decimal QuoteQuantity { get; set; }
    public DateTime CreateTime { get; set; }

    // OCO of StopLimit gerelateerd
    public decimal? StopPrice { get; set; }
    public decimal? LimitPrice { get; set; }
    public string? Order2Id { get; set; }

    // if error
    public HttpStatusCode? ResponseStatusCode { get; set; }
    public Error? Error { get; internal set; }

    // Debug (full json)
    public string? DebugJson { get; set; }
}
