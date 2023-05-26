using Binance.Net.Enums;

using CryptoSbmScanner.Binance;
using CryptoSbmScanner.Context;
using CryptoSbmScanner.Model;

using Dapper;
using Dapper.Contrib.Extensions;


namespace CryptoSbmScanner.Intern;

public class TradeTools
{

    static public decimal CalculateProfit(CryptoPosition position)
    {
        // Het nieuw algoritme via de steps (later nog eens controleren)
        // De steps doen we dmv queries (ondertussen wel genoeg in het geheugen)
        //string sql = string.Format("select * from positionsteps where PositionId={0} order by id");
        //List<PositionStep> steps = databaseThread.Query<PositionStep>(sql);
        //profileCount = db.QueryFirst<int>($"select count(*) from Profile where Id = @profileId", new { profileId })");


        // Bereken de geinvesteerde waarde (op basis van de administratie)
        decimal sold = 0;
        decimal soldQuantity = 0;

        decimal bought = 0;
        decimal boughtQuantity = 0;

        SortedList<int, int> done = new SortedList<int, int>();
        foreach (CryptoPositionStep step in position.Steps.Values)
        {
            int x;
            if (!done.TryGetValue(step.Id, out x))
            {
                done.Add(step.Id, 0);

                // De status maakt in dit geval niet uit omdat we met "filled" werken!
                if (step.IsBuy)
                {
                    bought += step.QuoteQuantityFilled;
                    boughtQuantity += step.QuantityFilled;
                    string s = string.Format("{0} CalculateProfit bought position={1} step={2} {3} filled={4}", position.Symbol.Name, position.Id, step.Id, step.Name, step.QuoteQuantityFilled);
                    GlobalData.AddTextToLogTab(s);
                }
                else
                {
                    // probleem: de sell zit twee keer in de steps! (=OCO)

                    sold += step.QuoteQuantityFilled;
                    soldQuantity += step.QuantityFilled;
                    string s = string.Format("{0} CalculateProfit bought position={1} step={2} {3} filled={4}", position.Symbol.Name, position.Id, step.Id, step.Name, step.QuoteQuantityFilled);
                    GlobalData.AddTextToLogTab(s);
                }
            }
        }

        // De winst
        position.Invested = bought;
        position.Profit = sold - bought;
        if (position.Invested != 0)
            position.Percentage = 100 * position.Profit / position.Invested;
        else position.Percentage = 0;

        string t = string.Format("{0} CalculateProfit sell invested={1} profit={2} bought={3} sold={4} count={5}",
            position.Symbol.Name, position.Invested, position.Profit, bought, sold, position.Steps.Count);
        GlobalData.AddTextToLogTab(t);

        return boughtQuantity - soldQuantity;
    }




    static private bool CheckOrder(CryptoSymbol symbol, OrderStatus? status, decimal? orderQuantity, long? orderId)
    {
        if ((orderId.HasValue) && (orderQuantity.HasValue))
        {
            decimal tradedQuantity = 0;

            foreach (CryptoTrade trade in symbol.TradeList.Values)
            {
                if (((long)orderId == trade.OrderId))
                    tradedQuantity += trade.Quantity;
            }
            if (tradedQuantity >= orderQuantity)
                return true;
        }
        return false;
    }


    static public async Task RefreshTrades(CryptoDatabase database, CryptoPosition position)
    {
        position.Symbol.TradeList.Clear();

        long orderId = long.MaxValue;
        foreach (CryptoPositionStep step in position.Steps.Values)
        {
            if (step.OrderId < orderId)
                orderId = step.OrderId;
        }

        // Bij het laden zijn niet alle trades in het geheugen ingelezen, dus deze alsnog inladen (of verversen)
        // Probleem, er zitten msec in de position.CreateTime en niet in de Trade.TradeTime (pfft)
        // string sql = string.Format("select * from trades where ExchangeId={0} and SymbolId={1} and TradeTime >='{2}' order by TradeId",
        // position.ExchangeId, position.SymbolId, position.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"));
        string sql = string.Format("select * from trades where ExchangeId={0} and SymbolId={1} and OrderId >='{2}' order by TradeId", position.ExchangeId, position.SymbolId, orderId);
        foreach (CryptoTrade trade in database.Connection.Query<CryptoTrade>(sql))
            GlobalData.AddTrade(trade);

        if ((orderId > 0) && (position.Symbol.LastTradefetched > orderId - 1))
            position.Symbol.LastTradefetched = orderId - 1; //1 lager zetten

        // Haal daarna de "nieuwe" trades van deze coin op
        await Task.Run(async () => { await BinanceFetchTrades.FetchTradesForSymbol(position.Symbol); }); // wachten tot deze klaar is
    }


    /// <summary>
    /// Na het opstarten is er behoefte om openstaande orders en trades te synchroniseren
    /// TODO: Dit is niet de juiste methode, want de trades worden niet opgehaald en toegepast.
    /// </summary>
    static public void CheckPosition(CryptoDatabase database, CryptoPosition position)
    {
        // De APP heeft stil gelegen en we moeten de trades nu toepassen op de positie/steps.
        // De trades zijn ingelezen door de TradeTools.RefreshTrades (staat hierboven)

        foreach (CryptoPositionStep step in position.Steps.Values)
        {
            step.QuantityFilled = 0;
            step.QuoteQuantityFilled = 0;
        }

        // via de trades de steps bijwerken
        foreach (CryptoTrade trade in position.Symbol.TradeList.Values)
        {
            CryptoPositionStep step;
            if (position.Steps.TryGetValue(trade.OrderId, out step))
            {
                step.QuantityFilled += trade.Quantity;
                step.QuoteQuantityFilled += trade.QuoteQuantity;

                // status alleen overschrijven indien het zeker is
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

        int openOrders = 0;
        foreach (CryptoPositionStep step in position.Steps.Values)
        {
            database.Connection.Update<CryptoPositionStep>(step);

            if (step.Status < OrderStatus.Filled)
                openOrders++;
        }

        // ************************
        // Winst en verlies bijwerken
        // ************************
        decimal quantityLeft = CalculateProfit(position);


        // Als er geen muntjes meer over zijn de positie sluiten
        if ((quantityLeft == 0) && (openOrders == 0) && (position.Status == CryptoPositionStatus.positionTrading))
        {
            position.CloseTime = DateTime.UtcNow;
            position.Status = CryptoPositionStatus.positionReady;
            GlobalData.AddTextToLogTab(string.Format("TradeTools: {0} status aangepast naar positie.status={1}", position.Symbol.Name, position.Status));
        }
        database.Connection.Update<CryptoPosition>(position);

        return;

    }

}