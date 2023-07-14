using System.Text;

using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Exchange;
using CryptoSbmScanner.Model;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Intern;

public class PositionTools
{
#if TRADEBOT
    private DateTime CurrentDate { get; set; }
    private CryptoTradeAccount TradeAccount { get; set; }
    private CryptoSymbol Symbol { get; set; }
    //private Model.CryptoExchange Exchange { get; set; }
    //private CryptoSymbolInterval SymbolInterval { get; set; }


    public PositionTools(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, DateTime currentDate)
    {
        TradeAccount = tradeAccount;

        //Exchange = symbol.Exchange;
        Symbol = symbol;
        //SymbolInterval = symbolInterval;
        CurrentDate = currentDate;
    }


    public static bool ValidTradeAccount(CryptoTradeAccount tradeAccount)
    {
        // Niet echt super, enumeratie oid hiervoor in het leven roepen, werkt verder wel
        if (tradeAccount.TradeAccountType == CryptoTradeAccountType.BackTest && GlobalData.BackTest)
            return true;
        if (tradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading && GlobalData.Settings.Trading.TradeViaExchange)
            return true;
        if (tradeAccount.TradeAccountType == CryptoTradeAccountType.PaperTrade && GlobalData.Settings.Trading.TradeViaPaperTrading)
            return true;

        return false;
    }

    /// <summary>
    /// Retourneer de part met name=x
    /// </summary>
    public static CryptoPositionPart FindPositionPart(CryptoPosition position, string name)
    {
        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            if (part.Name.Equals(name))
                return part;
        }
        return null;
    }

    /// <summary>
    /// Retourneer de part met id=x
    /// </summary>
    public static CryptoPositionPart FindPositionPart(CryptoPosition position, int Id)
    {
        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            if (part.Id == Id)
                return part;
        }
        return null;
    }

    /// <summary>
    /// Retourneer de openstaande step met naam=x
    /// </summary>
    public static CryptoPositionStep FindPositionPartStep(CryptoPositionPart part, string name, bool closed)
    {
        foreach (CryptoPositionStep step in part.Steps.Values.ToList())
        {
            if (step.Name.Equals(name) && step.Status != CryptoOrderStatus.Expired)
            {
                // Kan ook partial gevuld zijn, wat gebeurd er dan? ;-)
                if (closed && step.CloseTime.HasValue)
                    return step;
                else
                    if (!closed && !step.CloseTime.HasValue)
                    return step;
            }
        }
        return null;
    }

    public CryptoPosition CreatePosition(CryptoSignalStrategy strategy, CryptoSymbolInterval symbolInterval)
    {
        CryptoPosition position = new(); // bewust niet in een init struct gezet (vanwege debuggen)
        position.TradeAccount = TradeAccount;
        position.TradeAccountId = TradeAccount.Id;
        position.CreateTime = CurrentDate;
        position.Data = Symbol.Name; // ach ja, werd toch niet gebruikt
        position.Symbol = Symbol;
        position.SymbolId = Symbol.Id;
        position.Exchange = Symbol.Exchange;
        position.ExchangeId = Symbol.ExchangeId;
        position.Interval = symbolInterval.Interval;
        position.IntervalId = symbolInterval.Interval.Id;
        position.Status = CryptoPositionStatus.Waiting;
        position.Strategy = strategy;
        position.Side = CryptoOrderSide.Buy;
        return position;
    }

    static public void InsertPosition(CryptoDatabase database, CryptoPosition position)
    {
        database.Connection.Insert<CryptoPosition>(position);
    }

    static public void SavePosition(CryptoDatabase database, CryptoPosition position)
    {
        database.Connection.Update<CryptoPosition>(position);
    }

    public CryptoPositionPart CreatePositionPart(CryptoPosition position, string name, decimal buyPrice)
    {
        CryptoPositionPart part = new(); // bewust niet in een init struct gezet (vanwege debuggen)
        part.Name = name;
        part.BuyPrice = buyPrice;
        part.CreateTime = CurrentDate;
        part.PositionId = position.Id;
        part.Symbol = Symbol;
        part.SymbolId = Symbol.Id;
        part.Exchange = Symbol.Exchange;
        part.ExchangeId = Symbol.ExchangeId;
        //part.Interval = position.Interval;
        //part.IntervalId = position.Interval.Id;
        part.Status = CryptoPositionStatus.Waiting;
        part.Side = CryptoOrderSide.Buy;
        return part;
    }

    static public void InsertPositionPart(CryptoDatabase database, CryptoPositionPart part)
    {
        database.Connection.Insert<CryptoPositionPart>(part);
    }
    static public void SavePositionPart(CryptoDatabase database, CryptoPositionPart part)
    {
        database.Connection.Update<CryptoPositionPart>(part);
    }

    static public CryptoPositionStep CreatePositionStep(CryptoPosition position, CryptoPositionPart part, TradeParams tradeParams, string name, CryptoTrailing trailing = CryptoTrailing.None)
    {
        CryptoPositionStep step = new(); // bewust niet in een init struct gezet (vanwege debuggen)
        step.PositionId = position.Id;
        step.PositionPartId = part.Id;

        step.Name = name;
        step.Side = tradeParams.Side;
        step.Status = CryptoOrderStatus.New;
        step.OrderType = tradeParams.OrderType;
        step.CreateTime = tradeParams.CreateTime;
        
        step.Price = tradeParams.Price;
        step.StopPrice = tradeParams.StopPrice;
        step.StopLimitPrice = tradeParams.LimitPrice;

        step.Quantity = tradeParams.Quantity;
        step.QuantityFilled = 0;
        step.QuoteQuantityFilled = 0;

        step.OrderId = tradeParams.OrderId;
        step.Order2Id = tradeParams.Order2Id;
        //step.OrderListId = tradeParams.OrderListId; // onzin? maar ach (wat is/was het verhaal hierachter?)

        step.Trailing = trailing;
        //Dit wordt bij een 1e trade gedaan (zie DoOnSignal), maar hoe hou je dat bij (want die routine wordt aangepast)
        //part.BuyPrice = result.tradeParams.Price;
        //part.BuyAmount = result.tradeParams.QuoteQuantity; // voor het bepalen van het volgende aankoop bedrag (die in de settings kan wijzigen)
        //if (part.BuyAmount == 0)
        //    part.BuyAmount = result.tradeParams.Price * result.tradeParams.Quantity;
        return step;
    }

    static public void InsertPositionStep(CryptoDatabase database, CryptoPosition position, CryptoPositionStep step)
    {
        database.Connection.Insert<CryptoPositionStep>(step);

        // Genereer een fictieve order ID voor papertrading
        if (position.TradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading && step.OrderId == 0)
        {
            step.OrderId = step.Id;
            database.Connection.Update<CryptoPositionStep>(step);
        }
    }
    static public void SavePositionStep(CryptoDatabase database, CryptoPosition position, CryptoPositionStep step)
    {
        database.Connection.Update<CryptoPositionStep>(step);
    }

    public static void AddPosition(CryptoTradeAccount tradeAccount, CryptoPosition position)
    {
        position.TradeAccount = tradeAccount;
        if (GlobalData.ExchangeListId.TryGetValue(position.ExchangeId, out Model.CryptoExchange exchange))
        {
            position.Exchange = exchange;
            if (exchange.SymbolListId.TryGetValue(position.SymbolId, out CryptoSymbol symbol))
            {
                position.Symbol = symbol;
                if (GlobalData.IntervalListId.TryGetValue((int)position.IntervalId, out CryptoInterval interval))
                    position.Interval = interval;

                tradeAccount.PositionList.TryAdd(symbol.Name, new());
                if (tradeAccount.PositionList.TryGetValue(symbol.Name, out SortedList<int, CryptoPosition> positionList))
                    positionList.TryAdd(position.Id, position);
            }
        }
    }


    static public void AddPositionPart(CryptoPosition position, CryptoPositionPart part)
    {
        position.Parts.TryAdd(part.Id, part);
        part.Position = position; // parent
        part.Exchange = position.Exchange;
        part.Symbol = position.Symbol;
    }


    static public void AddPositionPartStep(CryptoPositionPart part, CryptoPositionStep step)
    {
        part.Steps.TryAdd(step.Id, step);

        // OrderId index aanvullen
        if (step.OrderId.HasValue)
            part.Position.Orders.TryAdd((long)step.OrderId, step);
        if (step.Order2Id.HasValue) 
            part.Position.Orders.TryAdd((long)step.Order2Id, step);
    }


    static public void AddPositionClosed(CryptoPosition position)
    {
        if (GlobalData.TradeAccountList.TryGetValue(position.TradeAccountId, out CryptoTradeAccount tradeAccount))
        {
            position.TradeAccount = tradeAccount;
            if (GlobalData.ExchangeListId.TryGetValue(position.ExchangeId, out Model.CryptoExchange exchange))
            {
                position.Exchange = exchange;
                if (exchange.SymbolListId.TryGetValue(position.SymbolId, out CryptoSymbol symbol))
                {
                    position.Symbol = symbol;
                    if (GlobalData.IntervalListId.TryGetValue((int)position.IntervalId, out CryptoInterval interval))
                        position.Interval = interval;

                    GlobalData.PositionsClosed.Add(position);
                }
            }
        }
    }


    static public void RemovePosition(CryptoTradeAccount tradeAccount, CryptoPosition position)
    {
        if (tradeAccount.PositionList.TryGetValue(position.Symbol.Name, out var positionList))
        {
            if (positionList.ContainsKey(position.Id))
            {
                positionList.Remove(position.Id);

                if (GlobalData.PositionsClosed.Any())
                    GlobalData.PositionsClosed.Add(position);
                else
                    GlobalData.PositionsClosed.Insert(0, position);
            }
            if (positionList.Count == 0)
                tradeAccount.PositionList.Remove(position.Symbol.Name);
        }
    }


    /// <summary>
    /// De break-even prijs berekenen vanuit de parts en steps
    /// </summary>
    public static void CalculateProfitAndBeakEvenPrice(CryptoPosition position, bool includeFee = true)
    {
        //https://dappgrid.com/binance-fees-explained-fee-calculation/
        // You should first divide your order size(total) by 100 and then multiply it by your fee rate which 
        // is 0.10 % for VIP 0 / regular users. So, if you buy Bitcoin with 200 USDT, you will basically get
        // $199.8 worth of Bitcoin.To calculate these fees, you can also use our Binance fee calculator:
        // (als je verder gaat dan wordt het vanwege de kickback's tamelijk complex)

        if (position.Parts.Count == 0)
            GlobalData.AddTextToLogTab(string.Format("CalculateProfitAndBeakEvenPrice - er zijn geen parts! {0}", position.Symbol.Name));

        position.Quantity = 0;
        position.Invested = 0;
        position.Returned = 0;
        position.Commission = 0;

        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            part.Quantity = 0;
            part.Invested = 0;
            part.Returned = 0;
            part.Commission = 0;

            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                if (step.Status == CryptoOrderStatus.Filled || step.Status == CryptoOrderStatus.PartiallyFilled)
                {
                    if (step.Side == CryptoOrderSide.Buy)
                    {
                        part.Quantity += step.Quantity;
                        part.Invested += step.QuoteQuantityFilled;
                    }
                    else if (step.Side == CryptoOrderSide.Sell)
                    {
                        part.Quantity -= step.Quantity;
                        part.Returned += step.QuoteQuantityFilled;
                    }

                    // TODO - De commission vanuit de TRADE (of step) overnemen (en rekening houden met CommisionAsset?)
                    // Maar dit is op zich ook een redelijk inschatting van de fee (ligt ook aan referral en kickback)
                    if (includeFee)
                        part.Commission += (0.075m / 100) * step.QuoteQuantityFilled;
                }
                //string s = string.Format("{0} CalculateProfit bought position={1} part={2} name={3} step={4} {5} price={6} stopprice={7} quantityfilled={8} QuoteQuantityFilled={9}",
                //   position.Symbol.Name, position.Id, part.Id, part.Name, step.Id, step.Name, step.Price, step.StopPrice, step.QuantityFilled, step.QuoteQuantityFilled);
                //GlobalData.AddTextToLogTab(s);
            }

            if (part.Invested != 0 && part.Status == CryptoPositionStatus.Waiting)
                part.Status = CryptoPositionStatus.Trading;

            part.Profit = part.Returned - part.Invested - part.Commission;
            part.Percentage = 0m;
            if (part.Invested != 0m)
                part.Percentage = 100m * (part.Returned - part.Commission) / part.Invested;
            if (part.Quantity > 0)
                part.BreakEvenPrice = (part.Invested - part.Returned + part.Commission) / part.Quantity;
            else
                part.BreakEvenPrice = 0; // mhh. denk fout? Als we in een dca zitten is de part.BE 0

            //string t = string.Format("{0} CalculateProfit sell invested={1} profit={2} bought={3} sold={4} steps={5}",
            //    position.Symbol.Name, part.Invested, part.Profit, part.Invested, part.Returned, part.Steps.Count);
            //GlobalData.AddTextToLogTab(t);


            position.Quantity += part.Quantity;
            position.Invested += part.Invested;
            position.Returned += part.Returned;
            position.Commission += part.Commission;
        }


        if (position.Invested != 0 && position.Status == CryptoPositionStatus.Waiting)
            position.Status = CryptoPositionStatus.Trading;

        position.Profit = position.Returned - position.Invested - position.Commission;
        position.Percentage = 0m;
        if (position.Invested != 0m)
            position.Percentage = 100m * (position.Returned - position.Commission) / position.Invested;
        if (position.Quantity > 0)
            position.BreakEvenPrice = (position.Invested - position.Returned + position.Commission) / position.Quantity;
        else
            position.BreakEvenPrice = 0;

        position.PartCount = position.Parts.Count;
    }




    /// <summary>
    /// Na het opstarten is er behoefte om openstaande orders en trades te synchroniseren
    /// (dependency: de trades en steps moeten hiervoor ingelezen zijn)
    /// </summary>
    static public void CalculatePositionResultsViaTrades(CryptoDatabase database, CryptoPosition position)
    {
        if (position.Parts.Count == 0)
            GlobalData.AddTextToLogTab(string.Format("CalculatePositionViaTrades - er zijn geen parts! {0}", position.Symbol.Name));


        // Reset eerste de filled
        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            // TODO: Commission vanuit de trades laten doorwerken in de part en de position
            // part.Commission = 0;
            // probleem met de gebruikte asset (quote of bnb?) Omrekenen, maar hoe?

            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                step.QuantityFilled = 0;
                step.QuoteQuantityFilled = 0;
            }
        }

        // De filled quantity in de steps opnieuw opbouwen vanuit de trades
        foreach (CryptoTrade trade in position.Symbol.TradeList.Values.ToList())
        {
            if (position.Orders.TryGetValue(trade.OrderId, out CryptoPositionStep step))
            {
                step.QuantityFilled += trade.Quantity;
                step.QuoteQuantityFilled += trade.QuoteQuantity;

                // Vanuit nieuwe trades moeten we de status wel bijwerken (opstarten applicatie)
                // Maar overschrijf de status alleen indien het absoluut zeker is..
                if (step.QuantityFilled >= step.Quantity)
                {
                    step.CloseTime = trade.TradeTime;
                    if (step.Status < CryptoOrderStatus.Filled || step.Status == CryptoOrderStatus.Expired)
                        step.Status = CryptoOrderStatus.Filled;
                }
                else if (step.QuantityFilled > 0)
                {
                    if (step.Status == CryptoOrderStatus.New)
                        step.Status = CryptoOrderStatus.PartiallyFilled;
                }
            }
        }

        // De positie doorrekenen (parts/steps)
        CalculateProfitAndBeakEvenPrice(position);


        // De parts en steps bewaren
        int openOrders = 0;
        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                database.Connection.Update<CryptoPositionStep>(step);

                if (step.Status < CryptoOrderStatus.Filled)
                    openOrders++;
            }

            database.Connection.Update<CryptoPositionPart>(part);
        }



        // Als alles verkocht is de positie alsnog sluiten
        if ((position.Quantity == 0) && (openOrders == 0) && (position.Status == CryptoPositionStatus.Trading))
        {
            position.CloseTime = DateTime.UtcNow; // TODO - Datum aanpassen voor emulator/backtest
            if (position.Status != CryptoPositionStatus.Ready)
            {
                position.Status = CryptoPositionStatus.Ready;
                GlobalData.AddTextToLogTab(string.Format("TradeTools: Positie {0} status aangepast naar {1}", position.Symbol.Name, position.Status));
            }
        }

        // De positie bewaren
        database.Connection.Update<CryptoPosition>(position);

        return;
    }


    static public void DumpPosition(CryptoPosition position, StringBuilder strings)
    {
        // Het is op niet echt super-leesbaar, Excel ding maken wellicht?
        // Zie BackTestExcel.cs, daar wordt een mooi rapportje gemaakt!!
        // Het kan regel georienteerd onder elkaar en de kolommen komen overeen lijkt me.
        // Nog eens een voorbeeld excel ding van maken, kan volgens mij erg mooi zijn.
        // (en in ieder geval begrijpbaarder en overzichtelijker dan onderstaande <g>)

        strings.AppendLine("");
        strings.AppendLine("-------------------");
        strings.AppendLine("Position dump:");
        strings.AppendLine("");
        strings.AppendLine("Position Id:" + position.Id.ToString());
        strings.AppendLine("Account:" + position.TradeAccount.Name);
        strings.AppendLine("Exchange:" + position.Symbol.Exchange.Name);
        strings.AppendLine("Name:" + position.Symbol.Name);
        strings.AppendLine("Strategie:" + position.StrategyText);
        strings.AppendLine("Interval:" + position.Interval.Name);
        strings.AppendLine("Status:" + position.Status.ToString());
        strings.AppendLine("OpenDate:" + position.CreateTime.ToLocalTime());
        if (position.CloseTime.HasValue)
            strings.AppendLine("CloseDate:" + position.CloseTime?.ToLocalTime());
        strings.AppendLine("BreakEvenPrice:" + position.BreakEvenPrice.ToString());

        strings.AppendLine("Invested:" + position.Invested.ToString(position.Symbol.QuoteData.DisplayFormat));
        strings.AppendLine("Commission:" + position.Commission.ToString(position.Symbol.QuoteData.DisplayFormat));
        strings.AppendLine("Returned:" + position.Returned.ToString(position.Symbol.QuoteData.DisplayFormat));
        strings.AppendLine("Profit:" + position.Profit.ToString(position.Symbol.QuoteData.DisplayFormat));
        strings.AppendLine("Percentage:" + position.Percentage.ToString("N2"));

        // debug
        strings.AppendLine("Quantity:" + position.Quantity.ToString());
        strings.AppendLine("BuyPrice:" + position.BuyPrice.ToString());
        strings.AppendLine("BuyAmount:" + position.BuyAmount.ToString());
        strings.AppendLine("SellPrice:" + position.SellPrice.ToString());

        strings.AppendLine("");
        strings.AppendLine("-------------------");
        strings.AppendLine("Parts");

        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            // TODO - informatie van de Part
            strings.AppendLine("  Part dump:");
            strings.AppendLine("");
            strings.AppendLine("  Part Id:" + part.Id.ToString());
            strings.AppendLine("  Name:" + part.Name);
            strings.AppendLine("  Status:" + part.Status.ToString());
            strings.AppendLine("  OpenDate:" + part.CreateTime.ToLocalTime());
            strings.AppendLine("  CloseDate:" + part.CloseTime?.ToLocalTime());
            strings.AppendLine("  BreakEvenPrice:" + part.BreakEvenPrice.ToString());

            strings.AppendLine("  Invested:" + part.Invested.ToString(position.Symbol.QuoteData.DisplayFormat));
            strings.AppendLine("  Commission:" + part.Commission.ToString(position.Symbol.QuoteData.DisplayFormat));
            strings.AppendLine("  Returned:" + part.Returned.ToString(position.Symbol.QuoteData.DisplayFormat));
            strings.AppendLine("  Profit:" + part.Profit.ToString(position.Symbol.QuoteData.DisplayFormat));
            strings.AppendLine("  Percentage:" + part.Percentage.ToString("N2"));

            // debug
            strings.AppendLine("  Quantity:" + part.Quantity.ToString());
            strings.AppendLine("  BuyPrice:" + part.BuyPrice.ToString());
            strings.AppendLine("  BuyAmount:" + part.BuyAmount.ToString());
            strings.AppendLine("  SellPrice:" + part.SellPrice.ToString());

            strings.AppendLine("  -------------------");
            strings.AppendLine("  Steps");
            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                string s = string.Format("    step#{0} {1} {2} order#{3} {4} ({5}) Price={6} StopPrice={7} StopLimitPrice={8} Quantity={9} QuantityFilled={10} QuoteQuantityFilled={11} close={12} {13}",
                    step.Id, step.Name, step.CreateTime.ToLocalTime(), step.OrderId, step.Side, step.OrderType,
                    step.Price.ToString(position.Symbol.PriceDisplayFormat), step.StopPrice?.ToString(position.Symbol.PriceDisplayFormat), step.StopLimitPrice?.ToString(position.Symbol.PriceDisplayFormat),
                    step.Quantity, step.QuantityFilled, step.QuoteQuantityFilled, step.CloseTime?.ToLocalTime(), step.Status.ToString());

                //if (Trailing > CryptoTrailing.TrailNone)
                //    s += string.Format(" Trailing={0} @={1}", Trailing, TrailActivatePrice?.ToString(format));
                strings.AppendLine(s);
            }
        }

        strings.AppendLine("");
        strings.AppendLine("-------------------");
        strings.AppendLine("Trades");
        foreach (CryptoTrade trade in position.Symbol.TradeList.Values.ToList())
        {
            strings.AppendLine("");
            strings.AppendLine("Side:" + trade.Side);
            strings.AppendLine("Id:" + trade.Id.ToString());
            strings.AppendLine("TradeId:" + trade.TradeId.ToString());
            strings.AppendLine("OrderId:" + trade.OrderId.ToString());
            strings.AppendLine("OpenDate:" + trade.TradeTime.ToLocalTime());

            strings.AppendLine("Price:" + trade.Price.ToString(position.Symbol.PriceDisplayFormat));
            strings.AppendLine("Quantity:" + trade.Quantity.ToString(position.Symbol.QuantityDisplayFormat));
            strings.AppendLine("QuoteQuantity:" + trade.QuoteQuantity.ToString(position.Symbol.QuantityDisplayFormat));

            strings.AppendLine("Commission:" + trade.Commission.ToString(position.Symbol.QuantityDisplayFormat));
            strings.AppendLine("CommissionAsset:" + trade.CommissionAsset);
        }
    }


    static public async Task LoadTradesfromDatabaseAndExchange(CryptoDatabase database, CryptoPosition position)
    {
        // Bij het laden zijn niet alle trades in het geheugen ingelezen, dus deze alsnog inladen (of verversen)
        // Probleem, er zitten msec in de position.CreateTime en niet in de Trade.TradeTime (pfft)
        // string sql = string.Format("select * from trades where ExchangeId={0} and SymbolId={1} and TradeTime >='{2}' order by TradeId",
        // position.ExchangeId, position.SymbolId, position.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"));
        //position.Symbol.TradeList.Clear(); // geen locking, gevaarlijk? alleen toevoegen is veiliger
        //DateTime fromDate = position.CreateTime;
        string sql = "select * from trade where SymbolId=@symbolId and TradeTime >= @fromDate order by TradeTime";
        foreach (CryptoTrade trade in database.Connection.Query<CryptoTrade>(sql, new { symbolId = position.SymbolId, fromDate = position.CreateTime }))
            GlobalData.AddTrade(trade);


        // Daarna de "nieuwe" trades van deze coin ophalen en die toegevoegen aan dezelfde tradelist
        // TODO: Afhankelijkheid uitfaseren of exchange-aware maken?
        if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading)
            await ExchangeHelper.FetchTradesAsync(position.TradeAccount, position.Symbol);
    }


    public static void LoadPosition(CryptoDatabase database, CryptoPosition position)
    {
        // De parts
        string sql = string.Format("select * from positionpart where PositionId={0} order by Id", position.Id);
        foreach (CryptoPositionPart part in database.Connection.Query<CryptoPositionPart>(sql))
        {
            AddPositionPart(position, part);
        }

        // De steps
        sql = string.Format("select * from positionstep where PositionId={0} order by Id", position.Id);
        foreach (CryptoPositionStep step in database.Connection.Query<CryptoPositionStep>(sql))
        {
            if (position.Parts.TryGetValue(step.PositionPartId, out CryptoPositionPart part))
                AddPositionPartStep(part, step);
        }
    }


    public static CryptoPosition HasPosition(CryptoTradeAccount tradeAccount, CryptoSymbol symbol) //, CryptoSymbolInterval symbolInterval
    {
        if (tradeAccount.PositionList.TryGetValue(symbol.Name, out var positionList))
        {
            foreach (var position in positionList.Values.ToList())
            {
                // Alleen voor long trades en het betrokken interval (wat is de redenatie hierachter?)
                // Een gelijk interval is niet handig, je wilt een bijkoop doen en interval is niet relevant.
                if (position.Side != CryptoOrderSide.Buy) // || position.IntervalId != symbolInterval.IntervalId
                    continue;

                return position;
            }
        }

        return null;
    }


    public static CryptoPositionStep IsTrailingPosition(CryptoPosition position)
    {
        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                if (step.Status == CryptoOrderStatus.New && step.Trailing == CryptoTrailing.Trailing)
                    return step;
            }
        }

        return null;
    }

    /// <summary>
    /// Is er een positie open (dan wel signalen maken voor deze munt)
    /// </summary>
    public static bool HasPosition(CryptoSymbol symbol)
    {
        foreach (CryptoTradeAccount tradeAccount in GlobalData.TradeAccountList.Values.ToList())
        {
            if (tradeAccount.PositionList.TryGetValue(symbol.Name, out var _))
                return true;
        }

        return false;
    }
#endif

}
