using CryptoSbmScanner.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Model;


/// <summary>
/// Een part is 1 een gedeelte van een positie (een trade binnen een trade)
/// </summary>
[Table("PositionPart")]
public class CryptoPositionPart
{
    [Key]
    public int Id { get; set; }

    public int PositionId { get; set; }
    [Computed]
    public virtual CryptoPosition Position { get; set; }

    public int ExchangeId { get; set; }
    [Computed]
    public virtual CryptoExchange Exchange { get; set; }

    public int SymbolId { get; set; }
    [Computed]
    public virtual CryptoSymbol Symbol { get; set; }

    public CryptoOrderSide Side { get; set; }
    [Computed]
    public string SideText { get { return Side switch { CryptoOrderSide.Buy => "long", CryptoOrderSide.Sell => "short", _ => "?", }; } }

    [Computed]
    public string DisplayText { get { return Symbol.Name + " " + Position.Interval.Name + " " + CreateTime.ToLocalTime() + " " + SideText + " " + Position.StrategyText; } }

    public string Name { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? CloseTime { get; set; }

    // Globale status van de positie (new, closed, wellicht andere enum?)
    public CryptoPositionStatus? Status { get; set; }

    public decimal Invested { get; set; }
    public decimal Returned { get; set; }
    public decimal Commission { get; set; }
    public decimal Profit { get; set; }
    public decimal Percentage { get; set; }

    public decimal Quantity { get; set; }
    public decimal BreakEvenPrice { get; set; }

    
    // Buy gegevens
    public decimal BuyPrice { get; set; } //(dat kan anders zijn dan die van het signaal)
    public decimal BuyAmount { get; set; } // slecht gekozen, meer een soort van QuoteQuantity, vanwege problemen met het achteraf opzoeken hier opgenomen
    public decimal? SellPrice { get; set; }

    [Computed]
    public SortedList<int, CryptoPositionStep> Steps { get; set; } = new();

}


