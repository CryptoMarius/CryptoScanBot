using CryptoScanBot.Enums;
using CryptoScanBot.Signal;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Model;


/// <summary>
/// Een part is een gedeelte van een positie (een trade binnen een trade)
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

    public CryptoPartPurpose Purpose { get; set; }
    public CryptoSignalStrategy Strategy { get; set; }

    public int PartNumber { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? CloseTime { get; set; }

    public decimal Invested { get; set; }
    public decimal Returned { get; set; }
    public decimal Commission { get; set; }
    public decimal Reserved { get; set; }
    public decimal Profit { get; set; }
    public decimal Percentage { get; set; }

    public decimal Quantity { get; set; }
    public decimal QuantityEntry { get; set; }
    public decimal QuantityTakeProfit { get; set; }
    public decimal BreakEvenPrice { get; set; }
    public decimal RemainingDust { get; set; } // overblijvend vanwege afrondingen
    // Even een experiment
    public decimal CommissionBase { get; set; }
    public decimal CommissionQuote { get; set; }

    // De initiele entry prijs van het signaal
    // (wordt gebruikt voor data overdracht)
    public decimal SignalPrice { get; set; } 

    // De bijkoop methode -> EntryMethod (zou wellicht uit de actuele instellingen gehaald kunnen worden)
    public CryptoEntryOrProfitMethod EntryMethod  { get; set; }
    // De verkoop methode -> ProfitMethod (zou wellicht uit de actuele instellingen gehaald kunnen worden)
    public CryptoEntryOrProfitMethod ProfitMethod { get; set; }

    // Eigenlijk zijn er maar 2 steps in een deelpositie die van belang zijn?
    // Helaas dat gaat niet 100% op, want die kunnen verdeeld zijn in meerdere orders
    // De entry orders (bijvoorbeeld 50% buy op de bid prijs en 50% op de ask prijs)
    // De takeprofit(s) (bijvoorbeeld 33% op 1%, 33% op 2% en rest op 3%)

    // Administratie voor het geval we handmatig een order openen (en we deze niet willen laten aanpassen)
    public bool ManualOrder { get; set; }

    [Computed]
    public bool IsChanged { get; set; }

    [Computed]
    public SortedList<int, CryptoPositionStep> Steps { get; set; } = [];
}


