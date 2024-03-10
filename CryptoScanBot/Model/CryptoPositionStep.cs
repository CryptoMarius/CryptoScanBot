using CryptoScanBot.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Model;

/// <summary>
/// Een step is een geplaatste buy of sell order en het is onderdeel van een positiestap (een zogenaamde groep van orders)
/// De exchanges afhandeling van een order en het lokaal verwijderen van een order kruisen elkaar wel eens (daarom wordt alles bewaard)
/// </summary>
[Table("PositionStep")]
public class CryptoPositionStep
{
    [Key]
    public int Id { get; set; }
    public int PositionId { get; set; }
    public int PositionPartId { get; set; }

    public DateTime CreateTime { get; set; }
    public DateTime? CloseTime { get; set; }

    public CryptoOrderSide Side { get; set; } // (buy of sell)
    public CryptoOrderStatus Status { get; set; } // New, Filled, PartiallFilled
    public CryptoOrderType OrderType { get; set; } // (limit, stop limit, oco enz)

    public string OrderId { get; set; } // Vanwege papertrading moet deze nullable zijn
    public string Order2Id { get; set; } // Eventuele limit order

    public decimal Price { get; set; } // Tevens de LimitPrice indien het een OCO is
    public decimal Quantity { get; set; }
    public decimal? StopPrice { get; set; }
    public decimal? StopLimitPrice { get; set; }

    public decimal AveragePrice { get; set; }
    public decimal QuantityFilled { get; set; }
    public decimal QuoteQuantityFilled { get; set; } // value

    public decimal RemainingDust { get; set; } // overblijvend vanwege afrondingen

    // Of we aan het trailen zijn (de order iedere keer een beetje verzetten)
    public CryptoTrailing Trailing { get; set; }

    // De definitieve commissie van alle onderliggende trades (meerdere trades ivm market of stoplimit)
    public decimal Commission { get; set; }
    public decimal CommissionBase { get; set; }  // debug, not really relevant
    public decimal CommissionQuote { get; set; }  // debug, not really relevant
    public string CommissionAsset { get; set; }  // debug, not really relevant

    [Computed]
    public bool CancelInProgress { get; set; }
    [Computed]
    public bool IsChanged { get; set; }
}


public static class CryptoOrderStatusHelper
{
    public static bool IsFilled(this CryptoOrderStatus status)
    {
        // Het zijn nu twee statussen, vervelend..
        return (status == CryptoOrderStatus.Filled || status == CryptoOrderStatus.PartiallyAndClosed);
    }
}