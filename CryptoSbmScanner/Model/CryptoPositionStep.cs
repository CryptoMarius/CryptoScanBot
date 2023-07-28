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

    public decimal Quantity { get; set; }
    public decimal QuantityFilled { get; set; }
    public decimal QuoteQuantityFilled { get; set; }

    public long? OrderId { get; set; } // Vanwege papertrading moet deze nullable zijn
    public long? Order2Id { get; set; } // Eventuele limit order

    [Computed]
    public decimal Commission { get; set; }

    // Emulator
    //[Computed]
    // De instap waarde, bij het trailen wordt de value en lockingmethod gebruikt 
    // om de nieuw quantity te beredeneren adhv de prijs. 
    //public decimal QuoteQuantityInitial { get; set; }


    // Emulator
    //[Computed]
    // De instap waarde, bij het trailen wordt de value en lockingmethod gebruikt 
    // om de nieuw quantity te beredeneren adhv de prijs. 
    //public int FromDcaIndex { get; set; }


    // Emulator
    //[Computed]
    // Of bij een trailing order de Quantity herberekend moet worden
    //public LockingMethod LockingMethod { get; set; }

    // Emulator
    public CryptoTrailing Trailing { get; set; }

    // Emulator
    //[Computed]
    // Prijs waarop de trail actief wordt?
    //public decimal? TrailActivatePrice { get; set; }


    [Computed]
    public bool TradeHandled { get; set; } // Bug bestreiding: vanwege dubbele afhandeling - TODO: Opsporen en deze verwijderen, gaat het via db wel goed?

    //public string DisplayText(string priceFormat)
    //{
    //    // Verdorie, de datum's missen nog.. ;-)

    //    string s = string.Format("step#{0} order#{1} {2} ({3}) Price={4} StopPrice={5} StopLimitPrice={6} Quantity={7} QuantityFilled={8} QuoteQuantityFilled={9}",
    //        Id, OrderId, Mode, OrderType,
    //        Price.ToString(priceFormat), StopPrice?.ToString(priceFormat), StopLimitPrice?.ToString(priceFormat),
    //        Quantity, QuantityFilled, QuoteQuantityFilled);

    //    //if (Trailing > CryptoTrailing.TrailNone)
    //    //    s += string.Format(" Trailing={0} @={1}", Trailing, TrailActivatePrice?.ToString(format));

    //    return s;
    //}

    //public string Short()
    //{
    //    return string.Format("{0} order#{1}", OrderType, OrderId);
    //}
}