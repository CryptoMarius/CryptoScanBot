using Binance.Net.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Model;

public enum CryptoTrailing
{
    TrailNone,
    TrailWaiting,
    TrailActive
}

public enum CryptoOrderType
{
    Market,             // Het "beste" bod van de markt
    Limit,              // Een standaard order
    StopLimit,          // Een stoplimit order
    Oco,                // OCO's alleen op Binance
    NotOnMarketYet      // Trailing buy orders worden pas geactiveerd als het onder een bepaalde grens is
}

public enum LockingMethod
{
    FixedValue,      // Bij een trailing buy krijg je meer munten naar mate de prijs daalt (fixed).
    FixedQuantity    // Bij een trailing sell wil je van alle munten af (en krijg je meer geld)
}

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

    public OrderStatus Status { get; set; } // New, Filled, PartiallFilled
    public CryptoTradeDirection Mode { get; set; } // (buy of sell)
    public CryptoOrderType OrderType { get; set; } // (limit, stop limit, oco enz)

    public decimal Price { get; set; } // Tevens de LimitPrice indien het een OCO is
    public decimal? StopPrice { get; set; }
    public decimal? StopLimitPrice { get; set; }

    public decimal Quantity { get; set; }
    public decimal QuantityFilled { get; set; }
    public decimal QuoteQuantityFilled { get; set; }

    public long? OrderId { get; set; } // Vanwege papertrading moet deze nullable zijn
    public long? Order2Id { get; set; } // Eventuele limit order
                                        //public long? OrderListId { get; set; } // overbodig, geen idee wat Binance daarmee bedoeld

    // Emulator
    [Computed]
    // De instap waarde, bij het trailen wordt de value en lockingmethod gebruikt 
    // om de nieuw quantity te beredeneren adhv de prijs. 
    public decimal QuoteQuantityInitial { get; set; }

    
    // Emulator
    [Computed]
    // De instap waarde, bij het trailen wordt de value en lockingmethod gebruikt 
    // om de nieuw quantity te beredeneren adhv de prijs. 
    public int FromDcaIndex { get; set; }


    // Emulator
    [Computed]
    // Of bij een trailing order de Quantity herberekend moet worden
    public LockingMethod LockingMethod { get; set; }

    // Emulator
    [Computed]
    public CryptoTrailing Trailing { get; set; }

    // Emulator
    [Computed]
    // Prijs waarop de trail actief wordt?
    public decimal? TrailActivatePrice { get; set; }


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