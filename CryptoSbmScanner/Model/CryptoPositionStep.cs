using CryptoSbmScanner.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Model;

/// <summary>
/// Een step is een geplaatste order en onderdeel van een positiestap
/// </summary>
[Table("PositionStep")]
public class CryptoPositionStep
{
    [Key]
    public int Id { get; set; }
    public int PositionId { get; set; }
    public int PositionPartId { get; set; }

    public string Name { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? CloseTime { get; set; }

    public CryptoOrderStatus Status { get; set; } // New, Filled, PartiallFilled
    // TODO: Renamen naar Side?
    public CryptoOrderSide Side { get; set; } // (buy of sell)
    // TODO: Renamen naar Type?
    public CryptoOrderType OrderType { get; set; } // (limit, stop limit, oco enz)

    public decimal Price { get; set; } // Tevens de LimitPrice indien het een OCO is
    public decimal? StopPrice { get; set; }
    public decimal? StopLimitPrice { get; set; }
    // Ik heb een soort van AvgPrice nodig (bij meerdere trades igv market of stoplimit orders)

    public decimal Quantity { get; set; }
    public decimal QuantityFilled { get; set; }
    public decimal QuoteQuantityFilled { get; set; }

    public long? OrderId { get; set; } // Vanwege papertrading moet deze nullable zijn
    public long? Order2Id { get; set; } // Eventuele limit order

    [Computed]
    public decimal Commission { get; set; }

    // De gemiddelde prijs dat het gekocht of verkocht is (meerdere trades ivm market of stoplimit)
    [Computed]
    public decimal AvgPrice { get; set; } 

    // Emulator
    public CryptoTrailing Trailing { get; set; }


    // Bug bestrijding: vanwege dubbele afhandeling - TODO: Opsporen en deze verwijderen, gaat het via db wel goed?
    // We handelen de market order nu direct af, dus wellicht is het nu ook opgelost? (weet ik niet zeker, testen)
    [Computed]
    public bool TradeHandled { get; set; } 

}