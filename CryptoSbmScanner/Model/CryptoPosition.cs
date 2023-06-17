using System.Text;
using Binance.Net.Enums;
using CryptoSbmScanner.Intern;
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

    public CryptoTradeDirection Mode { get; set; }
    [Computed]
    public string ModeText
    {
        get
        {
            return Mode switch
            {
                CryptoTradeDirection.Long => "long",
                CryptoTradeDirection.Short => "short",
                _ => "?",
            };
        }
    }

    [Computed]
    public string DisplayText { get { return Symbol.Name + " " + Interval.Name + " " + CreateTime.ToLocalTime() + " " + ModeText + " " + StrategyText; } }

    public SignalStrategy Strategy { get; set; }
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
    public decimal BuyPrice { get; set; } //(dat kan anders zijn dan die van het signaal) (kan eigenlijk weg, slechts ter debug)
    public decimal BuyAmount { get; set; } // slecht gekozen, meer een soort van QuoteQuantity... // Vanwege problemen met het achteraf opzoeken hier opgenomen
    public decimal? SellPrice { get; set; }


    public DateTime? CloseTime { get; set; }

    // Een experiment (die wellicht wegkan)
    public string Data { get; set; }

    [Computed]
    public SortedList<int, CryptoPositionPart> Parts { get; set; } = new();

    [Computed]
    // Geindexeerde orders die uitstaan via de parts/steps
    public SortedList<long, CryptoPositionStep> Orders { get; set; } = new();




    public CryptoPositionStep CreateOrder(CryptoTradeDirection orderSide, CryptoOrderType orderType, decimal price, decimal quantity, decimal stopPrice)
    {
        CryptoPositionStep step = new();
        step.Status = OrderStatus.New;

        step.Name = "?";

        step.Price = price;
        step.Price = step.Price.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);

        step.Quantity = quantity;
        step.Quantity = step.Quantity.Clamp(Symbol.QuantityMinimum, Symbol.QuantityMaximum, Symbol.QuantityTickSize);

        // stoplimit 
        if (stopPrice > 0)
        {
            step.StopPrice = stopPrice;
            step.StopPrice = step.StopPrice?.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
        }

        // Nu (nog) computed!
        step.Mode = orderSide;
        step.OrderType = orderType;

        //step.Id = orderId; // Vanwege emulator
        //step.OrderId = orderId; // Vanwege emulator
        //Steps.Add(step.Id, step);
        return step;
    }

}