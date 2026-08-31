using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

using Dapper;
using Dapper.Contrib.Extensions;

using System.Diagnostics;
using System.Text;

namespace CryptoScanner.Core.Trader;

public class TradeTools
{
    /// <summary>
    /// ALLE assets laden. Kept as the entry point the trader calls; the loading itself (including
    /// seeding the start capital) lives in <see cref="PaperAssets.LoadAssets"/> so there is one
    /// implementation instead of the copy that used to sit here and in GlobalData.
    /// </summary>
    public static void LoadAssets()
    {
        //GlobalData.AddTextToLogTab("Reading asset information");

        if (GlobalData.ActiveExchange != null)
            PaperAssets.LoadAssets(GlobalData.ActiveExchange);
    }


    public static async Task CheckOpenPositions()
    {
        // De openstaande posities controleren
        //GlobalData.AddTextToLogTab($"Checking open positions for {GlobalData.ActiveAccount!.AccountType}");

        using var database = new CryptoDatabase();
        foreach (var position in GlobalData.ActiveExchange!.Data.PositionList.Values.ToList())
        {
            position.ForceCheckPosition = true;
            await GlobalData.ThreadCheckPosition!.AddToQueue(position);
        }
    }


    /// <summary>
    /// De break-even prijs berekenen vanuit de parts en steps
    /// </summary>
    /// <summary>
    /// The take-profit levels the trader should use for this position. A per-signal TP override
    /// (position.TpPercentage, e.g. VBS RiskRewardRatio * SL%) becomes a single TP that closes the whole
    /// position; otherwise the global multi-level grid from Settings.Trading.TpList applies.
    /// </summary>
    public static List<CryptoTpEntry> EffectiveTpList(CryptoPosition position)
    {
        if (position.TpPercentage is decimal tpPct && tpPct > 0)
            return [new CryptoTpEntry { Factor = 100m, Percentage = tpPct }];
        return GlobalData.Settings.Trading.TpList;
    }

    public static void CalculateProfitAndBreakEvenPrice(CryptoPosition position)
    {
        // We do not return early here: the step statuses already in memory (from the last DB sync) are
        // sufficient for a meaningful recalculation.  The flag check is just a diagnostic safeguard.
        if (!position.HasOrdersAndTradesLoaded)
            ScannerLog.Logger.Warn($"{position.Symbol.Name} CalculateProfitAndBreakEvenPrice called without orders/trades loaded");

        //----
        // De positie doorrekene,  er wordt alleen gerekend, geen beslissingen over status
        //https://dappgrid.com/binance-fees-explained-fee-calculation/
        // You should first divide your order size(total) by 100 and then multiply it by your fee rate which
        // is 0.10 % for VIP 0 / regular users. So, if you buy Bitcoin with 200 USDT, you will basically get
        // $199.8 worth of Bitcoin.To calculate these fees, you can also use our Binance fee calculator:
        // (als je verder gaat dan wordt het vanwege de kickback's tamelijk complex)
        // Op Bybit futures heb je de fundingrates, dat wordt in tijdblokken berekend met varierende fr..
        StringBuilder stringBuilderOld = position.DumpPosition();

        position.Profit = 0;
        position.Quantity = 0;
        position.Invested = 0;
        position.Returned = 0;
        position.Reserved = 0;
        position.Commission = 0;
        position.CommissionBase = 0;
        position.CommissionQuote = 0;
        position.RemainingDust = 0;

        position.PartCount = 0;
        position.ActiveDca = false;
        position.BreakEvenPrice = 0;

        // Ondersteuning long/short
        CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
        CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();

        decimal totalValue = 0;
        decimal totalQuantity = 0;
        // Commission from Entry+Dca parts only (excludes TP parts) - used for TpGridBreakEvenPrice,
        // see below.
        decimal entryDcaCommission = 0;
        // One entry per TP level (multi-level take profit; a legacy/single-TP position just has one)
        List<CryptoPositionPart> tpParts = [];
        int steps = 0;
        int stepCanceled = 0;
        // Counts how many parts of each Purpose have been seen so far in this loop, so every part gets
        // a sequential, 1-based number within its own purpose (Entry 1, Dca 1/2/3, TP 1/2/3, ...) - same
        // rule as PositionTools.NextPartNumber uses when a new part is created. PartList is in creation
        // order (sorted by Id), so this reproduces the DcaList/TpList configuration order.
        Dictionary<CryptoPartPurpose, int> partNumberByPurpose = [];

        foreach (CryptoPositionPart part in position.PartList.Values.ToList())
        {
            part.Quantity = 0;
            part.Invested = 0;
            part.Returned = 0;
            part.Reserved = 0;
            part.Commission = 0;
            part.CommissionBase = 0;
            part.CommissionQuote = 0;
            part.RemainingDust = 0;
            part.BreakEvenPrice = 0;

            //int tradeCount = 0;
            foreach (CryptoPositionStep step in part.StepList.Values.ToList())
            {
                steps++;
                if (step.Side == entryOrderSide && step.Status >= CryptoOrderStatus.Canceled)
                    stepCanceled++;

                // || step.Status == CryptoOrderStatus.PartiallyFilled => niet doen, want dan worden de TP's iedere keer verplaatst..
                // Wellicht moet dat gedeelte op een andere manier ingeregeld worden zodat we hier wel de echte BE kunnen uitrekenen?
                if (step.Status.IsFilled()) // Filled or PartiallyFilledAndClosed
                {
                    decimal filledQuantity = step.QuantityFilled - step.CommissionBase;
                    if (step.Side == entryOrderSide)
                    {
                        part.Quantity += filledQuantity;
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
            }

            // De parts opnieuw instellen: ieder Purpose krijgt zijn eigen oplopende reeks, beginnend bij 1
            partNumberByPurpose.TryGetValue(part.Purpose, out int previousPartNumber);
            part.PartNumber = previousPartNumber + 1;
            partNumberByPurpose[part.Purpose] = part.PartNumber;

            // PartCount/ActiveDca staan los van de nummering hierboven - dat is het aantal daadwerkelijk
            // gevulde DCA's (gebruikt voor de DCA-slotcontrole en DcaList-lookup), geen volgnummer.
            if (part.Purpose == CryptoPartPurpose.Dca)
            {
                if (part.Invested > 0)
                    position.PartCount++;
                else
                    position.ActiveDca = true;
            }
            if (part.Purpose == CryptoPartPurpose.TakeProfit)
                tpParts.Add(part);
            else if (part.Purpose == CryptoPartPurpose.Entry || part.Purpose == CryptoPartPurpose.Dca)
                entryDcaCommission += part.Commission;


            //string t = string.Format("{0} CalculateProfit sell invested={1} profit={2} bought={3} sold={4} steps={5}",
            //    position.Symbol.Name, part.Invested, part.Profit, part.Invested, part.Returned, part.Steps.Count);
            //GlobalData.AddTextToLogTab(t);


            position.Quantity += part.Quantity;
            position.Invested += part.Invested;
            position.Returned += part.Returned;
            position.Reserved += part.Reserved;
            position.Commission += part.Commission;
            position.RemainingDust += part.RemainingDust;
            position.CommissionBase += part.CommissionBase;
            position.CommissionQuote += part.CommissionQuote;
        }


        // 3 strange conditions which should not have occured, but still here they are..
        // The first condition is a fix that can probably be removed at this moment, but added logging to be sure
        // The last 2 conditions will close the position if all entry orders are canceled (timeout etc) but the position was never closed..

        // Reset closetime if there is quantity left (it should not have been closed)
        // TODO: I reoved this, but it probably had a reason..
        // Exchange only or papertrade (we will find out)
        //if (position.Quantity > position.RemainingDust)
        //{
        //    foreach (CryptoPositionPart tpPart in tpParts)
        //    {
        //        if (tpPart.CloseTime.HasValue)
        //        {
        //            tpPart.CloseTime = null;
        //            GlobalData.AddTextToLogTab($"{position.Symbol.Name} resetting closeTime of part {tpPart.PartNumber} (debug, fixing position?)");
        //        }
        //    }
        //}

        // Reset status if there is a timeout (status Trading should not have been set) - nothing wil happen otherwise
        if (position.Quantity == 0 && position.Reserved == 0 && steps > 0 && steps == stepCanceled && position.Status == CryptoPositionStatus.Trading)
        {
            position.Status = CryptoPositionStatus.Timeout; // or timeout?
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} open position takes up a slot (debug, fixing position?)");
        }

        // Reset status if there is a timeout (status Trading should not have been set) - nothing wil happen otherwise
        if (position.Quantity == 0 && position.Reserved == 0 && steps > 0 && steps == stepCanceled && position.Status == CryptoPositionStatus.Waiting)
        {
            position.Status = CryptoPositionStatus.Timeout; // or timeout?
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} waiting position takes up a slot (debug, fixing position?)");
        }



        // Predicted commission (in quote), we need a fixed avg-price to calculate the TP-commission
        // (this is not the exact tp-commission, but we need to calculating anything)
        // if the position is closed the position.Quantity is 0 and the real commission will be calculated
        decimal avgPrice = 0;
        if (totalQuantity > 0)
            avgPrice = totalValue / totalQuantity;
        decimal predictedCommission = avgPrice * (decimal)position.Exchange.FeeRate * position.Quantity / 100m;

        // Fixed TP/DCA grid anchor: same shape as BreakEvenPrice below, but built from totalValue/
        // totalQuantity/entryDcaCommission - which only include Entry+Dca fills (Returned is
        // structurally 0 for those parts, so it is dropped here) - so a sibling TP filling never
        // moves it. Only a new DCA fill shifts it (averaging the cost basis), same as the fixed
        // grid in PositionMonitor.GetMissingFixedPercentageDcaPrices already assumes.
        if (totalQuantity > 0 && position.Status == CryptoPositionStatus.Trading)
        {
            decimal entryPredictedCommission = avgPrice * (decimal)position.Exchange.FeeRate * totalQuantity / 100m;
            position.TpGridBreakEvenPrice = position.Side == CryptoTradeSide.Long
                ? (totalValue + entryDcaCommission + entryPredictedCommission) / totalQuantity
                : (totalValue - entryDcaCommission - entryPredictedCommission) / totalQuantity;
        }

        decimal BreakEvenPriceOld = position.BreakEvenPrice;
        if (position.Side == CryptoTradeSide.Long)
        {
            // Long BE: need to sell high enough to recover cost + all commissions (entry paid + predicted exit).
            // Commission increases BE because every fee paid raises the bar for break-even.
            if (position.Quantity > 0 && position.Status == CryptoPositionStatus.Trading)
                position.BreakEvenPrice = (position.Invested - position.Returned + position.Commission + predictedCommission) / position.Quantity;

            decimal invested = position.Invested;
            if (position.RemainingDust > 0)
                invested -= position.RemainingDust * position.BreakEvenPrice;

            position.Profit = position.Returned - invested - position.Commission;
            position.Percentage = 0m;
            if (invested != 0m)
                position.Percentage = 100m + (100m * position.Profit / invested);
        }
        else
        {
            // Short BE: need to buy back low enough so that net proceeds cover all commissions.
            // Commission decreases BE because every fee paid lowers the price at which we can still break even.
            // Asymmetric sign vs Long is intentional — direction of the trade is reversed.
            if (position.Quantity > 0 && position.Status == CryptoPositionStatus.Trading)
                position.BreakEvenPrice = (position.Invested - position.Returned - position.Commission - predictedCommission) / position.Quantity;

            decimal invested = position.Invested;
            if (position.RemainingDust > 0)
                invested -= position.RemainingDust * position.BreakEvenPrice;

            position.Profit = invested - position.Returned - position.Commission;
            position.Percentage = 0m;
            if (invested != 0m)
                position.Percentage = 100m + (100m * position.Profit / invested);
        }

        if (BreakEvenPriceOld != position.BreakEvenPrice)
        {
            ScannerLog.Logger.Trace($"{position.Symbol.Name} aanpassing BE van {BreakEvenPriceOld} naar {position.BreakEvenPrice}");
            //ScannerLog.Logger.Trace(stringBuilderOld);
            //StringBuilder stringBuilderNew = position.DumpPosition();
            //ScannerLog.Logger.Debug(stringBuilderNew);
        }
    }


    private static void CalculateOrderFeeFromTrades(CryptoPosition position, CryptoPositionStep step)
    {
        //ScannerLog.Logger.Trace($"CalculateOrderFeeFromTrades: Positie {position.Symbol.Name} check step={step.OrderId}");

        if (!position.HasOrdersAndTradesLoaded)
            ScannerLog.Logger.Warn($"{position.Symbol.Name} CalculateOrderFeeFromTrades called without orders/trades loaded");

        // Calculate fee from the trades (Bybit V5 doesn't return fee via orders)
        // Afhankelijk van de asset wordt de commissie berekend (debug)
        // Voor de step is dit niet relevant (mits we het omrekenen naar base en quote)
        step.Commission = 0;
        step.CommissionBase = 0;
        step.CommissionQuote = 0;
        step.CommissionAsset = "";
        foreach (CryptoTrade trade in position.TradeList.Values.ToList())
        {
            if (trade != null && trade.OrderId == step.OrderId)
            {
                //ScannerLog.Logger.Trace($"CalculateOrderFeeFromTrades: Positie {position.Symbol.Name} check trade={trade.TradeId} order={trade.OrderId}");
                if (trade.CommissionAsset == position.Symbol.Base) // fee in base quantity
                {
                    decimal value = trade.Commission * trade.Price;
                    step.CommissionBase += trade.Commission; // debug, not really relevant after this
                    //step.CommissionQuote += value;
                    step.Commission += value;
                }
                else if (trade.CommissionAsset == position.Symbol.Quote || trade.CommissionAsset == "") // default, fee in quote quantity
                {
                    //decimal value = (decimal)trade.Commission / (decimal)trade.Price;
                    //step.CommissionBase += value;
                    step.CommissionQuote += trade.Commission; // debug, not really relevant after this
                    step.Commission += trade.Commission;
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
    public static async Task CalculatePositionResultsViaOrders(CryptoDatabase database, CryptoPosition position, bool forceCalculation = false)
    {
        // Als de positie reeds gesloten is gaan we niet meer aanpassen
        // (kan gesloten zijn vanwege een verkeerde beslissing, timeout?)
        // Die controle wordt door de ThreadCheckFinishedPosition gedaan
        //if (position.Status == CryptoPositionStatus.Ready)
        //    return;

        bool markedAsReady = false;
        bool orderStatusChanged = false;

        // Profiling: sub-breakdown of the positionCheck bucket (see PipelineProfiler). Tracks where
        // inside this method the time actually goes — the DB load, the per-order loop, the
        // profit/break-even recalculation, or the final persist transaction.
        long profLoadStart = Stopwatch.GetTimestamp();
        int count = await LoadOrdersFromDatabaseAndExchangeAsync(database, position);
        long profLoadOrdersTicks = Stopwatch.GetTimestamp() - profLoadStart;

        if (count > 0)
            forceCalculation = true;
        var oldPositionStatus = position.Status;

        //ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} {position.Status} force={forceCalculation}");


        // Build the filled quantity via the present orders & calculate fees
        long profOrderLoopStart = Stopwatch.GetTimestamp();
        DateTime? lastDateTime = null;
        foreach (CryptoOrder order in position.OrderList.Values.ToList())
        {
            if (order != null && order.OrderId != null && position.StepOrderList.TryGetValue(order.OrderId, out CryptoPositionStep? step))
            {
                // Remember the last datetime so we can close the position with this date if needed
                if (lastDateTime == null || order.UpdateTime > lastDateTime)
                    lastDateTime = order.UpdateTime;

                if (step.Status != order.Status || step.QuoteQuantityFilled != order.QuoteQuantityFilled || forceCalculation)
                {
                    orderStatusChanged = true;
                    //ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} check order {order.OrderId} {order.Side}");

                    CryptoPositionPart part = PositionTools.FindPositionPart(position, step.PositionPartId) ?? throw new Exception("Problem finding parent part");
                    string msgInfo = $"{position.Symbol.Name} " +
                        $"{order.Status.ToText().ToLower()} " + // ToText = PartiallyAndClosed -> Filled
                        $"{part.Purpose.ToString().ToLower()} " +
                        $"{order.Side.ToString().ToLower()} " +
                        $"{order.Type.ToString().ToLower()} " +
                        $"order={order.OrderId} " +
                        $"price={order.AveragePrice?.ToString0()} " +
                        $"quantity={order.QuantityFilled?.ToString0()} " +
                        $"value={order.QuoteQuantity.ToString0(position.Symbol.QuoteData.DisplayFormat)}";

                    CalculateOrderFeeFromTrades(position, step);

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
                            ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} check order {order.OrderId} -> Canceled by trader");
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
                                GlobalData.AddTextToTelegram(s, position, CryptoTelegramCategory.OrderFilled);
                            }
                            ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} check order {order.OrderId} {order.Side} -> Canceled by user");
                        }
                    }
                    else if (order.Status.IsFilled())
                    {
                        ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} check order {order.OrderId} {order.Side} -> IsFilled");

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
                            //ScannerLog.Logger.Trace($"TradeTools.CalculatePositionResultsViaOrders: {position.Symbol.Name} take profit -> position.Reposition = true");
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

                            //ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} check order{order.OrderId} -> IsFilled (entry)");
                        }


                        CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();
                        if (step.Side == takeProfitOrderSide)
                        {
                            part.CloseTime = order.UpdateTime;
                            //ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} check order {order.OrderId} -> IsFilled (takeprofit)");
                        }

                        // Geen melding geven bij afgesloten orders
                        if (!isOrderClosed)
                        {
                            GlobalData.AddTextToLogTab(msgInfo);
                            GlobalData.AddTextToTelegram(msgInfo, position, CryptoTelegramCategory.OrderFilled);
                        }

                        if (!step.IsCalculated)
                        {
                            // Claim the profits (on )papertrading/emulator)
                            PaperAssets.Change(GlobalData.ActiveExchange!, position.Symbol, position.Side, order.Side, CryptoOrderStatus.Filled, step.Quantity, step.QuoteQuantityFilled, "TradeTools.CalculatePositionResultsViaOrders.Filled");
                            // Extract the initial base commission (papertrading/emulator)
                            if (step.CommissionBase > 0 || step.CommissionQuote > 0)
                                PaperAssets.BookCommission(GlobalData.ActiveExchange!, position.Symbol, step.CommissionBase, step.CommissionQuote, "TradeTools.CalculatePositionResultsViaOrders.Fees");

                            step.IsCalculated = true;
                            database.Connection.Update<CryptoPositionStep>(step);
                        }
                    }

                    // De reden van annuleren niet overschrijven
                    if (step.Status < CryptoOrderStatus.Canceled)
                    {
                        step.Status = order.Status;
                        //ScannerLog.Logger.Trace($"CalculatePositionResultsViaOrders: Positie {position.Symbol.Name} check order {order.OrderId} -> set status to {order.Status}");
                    }
                }
            }
        }
        long profOrderLoopTicks = Stopwatch.GetTimestamp() - profOrderLoopStart;

        long profCalcProfitTicks = 0;
        long profPersistTicks = 0;
        if (orderStatusChanged || forceCalculation)
        {
            long profCalcStart = Stopwatch.GetTimestamp();
            CalculateProfitAndBreakEvenPrice(position);
            profCalcProfitTicks = Stopwatch.GetTimestamp() - profCalcStart;

            if (lastDateTime == null)
                lastDateTime = GlobalData.Clock.UtcNow;

            // quick fix to close positions with nothing attached to it... It does not belong here, just a quick and dirty fix for now....
            if (position.Status == CryptoPositionStatus.Waiting && position.PartList.Count == 0 && position.CreateTime.AddHours(1) < lastDateTime)
            {
                // Close if q=0 or less than the minimum amount we can sell
                //
                // Deliberately NOT weighed against QuoteValueMinimum any more (removed 31-08-2026).
                // A remainder worth less than the minimum ORDER value is not the same thing as dust.
                // An entry that was too small to begin with reads exactly like that while the whole
                // position is still sitting there untouched, and closing it here writes the position
                // off as a total loss while the coins are still held and the exit order is still on
                // the book. ZECUSDC.PERP on HyperLiquid did this on 31-08-2026: 0.01 ZEC bought for
                // 8.16 against a minimum order value of 10.
                //
                // Whether an entry can be entered AND exited at all is a question for the entry, and
                // it is asked there now - see CheckOrderSetAgainstSymbolLimits, called from
                // PositionMonitor before the position is created.
                decimal remaining = position.Quantity - position.RemainingDust;
                if (remaining <= 0 || remaining < position.Symbol.QuantityMinimum)
                {
                    markedAsReady = true;
                    orderStatusChanged = true;
                    position.Reposition = false;
                    position.CloseTime = lastDateTime;
                    position.UpdateTime = lastDateTime;
                    position.Status = CryptoPositionStatus.Timeout;

                    //#if DEBUG
                    //                    GlobalData.AddTextToLogTab($"TradeTools: Position {position.Symbol.Name} changed to {position.Status}");
                    //                    GlobalData.AddTextToLogTab($"TradeTools: debug ? Quantity={position.Quantity}");
                    //                    GlobalData.AddTextToLogTab($"TradeTools: debug ? Dust={position.RemainingDust}");
                    //                    GlobalData.AddTextToLogTab($"TradeTools: debug ? Remaining={remaining}");
                    //                    GlobalData.AddTextToLogTab($"TradeTools: debug ? Symbol.LastPrice={position.Symbol.LastPrice}");
                    //                    GlobalData.AddTextToLogTab($"TradeTools: debug ? Symbol.QuantityMinimum={position.Symbol.QuantityMinimum}");
                    //                    GlobalData.AddTextToLogTab($"TradeTools: debug ? Symbol.QuoteValueMinimum={position.Symbol.QuoteValueMinimum}");
                    //                    GlobalData.AddTextToLogTab($"TradeTools: debug ? closing if ({remaining} <= 0)");
                    //                    GlobalData.AddTextToLogTab($"TradeTools: debug ? closing if ({position.Quantity} < {position.Symbol.QuantityMinimum})");
                    //                    GlobalData.AddTextToLogTab($"TradeTools: debug ? closing if ({remaining * position.Symbol.LastPrice} < {position.Symbol.QuoteValueMinimum})");
                    //#endif
                }
            }

            // Er is in geinvesteerd en dus moet de positie ten minste actief zijn
            if (position.Status == CryptoPositionStatus.Waiting && position.Quantity != 0)
            {
                orderStatusChanged = true;
                position.CloseTime = null;
                position.Reposition = true;
                position.UpdateTime = lastDateTime;
                position.Status = CryptoPositionStatus.Trading;
                GlobalData.AddTextToLogTab($"TradeTools: Position {position.Symbol.Name} status changed to {position.Status} (should not occur)");
            }

            // Als alles verkocht is de positie alsnog sluiten. Maar wanneer weet je of alles echt verkocht is?
            if (position.Status == CryptoPositionStatus.Trading) // && position.Quantity != 0
            {
                // Close if q=0 or less than the minimum amount we can sell
                // (not weighed against QuoteValueMinimum - see the note on the Timeout branch above)
                decimal remaining = position.Quantity - position.RemainingDust;
                if (remaining <= 0 || remaining < position.Symbol.QuantityMinimum)
                {
                    markedAsReady = true;
                    orderStatusChanged = true;
                    position.Reposition = false;
                    position.CloseTime = lastDateTime;
                    position.UpdateTime = lastDateTime;
                    position.Status = CryptoPositionStatus.Ready;

                    // Starts the loss cooldown. Profit was recalculated by
                    // CalculateProfitAndBreakEvenPrice further up in this same method, so it is
                    // the final figure for this position here.
                    if (position.Profit < 0)
                        position.Symbol.LastLossDate = lastDateTime;

                    GlobalData.AddTextToLogTab($"TradeTools: Position {position.Symbol.Name} status changed to {position.Status}");
                    //GlobalData.AddTextToLogTab($"TradeTools: debug ? Quantity={position.Quantity}");
                    //GlobalData.AddTextToLogTab($"TradeTools: debug ? Dust={position.RemainingDust}");
                    //GlobalData.AddTextToLogTab($"TradeTools: debug ? Remaining={remaining}");
                    //GlobalData.AddTextToLogTab($"TradeTools: debug ? Symbol.LastPrice={position.Symbol.LastPrice}");
                    //GlobalData.AddTextToLogTab($"TradeTools: debug ? Symbol.QuantityMinimum={position.Symbol.QuantityMinimum}");
                    //GlobalData.AddTextToLogTab($"TradeTools: debug ? Symbol.QuoteValueMinimum={position.Symbol.QuoteValueMinimum}");
                    //GlobalData.AddTextToLogTab($"TradeTools: debug ? closing if ({remaining} <= 0)");
                    //GlobalData.AddTextToLogTab($"TradeTools: debug ? closing if ({position.Quantity} < {position.Symbol.QuantityMinimum})");
                    //GlobalData.AddTextToLogTab($"TradeTools: debug ? closing if ({remaining * position.Symbol.LastPrice} < {position.Symbol.QuoteValueMinimum})");
                }
            }


            // Hebben we per abuis een part afgesloten (vanwege niet opgemerkte trades) terwijl de positie eigenlijk nog openstaat?
            // Achteraf worden de trades alsnog ingeladen, wordt de positie opengezet, maar de part blijft gelosten en de trader doets niets...
            if (!position.CloseTime.HasValue)
            {
                foreach (CryptoPositionPart part in position.PartList.Values.ToList())
                {
                    if (part.CloseTime.HasValue && part.Invested != 0 && part.Returned == 0)
                    {
                        part.CloseTime = null;
                        orderStatusChanged = true;
                        GlobalData.AddTextToLogTab($"TradeTools: Part {position.Symbol.Name} reopened because of correction {position.Status}");
                    }
                }
            }


            // Persist position state (only when something actually changed — saves are expensive)
            if (orderStatusChanged || markedAsReady)
            {
                long profPersistStart = Stopwatch.GetTimestamp();
                using var transaction = database.BeginTransaction();
                try
                {
                    foreach (CryptoPositionPart part in position.PartList.Values.ToList())
                    {
                        foreach (CryptoPositionStep step in part.StepList.Values.ToList())
                            database.Connection.Update<CryptoPositionStep>(step, transaction);
                        database.Connection.Update<CryptoPositionPart>(part, transaction);
                    }
                    database.Connection.Update<CryptoPosition>(position, transaction);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ScannerLog.Logger.Error(ex, $"{position.Symbol.Name} CalculatePositionResultsViaOrders failed to persist position state");
                    throw;
                }
                profPersistTicks = Stopwatch.GetTimestamp() - profPersistStart;
            }


            // Een laatste controle laten uitvoeren en de nog openstaande DCA orders afsluiten/verplaatsen
            if (markedAsReady)
            {
                position.ForceCheckPosition = true;
                position.DelayUntil = GlobalData.Clock.UtcNow.AddSeconds(10);
                await GlobalData.ThreadCheckPosition!.AddToQueue(position);
            }

        }

        PipelineProfiler.RecordPositionResultPhases(
            loadOrders: profLoadOrdersTicks,
            orderLoop: profOrderLoopTicks,
            calcProfit: profCalcProfitTicks,
            persist: profPersistTicks);
    }



    static private async Task<int> LoadOrdersFromDatabaseAndExchangeAsync(CryptoDatabase database, CryptoPosition position)
    {
        int count = 0;

        await position.OrdersAndTradesSemaphore.WaitAsync();
        try
        {
            if (!position.HasOrdersAndTradesLoaded)
            {
                // In emulator/paper mode all orders+trades are added in-memory by CreatePaperTrade,
                // so skip the expensive DB round-trip (queries all orders for the symbol — up to 20k rows).
                if (GlobalData.IsEmulatorMode)
                {
                    position.HasOrdersAndTradesLoaded = true;
                }
                else
                {
                    //GlobalData.AddTextToLogTab($"TradeTools.LoadOrdersFromDatabaseAndExchangeAsync: Position {position.Symbol.Name} loading orders and trades from database {position.CreateTime}");
                    //ScannerLog.Logger.Trace($"TradeTools.LoadOrdersFromDatabaseAndExchangeAsync: Position {position.Symbol.Name} loading orders and trades from database {position.CreateTime}");

                    // Vanwege tijd afrondingen (msec)
                    DateTime from = position.CreateTime.AddMinutes(-1);

                    // Bij het laden zijn niet alle trades in het geheugen ingelezen, dus deze alsnog inladen (of verversen)
                    string sql = "select * from [order] where SymbolId=@symbolId and CreateTime >= @fromDate order by CreateTime";
                    foreach (CryptoOrder order in database.Connection.Query<CryptoOrder>(sql, new { symbolId = position.SymbolId, fromDate = from }))
                        position.OrderList.AddOrder(order, false);

                    sql = "select * from [trade] where SymbolId=@symbolId and TradeTime >= @fromDate order by TradeTime";
                    foreach (CryptoTrade trade in database.Connection.Query<CryptoTrade>(sql, new { symbolId = position.SymbolId, fromDate = from }))
                        position.TradeList.AddTrade(trade, false);

                    position.HasOrdersAndTradesLoaded = true;
                }
            }

            // Daarna de "nieuwe" orders van deze coin ophalen en die toegevoegen aan dezelfde orderlist
            //if (GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.RealTrading) // && loadFromExchange
            //{
            //    count += await GlobalData.ActiveExchange!.GetApiInstance().Order.GetOrders(database, position);
            //}

            // Daarna de "nieuwe" orders van deze coin ophalen en die toegevoegen aan dezelfde orderlist
            //if (GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.RealTrading) // && loadFromExchange
            //    count += await GlobalData.ActiveExchange!.GetApiInstance().Trade.GetTradesAsync(database, position);
        }
        finally
        {
            position.OrdersAndTradesSemaphore.Release();
        }

        return count;
    }


    public static async Task<(bool cancelled, TradeParams? tradeParams)> CancelOrder(CryptoDatabase database,
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
        var exchangeApi = GlobalData.ActiveExchange!.GetApiInstance();
        var result = await exchangeApi.Cancel(position, part, step);
        if (result.succes)
        {
            step.Status = newStatus;
            step.CloseTime = currentTime;
            database.Connection.Update<CryptoPositionStep>(step);

            PaperAssets.Change(GlobalData.ActiveExchange!, position.Symbol, position.Side, result.tradeParams!.OrderSide,
                CryptoOrderStatus.Canceled, result.tradeParams.Quantity, result.tradeParams.QuoteQuantity, "TradeTools.CancelOrder");
        }
        if (!result.succes || GlobalData.Settings.Trading.LogCanceledOrders)
            ExchangeBase.Dump(position, result.succes, result.tradeParams, cancelReason);

        return result;
    }



    public static async Task PlaceTakeProfitOrderAtPrice(CryptoDatabase database, CryptoPosition position, CryptoPositionPart part,
        decimal takeProfitPrice, decimal? tpStop, decimal? tpLimit, DateTime currentTime, string extraText,
        decimal quantity, bool includeDust = true)
    {
        CryptoSymbol symbol = position.Symbol;

        // Probleem? Wat als het plaatsen van eem order fout gaat? (hoe vangen we de fout op en hoe herstellen we dat?
        // Binance is een bitch af en toe!). Met name Binance wilde na het annuleren wel eens de assets niet
        // vrijgeven waardoor de assets/pf niet snel genoeg bijgewerkt werd en de volgende opdracht dan de fout
        // in zou kunnen gaan. Geld voor alles wat we in deze tool doen, qua buy en sell gaat de herkansing wel
        // goed, ook al zal je dan soms een repeterende fout voorbij zien komen (iedere minuut)


        // GetSymbolData available assets from the exchange (as late as possible because of webcall)
        var (success, reaction) = AssetTools.FetchAssets(GlobalData.ActiveExchange!, true);
        if (!success)
        {
            GlobalData.AddTextToLogTab($"{position.Symbol.Name} {reaction}");
            return;
        }

        // GetSymbolData asset amounts
        var info = AssetTools.GetAsset(GlobalData.ActiveExchange!, symbol);
        if (info.QuoteTotal <= 0)
        {
            GlobalData.AddTextToLogTab($"No assets available for {symbol.Quote}");
            return;
        }


        // This is the amount we want in the TP (the caller's share for this TP level/part, not
        // necessarily the whole position - see PositionMonitor multi-level TP handling)
        decimal remainingDust;
        decimal takeProfitQuantity = quantity;
        decimal takeProfitQuantityOriginal = quantity;
        if (position.Symbol.Exchange.TradingType != CryptoTradingType.Spot)
        {
            remainingDust = 0; // Futures deals with contracts and can never has dust
            takeProfitQuantity = quantity;
        }
        else
        {
            takeProfitQuantity = takeProfitQuantity.Clamp(position.Symbol.QuantityMinimum, position.Symbol.QuantityMaximum, position.Symbol.QuantityTickSize);
            remainingDust = takeProfitQuantityOriginal - takeProfitQuantity; // expected dust

            // DEBUG --- ADD DUST to TP (short are excluded for now <how does that work?>)
            //TODO: Short? / Margin?
            // Only the level that absorbs the remainder (includeDust) should also absorb leftover
            // exchange dust - otherwise it would get added to every TP level's order.
            if (includeDust && GlobalData.Settings.Trading.AddDustToTp && position.Side == CryptoTradeSide.Long &&
                position.Symbol.Exchange.TradingType == CryptoTradingType.Spot)
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

                    // quantity + dust == position.Quantity + (BaseFree - position.Quantity) == BaseFree
                    // when this level covers the whole position (single-level TP), same as before.
                    decimal takeProfitQuantityWithExtraDust = quantity + dust;
                    takeProfitQuantityWithExtraDust = takeProfitQuantityWithExtraDust.Clamp(symbol.QuantityMinimum, symbol.QuantityMaximum, symbol.QuantityTickSize);
                    stringBuilder.AppendLine($"new rounded quantity={takeProfitQuantityWithExtraDust} value={takeProfitQuantityWithExtraDust * symbol.LastPrice}...");

                    takeProfitQuantity = takeProfitQuantityWithExtraDust;
                    takeProfitQuantityOriginal = takeProfitQuantityWithExtraDust;
                }
                GlobalData.AddTextToLogTab(stringBuilder.ToString());
            }
            //END DEBUG
        }

        // This could be more than expected because of the (unexpected) dust
        // But hey, what else are you going to do with the stupid useless dust?
        decimal expectedDust = takeProfitQuantityOriginal - takeProfitQuantity;
        string text = $"{position.Symbol.Name} quantity={position.Quantity}, rounded={takeProfitQuantity}, expected dust = {expectedDust} free={info.BaseFree} total={info.BaseTotal}";
        GlobalData.AddTextToLogTab(text);



        CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();

        (bool result, TradeParams? tradeParams) result;
        var exchangeApi = GlobalData.ActiveExchange!.GetApiInstance();
        result = await exchangeApi.PlaceOrder(database, position, part, currentTime,
                CryptoOrderType.Limit, takeProfitOrderSide, takeProfitQuantity, takeProfitPrice, tpStop, tpLimit);
        if (result.tradeParams is not null)
        {
            if (result.result)
            {
                position.ProfitPrice = result.tradeParams.Price;
                var step = PositionTools.CreatePositionStep(position, part, result.tradeParams);
                step.RemainingDust = remainingDust; // takeProfitQuantityOriginal - takeProfitQuantity; // stick to original dust? for calculating profits
                database.Connection.Insert<CryptoPositionStep>(step);
                PositionTools.AddPositionPartStep(part, step);

                //part.ProfitMethod = CryptoEntryOrDcaStrategy.FixedPercentage;
                database.Connection.Update<CryptoPositionPart>(part);
                database.Connection.Update<CryptoPosition>(position);

                PaperAssets.Change(GlobalData.ActiveExchange!, position.Symbol, position.Side, result.tradeParams.OrderSide,
                    step.Status, result.tradeParams.Quantity, result.tradeParams.QuoteQuantity, "TradeTools.PlaceTakeProfitOrderAtPrice");
            }
            else
                position.ForceCheckPosition = true;
            ExchangeBase.Dump(position, result.result, result.tradeParams, extraText);
        }
    }



    /// <summary>
    /// Bepaal het entry bedrag
    /// </summary>
    public static decimal GetEntryAmount(CryptoSymbol symbol, decimal quoteAssetQuantity)
    {
        // Opmerking: Er is geen percentage bij papertrading mogelijk (of we moeten een werkende papertrade asset management implementeren)
        // That working paper-trade asset management now exists: the balances are maintained on every
        // order event and read back by AssetTools.GetAsset, so a percentage works for paper trading
        // and the emulator too.

        // Heeft de gebruiker een percentage of een aantal ingegeven?
        if (symbol.QuoteData!.EntryPercentage > 0)
            return (decimal)symbol.QuoteData.EntryPercentage * quoteAssetQuantity / 100.0m;
        else
            return symbol.QuoteData!.EntryAmount;
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
            GlobalData.AddTextToLogTab($"{symbol.Name} because of the quantity tick size {symbol.QuantityTickSize} we cannot enter with the far too high {clampedEntryValue} ({percentage:N2}%) (DEBUG)");
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
                    GlobalData.AddTextToLogTab($"{symbol.Name} because of the quantity tick size {symbol.QuantityTickSize} the entry value was raised to {newEntryValue} ({percentage:N2}%) (DEBUG)");
                return newEntryQuantity;
            }
        }

        return entryQuantity;
    }


    /// <summary>
    /// Whether EVERY order this position is going to produce fits inside the symbol's own limits:
    /// the entry, each DCA level behind it, and the exit orders that have to get us back out again.
    /// When one of them does not fit, no position is opened at all.
    /// <para>
    /// The point of asking all of this here is that the entry is the only moment at which refusing
    /// is free. Once the entry has filled, an exit order the exchange will not accept leaves a
    /// position that cannot be closed, and a DCA order it will not accept leaves a position that
    /// cannot be defended. Both used to be found out afterwards, one order at a time, from an
    /// exchange error - or worse, not found out at all: the paper trader accepts everything, so on
    /// 31-08-2026 ZECUSDC.PERP entered with 0.01 ZEC for 8.16 USDC against a minimum order value of
    /// 10, and the position was later written off as an unsellable remainder by the closing check in
    /// CalculatePositionResultsViaOrders while all of it was still sitting there.
    /// </para>
    /// <para>
    /// Prices are the ones known at entry time. The DCA levels and the profit targets are computed
    /// from TpGridBreakEvenPrice once the position is running, which is the entry price plus twice
    /// the commission - a fraction of a percent away from the entry price used here, and nowhere
    /// near the distances this method weighs.
    /// </para>
    /// </summary>
    /// <param name="signalSlPercentage">The SL distance the strategy asked for, when it supplied
    /// one. Decides both the SL price and which DCA levels are placed at all - levels at or beyond
    /// the SL never fill, so they are not weighed here either (same rule as
    /// PositionMonitor.GetMissingFixedPercentageDcaPrices).</param>
    /// <param name="reason">Filled with the first limit that is broken, empty when everything fits.</param>
    public static bool CheckOrderSetAgainstSymbolLimits(CryptoSymbol symbol, CryptoTradeSide side,
        decimal entryPrice, decimal entryQuantity, decimal? signalSlPercentage, out string reason)
    {
        reason = "";
        if (entryPrice <= 0 || entryQuantity <= 0)
        {
            reason = $"entry price {entryPrice} or quantity {entryQuantity} is zero";
            return false;
        }

        // One order against the symbol's quantity and value limits. A maximum of zero means the
        // exchange publishes none (the ordinary case on Alpaca, Bitvavo, BitMart, Mexc and
        // HyperLiquid), the same reading CandleHelpers.ClampCore uses.
        bool CheckOrder(string what, decimal quantity, decimal price, out string why)
        {
            why = "";
            if (quantity < symbol.QuantityMinimum)
            {
                why = $"{what} quantity {quantity} < minimum {symbol.QuantityMinimum}";
                return false;
            }
            if (symbol.QuantityMaximum > 0 && quantity > symbol.QuantityMaximum)
            {
                why = $"{what} quantity {quantity} > maximum {symbol.QuantityMaximum}";
                return false;
            }

            decimal value = quantity * price;
            if (symbol.QuoteValueMinimum > 0 && value < symbol.QuoteValueMinimum)
            {
                why = $"{what} value {value} {symbol.Quote} < minimum {symbol.QuoteValueMinimum}";
                return false;
            }
            if (symbol.QuoteValueMaximum > 0 && value > symbol.QuoteValueMaximum)
            {
                why = $"{what} value {value} {symbol.Quote} > maximum {symbol.QuoteValueMaximum}";
                return false;
            }
            return true;
        }


        // 1. The entry itself
        if (!CheckOrder("entry", entryQuantity, entryPrice, out reason))
            return false;
        decimal entryValue = entryQuantity * entryPrice;

        int multiplier = side == CryptoTradeSide.Long ? +1 : -1;


        // 2. Every DCA level behind it. Sized the way HandleDcaPart sizes them: the factor is a
        // percentage of the entry amount, and the entry amount is what the entry order really cost.
        decimal? extremeDcaPrice = null;
        for (int i = 0; i < GlobalData.Settings.Trading.DcaList.Count; i++)
        {
            var dcaEntry = GlobalData.Settings.Trading.DcaList[i];

            // A level at or beyond the signal SL is never placed, so it is not weighed either
            if (signalSlPercentage.HasValue && dcaEntry.Percentage >= signalSlPercentage.Value)
                continue;

            decimal dcaPrice = entryPrice - (multiplier * entryPrice * Math.Abs(dcaEntry.Percentage) / 100m);
            dcaPrice = dcaPrice.ClampPrice(side, symbol.PriceMinimum, symbol.PriceMaximum, symbol.PriceTickSize);
            if (dcaPrice <= 0)
            {
                reason = $"dca {i + 1} price {dcaPrice} is zero";
                return false;
            }

            decimal dcaValue = entryValue * dcaEntry.Factor / 100m;
            decimal dcaQuantity = (dcaValue / dcaPrice).Clamp(symbol.QuantityMinimum, symbol.QuantityMaximum, symbol.QuantityTickSize);
            if (!CheckOrder($"dca {i + 1} ({dcaEntry.Percentage}%)", dcaQuantity, dcaPrice, out reason))
                return false;

            // The list runs from near to far, so the last one placed is the extreme
            extremeDcaPrice = dcaPrice;
        }


        // 3. The exit. Worst case is the entry filling on its own and the exit having to carry only
        // that quantity - a DCA that fills only ever makes the exit order bigger. Every take profit
        // level is placed as its own order (its share of the quantity) carrying the stop loss prices,
        // so the profit price and the stop limit price both have to hold up against the limits.
        //
        // Only paper trading places stop loss orders at all (real trading would need OCO, which is
        // not implemented) - the same condition PositionMonitor.CalculateSlPrices applies. Weighing
        // a stop price that is never going to be sent would refuse entries for an order that does
        // not exist.
        StopLossCalculator.SlResult slResult = new() { Stop = null, Limit = null, Source = StopLossCalculator.SlSource.None };
        if (GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.PaperTrade ||
            GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.PaperTradingAndAltrady)
        {
            var slInput = new StopLossCalculator.SlInput
            {
                Side = side,
                SlPercentage = signalSlPercentage,
                EntryPrice = entryPrice,
                ExtremeDcaPrice = extremeDcaPrice,
                GlobalStopLossPercentage = GlobalData.Settings.Trading.StopLossPercentage,
                GlobalStopLossLimitPercentage = GlobalData.Settings.Trading.StopLossLimitPercentage,
            };
            slResult = StopLossCalculator.Calculate(slInput);
        }

        var tpList = GlobalData.Settings.Trading.TpList;
        decimal factorSum = 0;
        foreach (var tpEntry in tpList)
            factorSum += tpEntry.Factor;

        decimal allocated = 0;
        for (int i = 0; i < tpList.Count; i++)
        {
            // The same split PositionMonitor.ComputeTargets makes: every level but the last takes its
            // weighted share, the last one absorbs whatever is left over.
            decimal tpQuantity;
            if (i == tpList.Count - 1)
                tpQuantity = entryQuantity - allocated;
            else
            {
                decimal fraction = factorSum > 0 ? tpList[i].Factor / factorSum : 0;
                tpQuantity = (entryQuantity * fraction).Clamp(symbol.QuantityMinimum, symbol.QuantityMaximum, symbol.QuantityTickSize);
                allocated += tpQuantity;
            }

            decimal tpPrice = entryPrice + (multiplier * entryPrice * tpList[i].Percentage / 100m);
            tpPrice = tpPrice.ClampPrice(side, symbol.PriceMinimum, symbol.PriceMaximum, symbol.PriceTickSize);
            if (!CheckOrder($"take profit {i + 1} ({tpList[i].Percentage}%)", tpQuantity, tpPrice, out reason))
                return false;

            // The stop prices ride along on this same order, and for a long they sit BELOW the entry
            // - which is where an exit the exchange refuses comes from on a position that was only
            // just large enough to enter.
            if (slResult.Limit.HasValue)
            {
                decimal slLimit = slResult.Limit.Value.ClampPrice(side, symbol.PriceMinimum, symbol.PriceMaximum, symbol.PriceTickSize);
                if (slLimit > 0 && !CheckOrder($"stop loss {i + 1}", tpQuantity, slLimit, out reason))
                    return false;
            }
        }

        return true;
    }
}
