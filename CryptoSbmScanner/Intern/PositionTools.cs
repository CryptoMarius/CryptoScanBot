using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Exchange;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Trader;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Intern;

public class PositionTools
{
#if TRADEBOT
    private DateTime CurrentDate { get; set; }
    private CryptoTradeAccount TradeAccount { get; set; }
    private CryptoSymbol Symbol { get; set; }
    

    public PositionTools(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, DateTime currentDate)
    {
        TradeAccount = tradeAccount;

        Symbol = symbol;
        CurrentDate = currentDate;
    }


    public static bool ValidTradeAccount(CryptoTradeAccount tradeAccount, CryptoSymbol symbol)
    {
        // De exchanges moet uiteraard matchen
        if (symbol.ExchangeId == GlobalData.Settings.General.ExchangeId
            && tradeAccount.ExchangeId == GlobalData.Settings.General.ExchangeId) 
        {
            // Niet echt super, enumeratie oid hiervoor in het leven roepen, werkt verder wel
            if (tradeAccount.TradeAccountType == CryptoTradeAccountType.BackTest && GlobalData.BackTest)
                return true;
            if (tradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading && GlobalData.Settings.Trading.TradeViaExchange)
                return true;
            if (tradeAccount.TradeAccountType == CryptoTradeAccountType.PaperTrade && GlobalData.Settings.Trading.TradeViaPaperTrading)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Retourneer de part met name=x
    /// </summary>
    //public static CryptoPositionPart FindPositionPart(CryptoPosition position, string name)
    //{
    //    foreach (CryptoPositionPart part in position.Parts.Values.ToList())
    //    {
    //        if (part.Name.Equals(name))
    //            return part;
    //    }
    //    return null;
    //}

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
    /// Retourneer de openstaande "order" met naam=x
    /// </summary>
    public static CryptoPositionStep FindPositionPartStep(CryptoPositionPart part, CryptoOrderSide side, bool closed)
    {
        foreach (CryptoPositionStep step in part.Steps.Values.ToList())
        {
            // Alle geannuleerde orders overslagen
            if (step.Side == side && step.Status < CryptoOrderStatus.Canceled)
            {
                // Kan ook partial gevuld zijn, wat gebeurd er dan? (-> extra dca, is okay)
                if (closed && step.CloseTime.HasValue)
                    return step;
                else
                    if (!closed && !step.CloseTime.HasValue)
                    return step;
            }
        }
        return null;
    }


    public static CryptoPositionStep GetLowestClosedBuy(CryptoPosition position)
    {
        // Retourneer de buy order van een niet afgesloten part (de laagste)
        CryptoPositionStep step = null;
        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            // Afgesloten DCA parts sluiten we uit (omdat we zogenaamde jojo's uitvoeren)
            if (part.CloseTime == null)
            {
                foreach (CryptoPositionStep stepX in part.Steps.Values.ToList())
                {
                    // Voor de zekerheid enkel de Status=Filled erbij (onzeker wat er exact gebeurd met een cancel en het kan geen kwaad)
                    if (stepX.Side == CryptoOrderSide.Buy && stepX.CloseTime.HasValue && stepX.Status == CryptoOrderStatus.Filled)
                    {
                        if (step == null || stepX.Price < step.Price)
                            step = stepX;
                    }
                }
            }
        }
        return step;
    }


    public CryptoPosition CreatePosition(CryptoSignalStrategy strategy, CryptoSymbolInterval symbolInterval)
    {
        CryptoPosition position = new()
        {
            TradeAccount = TradeAccount,
            TradeAccountId = TradeAccount.Id,
            CreateTime = CurrentDate,
            UpdateTime = CurrentDate,
            Data = Symbol.Name, // mag vervallen (maar ach)
            Symbol = Symbol,
            SymbolId = Symbol.Id,
            Exchange = Symbol.Exchange,
            ExchangeId = Symbol.ExchangeId,
            Interval = symbolInterval.Interval,
            IntervalId = symbolInterval.Interval.Id,
            Status = CryptoPositionStatus.Waiting,
            Strategy = strategy,
            Side = CryptoOrderSide.Buy
        };
        return position;
    }


    static public CryptoPositionStep CreatePositionStep(CryptoPosition position, CryptoPositionPart part, 
        TradeParams tradeParams, string name, CryptoTrailing trailing = CryptoTrailing.None)
    {
        CryptoPositionStep step = new()
        {
            PositionId = position.Id,
            PositionPartId = part.Id,

            Side = tradeParams.OrderSide,
            Status = CryptoOrderStatus.New,
            OrderType = tradeParams.OrderType,
            CreateTime = tradeParams.CreateTime,

            Price = tradeParams.Price,
            StopPrice = tradeParams.StopPrice,
            StopLimitPrice = tradeParams.LimitPrice,

            Quantity = tradeParams.Quantity,
            QuantityFilled = 0,
            QuoteQuantityFilled = 0,

            OrderId = tradeParams.OrderId,
            Order2Id = tradeParams.Order2Id,

            Trailing = trailing
        };

        position.UpdateTime = step.CreateTime;
        return step;
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
        if (step.OrderId != null && step.OrderId != "")
            part.Position.Orders.TryAdd(step.OrderId, step);
        if (step.Order2Id != null && step.Order2Id != "") 
            part.Position.Orders.TryAdd(step.Order2Id, step);
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
    public static void CalculateProfitAndBreakEvenPrice(CryptoPosition position)
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

            int sellCount = 0;
            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                if (step.Status == CryptoOrderStatus.Filled || step.Status == CryptoOrderStatus.PartiallyFilled)
                {
                    if (step.Side == CryptoOrderSide.Buy)
                    {
                        part.Commission += step.Commission;
                        part.Quantity += step.QuantityFilled;
                        part.Invested += step.QuoteQuantityFilled;
                    }
                    else if (step.Side == CryptoOrderSide.Sell)
                    {
                        sellCount++;
                        part.Commission += step.Commission;
                        part.Quantity -= step.QuantityFilled;
                        part.Returned += step.QuoteQuantityFilled;
                    }
                }
                //string s = string.Format("{0} CalculateProfit bought position={1} part={2} name={3} step={4} {5} price={6} stopprice={7} quantityfilled={8} QuoteQuantityFilled={9}",
                //   position.Symbol.Name, position.Id, part.Id, part.Name, step.Id, step.Name, step.Price, step.StopPrice, step.QuantityFilled, step.QuoteQuantityFilled);
                //GlobalData.AddTextToLogTab(s);
            }

            // Rekening houden met de toekomstige kosten van de sell orders.
            // NB: Dit klopt niet 100% als een order gedeeltelijk gevuld wordt!
            if (sellCount == 0 && !part.CloseTime.HasValue)
                part.Commission *= 2;


            part.Profit = part.Returned - part.Invested - part.Commission;
            part.Percentage = 0m;
            if (part.Invested != 0m)
                part.Percentage = 100m * (part.Returned - part.Commission) / part.Invested;
            if (part.Quantity > 0)
                part.BreakEvenPrice = (part.Invested + part.Commission - part.Returned) / part.Quantity;
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

        bool isChanged = false;

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

        // De filled quantity in de steps opnieuw opbouwen vanuit de trades
        foreach (CryptoTrade trade in position.Symbol.TradeList.Values.ToList())
        {
            if (position.Orders.TryGetValue(trade.OrderId, out CryptoPositionStep step))
            {
                step.Commission += trade.Commission; // probleem, het asset (BUSD enzovoort)
                step.QuantityFilled += trade.Quantity;
                step.QuoteQuantityFilled += trade.QuoteQuantity;

                step.AvgPrice = step.QuoteQuantityFilled / step.QuantityFilled;

                // Vanuit nieuwe trades moeten we de status wel bijwerken (opstarten applicatie)
                // Maar overschrijf de status alleen indien het absoluut zeker is..
                if (step.QuantityFilled >= step.Quantity)
                {
                    if (step.CloseTime != trade.TradeTime)
                        isChanged = true;
                    step.CloseTime = trade.TradeTime;
                    // Die Expired begrijp ik niet, waarom? (het lijkt een soort van correctie te zijn)
                    if (step.Status < CryptoOrderStatus.Filled || step.Status == CryptoOrderStatus.Expired)
                    {
                        if (step.Status != CryptoOrderStatus.Filled)
                            isChanged = true;
                        step.Status = CryptoOrderStatus.Filled;
                    }
                }
                else if (step.QuantityFilled > 0)
                {
                    if (step.Status == CryptoOrderStatus.New)
                    {
                        if (step.Status != CryptoOrderStatus.PartiallyFilled)
                            isChanged = true;
                        step.Status = CryptoOrderStatus.PartiallyFilled;
                    }
                }
            }
        }

        // De positie doorrekenen (parts/steps)
        CalculateProfitAndBreakEvenPrice(position);


        // De parts en steps bewaren
        int openOrders = 0;
        foreach (CryptoPositionPart part in position.Parts.Values.ToList())
        {
            foreach (CryptoPositionStep step in part.Steps.Values.ToList())
            {
                if (step.Status < CryptoOrderStatus.Filled)
                    openOrders++;
            }
        }



        // Als alles verkocht is de positie alsnog sluiten
        // Of inden alle orders geannuleerd zijn alsnog sluitne (timeout's)
        if (position.Status == CryptoPositionStatus.Trading || position.Status == CryptoPositionStatus.Waiting)
        {
            if (position.Quantity == 0 && openOrders == 0)
            {
                isChanged = true;
                position.CloseTime = DateTime.UtcNow;
                if (position.Invested > 0)
                    position.Status = CryptoPositionStatus.Ready;
                else
                    position.Status = CryptoPositionStatus.Timeout;
                GlobalData.AddTextToLogTab(string.Format("TradeTools: Positie {0} status aangepast naar {1}", position.Symbol.Name, position.Status));
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


    static public async Task LoadTradesfromDatabaseAndExchange(CryptoDatabase database, CryptoPosition position)
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
        if (position.TradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading)
            await ExchangeHelper.FetchTradesAsync(position.TradeAccount, position.Symbol);
    }


    public static void LoadPosition(CryptoDatabase database, CryptoPosition position)
    {
        // De parts
        string sql = string.Format("select * from positionpart where PositionId={0} order by Id", position.Id);
        foreach (CryptoPositionPart part in database.Connection.Query<CryptoPositionPart>(sql))
        {
            if (part.IntervalId.HasValue && GlobalData.IntervalListId.TryGetValue((int)position.IntervalId, out CryptoInterval interval))
               part.Interval = interval;
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


    /// <summary>
    /// Controles die noodzakelijk zijn voor een eerste koop
    /// </summary>
    public static bool ValidFirstBuyConditions(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoCandle lastCandle1m, out string reaction)
    {
        //// Is de barometer goed genoeg dat we willen traden?
        //if (!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval15m, GlobalData.Settings.Trading.Barometer15mBotMinimal, out reaction) ||
        //(!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval30m, GlobalData.Settings.Trading.Barometer30mBotMinimal, out reaction)) ||
        //(!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval1h, GlobalData.Settings.Trading.Barometer01hBotMinimal, out reaction)) ||
        //(!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval4h, GlobalData.Settings.Trading.Barometer04hBotMinimal, out reaction)) ||
        //(!SymbolTools.CheckValidBarometer(symbol.QuoteData, CryptoIntervalPeriod.interval1d, GlobalData.Settings.Trading.Barometer24hBotMinimal, out reaction)))
        //    return false;

        if (!TradingRules.CheckBarometerValues(symbol, lastCandle1m))
        {
            reaction = symbol.QuoteData.PauseTrading.Text;
            return false;
        }


        // Staat op de whitelist (kan leeg zijn)
        if (!SymbolTools.CheckSymbolWhiteListOversold(symbol, CryptoOrderSide.Buy, out reaction))
            return false;


        // Staat niet in de blacklist
        if (!SymbolTools.CheckSymbolBlackListOversold(symbol, CryptoOrderSide.Buy, out reaction))
            return false;


        // Heeft de munt genoeg 24h volume
        if (!SymbolTools.CheckValidMinimalVolume(symbol, out reaction))
            return false;


        // Heeft de munt een redelijke prijs
        if (!SymbolTools.CheckValidMinimalPrice(symbol, out reaction))
            return false;


        // Is de munt te nieuw? (hebben we vertrouwen in nieuwe munten?)
        if (!SymbolTools.CheckNewCoin(symbol, out reaction))
            return false;


        // Munten waarvan de ticksize percentage nogal groot is (barcode charts)
        if (!SymbolTools.CheckMinimumTickPercentage(symbol, out reaction))
            return false;

        if (!SymbolTools.CheckAvailableSlots(tradeAccount, symbol, out reaction))
            return false;


        return true;
    }


    public static bool CheckTradingAndSymbolConditions(CryptoSymbol symbol, CryptoCandle lastCandle1m, out string reaction)
    {
        // Als de bot niet actief is dan ook geen monitoring (queue leegmaken)
        // Blijkbaar is de bot dan door de gebruiker uitgezet, verwijder de signalen
        if (!GlobalData.Settings.Trading.Active)
        {
            reaction = "trade-bot deactivated";
            return false;
        }

        // we doen (momenteel) alleen long posities
        if (!symbol.LastPrice.HasValue)
        {
            reaction = "symbol price null";
            return false;
        }

        // Om te voorkomen dat we te snel achter elkaar in dezelfde munt stappen
        if (symbol.LastTradeDate.HasValue && symbol.LastTradeDate?.AddMinutes(GlobalData.Settings.Trading.GlobalBuyCooldownTime) > lastCandle1m.Date) // DateTime.UtcNow
        {
            reaction = "is in cooldown";
            return false;
        }

        // Als een munt snel is gedaald dan stoppen
        if (TradingRules.CheckTradingRules(lastCandle1m))
        {
            reaction = string.Format(" de bot is gepauseerd omdat {0}", GlobalData.PauseTrading.Text);
            return false;
        }

        reaction = "";
        return true;
    }


    public bool CheckAvaliableAssets(CryptoTradeAccount tradeAccount, out decimal assetQuantity, out string reaction)
    {
        reaction = "";
        assetQuantity = 0;
        if (!PositionTools.ValidTradeAccount(tradeAccount, Symbol))
            return false;

        if (tradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading)
        {
            var (result, value) = SymbolTools.CheckPortFolio(tradeAccount, Symbol);
            if (!result)
                return false;
            assetQuantity = value;
        }
        else
            assetQuantity = 1000000m; // ruim genoeg voor backtest of papertrading (todo)

        return true;
    }


    public bool CheckExchangeApiKeys(CryptoTradeAccount tradeAccount, out string reaction)
    {
        reaction = "";
        if (!PositionTools.ValidTradeAccount(tradeAccount, Symbol))
            return false;

        if (tradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading)
        {
            // Is er een API key aanwezig (met de juiste opties)
            if (!SymbolTools.CheckValidApikey(out reaction))
                return false;
        }

        return true;
    }

#endif

}