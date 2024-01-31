using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Exchange;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Trader;

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
                    await LoadTradesfromDatabaseAndExchange(database, position);
                    CalculatePositionResultsViaTrades(database, position);
                }
            }
        }
    }

    /// <summary>
    /// De break-even prijs berekenen vanuit de parts en steps
    /// </summary>
    private static void CalculateProfitAndBreakEvenPrice(CryptoPosition position)
    {
        //https://dappgrid.com/binance-fees-explained-fee-calculation/
        // You should first divide your order size(total) by 100 and then multiply it by your fee rate which 
        // is 0.10 % for VIP 0 / regular users. So, if you buy Bitcoin with 200 USDT, you will basically get
        // $199.8 worth of Bitcoin.To calculate these fees, you can also use our Binance fee calculator:
        // (als je verder gaat dan wordt het vanwege de kickback's tamelijk complex)
        // Op Bybit futures heb je de fundingrates, dat wordt in tijdblokken berekend met varierende fr..

        if (position.Parts.Count == 0)
            GlobalData.AddTextToLogTab(string.Format("CalculateProfitAndBeakEvenPrice - er zijn geen parts! {0}", position.Symbol.Name));

        position.Quantity = 0;
        position.Invested = 0;
        position.Returned = 0;
        position.Commission = 0;
        bool hasActiveDca = false;

        // Ondersteuning long/short
        CryptoOrderSide entryOrderSide = position.GetEntryOrderSide();
        CryptoOrderSide takeProfitOrderSide = position.GetTakeProfitOrderSide();

        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            part.Quantity = 0;
            part.Invested = 0;
            part.Returned = 0;
            part.Commission = 0;

            int tradeCount = 0;
            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                if (step.Status == CryptoOrderStatus.Filled)
                    // || step.Status == CryptoOrderStatus.PartiallyFilled => niet doen, want dan worden de TP's iedere keer verplaatst..
                    // Wellicht moet dat gedeelte op een andere manier ingeregeld worden zodat we hier wel de echte BE kunnen uitrekenen?
                {
                    part.Commission += step.Commission;
                    if (step.Side == entryOrderSide)
                    {
                        part.Quantity += step.QuantityFilled;
                        part.Invested += step.QuoteQuantityFilled;
                    }
                    else if (step.Side == takeProfitOrderSide)
                    {
                        tradeCount++;
                        part.Quantity -= step.QuantityFilled;
                        part.Returned += step.QuoteQuantityFilled;
                    }
                }
                //string s = string.Format("{0} CalculateProfit bought position={1} part={2} name={3} step={4} {5} price={6} stopprice={7} quantityfilled={8} QuoteQuantityFilled={9}",
                //   position.Symbol.Name, position.Id, part.Id, part.Purpose, step.Id, step.Name, step.Price, step.StopPrice, step.QuantityFilled, step.QuoteQuantityFilled);
                //GlobalData.AddTextToLogTab(s);
            }

            // Rekening houden met de toekomstige kosten van de sell orders.
            // NB: Dit klopt niet 100% als een order gedeeltelijk gevuld wordt!
            if (tradeCount == 0 && !part.CloseTime.HasValue)
                part.Commission *= 2;

            if (position.Side == CryptoTradeSide.Long)
            {
                part.Profit = part.Returned - part.Invested - part.Commission;
                part.Percentage = 0m;
                if (part.Invested != 0m)
                    //part.Percentage = 100m * (part.Returned - part.Commission) / part.Invested;
                    part.Percentage = 100m + (100m * part.Profit / part.Invested);
                if (part.Quantity > 0)
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
                    part.BreakEvenPrice = (part.Invested - part.Returned - part.Commission) / part.Quantity;
                else
                    part.BreakEvenPrice = 0;
            }
            if (part.Invested == 0)
                hasActiveDca = true;

            //string t = string.Format("{0} CalculateProfit sell invested={1} profit={2} bought={3} sold={4} steps={5}",
            //    position.Symbol.Name, part.Invested, part.Profit, part.Invested, part.Returned, part.Steps.Count);
            //GlobalData.AddTextToLogTab(t);


            position.Quantity += part.Quantity;
            position.Invested += part.Invested;
            position.Returned += part.Returned;
            position.Commission += part.Commission;
        }


        // Er is in geinvesteerd en dus moet de positie actief zijn
        if (position.Invested != 0 && position.Status == CryptoPositionStatus.Waiting)
        {
            position.CloseTime = null;
            position.Status = CryptoPositionStatus.Trading;
        }

        if (position.Side == CryptoTradeSide.Long)
        {
            position.Profit = position.Returned - position.Invested - position.Commission;
            position.Percentage = 0m;
            if (position.Invested != 0m)
                //position.Percentage = 100m * (position.Returned - position.Commission) / position.Invested;
                position.Percentage = 100m + (100m * position.Profit / position.Invested);
            if (position.Quantity > 0)
                position.BreakEvenPrice = (position.Invested - position.Returned + position.Commission) / position.Quantity;
            else
                position.BreakEvenPrice = 0;
        }
        else
        {
            position.Profit = position.Invested - position.Returned - position.Commission;
            position.Percentage = 0m;
            if (position.Returned != 0m)
                position.Percentage = 100m + (100m * position.Profit / position.Invested);
            if (position.Quantity > 0)
                position.BreakEvenPrice = (position.Invested - position.Returned - position.Commission) / position.Quantity; //?
            else
                position.BreakEvenPrice = 0;
        }

        // Correcties omdat de ActiveDca achteraf geintroduceerd is
        if (position.ActiveDca && !hasActiveDca)
        {
            position.ActiveDca = false;
            position.PartCount = position.Parts.Count;
        }
        if (position.ActiveDca)
        {
            //position.ActiveDca = true;
            position.PartCount = position.Parts.Count - 1;
        }
    }




    /// <summary>
    /// Na het opstarten is er behoefte om openstaande orders en trades te synchroniseren
    /// (dependency: de trades en steps moeten hiervoor ingelezen zijn)
    /// </summary>
    static public void CalculatePositionResultsViaTrades(CryptoDatabase database, CryptoPosition position, bool addToDoubleCheckPosition = true, bool saveChangesAnywhay = false)
    {
        if (position.Parts.Count == 0)
            GlobalData.AddTextToLogTab(string.Format("CalculatePositionViaTrades - er zijn geen parts! {0}", position.Symbol.Name));

        bool isChanged = saveChangesAnywhay;

        // Reset eerste de filled
        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            // TODO: Commission vanuit de trades laten doorwerken in de part en de position
            // part.Commission = 0;
            // probleem met de gebruikte asset (quote of bnb?) Omrekenen, maar hoe?

            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                step.Commission = 0;
                step.QuantityFilled = 0;
                step.QuoteQuantityFilled = 0;
            }
        }


        List<CryptoTrade> tradelist = position.Symbol.TradeList.Values.ToList();
        tradelist.Sort((x, y) => x.TradeTime.CompareTo(y.TradeTime));

        // De filled quantity in de steps opnieuw opbouwen vanuit de trades
        foreach (CryptoTrade trade in tradelist)
        {
            if (position.Orders.TryGetValue(trade.OrderId, out CryptoPositionStep step))
            {
                step.Commission += trade.Commission; // probleem, het asset (ETH enzovoort)
                step.QuantityFilled += trade.Quantity;
                step.QuoteQuantityFilled += trade.QuoteQuantity;
                step.AvgPrice = step.QuoteQuantityFilled / step.QuantityFilled;

                // Als er 1 (of meerdere trades zijn) dan zitten we in de trade (de user ticker valt wel eens stil)
                // Eventuele handmatige correctie geven daarna problemen (we mogen eigenlijk niet handmatig corrigeren)
                // (Dit geeft te denken aan de problemen als we straks een lopende order gaan opnemen als een positie)
                if (position.Status == CryptoPositionStatus.Waiting)
                {
                    isChanged = true;
                    position.CloseTime = null;
                    position.Status = CryptoPositionStatus.Trading;
                }

                // Vanuit nieuwe trades moeten we de status bijwerken (bij het opstarten)
                // Maar overschrijf de status alleen indien het absoluut zeker is..
                if (step.QuantityFilled >= step.Quantity)
                {
                    if (step.CloseTime != trade.TradeTime)
                    {
                        isChanged = true;
                        step.CloseTime = trade.TradeTime;
                    }

                    // Trades komen later binnen en soms tegelijk met andere thread spullen (eigenlijk conflicten!)
                    // En dan worden er vanwege multithreading zaken door elkaar gehaald, hier dan alsnog correctie!
                    // ==> Bottomline, de order blijkt gevuld te zijn, andere statussen zijn totaal niet meer relevant.
                    if (step.Status != CryptoOrderStatus.Filled)
                    {
                        isChanged = true;
                        step.Status = CryptoOrderStatus.Filled;
                    }
                }
                // Dit veroorzaakt altijd een isChange (duurt vrij lang vanwege bewaren), extra loopje
                //else if (step.QuantityFilled > 0)
                //{
                //// Mhhh, als we achteraf (na een gedeeltelijke fill) de order annuleren gaat het wellicht fout?
                //// Maar dat zien we dan wel weer...
                //if (step.Status == CryptoOrderStatus.New)
                //{
                //    if (step.Status != CryptoOrderStatus.PartiallyFilled)
                //        isChanged = true;
                //    step.Status = CryptoOrderStatus.PartiallyFilled;
                //}
                //}
            }
        }

        // Extra loop (anders wordt de status van een order met meerdere trades altijd bewaard vanwege de isChanged)
        int openOrders = 0;
        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                if (step.QuantityFilled > 0 && step.QuantityFilled < step.Quantity)
                {
                    // Mhhh, als we achteraf (na een gedeeltelijke fill) de order annuleren gaat het wellicht fout?
                    // Maar dat zien we dan wel weer...
                    //if (step.Status == CryptoOrderStatus.New) doe maar altijd (de status wordt nu per abuis op ChangedSettings gezet)
                    {
                        if (step.Status != CryptoOrderStatus.PartiallyFilled)
                            isChanged = true;
                        step.Status = CryptoOrderStatus.PartiallyFilled;
                    }
                }

                if (step.Status < CryptoOrderStatus.Filled)
                    openOrders++;
            }

        }

        // De positie doorrekenen (parts/steps)
        CalculateProfitAndBreakEvenPrice(position);


        // Als alles verkocht is de positie alsnog sluiten
        if (position.Status == CryptoPositionStatus.Trading)
        {
            // We hebben niets over en er is geinvesteerd (niet enkel openstaande orders)
            if (position.Quantity == 0 && position.Invested != 0)
            {
                isChanged = true;
                position.CloseTime = DateTime.UtcNow;
                if (position.Invested > 0)
                {
                    // Gevaarlijk, als er een buy niet gedetecteerd is dan wordt de trade zomaar afgesloten
                    // En dat lijkt soms wel te gebeuren vanwege de exchange, internet of datetime perikelen.
                    position.UpdateTime = DateTime.UtcNow;
                    position.Status = CryptoPositionStatus.Ready;
                    if (addToDoubleCheckPosition)
                        GlobalData.ThreadDoubleCheckPosition.AddToQueue(position);
                }
                GlobalData.AddTextToLogTab($"TradeTools: Positie {position.Symbol.Name} status aangepast naar {position.Status}");
            }
        }


        // Hebben we per abuis een part afgesloten (vanwege niet opgemerkte trades) terwijl de positie eigenlijk nog openstaat?
        // Achetraf worden de trades alsnog ingeladen, wordt de positie opengezet, maar de part blijft gelosten en de trader doets niets...
        if (!position.CloseTime.HasValue)
        {
            foreach (CryptoPositionPart part in position.Parts.Values.ToList())
            {
                if (part.CloseTime.HasValue && part.Invested != 0 && part.Returned == 0)
                {
                    isChanged = true;
                    part.CloseTime = null;
                    GlobalData.AddTextToLogTab($"TradeTools: Part {position.Symbol.Name} weer opengezet vanwege correctie?????? {position.Status}");
                }
            }
        }


        // De positie bewaren (dit kost nogal wat tijd, dus extra isChanged stuff)
        if (isChanged)
        {
            foreach (CryptoPositionPart part in position.Parts.Values.ToList())
            {
                foreach (CryptoPositionStep step in part.Steps.Values.ToList())
                    database.Connection.Update<CryptoPositionStep>(step);
                database.Connection.Update<CryptoPositionPart>(part);
            }
            database.Connection.Update<CryptoPosition>(position);
        }

        return;
    }


    static public async Task LoadTradesfromDatabaseAndExchange(CryptoDatabase database, CryptoPosition position, bool loadFromExchange = true)
    {
        // Bij het laden zijn niet alle trades in het geheugen ingelezen, dus deze alsnog inladen (of verversen)
        // Probleem, er zitten msec in de position.CreateTime en niet in de Trade.TradeTime (pfft)
        // string sql = string.Format("select * from trades where ExchangeId={0} and SymbolId={1} and TradeTime >='{2}' order by TradeId",
        // position.ExchangeId, position.SymbolId, position.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"));
        string sql = "select * from trade where SymbolId=@symbolId and TradeTime >= @fromDate order by TradeTime";
        foreach (CryptoTrade trade in database.Connection.Query<CryptoTrade>(sql, new { symbolId = position.SymbolId, fromDate = position.CreateTime }))
            GlobalData.AddTrade(trade);


        // Daarna de "nieuwe" trades van deze coin ophalen en die toegevoegen aan dezelfde tradelist
        // TODO: Afhankelijkheid uitfaseren of exchange-aware maken?
        if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading && loadFromExchange)
            await ExchangeHelper.FetchTradesAsync(position.TradeAccount, position.Symbol);
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
        takeProfitQuantity = takeProfitQuantity.Clamp(position.Symbol.QuantityMinimum, position.Symbol.QuantityMaximum, position.Symbol.QuantityTickSize);

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
            database.Connection.Insert<CryptoPositionStep>(step);
            PositionTools.AddPositionPartStep(part, step);
            part.ProfitMethod = CryptoEntryOrProfitMethod.FixedPercentage;
            database.Connection.Update<CryptoPositionPart>(part);
            database.Connection.Update<CryptoPosition>(position);

            if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.PaperTrade)
                PaperAssets.Change(position.TradeAccount, position.Symbol, result.tradeParams.OrderSide,
                    step.Status, result.tradeParams.Quantity, result.tradeParams.QuoteQuantity);

            ExchangeBase.Dump(position.Symbol, result.result, result.tradeParams, extraText);
        }
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
            return entryQuantity;


        // Is er een grote afwijking van tenminste -X%
        decimal clampedEntryValue = entryQuantity * entryPrice;
        decimal percentage = 100 * (clampedEntryValue - entryValue) / entryValue;

        // Het verschil is te groot, hier kunnen we niet instappen
        if (percentage > 125)
        {
            GlobalData.AddTextToLogTab($"{symbol.Name} vanwege de quantity ticksize {symbol.PriceTickSize} kunnen we niet instappen met de veel te hoge {clampedEntryValue} ({percentage}%) (DEBUG)");
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
                GlobalData.AddTextToLogTab($"{symbol.Name} vanwege de quantity ticksize {symbol.PriceTickSize} is de entry value verhoogd naar {newEntryValue} ({percentage}%) (DEBUG)");
                return newEntryQuantity;
            }
        }

        return entryQuantity;
    }
}
#endif
