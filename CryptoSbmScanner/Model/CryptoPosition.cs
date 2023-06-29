using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Signal;

using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Model;


/// <summary>
/// Een position is 1 een samenvatting van 1 of meerdere orders
/// </summary>
[Table("Position")]
public class CryptoPosition
{
    [Key]
    public int Id { get; set; }
    public DateTime CreateTime { get; set; }

    public int TradeAccountId { get; set; }
    [Computed]
    public virtual CryptoTradeAccount TradeAccount { get; set; }

    public int ExchangeId { get; set; }
    [Computed]
    public virtual CryptoExchange Exchange { get; set; }

    public int SymbolId { get; set; }
    [Computed]
    public virtual CryptoSymbol Symbol { get; set; }

    public int? IntervalId { get; set; }
    [Computed]
    public virtual CryptoInterval Interval { get; set; }

    public CryptoOrderSide Side { get; set; }
    [Computed]
    public string SideText { get { return Side switch { CryptoOrderSide.Buy => "long", CryptoOrderSide.Sell => "short", _ => "?", }; } }

    [Computed]
    public string DisplayText { get { return Symbol.Name + " " + Interval.Name + " " + CreateTime.ToLocalTime() + " " + SideText + " " + StrategyText; } }

    public CryptoSignalStrategy Strategy { get; set; }
    [Computed]
    public string StrategyText { get { return SignalHelper.GetSignalAlgorithmText(Strategy); } }

    // Om in de database de bron te kunnen zien
    //public bool BackTest { get; set; } = false;    
    //public bool PaperTrade { get; set; } = false;

    // Globale status van de positie (new, closed, wellicht andere enum?)
    public CryptoPositionStatus? Status { get; set; }

    public decimal Invested { get; set; }
    public decimal Returned { get; set; }
    public decimal Commission { get; set; }
    public decimal Profit { get; set; }
    public decimal Percentage { get; set; }

    public decimal Quantity { get; set; }
    public decimal BreakEvenPrice { get; set; }

    // Slonzige gegevens, deze 3 mogen wat mij betreft weg, staat allemaal in de steps (ooit)
    public decimal? BuyPrice { get; set; } //(dat kan anders zijn dan die van het signaal) (kan eigenlijk weg, slechts ter debug)
    public decimal? BuyAmount { get; set; } // slecht gekozen, meer een soort van QuoteQuantity... // Vanwege problemen met het achteraf opzoeken hier opgenomen
    public decimal? SellPrice { get; set; }


    public DateTime? CloseTime { get; set; }

    // Een experiment (die wellicht wegkan)
    public string Data { get; set; }

    // Soort van Parts.Count (maar dan hoeft niet alles geladen te zijn)
    public int PartCount { get; set; }

    [Computed]
    public bool RepositionSell { get; set; }

    [Computed]
    public SortedList<int, CryptoPositionPart> Parts { get; set; } = new();

    [Computed]
    // Orders die uitstaan via de parts/steps
    public SortedList<long, CryptoPositionStep> Orders { get; set; } = new();
}