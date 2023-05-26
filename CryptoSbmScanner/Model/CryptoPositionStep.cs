using Binance.Net.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Model;


// Experiment....
public enum CryptoOrderSide
{
    Sell,
    Buy
}

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

public enum CryptoOrderXXXX
{
    Market,             // Het "beste" bod van de markt
    Limit,              // Een standaard order
    LimitMaker,         // De positieve kant van de stoplimit / OCO
    StopLossLimit       // De negatieve kant van de stoplimit / OCO
}

public enum LockingMethod
{
    FixedValue,      // Bij een trailing buy krijg je meer munten naar mate de prijs daalt (fixed).
    FixedQuantity    // Bij een trailing sell wil je van alle munten af (en krijg je meer geld)
}

/// <summary>
/// Een position is 1 een samenvatting van 1 of meerdere orders
/// </summary>
[Table("PositionSteps")]
public class CryptoPositionStep
{
    [Key]
    public int Id { get; set; }
    public int PositionId { get; set; }
    public string Name { get; set; }

    /// <summary>
    /// Aanmaak datum van deze order/stap
    /// </summary>
    public DateTime CreateTime { get; set; }

    public OrderStatus? Status { get; set; }
    public bool IsBuy { get; set; }
    [Computed]
    // Deze vervangt de IsBuy (en heeft meer mogelijkheden)
    public CryptoOrderSide OrderSide { get; set; }

    public decimal Price { get; set; } // Tevens de LimitPrice indien het een OCO is
    public decimal? StopPrice { get; set; }
    public decimal? StopLimitPrice { get; set; }

    public decimal Quantity { get; set; }
    public decimal QuantityFilled { get; set; }
    public decimal QuoteQuantityFilled { get; set; }

    [Computed]
    // De instap waarde, bij het trailen wordt de value en lockingmethod gebruikt 
    // om de nieuw quantity te beredeneren adhv de prijs. 
    public decimal QuoteQuantityInitial { get; set; }


    [Computed]
    // Wat voor type order is het (limit, stop limit, oco etc)
    public CryptoOrderType OrderType { get; set; }

    public long OrderId { get; set; }
    public long? Order2Id { get; set; }
    public long? OrderListId { get; set; }

    public DateTime? CloseTime { get; set; }


    [Computed]
    // Of bij een trailing order de Quantity herberekend moet worden
    public LockingMethod LockingMethod { get; set; }

    [Computed]
    public CryptoTrailing Trailing { get; set; }
    [Computed]
    // Prijs waarop de trail actief wordt?
    public decimal? TrailActivatePrice { get; set; }

    [Computed]
    public int FromDcaIndex { get; set; }


    public string AsString(string fmt)
    {
        string s = string.Format("order#{0} {1} ({2}) Price={3} StopPrice={4} Quantity={5}", OrderId,
            OrderSide, OrderType, Price.ToString(fmt), StopPrice?.ToString(fmt), Quantity);

        if (Trailing > CryptoTrailing.TrailNone)
            s += string.Format(" Trailing={0} @={1}", Trailing, TrailActivatePrice?.ToString(fmt));

        return s;

    }

    public string Short()
    {
        return string.Format("{0} order#{1}", OrderType, OrderId);
    }
}


