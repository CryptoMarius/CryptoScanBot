using CryptoScanBot.Context;
using CryptoScanBot.Enums;
using CryptoScanBot.Exchange;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;
using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Trader;

#if TRADEBOT
public class TradeTools
{
    static public void LoadAssets()
    {
        GlobalData.AddTextToLogTab("Reading asset information");

        // ALLE assets laden
        foreach (var account in GlobalData.ActiveTradeAccountList.Values.ToList())
        {
            account.AssetList.Clear();
        }


        using var database = new CryptoDatabase();
        foreach (CryptoAsset asset in database.Connection.GetAll<CryptoAsset>())
        {
            if (GlobalData.ActiveTradeAccountList.TryGetValue(asset.TradeAccountId, out var account))
            {
                account.AssetList.TryAdd(asset.Name, asset);
            }
        }

    }


    static public void LoadClosedPositions()
    {
        // Alle gesloten posities lezen 
        // TODO - beperken tot de laatste 2 dagen? (en wat handigheden toevoegen wellicht)
        GlobalData.AddTextToLogTab("Reading closed position");
#if SQLDATABASE
        string sql = "select top 250 * from position where not closetime is null order by id desc";
#else
        string sql = "select * from position where not closetime is null order by id desc limit 250";
#endif
        using var database = new CryptoDatabase();
        foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql))
            PositionTools.AddPositionClosed(position);
    }

    static public void LoadOpenPositions()
    {
        // Alle openstaande posities lezen 
        GlobalData.AddTextToLogTab("Reading open position");

        using var database = new CryptoDatabase();
        string sql = "select * from position where closetime is null and status < 2";
        foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql))
        {
            if (!GlobalData.TradeAccountList.TryGetValue(position.TradeAccountId, out CryptoTradeAccount tradeAccount))
                throw new Exception("Geen trade account gevonden");

            PositionTools.AddPosition(tradeAccount, position);
            PositionTools.LoadPosition(database, position);
        }
    }

    static async public Task CheckOpenPositions()
    {
        // De openstaande posities controleren
        GlobalData.AddTextToLogTab("Checking open positions");

        using var database = new CryptoDatabase();
        foreach (CryptoTradeAccount tradeAccount in GlobalData.TradeAccountList.Values.ToList())
        {
            foreach (var positionList in tradeAccount.PositionList.Values.ToList())
            {
                foreach (var position in positionList.Values.ToList())
                {
                    GlobalData.ThreadDoubleCheckPosition.AddToQueue(position, true, "CheckOpenPositions");
                }
            }
        }
    }

    /// <summary>
    /// De break-even prijs berekenen vanuit de parts en steps
    /// </summary>
    private static void CalculateProfitAndBreakEvenPrice(CryptoPosition position)
    {
        //----
        // De positie doorrekene,  er wordt alleen gerekend, geen beslissingen over status
        //https://dappgrid.com/binance-fees-explained-fee-calculation/
        // You should first divide your order size(total) by 100 and then multiply it by your fee rate which 
        // is 0.10 % for VIP 0 / regular users. So, if you buy Bitcoin with 200 USDT, you will basically get
        // $199.8 worth of Bitcoin.To calculate these fees, you can also use our Binance fee calculator:
        // (als je verder gaat dan wordt het vanwege de kickback's tamelijk complex)
        // Op Bybit futures heb je de fundingrates, dat wordt in tijdblokken berekend met varierende fr..

        position.Quantity = 0;
        position.QuantityEntry = 0;
        position.QuantityTakeProfit = 0;
        position.Invested = 0;
        position.Returned = 0;
        position.Reserved = 0;
        position.Commission = 0;
        position.CommissionBase = 0;
        position.CommissionQuote = 0;
        position.RemainingDust = 0;
        bool hasActiveDca = false;

        // Ondersteuning long/short
        CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
        CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();

        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            part.Quantity = 0;
            part.QuantityEntry = 0;
            part.QuantityTakeProfit = 0;
            part.Invested = 0;
            part.Returned = 0;
            part.Reserved = 0;
            part.Commission = 0;
            part.CommissionBase = 0;
            part.CommissionQuote = 0;
            part.RemainingDust = 0;

            int tradeCount = 0;
            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                // || step.Status == CryptoOrderStatus.PartiallyFilled => niet doen, want dan worden de TP's iedere keer verplaatst..
                // Wellicht moet dat gedeelte op een andere manier ingeregeld worden zodat we hier wel de echte BE kunnen uitrekenen?
                if (step.Status.IsFilled())
                {
                    if (step.Side == entryOrderSide)
                    {
                        part.Quantity += step.QuantityFilled;
                        part.QuantityEntry += step.QuantityFilled;
                        part.Invested += step.QuoteQuantityFilled;

                        // Bybit spot fix
                        if (step.CommissionAsset != null && step.CommissionAsset == position.Symbol.Base)
                            part.Quantity -= step.CommissionBase;
                    }
                    else if (step.Side == takeProfitOrderSide)
                    {
                        tradeCount++;
                        part.Quantity -= step.QuantityFilled;
                        part.QuantityTakeProfit += step.QuantityFilled;
                        part.Returned += step.QuoteQuantityFilled;

                        // Bybit spot fix
                        if (step.CommissionAsset != null && step.CommissionAsset == position.Symbol.Base)
                            part.Quantity -= step.CommissionQuote;
                    }
                    part.Commission += step.Commission;
                    part.CommissionBase += step.CommissionBase;
                    part.CommissionQuote += step.CommissionQuote;
                }
                else if (step.Status == CryptoOrderStatus.New || step.Status == CryptoOrderStatus.PartiallyFilled)
                {
                    // Voluit, ook als ie voor 99% gevuld is..
                    part.Reserved += step.Price * step.Quantity;
                }
                part.RemainingDust += step.RemainingDust;

                //string s = string.Format("{0} CalculateProfit bought position={1} part={2} name={3} step={4} {5} price={6} stopprice={7} quantityfilled={8} QuoteQuantityFilled={9}",
                //   position.Symbol.Name, position.Id, part.Id, part.Purpose, step.Id, step.Name, step.Price, step.StopPrice, step.QuantityFilled, step.QuoteQuantityFilled);
                //GlobalData.AddTextToLogTab(s);
            }

            // Rekening houden met de toekomstige kosten van de sell orders.
            // NB: Dit klopt niet 100% als een order gedeeltelijk gevuld wordt!
            if (tradeCount == 0 && !part.CloseTime.HasValue)
                part.Commission *= 2;

            // Voor de BE de originele quantity gebruiken (niet de gecorrigeerde met EntryQuantity-commissionBase dus daarom een correctie)
            if (position.Side == CryptoTradeSide.Long)
            {
                part.Profit = part.Returned - part.Invested - part.Commission;
                part.Percentage = 0m;
                if (part.Invested != 0m)
                    //part.Percentage = 100m * (part.Returned - part.Commission) / part.Invested;
                    part.Percentage = 100m + (100m * part.Profit / part.Invested);
                // TODO: Dit werkt niet op spot ICM dust (bepaling part.BE)
                if (part.Quantity > 0)
                    part.BreakEvenPrice = (part.Invested - part.Returned + part.Commission) / (part.Quantity + part.CommissionBase);
                else
                    part.BreakEvenPrice = 0;
            }
            else
            {
                // Short : We krijgen minder terug, omdraaien
                part.Profit = part.Invested - part.Returned - part.Commission;
                part.Percentage = 0m;
                if (part.Invested != 0m)
                    part.Percentage = 100m + (100m * part.Profit / part.Invested);
                // TODO: Dit werkt niet op spot ICM dust (bepaling part.BE)
                if (part.Quantity > 0)
                    part.BreakEvenPrice = (part.Invested - part.Returned - part.Commission) / (part.Quantity + part.CommissionBase);
                else
                    part.BreakEvenPrice = 0;
            }
            if (part.Invested <= 0)
                hasActiveDca = true;

            //string t = string.Format("{0} CalculateProfit sell invested={1} profit={2} bought={3} sold={4} steps={5}",
            //    position.Symbol.Name, part.Invested, part.Profit, part.Invested, part.Returned, part.Steps.Count);
            //GlobalData.AddTextToLogTab(t);


            position.Quantity += part.Quantity;
            position.QuantityEntry += part.QuantityEntry;
            position.QuantityTakeProfit += part.QuantityTakeProfit;
            position.Invested += part.Invested;
            position.Returned += part.Returned;
            position.Reserved += part.Reserved;
            position.Commission += part.Commission;
            position.RemainingDust += part.RemainingDust;
            position.CommissionBase += part.CommissionBase;
            position.CommissionQuote += part.CommissionQuote;
        }


        // Voor de BE de originele quantity gebruiken (niet de gecorrigeerde met EntryQuantity-commissionBase dus daarom een correctie)
        if (position.Side == CryptoTradeSide.Long)
        {
            position.Profit = position.Returned - position.Invested - position.Commission;
            position.Percentage = 0m;
            if (position.Invested != 0m)
                position.Percentage = 100m + (100m * position.Profit / position.Invested);
            if (position.Quantity > 0 && position.Status == CryptoPositionStatus.Trading)
                position.BreakEvenPrice = (position.Invested - position.Returned + position.Commission) / (position.Quantity + position.CommissionBase);
            else
                position.BreakEvenPrice = 0;
        }
        else
        {
            position.Profit = position.Invested - position.Returned - position.Commission;
            position.Percentage = 0m;
            if (position.Returned != 0m)
                position.Percentage = 100m + (100m * position.Profit / position.Invested);
            if (position.Quantity > 0 && position.Status == CryptoPositionStatus.Trading)
                position.BreakEvenPrice = (position.Invested - position.Returned - position.Commission) / (position.Quantity + position.CommissionBase);
            else
                position.BreakEvenPrice = 0;
        }

        // Correcties omdat de ActiveDca achteraf geintroduceerd is
        position.ActiveDca = hasActiveDca;
    }


    private static void CalculateOrderFeeFromTrades(CryptoSymbol symbol, CryptoPositionStep step)
    {
        ScannerLog.Logger.Trace($"CalculateOrderFeeFromTrades: Positie {symbol.Name} check step={step.OrderId}");

        // Calculate fee from the trades (Bybit V5 doesn't return fee via orders)
        // Afhankelijk van de asset wordt de commissie berekend (debug)
        // Voor de step is dit niet relevant (mits we het omrekenen naar base en quote)
        step.Commission = 0;
        step.CommissionBase = 0;
        step.CommissionQuote = 0;
        step.CommissionAsset = "";
        foreach (CryptoTrade trade in symbol.TradeList.Values.ToList())
        {
            if (trade.OrderId == step.OrderId)
            {
                ScannerLog.Logger.Trace($"CalculateOrderFeeFromTrades: Positie {symbol.Name} check trade={trade.TradeId} order={trade.OrderId}");
                if (trade.CommissionAsset == symbol.Base) // fee in base quantity
                {
                    decimal value = (decimal)trade.Commission * (decimal)trade.Price;
                    step.CommissionBase += (decimal)trade.Commission; // debug, not really relevant after this
                    //step.CommissionQuote += value;
                    step.Commission += value;
                }
                else if (trade.CommissionAsset == symbol.Quote || trade.CommissionAsset == "") // default, fee in quote quantity
                {
                    //decimal value = (decimal)trade.Commission / (decimal)trade.Price;
                    //step.CommissionBase += value;
                    step.CommissionQuote += (decimal)trade.Commission; // debug, not really relevant after this
                    step.Commission += (decimal)trade.Commission;
                }

                step.CommissionAsset = trade.CommissionAsset; // debug, not really relevant after this

                // De order heeft een trade, dus het kan nooit canceled of hoger zijn!
                if (step.Status >= CryptoOrderStatus.Canceled && step.QuantityFilled > 0)
                    step.Status = CryptoOrderStatus.Filled; // Eigenlijk niet de juiste status, maar beter dan canceled?

            }
        }
    }


    /// <summary>
    /// Na het opstarten is er behoefte om openstaande orders en trades te synchroniseren
    /// (dependency: de trades en steps moeten hiervoor ingelezen zijn)
    /// </summary>
    static public async Task CalculatePositionResultsViaOrders(CryptoDatabase database, CryptoPosition position, bool forceCalculation = false)
    {
        // Als de positie reeds gesloten is gaan we niet meer aanpassen
        // (kan gesloten zijn vanwege een verkeerde beslissing, timeout?)
        // Die controle wordt door de ThreadCheckFinishedPosition gedaan
        //if (position.Status == CryptoPositionStatus.Ready)
        //    return;

        bool markedAsReady = false;
        bool orderStatusChanged = false;
        int orderCount = await ExchangeHelper.GetOrdersForPositionAsync(database, position);
        int tradeCount = await ExchangeHelper.GetTradesForPositionAsync(database, position);

        if (orderCount + tradeCount > 0)
            forceCalculation = true;

        ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} {position.Status} force={forceCalculation} orders={orderCount} trades={tradeCount}");


        // De filled quantity in de steps opnieuw opbouwen vanuit de trades
        foreach (CryptoOrder order in position.Symbol.OrderList.Values.ToList())
        {
            if (position.Orders.TryGetValue(order.OrderId, out CryptoPositionStep step))
            {
                if (step.Status != order.Status || step.QuoteQuantityFilled != order.QuoteQuantityFilled || forceCalculation)
                {
                    orderStatusChanged = true;
                    ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} check {order.OrderId}");

                    //  Bij iedere step hoort een part (maar technisch kan alles)
                    CryptoPositionPart part = PositionTools.FindPositionPart(position, step.PositionPartId);
                    if (part == null)
                        throw new Exception("Probleem met het vinden van een part");

                    string s;
                    string msgInfo = $"{position.Symbol.Name} side={order.Side} type={order.Type} status={order.Status} order={order.OrderId} " +
                        $"price={order.AveragePrice?.ToString0()} quantity={order.QuantityFilled?.ToString0()} value={order.QuoteQuantity.ToString0()}";

                    CalculateOrderFeeFromTrades(position.Symbol, step);

                    // Avoid messages to the user if already closed
                    bool isOrderClosed = step.CloseTime.HasValue;

                    // Hebben wij de order geannuleerd? (we gebruiken tenslotte ook een cancel order om orders weg te halen)
                    if (order.Status == CryptoOrderStatus.Canceled)
                    {
                        if (step.CancelInProgress) //|| step.Status > CryptoOrderStatus.Canceled
                        {
                            // Wij hebben de order geannuleerd via de CancelStep/CancelOrder/Status
                            // Probleem is dat de step.Status pas na het annuleren wordt gezet en bewaard. 
                            // Geconstateerd: een cancel via de user stream kan (te) snel gaan

                            // NB: Er is nu wat overlappende code door die CancelInProgress
                            step.CloseTime = order.UpdateTime;
                            ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} check {order.OrderId} -> Canceled by trader");
                        }
                        else
                        {
                            // De gebruiker heeft de positie geannuleerd
                            // Vanaf nu zijn/haar probleem/verantwoordelijkheid
                            step.CloseTime = order.UpdateTime;

                            // Om de statistieken niet te beinvloeden zetten we alles op 0
                            part.Profit = 0;
                            part.Invested = 0;
                            part.Returned = 0;
                            part.Reserved = 0;
                            part.Commission = 0;
                            part.CommissionBase = 0;
                            part.CommissionQuote = 0;
                            part.Percentage = 0;
                            part.CloseTime = order.UpdateTime;

                            //s = string.Format("handletrade#7 {0} positie part cancelled, user takeover?", msgInfo);
                            //GlobalData.AddTextToLogTab(s);
                            //GlobalData.AddTextToTelegram(s);

                            position.Profit = 0;
                            position.Invested = 0;
                            position.Returned = 0;
                            position.Reserved = 0;
                            position.Commission = 0;
                            position.CommissionBase = 0;
                            position.CommissionQuote = 0;
                            position.Percentage = 0;
                            position.CloseTime = order.UpdateTime;
                            position.Status = CryptoPositionStatus.TakeOver;

                            // Geen melding geven bij afgesloten orders
                            if (!isOrderClosed)
                            {
                                s = $"handletrade#7 {msgInfo} positie cancelled, user takeover? position.status={position.Status}";
                                GlobalData.AddTextToLogTab(s);
                                GlobalData.AddTextToTelegram(s, position);
                            }
                            ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} check {order.OrderId} -> Canceled by user");
                        }
                    }
                    else if (order.Status.IsFilled())
                    {
                        ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} check {order.OrderId} -> IsFilled");
                        s = $"handletrade {msgInfo}";

                        // Statistics entry or take profit order.
                        step.CloseTime = order.UpdateTime;

                        // Overnemen, kan aangepast zijn (exchange is alway's leading)
                        // Bij nader inzien geeft dit problemen met de partially filled, afgesterd


                        // Bybit Spot Market order, niet alles kan gevuld worden vanwege tick sizes enz.
                        if (order.Status == CryptoOrderStatus.PartiallyAndClosed && order.Type == CryptoOrderType.Market)
                        {
                            if (!isOrderClosed)
                            {
                                ScannerLog.Logger.Trace($"TradeTools.CalculatePositionResultsViaOrders: {position.Symbol.Name} status=PartiallyAndClosed reduced quantity from {step.Quantity} to {order.QuantityFilled}");
                            }

                            // Bybit Spot: Bij een market order bevat de order.Quantity de USDT value en de Order.Price is leeg
                            // We proberen hier iets te repareren in de originele opdracht (dat is tamelijk vervelend)

                            // Notitie: Bij nader inzien geeft dit problemen met de partially filled? Hoezo?
                            // Want op een PartialFill volgt namelijk ook een PartiallyAndClosed!!! Verdorie!


                            step.Price = (decimal)order.Price;
                            step.Quantity = (decimal)order.QuantityFilled;
                            //step.QuoteQuantity = (decimal)order.QuoteQuantityFilled; is er niet
                        }

                        step.AveragePrice = (decimal)order.AveragePrice;
                        step.QuantityFilled = (decimal)order.QuantityFilled;
                        step.QuoteQuantityFilled = (decimal)order.QuoteQuantityFilled;

                        // Needed for Bybit Spot + market order && status=CryptoOrderStatus.PartiallyAndClosed
                        // (the exchange sligtly diverted from the task, adapt to the new situation)
                        // (Maar achteraf: vraag ik me af of dit daadwerkelijk het geval is, nakijken!)

                        //if (order.Status == CryptoOrderStatus.PartiallyAndClosed)
                        //    step.Quantity = order.Quantity;

                        // Can't be cancelled anymore if its filled
                        if (step.Status > CryptoOrderStatus.Canceled)
                            step.Status = CryptoOrderStatus.Filled;


                        // Geen melding geven bij afgesloten orders
                        if (!isOrderClosed)
                        {
                            // Statistics position
                            position.Reposition = true;
                            ScannerLog.Logger.Trace($"TradeTools.CalculatePositionResultsViaOrders: {position.Symbol.Name} take profit -> position.Reposition = true");
                        }

                        // Statistics symbol (cooldown)
                        position.Symbol.LastTradeDate = order.UpdateTime;


                        CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
                        if (step.Side == entryOrderSide)
                        {
                            s += " entry";

                            // Als er 1 (of meerdere trades zijn) dan zitten we in de trade (de user ticker valt wel eens stil)
                            // Eventuele handmatige correctie geven daarna problemen (we mogen eigenlijk niet handmatig corrigeren)
                            // (Dit geeft te denken aan de problemen als we straks een lopende order gaan opnemen als een positie)
                            if (position.Status == CryptoPositionStatus.Waiting)
                            {
                                position.CloseTime = null; // reopen
                                position.UpdateTime = order.UpdateTime;
                                position.Status = CryptoPositionStatus.Trading;
                            }

                            ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} check {order.OrderId} -> IsFilled (entry)");
                        }


                        CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();
                        if (step.Side == takeProfitOrderSide)
                        {
                            part.CloseTime = order.UpdateTime;
                            if (position.Status == CryptoPositionStatus.Ready)
                                s += $" ready"; //  ({position.Percentage:N2}%)
                            else
                                s += $" takeprofit"; //  ({part.Percentage:N2}%) is die wel berekend?

                            ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} check {order.OrderId} -> IsFilled (takeprofit)");
                        }

                        // Geen melding geven bij afgesloten orders
                        if (!isOrderClosed)
                        {
                            GlobalData.AddTextToLogTab(s);
                            GlobalData.AddTextToTelegram(s, position);
                        }
                    }

                    // De reden van annuleren niet overschrijven
                    if (step.Status < CryptoOrderStatus.Canceled)
                    {
                        step.Status = order.Status;
                        ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} check {order.OrderId} -> set status to {order.Status}");
                    }
                }
            }
        }


        if (orderStatusChanged || forceCalculation)
        {
            CalculateProfitAndBreakEvenPrice(position);


            // Er is in geinvesteerd en dus moet de positie ten minste actief zijn
            if (position.QuantityEntry != 0 && position.Status == CryptoPositionStatus.Waiting)
            {
                orderStatusChanged = true;
                position.CloseTime = null;
                position.Reposition = true;
                position.UpdateTime = DateTime.UtcNow;
                position.Status = CryptoPositionStatus.Trading;
                GlobalData.AddTextToLogTab($"TradeTools: Position {position.Symbol.Name} status aangepast naar {position.Status} (should not occur)");
            }

            // Als alles verkocht is de positie alsnog sluiten. Maar wanneer weet je of alles echt verkocht is?
            if (position.QuantityEntry != 0 && position.Status == CryptoPositionStatus.Trading && position.QuantityEntry - position.QuantityTakeProfit - position.RemainingDust - position.CommissionBase <= 0)
            {
                markedAsReady = true;
                orderStatusChanged = true;
                position.Reposition = false;
                position.CloseTime = DateTime.UtcNow;
                position.UpdateTime = DateTime.UtcNow;
                position.Status = CryptoPositionStatus.Ready;
                GlobalData.AddTextToLogTab($"TradeTools: Position {position.Symbol.Name} status aangepast naar {position.Status}");
            }


            // Hebben we per abuis een part afgesloten (vanwege niet opgemerkte trades) terwijl de positie eigenlijk nog openstaat?
            // Achteraf worden de trades alsnog ingeladen, wordt de positie opengezet, maar de part blijft gelosten en de trader doets niets...
            if (!position.CloseTime.HasValue)
            {
                foreach (CryptoPositionPart part in position.Parts.Values.ToList())
                {
                    if (part.CloseTime.HasValue && part.Invested != 0 && part.Returned == 0)
                    {
                        part.CloseTime = null;
                        orderStatusChanged = true;
                        GlobalData.AddTextToLogTab($"TradeTools: Part {position.Symbol.Name} opnieuw opengezet vanwege correctie {position.Status}");
                    }
                }
            }


            // De positie bewaren (dit kost nogal wat tijd, dus extra controle of het nodig is)
            if (orderStatusChanged || markedAsReady)
            {
                foreach (CryptoPositionPart part in position.Parts.Values.ToList())
                {
                    foreach (CryptoPositionStep step in part.Steps.Values.ToList())
                        database.Connection.Update<CryptoPositionStep>(step);
                    database.Connection.Update<CryptoPositionPart>(part);
                }
                database.Connection.Update<CryptoPosition>(position);
            }


            // Een laatste controle laten uitvoeren en de nog openstaande DCA orders afsluiten/verplaatsen
            if (markedAsReady)
            {
                GlobalData.ThreadDoubleCheckPosition.AddToQueue(position, true, "CalculatePositionResultsViaOrders positie ready", true);
            }
        }
    }



    static public async Task LoadOrdersFromDatabaseAndExchangeAsync(CryptoDatabase database, CryptoPosition position) //, bool loadFromExchange = true
    {
        if (!position.Symbol.HasOrdersAndTradesLoaded)
        {
            ScannerLog.Logger.Trace($"TradeTools.LoadOrdersFromDatabaseAndExchangeAsync: Position {position.Symbol.Name} loading orders and trades from database");

            // Vanwege tijd afrondingen (msec)
            DateTime from = position.CreateTime.AddMinutes(-1);

            //// Bij het laden zijn niet alle trades in het geheugen ingelezen, dus deze alsnog inladen (of verversen)
            string sql = "select * from [order] where SymbolId=@symbolId and CreateTime >= @fromDate order by CreateTime";
            foreach (CryptoOrder order in database.Connection.Query<CryptoOrder>(sql, new { symbolId = position.SymbolId, fromDate = from }))
                GlobalData.AddOrder(order, false);

            sql = "select * from [trade] where SymbolId=@symbolId and TradeTime >= @fromDate order by TradeTime";
            foreach (CryptoTrade trade in database.Connection.Query<CryptoTrade>(sql, new { symbolId = position.SymbolId, fromDate = from }))
                GlobalData.AddTrade(trade, false);

            position.Symbol.HasOrdersAndTradesLoaded = true;
        }

        // Daarna de "nieuwe" orders van deze coin ophalen en die toegevoegen aan dezelfde orderlist
        if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading) // && loadFromExchange
            await ExchangeHelper.GetOrdersForPositionAsync(database, position);

        // Daarna de "nieuwe" orders van deze coin ophalen en die toegevoegen aan dezelfde orderlist
        if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading) // && loadFromExchange
            await ExchangeHelper.GetTradesForPositionAsync(database, position);
    }


    static public async Task<(bool cancelled, TradeParams tradeParams)> CancelOrder(CryptoDatabase database, CryptoPosition position, CryptoPositionPart part,
        CryptoPositionStep step, DateTime currentTime, CryptoOrderStatus newStatus = CryptoOrderStatus.Expired)
    {
        position.UpdateTime = currentTime;
        database.Connection.Update<CryptoPosition>(position);

        // Aankondiging dat we deze order gaan annuleren (de tradehandler weet dan dat wij het waren en het niet de user was via de exchange)
        step.CancelInProgress = true;

        // Annuleer de order
        var exchangeApi = ExchangeHelper.GetExchangeInstance(GlobalData.Settings.General.ExchangeId);
        var result = await exchangeApi.Cancel(position.TradeAccount, position.Symbol, step);
        if (result.succes)
        {
            step.Status = newStatus;
            step.CloseTime = currentTime;
            database.Connection.Update<CryptoPositionStep>(step);

            if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.PaperTrade)
                PaperAssets.Change(position.TradeAccount, position.Symbol, result.tradeParams.OrderSide,
                    CryptoOrderStatus.Canceled, result.tradeParams.Quantity, result.tradeParams.QuoteQuantity);

        }
        return result;
    }

    static public async Task PlaceTakeProfitOrderAtPrice(CryptoDatabase database, CryptoPosition position, CryptoPositionPart part,
        decimal takeProfitPrice, DateTime currentTime, string extraText)
    {
        // Probleem? Wat als het plaatsen van eem order fout gaat? (hoe vangen we de fout op en hoe herstellen we dat?
        // Binance is een bitch af en toe!). Met name Binance wilde na het annuleren wel eens de assets niet
        // vrijgeven waardoor de assets/pf niet snel genoeg bijgewerkt werd en de volgende opdracht dan de fout
        // in zou kunnen gaan. Geld voor alles wat we in deze tool doen, qua buy en sell gaat de herkansing wel 
        // goed, ook al zal je dan soms een repeterende fout voorbij zien komen (iedere minuut)

        decimal takeProfitQuantity = part.Quantity;
        decimal takeProfitQuantityOriginal = part.Quantity;
        takeProfitQuantity = takeProfitQuantity.Clamp(position.Symbol.QuantityMinimum, position.Symbol.QuantityMaximum, position.Symbol.QuantityTickSize);

        string text = $"{position.Symbol.Name} quantity part={part.Quantity}, rounded={takeProfitQuantity}, expected dust = {takeProfitQuantityOriginal - takeProfitQuantity}";
        GlobalData.AddTextToLogTab(text);

        CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();

        (bool result, TradeParams tradeParams) result;
        var exchangeApi = ExchangeHelper.GetExchangeInstance(GlobalData.Settings.General.ExchangeId);
        result = await exchangeApi.PlaceOrder(database,
            position.TradeAccount, position.Symbol, position.Side, currentTime,
            CryptoOrderType.Limit, takeProfitOrderSide, takeProfitQuantity, takeProfitPrice, null, null);

        if (result.result)
        {
            if (part.Purpose == CryptoPartPurpose.Entry)
                position.ProfitPrice = result.tradeParams.Price;
            var step = PositionTools.CreatePositionStep(position, part, result.tradeParams);
            step.RemainingDust = takeProfitQuantityOriginal - takeProfitQuantity; // de verwachte dust
            database.Connection.Insert<CryptoPositionStep>(step);
            PositionTools.AddPositionPartStep(part, step);

            part.ProfitMethod = CryptoEntryOrProfitMethod.FixedPercentage;
            database.Connection.Update<CryptoPositionPart>(part);
            database.Connection.Update<CryptoPosition>(position);

            if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.PaperTrade)
                PaperAssets.Change(position.TradeAccount, position.Symbol, result.tradeParams.OrderSide,
                    step.Status, result.tradeParams.Quantity, result.tradeParams.QuoteQuantity);
        }
        ExchangeBase.Dump(position, result.result, result.tradeParams, extraText);
    }


    /// <summary>
    /// Bepaal het entry bedrag 
    /// </summary>
    public static decimal GetEntryAmount(CryptoSymbol symbol, decimal currentAssetQuantity, CryptoTradeAccountType tradeAccountType)
    {
        // TODO Er is geen percentage bij papertrading mogelijk (of we moeten een werkende papertrade asset management implementeren)

        // Heeft de gebruiker een percentage of een aantal ingegeven?
        if (tradeAccountType == CryptoTradeAccountType.RealTrading && symbol.QuoteData.EntryPercentage > 0m)
            return symbol.QuoteData.EntryPercentage * currentAssetQuantity / 100;
        else
            return symbol.QuoteData.EntryAmount;
    }


    public static decimal CorrectEntryQuantityIfWayLess(CryptoSymbol symbol, decimal entryValue, decimal entryQuantity, decimal entryPrice)
    {
        // Daar kunnen we niets mee aanvangen
        if (entryValue == 0 || entryQuantity == 0 || entryPrice == 0)
            return 0;


        // Is er een grote afwijking van tenminste -X%
        decimal clampedEntryValue = entryQuantity * entryPrice;
        decimal percentage = 100 * (clampedEntryValue - entryValue) / entryValue;

        // Het verschil is te groot, hier kunnen we niet instappen
        if (percentage > 125)
        {
            GlobalData.AddTextToLogTab($"{symbol.Name} vanwege de quantity ticksize {symbol.QuantityTickSize} kunnen we niet instappen met de veel te hoge {clampedEntryValue} ({percentage:N2}%) (DEBUG)");
            return 0;
        }

        if (clampedEntryValue < entryValue)
        {
            // Wellicht er iets bijtellen?
            decimal newEntryQuantity = entryQuantity + symbol.QuantityTickSize;
            decimal newEntryValue = newEntryQuantity * entryPrice;
            percentage = 100 * (newEntryValue - entryValue) / entryValue; // 100 * (16 - 2.50) / 2.50 = 540
            if (percentage.IsBetween(-2.5m, 2.5m)) 
            {
                // 2.5% marge is okay, we willen er niet te ver boven
                GlobalData.AddTextToLogTab($"{symbol.Name} vanwege de quantity ticksize {symbol.QuantityTickSize} is de entry value verhoogd naar {newEntryValue} ({percentage:N2}%) (DEBUG)");
                return newEntryQuantity;
            }
        }

        return entryQuantity;
    }
}
#endif
