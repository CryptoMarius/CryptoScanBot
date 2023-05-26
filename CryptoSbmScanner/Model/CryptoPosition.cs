using System.Text;
using Binance.Net.Enums;
using CryptoSbmScanner.Intern;
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
    public int ExchangeId { get; set; }
    [Computed]
    public virtual Model.CryptoExchange Exchange { get; set; }

    public int? SignalId { get; set; }
    [Computed]
    public virtual CryptoSignal Signal { get; set; }

    public int SymbolId { get; set; }
    [Computed]
    public virtual CryptoSymbol Symbol { get; set; }

    public int? IntervalId { get; set; }
    [Computed]
    public virtual CryptoInterval Interval { get; set; }

    public bool PaperTrade { get; set; } = false;

    // Globale status van de positie (new, closed, wellicht andere enum?)
    public CryptoPositionStatus? Status { get; set; }

    // Statistiek velden
    public decimal Invested { get; set; }
    public decimal Returned { get; set; }
    public decimal Commission { get; set; }

    public decimal BreakEvenPrice { get; set; }

    public decimal Quantity { get; set; }

    public decimal Profit { get; set; }
    public decimal Percentage { get; set; }

    // deze 3 mogen wat mij betreft weg, staat allemaal in de steps..

    // Buy gegevens
    public decimal BuyPrice { get; set; } //(dat kan anders zijn dan die van het signaal)
    public decimal BuyAmount { get; set; } // slecht gekozen, meer een soort van QuoteQuantity...
                                           // Vanwege problemen met het achteraf opzoeken hier opgenomen
    public decimal? SellPrice { get; set; }


    public DateTime? CloseTime { get; set; }

    // Een experiment (die wellicht wegkan)
    public string Data { get; set; }

    [Computed]
    public SortedList<long, CryptoPositionStep> Steps { get; set; } = new();



    /// <summary>
    /// De break-even prijs berekenen
    /// </summary>
    public void CalculateBeakEvenPrice(bool includeFee = true)
    {
        //https://dappgrid.com/binance-fees-explained-fee-calculation/
        // You should first divide your order size(total) by 100 and then multiply it by your fee rate which 
        // is 0.10 % for VIP 0 / regular users. So, if you buy Bitcoin with 200 USDT, you will basically get
        // $199.8 worth of Bitcoin.To calculate these fees, you can also use our Binance fee calculator:
        // (als je verder gaat dan wordt het vanwege de kickback's tamelijk complex)

        Quantity = 0;
        Invested = 0;
        Returned = 0;
        Commission = 0;

        foreach (CryptoPositionStep order in Steps.Values)
        {
            if (order.Status == OrderStatus.Filled)
            {
                decimal value = order.Price * order.Quantity;
                if (includeFee)
                    Commission += (0.075m / 100) * value; // De commissie% (met kickback in dit geval)
                if (order.OrderSide == CryptoOrderSide.Buy)
                {
                    Invested += value;
                    Quantity += order.Quantity;
                }
                else if (order.OrderSide == CryptoOrderSide.Sell)
                {
                    Returned += value;
                    Quantity -= order.Quantity;
                }
            }
        }

        if (Quantity > 0)
        {
            BreakEvenPrice = (Invested - Returned + Commission) / Quantity;
            // Nee, dit doen we later bij het afronden van de prijzen, niet hier uitvoeren
            //BreakEvenPrice = BreakEvenPrice.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
        }
        else
        {
            BreakEvenPrice = 0;
        }

        Profit = Returned - Invested - Commission;

        Percentage = 0m;
        if (Invested != 0m)
            Percentage = 100m * (Returned - Commission) / Invested;
    }


    public decimal BreakEvenPriceViaTrades()
    {
        // Deze gaat uit van trades. Terwijl die juist (met veel moeite) geregistreerd worden in de steps
        // De bovenstaande code is een betere variant, die rekent vanuit de steps!

        decimal SumValue = 0;
        decimal SumQuantity = 0;

        foreach (CryptoTrade trade in Symbol.TradeList.Values)
        {
            CryptoPositionStep step;
            if (Steps.TryGetValue(trade.OrderId, out step))
            {
                // enig debug werk, soms wordt het niet ingevuld!
                if (trade.QuoteQuantity == 0)
                {
                    trade.QuoteQuantity = trade.Price * trade.Quantity;
                    GlobalData.AddTextToLogTab(string.Format("{0} BreakEvenPriceViaTrades QuoteQuantity is 0 for order TradeId={1}!", Symbol.Name, trade.TradeId));
                }

                if (trade.IsBuyer)
                {
                    SumQuantity += trade.Quantity; // het aantal gekochte muntjes
                    SumValue += trade.QuoteQuantity; // het "usdt" bedrag (price * quantity)
                }
                else
                {
                    SumQuantity -= trade.Quantity; // het aantal verkochte muntjes
                    SumValue += trade.QuoteQuantity; // het "usdt" bedrag (price * quantity)
                }
            }
        }

        return SumValue / SumQuantity;
    }


    public void Dump(StringBuilder log)
    {
        log.AppendLine();
        string s;
        foreach (CryptoPositionStep order in Steps.Values)
        {
            s = string.Format("{0,-18} Side={1,-10} Status={2,-8} price={3:N8} quantity={4:N8} OrderType={5}", order.Name, order.OrderSide, order.Status, order.Price, order.Quantity, order.OrderType);
            log.AppendLine(s);
        }

        s = string.Format("BreakEven={0:N8} Total Quantity={1}", BreakEvenPrice, Quantity);
        log.AppendLine(s);
        log.AppendLine();
    }

    public CryptoPositionStep FindOrder(string name)
    {
        foreach (CryptoPositionStep order in Steps.Values)
        {
            if (order.Name.Equals(name))
                return order;
        }

        return null;
    }

    public CryptoPositionStep CreateOrder(int orderId, CryptoOrderSide orderSide, CryptoOrderType orderType, decimal price, decimal quantity, decimal stopPrice)
    {
        CryptoPositionStep step = new CryptoPositionStep();
        step.Status = OrderStatus.New;
        if (orderSide == CryptoOrderSide.Buy)
            step.IsBuy = true;
        else
            step.IsBuy = false;

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
        step.OrderSide = orderSide;
        step.OrderType = orderType;

        step.Id = orderId; // Vanwege emulator
        step.OrderId = orderId; // Vanwege emulator
        Steps.Add(step.Id, step);
        return step;
    }


}


