using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Signal;

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

    public int? IntervalId { get; set; }
    [Computed]
    public CryptoInterval Interval { get; set; }

    public CryptoOrderSide Side { get; set; }
    [Computed]
    public string SideText { get { return Side switch { CryptoOrderSide.Buy => "long", CryptoOrderSide.Sell => "short", _ => "?", }; } }

    [Computed]
    public string DisplayText { get { return Symbol.Name + " " + Position.Interval.Name + " " + CreateTime.ToLocalTime() + " " + SideText + " " + StrategyText; }}

    public CryptoSignalStrategy Strategy { get; set; }
    [Computed]
    public string StrategyText { get { return Strategy.ToString(); } }

    public string Name { get; set; }
    public int PartNumber { get; set; } // En dan kan de Name vervallen (BUY/SELL = 1, DCA = PartNumber >= 2) - of null based, whatever
    public DateTime CreateTime { get; set; }
    public DateTime? CloseTime { get; set; }

    public decimal Invested { get; set; }
    public decimal Returned { get; set; }
    public decimal Commission { get; set; }
    public decimal Profit { get; set; }
    public decimal Percentage { get; set; }

    public decimal Quantity { get; set; }
    public decimal BreakEvenPrice { get; set; }
    
    // Buy gegevens
    public decimal SignalPrice { get; set; } // initiele prijs van het signaal (data overdracht)

    public CryptoStepInMethod StepInMethod  { get; set; } // De reden van bijkoop
    public CryptoStepInMethod StepOutMethod { get; set; } // De reden van verkoop

    // Eigenlijk zijn er maar 2 steps in een deelpositie die van belang zijn?
    // Helaas dat gaat niet 100% op, want die kunnen verdeeld zijn in meerdere orders
    // De buy (probleem, 50% buy op de bid prijs en 50% op de ask prijs)
    // De sell (probleem, hoe doe je dat als je meerdere TP's gebruikt?)

    [Computed]
    public SortedList<int, CryptoPositionStep> Steps { get; set; } = new();
}


