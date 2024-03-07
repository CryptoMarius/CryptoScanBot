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
        // Alle openstaande posities lezen 
        GlobalData.AddTextToLogTab("Checking open positions");

        using var database = new CryptoDatabase();
        foreach (CryptoTradeAccount tradeAccount in GlobalData.TradeAccountList.Values.ToList())
        {
            //foreach (CryptoPosition position in tradeAccount.PositionList.Values.ToList())
            foreach (var positionList in tradeAccount.PositionList.Values.ToList())
            {
                foreach (var position in positionList.Values.ToList())
                {
                    // Controleer de openstaande orders, zijn ze ondertussen gevuld
                    // Haal de trades van deze positie op vanaf de 1e order
                    // TODO - Hoe doen we dit met papertrading (er is niets geregeld!)
                    await LoadOrdersFromDatabaseAndExchangeAsync(database, position);
                    await CalculatePositionResultsViaOrders(database, position);
                    GlobalData.ThreadDoubleCheckPosition.AddToQueue(position, true);
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
                if (step.Status == CryptoOrderStatus.Filled)
                {
                    if (step.Side == entryOrderSide)
                    {
                        part.Quantity += step.QuantityFilled;
                        part.Invested += step.QuoteQuantityFilled;

                        // Bybit spot fix
                        if (step.CommissionAsset != null && step.CommissionAsset == position.Symbol.Base)
                            part.Quantity -= step.CommissionBase;
                    }
                    else if (step.Side == takeProfitOrderSide)
                    {
                        tradeCount++;
                        part.Quantity -= step.QuantityFilled;
                        part.Returned += step.QuoteQuantityFilled;

                        // Bybit spot fix
                        if (step.CommissionAsset != null && step.CommissionAsset == position.Symbol.Base)
                            part.Quantity -= step.CommissionQuote;
                    }
                    part.Commission += step.Commission;
                    part.RemainingDust += step.RemainingDust;
                    part.CommissionBase += step.CommissionBase;
                    part.CommissionQuote += step.CommissionQuote;
                }
                else if (step.Status == CryptoOrderStatus.New || step.Status == CryptoOrderStatus.PartiallyFilled || step.Status == CryptoOrderStatus.PartiallyAndClosed)
                {
                    // Voluit, ook als ie voor 99% gevuld is..
                    part.Reserved += step.Price * step.Quantity;
                }

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
            position.Invested += part.Invested;
            position.Returned += part.Returned;
            position.Reserved += part.Reserved;
            position.Commission += part.Commission;
            position.RemainingDust += part.RemainingDust;
            position.CommissionBase += part.CommissionBase;
            position.CommissionQuote += part.CommissionQuote;
        }


        // Er is in geinvesteerd en dus moet de positie actief zijn
        if (position.Invested != 0 && position.Status == CryptoPositionStatus.Waiting)
        {
            position.CloseTime = null;
            position.Status = CryptoPositionStatus.Trading;
        }

        // Voor de BE de originele quantity gebruiken (niet de gecorrigeerde met EntryQuantity-commissionBase dus daarom een correctie)
        if (position.Side == CryptoTradeSide.Long)
        {
            position.Profit = position.Returned - position.Invested - position.Commission;
            position.Percentage = 0m;
            if (position.Invested != 0m)
                position.Percentage = 100m + (100m * position.Profit / position.Invested);
            // TODO: Dit werkt niet op spot ICM dust (bepaling position.BE)
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
            // TODO: Dit werkt niet op spot ICM dust (bepaling position.BE)
            if (position.Quantity > 0 && position.Status == CryptoPositionStatus.Trading)
                position.BreakEvenPrice = (position.Invested - position.Returned - position.Commission) / (position.Quantity + position.CommissionBase);
            else
                position.BreakEvenPrice = 0;
        }

        // Correcties omdat de ActiveDca achteraf geintroduceerd is
        position.ActiveDca = hasActiveDca;
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
        if (position.Status == CryptoPositionStatus.Ready)
            return;


        bool markedAsReady = false;
        bool positionChanged = false;
        await ExchangeHelper.GetOrdersForPositionAsync(database, position);


        // De filled quantity in de steps opnieuw opbouwen vanuit de trades
        foreach (CryptoOrder order in position.Symbol.OrderList.Values.ToList())
        {
            if (position.Orders.TryGetValue(order.OrderId, out CryptoPositionStep step))
            {
                if (step.Status != order.Status || forceCalculation)
                {
                    positionChanged = true;
                    //step.Status = order.Status; pas op het einde

                    //  Bij iedere step hoort een part (maar technisch kan alles)
                    CryptoPositionPart part = PositionTools.FindPositionPart(position, step.PositionPartId);
                    if (part == null)
                        throw new Exception("Probleem met het vinden van een part");

                    string s;
                    string msgInfo = $"{position.Symbol.Name} side={order.Side} type={order.Type} status={order.Status} order={order.OrderId} " +
                        $"price={order.AveragePrice?.ToString0()} quantity={order.QuantityFilled?.ToString0()} value={order.QuoteQuantity.ToString0()}";



                    // Calculate the fee from te orders
                    // (Bybit V5 does not return the fee via orders)
                    step.Commission = 0;
                    step.CommissionBase = 0;
                    step.CommissionQuote = 0;
                    step.CommissionAsset = ""; //?
                    foreach (CryptoTrade trade in position.Symbol.TradeList.Values.ToList())
                    {
                        if (trade.OrderId == step.OrderId)
                        {
                            // Afhankelijk van de asset wordt de commissie berekend (debug)
                            // Voor de step is dit niet relevant (mits we het omrekenen naar base en quote)
                            step.CommissionAsset = trade.CommissionAsset;

                            if (trade.CommissionAsset == position.Symbol.Base)
                            {
                                decimal value = (decimal)trade.Commission * (decimal)trade.Price;
                                step.CommissionBase += (decimal)trade.Commission;
                                step.CommissionQuote += value;
                                step.Commission += value;
                            }
                            else if (trade.CommissionAsset == position.Symbol.Quote || trade.CommissionAsset == "")
                            {
                                decimal value = (decimal)trade.Commission / (decimal)trade.Price;
                                step.CommissionBase += value;
                                step.CommissionQuote += (decimal)trade.Commission;
                                step.Commission += (decimal)trade.Commission;
                            }
                            //else
                            //{
                            //    // dan doen we de aanname dat het in quote is
                            //    decimal value = (decimal)trade.Commission / (decimal)trade.Price;
                            //    step.CommissionBase += value;
                            //    step.CommissionQuote += (decimal)trade.Commission;
                            //    step.Commission += (decimal)trade.Commission;
                            //}
                        }
                    }


                    // A Bybit Spot special / marketorder (some weird status).
                    // Exchange is van de opdracht afgeweken, we passen de originele order aan.
                    if (order.Status == CryptoOrderStatus.PartiallyAndClosed)
                    {
                        //step.Status = order.Status;
                        step.Quantity = order.Quantity;
                        step.QuantityFilled = (decimal)order.QuantityFilled;
                        step.QuoteQuantityFilled = (decimal)order.AveragePrice * (decimal)order.QuantityFilled;
                    }


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
                            //step.Status = CryptoOrderStatus.Canceled;
                        }
                        else
                        {
                            step.CloseTime = order.UpdateTime;
                            //step.Status = CryptoOrderStatus.Canceled;

                            // De gebruiker heeft de order geannuleerd, het is vanaf nu zijn/haar verantwoordelijkheid...
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
                            //database.Connection.Update<CryptoPositionPart>(part);

                            //s = string.Format("handletrade#7 {0} positie part cancelled, user takeover?", msgInfo);
                            //GlobalData.AddTextToLogTab(s);
                            //GlobalData.AddTextToTelegram(s);

                            // De gebruiker heeft de positie geannuleerd
                            position.Profit = 0;
                            position.Invested = 0;
                            position.Returned = 0;
                            position.Reserved = 0;
                            position.Commission = 0;
                            position.CommissionBase = 0;
                            position.CommissionQuote = 0;
                            position.Percentage = 0;
                            position.CloseTime = order.UpdateTime;
                            if (!position.CloseTime.HasValue)
                                position.CloseTime = DateTime.UtcNow;

                            position.Status = CryptoPositionStatus.TakeOver;
                            //database.Connection.Update<CryptoPosition>(position);

                            s = $"handletrade#7 {msgInfo} positie cancelled, user takeover? position.status={position.Status}";
                            GlobalData.AddTextToLogTab(s);
                            GlobalData.AddTextToTelegram(s, position);
                        }
                    }
                    else if (order.Status == CryptoOrderStatus.Filled || order.Status == CryptoOrderStatus.PartiallyAndClosed)
                    {
                        step.CloseTime = order.UpdateTime;
                        step.QuantityFilled = (decimal)order.QuantityFilled;
                        step.QuoteQuantityFilled = (decimal)order.QuoteQuantityFilled;


                        // Als er 1 (of meerdere trades zijn) dan zitten we in de trade (de user ticker valt wel eens stil)
                        // Eventuele handmatige correctie geven daarna problemen (we mogen eigenlijk niet handmatig corrigeren)
                        // (Dit geeft te denken aan de problemen als we straks een lopende order gaan opnemen als een positie)
                        if (position.Status == CryptoPositionStatus.Waiting)
                        {
                            position.CloseTime = null;
                            position.Status = CryptoPositionStatus.Trading;
                            step.AveragePrice = (decimal)order.AveragePrice;
                        }




                        CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();
                        if (step.Side == takeProfitOrderSide)
                        {
                            // De take profit order is uitgevoerd, de positie afmelden
                            //s = $"handletrade {msgInfo} part takeprofit ({part.Percentage:N2}%)";
                            //GlobalData.AddTextToLogTab(s);
                            //GlobalData.AddTextToTelegram(s, position);

                            part.CloseTime = order.UpdateTime;
                            //database.Connection.Update<CryptoPositionPart>(part);

                            // Sluit de positie indien afgerond
                            // Dit is ook "gevaarlijk", want als er een dca buy niet gedetecteerd is wordt de trade afgesloten
                            // En dat lijkt soms wel te gebeuren vanwege vertraging/storingen scanner/exchange, internet of
                            // computer gerelateerde tijd perikelen.  Wat doe je eraan, het is niet 100% perfect..
                            if (position.Invested > 0 && position.Quantity <= position.RemainingDust)
                            {
                                position.CloseTime = order.UpdateTime;
                                position.Status = CryptoPositionStatus.Ready;
                            }

                            // Dca orders bijstellen
                            position.Reposition = true;
                            position.UpdateTime = DateTime.UtcNow;
                            database.Connection.Update<CryptoPosition>(position);

                            // Statistiek symbol niveau (voor de cooldown)
                            position.Symbol.LastTradeDate = position.CloseTime;
                            //database.Connection.Update<CryptoSymbol>(position.Symbol);


                            if (position.Status == CryptoPositionStatus.Timeout)
                                s = $"handletrade {msgInfo} position timeout ({position.Percentage:N2}%)";
                            else if (position.Status == CryptoPositionStatus.Ready)
                                s = $"handletrade {msgInfo} position ready ({position.Percentage:N2}%)";
                            else
                                s = $"handletrade {msgInfo} part takeprofit ({part.Percentage:N2}%)";
                            GlobalData.AddTextToLogTab(s);
                            GlobalData.AddTextToTelegram(s, position);
                        }



                        CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
                        if (step.Side == entryOrderSide)
                        {
                            s = $"handletrade {msgInfo} part entry";
                            GlobalData.AddTextToLogTab(s);
                            GlobalData.AddTextToTelegram(s, position);

                            position.Reposition = true;
                            //database.Connection.Update<CryptoPosition>(position);
                        }
                    }

                    // De reden van annuleren niet overschrijven (kunnen wat mij btereft ook weg, extra text veld voor annuleren wellicht?)
                    if (step.Status < CryptoOrderStatus.Canceled)
                        step.Status = order.Status;
                    //database.Connection.Update<CryptoPositionStep>(step);
                }
            }
        }


        if (positionChanged || forceCalculation)
        {
            CalculateProfitAndBreakEvenPrice(position);



            // Als alles verkocht is de positie alsnog sluiten
            // TODO: Controle of dit wel goed komt, vergelijken met quantity icm met dust is vragen om problemen.. Maar het kan wellicht
            if (position.Status == CryptoPositionStatus.Trading && position.Invested != 0 && position.Quantity <= position.RemainingDust)
            {
                positionChanged = true;
                markedAsReady = true;
                position.CloseTime = DateTime.UtcNow;
                position.UpdateTime = DateTime.UtcNow;
                position.Status = CryptoPositionStatus.Ready;
                GlobalData.AddTextToLogTab($"TradeTools: Positie {position.Symbol.Name} status aangepast naar {position.Status}");
            }


            // Hebben we per abuis een part afgesloten (vanwege niet opgemerkte trades) terwijl de positie eigenlijk nog openstaat?
            // Achteraf worden de trades alsnog ingeladen, wordt de positie opengezet, maar de part blijft gelosten en de trader doets niets...
            if (!position.CloseTime.HasValue)
            {
                foreach (CryptoPositionPart part in position.Parts.Values.ToList())
                {
                    if (part.CloseTime.HasValue && part.Invested != 0 && part.Returned == 0)
                    {
                        positionChanged = true;
                        part.CloseTime = null;
                        GlobalData.AddTextToLogTab($"TradeTools: Part {position.Symbol.Name} weer opengezet vanwege correctie?????? {position.Status}");
                    }
                }
            }


            // De positie bewaren (dit kost nogal wat tijd, dus extra controle of het nodig is)
            //if (positionChanged)
            {
                foreach (CryptoPositionPart part in position.Parts.Values.ToList())
                {
                    foreach (CryptoPositionStep step in part.Steps.Values.ToList())
                        database.Connection.Update<CryptoPositionStep>(step);
                    database.Connection.Update<CryptoPositionPart>(part);
                }
                database.Connection.Update<CryptoPosition>(position);
            }


            // Laatste controles uitvoeren en de nog openstaande DCA orders afsluiten
            if (markedAsReady)
            {
                position.DelayUntil = position.UpdateTime.Value.AddSeconds(10);
                GlobalData.ThreadDoubleCheckPosition.AddToQueue(position, true);
            }
        }
    }



    static public async Task LoadOrdersFromDatabaseAndExchangeAsync(CryptoDatabase database, CryptoPosition position) //, bool loadFromExchange = true
    {
        if (!position.Symbol.HasOrdersAndTradesLoaded)
        {
            // Vanwege tijd afrondingen (msec)
            DateTime from = position.CreateTime.AddMinutes(-1);

            //// Bij het laden zijn niet alle trades in het geheugen ingelezen, dus deze alsnog inladen (of verversen)
            string sql = "select * from [order] where SymbolId=@symbolId and CreateTime >= @fromDate order by CreateTime";
            foreach (CryptoOrder order in database.Connection.Query<CryptoOrder>(sql, new { symbolId = position.SymbolId, fromDate = from }))
                GlobalData.AddOrder(order);

            sql = "select * from [trade] where SymbolId=@symbolId and TradeTime >= @fromDate order by TradeTime";
            foreach (CryptoTrade trade in database.Connection.Query<CryptoTrade>(sql, new { symbolId = position.SymbolId, fromDate = from }))
                GlobalData.AddTrade(trade);

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
