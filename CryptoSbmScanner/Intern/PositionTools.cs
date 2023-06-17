using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Binance.Net.Enums;

using CryptoSbmScanner.Binance;
using CryptoSbmScanner.Context;
using CryptoSbmScanner.Model;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Intern;

public class PositionTools
{
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
        if (tradeAccount.AccountType == CryptoTradeAccountType.BackTest && GlobalData.BackTest)
            return true;
        if (tradeAccount.AccountType == CryptoTradeAccountType.RealTrading && GlobalData.Settings.Trading.TradeViaExchange)
            return true;
        if (tradeAccount.AccountType == CryptoTradeAccountType.PaperTrade && GlobalData.Settings.Trading.TradeViaPaperTrading)
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
            if (step.Name.Equals(name) && step.Status != OrderStatus.Expired)
            {
                if (closed && step.CloseTime.HasValue)
                    return step;
                else
                    if (!closed && !step.CloseTime.HasValue)
                    return step;
            }
        }
        return null;
    }

    public CryptoPosition CreatePosition(SignalStrategy strategy, CryptoSymbolInterval symbolInterval)
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
        position.Mode = CryptoTradeDirection.Long;
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

    public CryptoPositionPart CreatePositionPart(CryptoPosition position)
    {
        CryptoPositionPart part = new(); // bewust niet in een init struct gezet (vanwege debuggen)
        part.CreateTime = CurrentDate;
        part.PositionId = position.Id;
        part.Symbol = Symbol;
        part.SymbolId = Symbol.Id;
        part.Exchange = Symbol.Exchange;
        part.ExchangeId = Symbol.ExchangeId;
        //part.Interval = position.Interval;
        //part.IntervalId = position.Interval.Id;
        part.Status = CryptoPositionStatus.Waiting;
        part.Mode = CryptoTradeDirection.Long;
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

    static public CryptoPositionStep CreatePositionStep(CryptoPosition position, CryptoPositionPart part, TradeParams tradeParams, string name)
    {
        CryptoPositionStep step = new(); // bewust niet in een init struct gezet (vanwege debuggen)
        step.PositionId = position.Id;
        step.PositionPartId = part.Id;

        step.Name = name;
        step.Mode = tradeParams.Side;
        step.Status = OrderStatus.New;
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
        if (position.TradeAccount.AccountType != CryptoTradeAccountType.RealTrading && step.OrderId == 0)
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
            if (!GlobalData.BackTest && GlobalData.ApplicationStatus == ApplicationStatus.AppStatusRunning)
                GlobalData.PositionsHaveChanged("");
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

        // Index op openstaande orders bijwerken (wellicht niet zinvol als de status filled is?)
        if (step.OrderId.HasValue && !part.Position.Orders.ContainsKey((long)step.OrderId))
            part.Position.Orders.Add((long)step.OrderId, step);
        if (step.Order2Id.HasValue && !part.Position.Orders.ContainsKey((long)step.Order2Id))
            part.Position.Orders.Add((long)step.Order2Id, step);
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


            if (!GlobalData.BackTest && GlobalData.ApplicationStatus == ApplicationStatus.AppStatusRunning)
                GlobalData.PositionsHaveChanged("");
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

        position.Quantity = 0;
        position.Invested = 0;
        position.Returned = 0;
        position.Commission = 0;

        foreach (CryptoPositionPart part in position.Parts.Values)
        {
            part.Quantity = 0;
            part.Invested = 0;
            part.Returned = 0;
            part.Commission = 0;

            foreach (CryptoPositionStep step in part.Steps.Values)
            {
                if (step.Status == OrderStatus.Filled || step.Status == OrderStatus.PartiallyFilled)
                {
                    if (step.Mode == CryptoTradeDirection.Long)
                    {
                        part.Invested += step.QuoteQuantityFilled;
                        part.Quantity += step.Quantity;
                    }
                    else if (step.Mode == CryptoTradeDirection.Short)
                    {
                        part.Returned += step.QuoteQuantityFilled;
                        part.Quantity -= step.Quantity;
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

            if (part.Invested > 0 && part.Status == CryptoPositionStatus.Waiting)
                part.Status = CryptoPositionStatus.Trading;

            part.Profit = part.Returned - part.Invested - part.Commission;
            part.Percentage = 0m;
            if (part.Invested != 0m)
                part.Percentage = 100m * (part.Returned - part.Commission) / part.Invested;
            if (part.Quantity > 0)
                part.BreakEvenPrice = (part.Invested - part.Returned + part.Commission) / part.Quantity;
            else
                part.BreakEvenPrice = 0;

            string t = string.Format("{0} CalculateProfit sell invested={1} profit={2} bought={3} sold={4} steps={5}",
                position.Symbol.Name, part.Invested, part.Profit, part.Invested, part.Returned, part.Steps.Count);
            GlobalData.AddTextToLogTab(t);


            position.Quantity += part.Quantity;
            position.Invested += part.Invested;
            position.Returned += part.Returned;
            position.Commission += part.Commission;
        }


        if (position.Invested > 0 && position.Status == CryptoPositionStatus.Waiting)
            position.Status = CryptoPositionStatus.Trading;

        position.Profit = position.Returned - position.Invested - position.Commission;
        position.Percentage = 0m;
        if (position.Invested != 0m)
            position.Percentage = 100m * (position.Returned - position.Commission) / position.Invested;
        if (position.Quantity > 0)
            position.BreakEvenPrice = (position.Invested - position.Returned + position.Commission) / position.Quantity;
        else
            position.BreakEvenPrice = 0;
    }



    /// <summary>
    /// Na het opstarten is er behoefte om openstaande orders en trades te synchroniseren
    /// (dependency: de trades en steps moeten hiervoor ingelezen zijn)
    /// </summary>
    static public void CalculatePositionViaTrades(CryptoDatabase database, CryptoPosition position)
    {
        // Reset eerste de filled
        foreach (CryptoPositionPart part in position.Parts.Values)
        {
            // TODO: Commission vanuit de trades laten doorwerken in de part en de position
            // part.Commission = 0;
            // probleem met de gebruikte asset (quote of bnb?) Omrekenen, maar hoe?

            foreach (CryptoPositionStep step in part.Steps.Values)
            {
                step.QuantityFilled = 0;
                step.QuoteQuantityFilled = 0;
            }
        }

        // De steps opbouwen vanuit de trades
        foreach (CryptoTrade trade in position.Symbol.TradeList.Values)
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
                    if (step.Status < OrderStatus.Filled)
                        step.Status = OrderStatus.Filled;
                }
                else if (step.QuantityFilled > 0)
                {
                    if (step.Status == OrderStatus.New)
                        step.Status = OrderStatus.PartiallyFilled;
                }
            }
        }

        // De positie doorrekenen (parts/steps)
        CalculateProfitAndBeakEvenPrice(position);


        // De parts en steps bewaren
        int openOrders = 0;
        foreach (CryptoPositionPart part in position.Parts.Values)
        {
            foreach (CryptoPositionStep step in part.Steps.Values)
            {
                database.Connection.Update<CryptoPositionStep>(step);

                if (step.Status < OrderStatus.Filled)
                    openOrders++;
            }

            database.Connection.Update<CryptoPositionPart>(part);
        }



        // Als alles verkocht is de positie alsnog sluiten
        if ((position.Quantity == 0) && (openOrders == 0) && (position.Status == CryptoPositionStatus.Trading))
        {
            position.CloseTime = DateTime.UtcNow; // TODO - Datum aanpassen voor emulator/backtest
            position.Status = CryptoPositionStatus.Ready;
            GlobalData.AddTextToLogTab(string.Format("TradeTools: Positie {0} status aangepast naar {1}", position.Symbol.Name, position.Status));
        }

        // De positie bewaren
        database.Connection.Update<CryptoPosition>(position);

        return;
    }


    //public static void Dump(CryptoPosition position, StringBuilder strings)
    //{
    //    string s;
    //    strings.AppendLine();
    //    foreach (CryptoPositionPart part in position.Parts.Values)
    //    {
    //        foreach (CryptoPositionStep order in part.Steps.Values)
    //        {
    //            s = string.Format("{0,-18} Side={1,-10} Status={2,-8} price={3:N8} quantity={4:N8} OrderType={5}",
    //                order.Name, order.Mode, order.Status, order.Price, order.Quantity, order.OrderType);
    //            strings.AppendLine(s);
    //        }
    //    }

    //    s = string.Format("BreakEven={0:N8} Total Quantity={1}", position.BreakEvenPrice, position.Quantity);
    //    strings.AppendLine(s);
    //    strings.AppendLine();
    //}

    //static public void DumpPosition(CryptoPosition position, StringBuilder strings)
    //{
    //    // Het is op deze manier niet echt leesbaar, Excel ding maken wellicht?

    //    strings.AppendLine("");
    //    strings.AppendLine("-------------------");
    //    strings.AppendLine("Position dump:");
    //    strings.AppendLine("");
    //    strings.AppendLine("Position Id:" + position.Id.ToString());
    //    strings.AppendLine("Name:" + position.Symbol.Name);
    //    strings.AppendLine("Status:" + position.Status.ToString());
    //    strings.AppendLine("OpenDate:" + position.CreateTime.ToLocalTime());
    //    strings.AppendLine("CloseDate:" + position.CloseTime?.ToLocalTime());

    //    strings.AppendLine("Invested:" + position.Invested.ToString(position.Symbol.QuoteData.DisplayFormat));
    //    strings.AppendLine("Commission:" + position.Commission.ToString(position.Symbol.QuoteData.DisplayFormat));
    //    strings.AppendLine("Returned:" + position.Returned.ToString(position.Symbol.QuoteData.DisplayFormat));
    //    strings.AppendLine("Profit:" + position.Profit.ToString(position.Symbol.QuoteData.DisplayFormat));
    //    strings.AppendLine("Percentage:" + position.Percentage.ToString("N2"));

    //    strings.AppendLine("");
    //    strings.AppendLine("-------------------");
    //    strings.AppendLine("Steps");

    //    foreach (CryptoPositionPart part in position.Parts.Values)
    //    {
    //        // TODO - informatie van de Part


    //        foreach (CryptoPositionStep step in part.Steps.Values)
    //        {
    //            strings.AppendLine("" + step.DisplayText(position.Symbol.PriceDisplayFormat));
    //        }
    //    }

    //    strings.AppendLine("");
    //    strings.AppendLine("-------------------");
    //    strings.AppendLine("Trades");
    //    foreach (CryptoTrade trade in position.Symbol.TradeList.Values)
    //    {
    //        strings.AppendLine("");
    //        strings.AppendLine("Id:" + trade.Id.ToString());
    //        strings.AppendLine("TradeId:" + trade.TradeId.ToString());
    //        strings.AppendLine("OrderId:" + trade.OrderId.ToString());
    //        strings.AppendLine("OpenDate:" + trade.TradeTime.ToLocalTime());

    //        strings.AppendLine("Price:" + trade.Price.ToString(position.Symbol.PriceDisplayFormat));
    //        strings.AppendLine("Quantity:" + trade.Quantity.ToString(position.Symbol.QuantityDisplayFormat));
    //        strings.AppendLine("QuoteQuantity:" + trade.QuoteQuantity.ToString(position.Symbol.QuantityDisplayFormat));

    //        strings.AppendLine("Commission:" + trade.Commission.ToString(position.Symbol.QuantityDisplayFormat));
    //        strings.AppendLine("CommissionAsset:" + trade.CommissionAsset);
    //    }
    //}


    static public async Task LoadTradesfromDatabaseAndBinance(CryptoDatabase database, CryptoPosition position)
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


        // Daarna de "nieuwe" trades van deze coin ophalen en die worden ook toegevoegd aan die tradelist
        if (position.TradeAccount.AccountType == CryptoTradeAccountType.RealTrading)
            await Task.Run(async () => { await BinanceFetchTrades.FetchTradesForSymbol(position.TradeAccount, position.Symbol); });
    }

    /// <summary>
    /// De administratie bijwerken en de positie bewaren
    /// </summary>
    public static void HandleAdministration(CryptoDatabase databaseThread, CryptoPosition position)
    {
        //TradeTools.CalculateProfit(position); is al gedaan aan het begin van de trade

        if ((position.Status == CryptoPositionStatus.TakeOver) || (position.Status == CryptoPositionStatus.Timeout))
        {
            // Dat is dan niet meer relevant
            position.Profit = 0;
            position.Invested = 0;
            position.Returned = 0;
            position.Commission = 0;
            position.Percentage = 0;
        }

        using (var transaction = databaseThread.BeginTransaction())
        {
            try
            {
                databaseThread.Connection.Update<CryptoPosition>(position, transaction);
                transaction.Commit();
            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                transaction.Rollback();
                throw;
            }
        }

        //if (position.CloseTime.HasValue)
        //{
        //    RemovePosition(position);
        //    GlobalData.AddTextToLogTab(String.Format("Debug: positie removed {0} {1}", position.Symbol.Name, position.Status.ToString()));
        //}
    }

    public static void LoadPosition(CryptoDatabase database, CryptoPosition position)
    {
        string sql = string.Format("select * from positionpart where PositionId={0} order by Id", position.Id);
        foreach (CryptoPositionPart part in database.Connection.Query<CryptoPositionPart>(sql))
        {
            PositionTools.AddPositionPart(position, part);

            // De steps inlezen en dan vervolgens de status van eventueel openstaande orders opvragen
            sql = string.Format("select * from positionstep where PositionId={0} order by Id", position.Id);
            foreach (CryptoPositionStep step in database.Connection.Query<CryptoPositionStep>(sql))
            {
                PositionTools.AddPositionPartStep(part, step);
            }
        }
    }
}
