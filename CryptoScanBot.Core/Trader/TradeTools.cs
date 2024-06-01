using System.Text;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Trader;

#if TRADEBOT
public class TradeTools
{
    public static void LoadAssets()
    {
        GlobalData.AddTextToLogTab("Reading asset information");

        if (GlobalData.ActiveAccount != null)
        {
            // ALLE assets laden
            GlobalData.ActiveAccount.AssetList.Clear();

            using var database = new CryptoDatabase();
            foreach (CryptoAsset asset in database.Connection.GetAll<CryptoAsset>())
            {
                GlobalData.ActiveAccount.AssetList.TryAdd(asset.Name, asset);
            }
        }
    }

    public static void LoadClosedPositions()
    {
        // Alle gesloten posities lezen 
        // TODO - beperken tot de laatste 2 dagen? (en wat handigheden toevoegen wellicht)
        GlobalData.AddTextToLogTab("Reading closed position");
        string sql = "select * from position where not closetime is null order by id desc limit 250";
        using var database = new CryptoDatabase();
        foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql))
            PositionTools.AddPositionClosed(position);
    }

    public static void LoadOpenPositions()
    {
        // Alle openstaande posities lezen 
        GlobalData.AddTextToLogTab("Reading open position");

        using var database = new CryptoDatabase();
        string sql = "select * from position where closetime is null and status < 2";
        foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql))
        {
            if (!GlobalData.TradeAccountList.TryGetValue(position.TradeAccountId, out CryptoTradeAccount? tradeAccount))
                throw new Exception("Geen trade account gevonden");

            PositionTools.AddPosition(tradeAccount, position);
            PositionTools.LoadPosition(database, position);
        }
    }

    public static async Task CheckOpenPositions()
    {
        // De openstaande posities controleren
        GlobalData.AddTextToLogTab("Checking open positions");

        using var database = new CryptoDatabase();
        foreach (CryptoTradeAccount tradeAccount in GlobalData.TradeAccountList.Values.ToList())
        {
            foreach (var position in tradeAccount.PositionList.Values.ToList())
            {
                position.ForceCheckPosition = true;
                await GlobalData.ThreadDoubleCheckPosition!.AddToQueue(position);
            }
        }
    }


    private static StringBuilder DumpPosition(CryptoPosition position)
    {
        StringBuilder stringBuilder = new(); // debug
        stringBuilder.AppendLine($"positie {position.Symbol.Name}");
        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            stringBuilder.AppendLine($"dca {part.PartNumber} {part.Invested} {part.Commission} {part.CommissionQuote} {part.CommissionBase}");
            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                stringBuilder.AppendLine($"step {step.Side} {step.Status} {step.OrderId} {step.QuoteQuantityFilled} {step.Commission} {step.CommissionQuote} {step.CommissionBase}");
            }
        }
        stringBuilder.AppendLine($"berekening={position.BreakEvenPrice}=({position.Invested}-{position.Returned}+{position.Commission})/({position.Quantity} + {position.CommissionBase})");
        return stringBuilder;
    }

    /// <summary>
    /// De break-even prijs berekenen vanuit de parts en steps
    /// </summary>
    public static void CalculateProfitAndBreakEvenPrice(CryptoPosition position, decimal? takeProfitPrice = null)
    {
        //----
        // De positie doorrekene,  er wordt alleen gerekend, geen beslissingen over status
        //https://dappgrid.com/binance-fees-explained-fee-calculation/
        // You should first divide your order size(total) by 100 and then multiply it by your fee rate which 
        // is 0.10 % for VIP 0 / regular users. So, if you buy Bitcoin with 200 USDT, you will basically get
        // $199.8 worth of Bitcoin.To calculate these fees, you can also use our Binance fee calculator:
        // (als je verder gaat dan wordt het vanwege de kickback's tamelijk complex)
        // Op Bybit futures heb je de fundingrates, dat wordt in tijdblokken berekend met varierende fr..
        StringBuilder stringBuilderOld = DumpPosition(position);

        position.Profit = 0;
        position.BreakEvenPrice = 0;
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

        position.PartCount = 0;
        position.ActiveDca = false;

        // Ondersteuning long/short
        CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
        CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();

        decimal totalValue = 0;
        decimal totalQuantity = 0;

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

            //int tradeCount = 0;
            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                // || step.Status == CryptoOrderStatus.PartiallyFilled => niet doen, want dan worden de TP's iedere keer verplaatst..
                // Wellicht moet dat gedeelte op een andere manier ingeregeld worden zodat we hier wel de echte BE kunnen uitrekenen?
                if (step.Status.IsFilled()) // Filled or PartiallyFilledAndClosed
                {
                    decimal filledQuantity = step.QuantityFilled - step.CommissionBase;
                    if (step.Side == entryOrderSide)
                    {
                        part.Quantity += filledQuantity;
                        part.QuantityEntry += filledQuantity;
                        part.Invested += step.AveragePrice * filledQuantity;

                        totalValue += step.AveragePrice * filledQuantity;
                        totalQuantity += filledQuantity;

                        // Bybit spot fix
                        //if (step.CommissionAsset != null && step.CommissionAsset == position.Symbol.Base)
                        //    part.Quantity -= step.CommissionBase;
                    }
                    else if (step.Side == takeProfitOrderSide)
                    {
                        part.Quantity -= filledQuantity;
                        part.QuantityTakeProfit += filledQuantity;
                        part.Returned += step.AveragePrice * filledQuantity;

                        // Bybit spot fix
                        //if (step.CommissionAsset != null && step.CommissionAsset == position.Symbol.Base)
                        //    part.Quantity -= step.CommissionQuote;
                    }
                    // De berekende kosten
                    part.Commission += step.Commission;
                    part.CommissionBase += step.CommissionBase;
                    part.CommissionQuote += step.CommissionQuote;
                }
                else if (step.Status == CryptoOrderStatus.New || step.Status == CryptoOrderStatus.PartiallyFilled)
                {
                    // Blijft een constante totdat de order gevuld is
                    decimal value = step.Price * step.Quantity;
                    // Predicted TP commission (but this gives problems because tp's are later adjusted)
                    //step.Commission = (decimal)position.Exchange.FeeRate * value / 100m; // Estimated commission in quote
                    //part.Commission += step.Commission;
                    if (step.Side == entryOrderSide)
                        part.Reserved += value;
                }
                part.RemainingDust += step.RemainingDust;

                //string s = string.Format("{0} CalculateProfit bought position={1} part={2} name={3} step={4} {5} price={6} stopprice={7} quantityfilled={8} QuoteQuantityFilled={9}",
                //   position.Symbol.Name, position.Id, part.Id, part.Purpose, step.Id, step.Name, step.Price, step.StopPrice, step.QuantityFilled, step.QuoteQuantityFilled);
                //GlobalData.AddTextToLogTab(s);
            }


            // Voor de BE de originele quantity gebruiken (niet de gecorrigeerde met EntryQuantity-commissionBase dus daarom een correctie)
            if (position.Side == CryptoTradeSide.Long)
            {
                part.Profit = part.Returned - part.Invested - part.Commission;
                part.Percentage = 0m;
                if (part.Invested != 0m)
                    part.Percentage = 100m + (100m * part.Profit / part.Invested);

                if (part.Quantity > 0)
                    //part.BreakEvenPrice = (part.Invested - part.Returned + part.Commission) / (part.Quantity + part.CommissionBase);
                    part.BreakEvenPrice = (part.Invested - part.Returned + part.Commission) / part.Quantity;
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

                if (part.Quantity > 0)
                    //part.BreakEvenPrice = (part.Invested - part.Returned - part.Commission) / (part.Quantity + part.CommissionBase);
                    part.BreakEvenPrice = (part.Invested - part.Returned - part.Commission) / part.Quantity;
                else
                    part.BreakEvenPrice = 0;
            }

            // De parts opnieuw instellen
            if (part.Purpose == CryptoPartPurpose.Entry)
                part.PartNumber = 0;
            if (part.Purpose == CryptoPartPurpose.Dca)
            {
                if (part.Invested > 0)
                {
                    position.PartCount++;
                    part.PartNumber = position.PartCount;
                }
                else
                {
                    position.ActiveDca = true;
                    part.PartNumber = position.PartCount + 1;
                }
            }
            // fix..
            if (part.Purpose == CryptoPartPurpose.TakeProfit)
                part.PartNumber = 9999;


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



        // Predicted commission (in quote), we need a fixed avg-price to calculate the TP-commission
        // (this is not the exact tp-commission, but we need to calculating anything)
        // if the position is closed the position.Quantity is 0 and the real commission will be calculated
        decimal avgPrice = totalValue / totalQuantity;
        decimal predictedCommission = avgPrice * (decimal)position.Exchange.FeeRate * position.Quantity / 100m;


        decimal BreakEvenPriceOld = position.BreakEvenPrice;
        if (position.Side == CryptoTradeSide.Long)
        {
            if (position.Quantity > 0 && position.Status == CryptoPositionStatus.Trading)
                //position.BreakEvenPrice = (position.Invested - position.Returned + position.Commission - predictedCommission) / (position.Quantity + position.CommissionBase);
                position.BreakEvenPrice = (position.Invested - position.Returned + position.Commission + predictedCommission) / position.Quantity;
            else
                position.BreakEvenPrice = (decimal)position.ProfitPrice; // last TP-price

            decimal invested = position.Invested;
            if (position.RemainingDust > 0)
                invested -= position.RemainingDust * position.BreakEvenPrice;

            position.Profit = position.Returned - invested - position.Commission;
            position.Percentage = 0m;
            if (position.Invested != 0m)
                position.Percentage = 100m + (100m * position.Profit / invested);
        }
        else
        {
            if (position.Quantity > 0 && position.Status == CryptoPositionStatus.Trading)
                //position.BreakEvenPrice = (position.Invested - position.Returned - position.Commission - predictedCommission) / (position.Quantity + position.CommissionBase);
                position.BreakEvenPrice = (position.Invested - position.Returned - position.Commission - predictedCommission) / position.Quantity;
            else
                position.BreakEvenPrice = (decimal)position.ProfitPrice;

            // TODO: Test if this is the right formula? (think I messed up here?)
            decimal invested = position.Invested;
            if (position.RemainingDust > 0)
                invested -= position.RemainingDust * position.BreakEvenPrice;

            position.Profit = invested - position.Returned - position.Commission;
            position.Percentage = 0m;
            if (position.Returned != 0m)
                position.Percentage = 100m + (100m * position.Profit / invested);
        }
        if (BreakEvenPriceOld != position.BreakEvenPrice)
        {
            ScannerLog.Logger.Trace($"{position.Symbol.Name} aanpassing BE van {BreakEvenPriceOld} naar {position.BreakEvenPrice}");
            ScannerLog.Logger.Trace(stringBuilderOld);
            StringBuilder stringBuilderNew = DumpPosition(position);
            ScannerLog.Logger.Trace(stringBuilderNew);
        }
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
        int count = await LoadOrdersFromDatabaseAndExchangeAsync(database, position);
        if (count > 0)
            forceCalculation = true;

        ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} {position.Status} force={forceCalculation}");


        // Build the filled quantity via the present orders & calculate fees
        DateTime? lastDateTime = null;
        foreach (CryptoOrder order in position.Symbol.OrderList.Values.ToList())
        {
            if (position.Orders.TryGetValue(order.OrderId, out CryptoPositionStep? step))
            {
                // Remember the last datetime so we can close the position with this date if needed
                if (lastDateTime == null || order.UpdateTime > lastDateTime)
                    lastDateTime = order.UpdateTime;

                if (step.Status != order.Status || step.QuoteQuantityFilled != order.QuoteQuantityFilled || forceCalculation)
                {
                    orderStatusChanged = true;
                    ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} check {order.OrderId}");

                    CryptoPositionPart part = PositionTools.FindPositionPart(position, step.PositionPartId) ?? throw new Exception("Problem finding parent part");
                    string msgInfo = $"{position.Symbol.Name} " +
                        $"{order.Status.ToString().ToLower()} " + // PartiallyAndClosed -> Filled
                        $"{part.Purpose.ToString().ToLower()} " +
                        $"{order.Side.ToString().ToLower()} " +
                        $"{order.Type.ToString().ToLower()} " +
                        $"order={order.OrderId} " +
                        $"price={order.AveragePrice?.ToString0()} " +
                        $"quantity={order.QuantityFilled?.ToString0()} " +
                        $"value={order.QuoteQuantity.ToString0(position.Symbol.QuoteData.DisplayFormat)}";

                    CalculateOrderFeeFromTrades(position.Symbol, step);

                    // Avoid messages to the user if already closed
                    bool isOrderClosed = step.CloseTime.HasValue;

                    // Hebben wij de order geannuleerd? (we gebruiken tenslotte ook een cancel order om orders weg te halen)
                    if (order.Status == CryptoOrderStatus.Canceled)
                    {
                        if (step.CancelInProgress)
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
                                string s = $"{msgInfo} user takeover";
                                GlobalData.AddTextToLogTab(s);
                                GlobalData.AddTextToTelegram(s, position);
                            }
                            ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} check {order.OrderId} -> Canceled by user");
                        }
                    }
                    else if (order.Status.IsFilled())
                    {
                        ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} check {order.OrderId} -> IsFilled");

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


                            step.Price = order.Price ?? 0;
                            step.Quantity = order.QuantityFilled ?? 0;
                            //step.QuoteQuantity = (decimal)order.QuoteQuantityFilled; is er niet
                        }

                        step.AveragePrice = order.AveragePrice ?? 0;
                        step.QuantityFilled = order.QuantityFilled ?? 0;
                        step.QuoteQuantityFilled = order.QuoteQuantityFilled ?? 0;

                        // Needed for Bybit Spot + market order && status=CryptoOrderStatus.PartiallyAndClosed
                        // (the exchange sligtly diverted from the task, adapt to the new situation)
                        // (Maar achteraf: vraag ik me af of dit daadwerkelijk het geval is, nakijken!)

                        //if (order.Status == CryptoOrderStatus.PartiallyAndClosed)
                        //    step.Quantity = order.Quantity;

                        // Fix, it cannot be status=cancelled anymore if it was filled...
                        // (doubt if this will happen, but it did in the past <timing>)
                        if (step.Status > CryptoOrderStatus.Canceled)
                            step.Status = CryptoOrderStatus.Filled;


                        // Geen melding geven bij afgesloten orders
                        if (!isOrderClosed)
                        {
                            // Statistics position
                            position.Reposition = true;
                            ScannerLog.Logger.Trace($"TradeTools.CalculatePositionResultsViaOrders: {position.Symbol.Name} take profit -> position.Reposition = true");
                        }

                        // Sluit de part als het gevuld is (probleem igv meerdere entries)
                        //if (part.Purpose != CryptoPartPurpose.TakeProfit && !part.CloseTime.HasValue)
                        //    part.CloseTime = step.CloseTime;


                        // Statistics symbol (cooldown)
                        position.Symbol.LastTradeDate = order.UpdateTime;


                        CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
                        if (step.Side == entryOrderSide)
                        {
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
                            ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} check {order.OrderId} -> IsFilled (takeprofit)");
                        }

                        // Geen melding geven bij afgesloten orders
                        if (!isOrderClosed)
                        {
                            GlobalData.AddTextToLogTab(msgInfo);
                            GlobalData.AddTextToTelegram(msgInfo, position);
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

            if (lastDateTime == null)
                lastDateTime = DateTime.UtcNow; ;

            // Er is in geinvesteerd en dus moet de positie ten minste actief zijn
            if (position.QuantityEntry != 0 && position.Status == CryptoPositionStatus.Waiting)
            {
                orderStatusChanged = true;
                position.CloseTime = null;
                position.Reposition = true;
                position.UpdateTime = lastDateTime;
                position.Status = CryptoPositionStatus.Trading;
                GlobalData.AddTextToLogTab($"TradeTools: Position {position.Symbol.Name} status aangepast naar {position.Status} (should not occur)");
            }

            // Als alles verkocht is de positie alsnog sluiten. Maar wanneer weet je of alles echt verkocht is?
            if (position.QuantityEntry != 0 && position.Status == CryptoPositionStatus.Trading)
            {
                // Close if q=0 or less than the minimum amount we can sell
                //decimal remaining = position.QuantityEntry - position.QuantityTakeProfit - position.RemainingDust - position.CommissionBase;
                decimal remaining = position.QuantityEntry - position.QuantityTakeProfit - position.RemainingDust;
                if (remaining <= 0 || remaining < position.Symbol.QuantityMinimum || 
                    remaining * position.Symbol.LastPrice < position.Symbol.QuoteValueMinimum)
                //if (remaining <= 0)
                {
                    markedAsReady = true;
                    orderStatusChanged = true;
                    position.Reposition = false;
                    position.CloseTime = lastDateTime;
                    position.UpdateTime = lastDateTime;
                    position.Status = CryptoPositionStatus.Ready;

                    GlobalData.AddTextToLogTab($"TradeTools: Position {position.Symbol.Name} status aangepast naar {position.Status}");
                    GlobalData.AddTextToLogTab($"TradeTools: debug ? closing if ({remaining} <= 0)");
                    GlobalData.AddTextToLogTab($"TradeTools: debug ? closing if ({remaining} < {position.Symbol.QuantityMinimum})");
                    GlobalData.AddTextToLogTab($"TradeTools: debug ? closing if ({remaining * position.Symbol.LastPrice} < {position.Symbol.QuoteValueMinimum})");

                    GlobalData.AddTextToLogTab($"TradeTools: debug ? Symbol.LastPrice={position.Symbol.LastPrice}");
                    GlobalData.AddTextToLogTab($"TradeTools: debug ? Symbol.QuantityMinimum={position.Symbol.QuantityMinimum}");
                    GlobalData.AddTextToLogTab($"TradeTools: debug ? Symbol.QuoteValueMinimum={position.Symbol.QuoteValueMinimum}");
                    GlobalData.AddTextToLogTab($"TradeTools: debug ? QuantityEntry={position.QuantityEntry}");
                    GlobalData.AddTextToLogTab($"TradeTools: debug ? QuantityTakeProfit={position.QuantityTakeProfit}");
                    GlobalData.AddTextToLogTab($"TradeTools: debug ? RemainingDust={position.RemainingDust}");
                    GlobalData.AddTextToLogTab($"TradeTools: debug ? remaining={remaining}");
                }
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
                position.ForceCheckPosition = true;
                position.DelayUntil = DateTime.UtcNow.AddSeconds(10);
                await GlobalData.ThreadDoubleCheckPosition!.AddToQueue(position);
            }
        }
    }



    static private async Task<int> LoadOrdersFromDatabaseAndExchangeAsync(CryptoDatabase database, CryptoPosition position)
    {
        if (!position.Symbol.HasOrdersAndTradesLoaded)
        {
            //GlobalData.AddTextToLogTab($"TradeTools.LoadOrdersFromDatabaseAndExchangeAsync: Position {position.Symbol.Name} loading orders and trades from database {position.CreateTime}");
            ScannerLog.Logger.Trace($"TradeTools.LoadOrdersFromDatabaseAndExchangeAsync: Position {position.Symbol.Name} loading orders and trades from database {position.CreateTime}");

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

        int count = 0;

        // Daarna de "nieuwe" orders van deze coin ophalen en die toegevoegen aan dezelfde orderlist
        if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading) // && loadFromExchange
            count += await ExchangeHelper.GetOrdersAsync(database, position);

        // Daarna de "nieuwe" orders van deze coin ophalen en die toegevoegen aan dezelfde orderlist
        if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading) // && loadFromExchange
            count += await ExchangeHelper.GetTradesAsync(database, position);

        return count;
    }


    static public async Task<(bool cancelled, TradeParams tradeParams)> CancelOrder(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step,
        DateTime currentTime, CryptoOrderStatus newStatus, string cancelReason)
    {
        ScannerLog.Logger.Trace($"{position.Symbol.Name} {part.Purpose} cancelling {step.Side} {step.OrderType} order={step.OrderId} {cancelReason}");

        position.UpdateTime = currentTime;
        database.Connection.Update<CryptoPosition>(position);

        // Aankondiging dat we deze order gaan annuleren (de tradehandler weet dan dat wij het waren en het niet de user was via de exchange)
        step.CancelInProgress = true;
        database.Connection.Update<CryptoPositionStep>(step);

        // Annuleer de order
        var exchangeApi = ExchangeHelper.GetExchangeInstance(GlobalData.Settings.General.ExchangeId);
        var result = await exchangeApi.Cancel(position, part, step);
        if (result.succes)
        {
            step.Status = newStatus;
            step.CloseTime = currentTime;
            database.Connection.Update<CryptoPositionStep>(step);

            if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.PaperTrade)
                PaperAssets.Change(position.TradeAccount, position.Symbol, result.tradeParams.OrderSide,
                    CryptoOrderStatus.Canceled, result.tradeParams.Quantity, result.tradeParams.QuoteQuantity);
        }
        if (!result.succes || GlobalData.Settings.Trading.LogCanceledOrders)
            ExchangeBase.Dump(position, result.succes, result.tradeParams, cancelReason);

        return result;
    }



    static public async Task PlaceTakeProfitOrderAtPrice(CryptoDatabase database, CryptoPosition position, CryptoPositionPart part,
        decimal takeProfitPrice, DateTime currentTime, string extraText)
    {
        CryptoSymbol symbol = position.Symbol;

        // Probleem? Wat als het plaatsen van eem order fout gaat? (hoe vangen we de fout op en hoe herstellen we dat?
        // Binance is een bitch af en toe!). Met name Binance wilde na het annuleren wel eens de assets niet
        // vrijgeven waardoor de assets/pf niet snel genoeg bijgewerkt werd en de volgende opdracht dan de fout
        // in zou kunnen gaan. Geld voor alles wat we in deze tool doen, qua buy en sell gaat de herkansing wel 
        // goed, ook al zal je dan soms een repeterende fout voorbij zien komen (iedere minuut)


        // Get available assets from the exchange (as late as possible because of webcall)
        var (success, reaction) = await AssetTools.FetchAssetsAsync(position.TradeAccount, true);
        if (!success)
        {
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} {reaction}");
            return;
        }

        // Get asset amounts
        var info = AssetTools.GetAsset(position.TradeAccount, symbol);
        if (info.QuoteTotal <= 0)
        {
            GlobalData.AddTextToLogTab($"No assets available for {symbol.Quote}");
            return;
        }


        // This is the amount we want in the TP
        decimal takeProfitQuantity = position.Quantity;
        decimal takeProfitQuantityOriginal = position.Quantity;
        takeProfitQuantity = takeProfitQuantity.Clamp(position.Symbol.QuantityMinimum, position.Symbol.QuantityMaximum, position.Symbol.QuantityTickSize);
        decimal remainingDust = takeProfitQuantityOriginal - takeProfitQuantity; // expected dust


        // DEBUG --- ADD DUST to TP (short are excluded for now <how does that work?>)
        if (GlobalData.Settings.Trading.AddDustToTp && position.Side == CryptoTradeSide.Long)
        {
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine($"");
            stringBuilder.AppendLine($"Symbol = {symbol.Name}");
            stringBuilder.AppendLine($"position.Quantity = {position.Quantity}");
            stringBuilder.AppendLine($"info.BaseFree = {info.BaseFree}");
            stringBuilder.AppendLine($"info.BaseTotal = {info.BaseTotal}");
            stringBuilder.AppendLine($"info.QuoteFree = {info.QuoteFree}");
            stringBuilder.AppendLine($"info.QuoteTotal = {info.QuoteTotal}");

            decimal dust = info.BaseFree - position.Quantity;
            stringBuilder.AppendLine($"can we add quantity={dust} value={dust * position.Symbol.LastPrice}?");
            if (dust > 0 && dust * symbol.LastPrice < 1.0m)
            {
                stringBuilder.AppendLine($"yes we can add extra dust={dust} value dust ={dust * symbol.LastPrice}");

                decimal takeProfitQuantityWithExtraDust = info.BaseFree;
                takeProfitQuantityWithExtraDust = takeProfitQuantityWithExtraDust.Clamp(symbol.QuantityMinimum, symbol.QuantityMaximum, symbol.QuantityTickSize);
                stringBuilder.AppendLine($"new rounded quantity={takeProfitQuantityWithExtraDust} value={takeProfitQuantityWithExtraDust * symbol.LastPrice}...");

                takeProfitQuantity = takeProfitQuantityWithExtraDust;
                takeProfitQuantityOriginal = takeProfitQuantityWithExtraDust;
            }
            GlobalData.AddTextToLogTab(stringBuilder.ToString());
        }
        //END DEBUG


        // This could be more than expected because of the (unexpected) dust
        // But hey, what else are you going to do with the stupid useless dust?
        decimal expectedDust = takeProfitQuantityOriginal - takeProfitQuantity;
        string text = $"{position.Symbol.Name} quantity={position.Quantity}, rounded={takeProfitQuantity}, expected dust = {expectedDust} free={info.BaseFree} total={info.BaseTotal}";
        GlobalData.AddTextToLogTab(text);



        CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();

        (bool result, TradeParams? tradeParams) result;
        var exchangeApi = ExchangeHelper.GetExchangeInstance(GlobalData.Settings.General.ExchangeId);
        result = await exchangeApi.PlaceOrder(database, position, part, position.Side, currentTime,
            CryptoOrderType.Limit, takeProfitOrderSide, takeProfitQuantity, takeProfitPrice, null, null);

        if (result.tradeParams is not null)
        {
            if (result.result)
            {
                position.ProfitPrice = result.tradeParams.Price;
                var step = PositionTools.CreatePositionStep(position, part, result.tradeParams);
                step.RemainingDust = remainingDust; // takeProfitQuantityOriginal - takeProfitQuantity; // stick to original dust? for calculating profits
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
    }



    /// <summary>
    /// Bepaal het entry bedrag 
    /// </summary>
    public static decimal GetEntryAmount(CryptoSymbol symbol, decimal quoteAssetQuantity, CryptoTradeAccountType tradeAccountType)
    {
        // Opmerking: Er is geen percentage bij papertrading mogelijk (of we moeten een werkende papertrade asset management implementeren)

        // Heeft de gebruiker een percentage of een aantal ingegeven?
        if (tradeAccountType == CryptoTradeAccountType.RealTrading && symbol.QuoteData.EntryPercentage > 0m)
            return symbol.QuoteData.EntryPercentage * quoteAssetQuantity / 100;
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
                if (percentage > 0.1m) // hele kleine verschillen willen we liever niet zien
                    GlobalData.AddTextToLogTab($"{symbol.Name} vanwege de quantity ticksize {symbol.QuantityTickSize} is de entry value verhoogd naar {newEntryValue} ({percentage:N2}%) (DEBUG)");
                return newEntryQuantity;
            }
        }

        return entryQuantity;
    }
}
#endif
