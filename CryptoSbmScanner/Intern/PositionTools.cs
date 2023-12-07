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
                    // TODO: Long/Short! (en naamgeving)
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


    public CryptoPosition CreatePosition(CryptoSignalStrategy strategy, CryptoTradeSide side, CryptoSymbolInterval symbolInterval)
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
            PartCount = 0,
            Side = side,
        };
        return position;
    }


    static public CryptoPositionStep CreatePositionStep(CryptoPosition position, CryptoPositionPart part, 
        TradeParams tradeParams, CryptoTrailing trailing = CryptoTrailing.None)
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
                if (position.Side != CryptoTradeSide.Long) // || position.IntervalId != symbolInterval.IntervalId
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
    public static bool ValidFirstBuyConditions(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoCandle lastCandle1m, CryptoTradeSide side, out string reaction)
    {
        // Is de barometer goed genoeg dat we willen traden?
        if (!TradingRules.CheckBarometerValues(symbol.QuoteData.PauseBarometer[side], symbol.QuoteData, side, lastCandle1m, out reaction))
            return false;


        // Staat op de whitelist (kan leeg zijn)
        if (!SymbolTools.CheckSymbolWhiteListOversold(symbol, side, out reaction))
            return false;


        // Staat niet in de blacklist
        if (!SymbolTools.CheckSymbolBlackListOversold(symbol, side, out reaction))
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


    /// <summary>
    /// Zijn de aangevinkte intervallen UP?
    /// </summary>
    public static bool ValidTrendConditions(CryptoTradeSide mode, CryptoSignal signal, out string reaction)
    {
        foreach (KeyValuePair<CryptoIntervalPeriod, CryptoTrendIndicator> entry in TradingConfig.Trading[mode].Trend)
        {
            var symbolPeriod = signal.Symbol.GetSymbolInterval(entry.Key);
            if (symbolPeriod.TrendIndicator != entry.Value)
            {
                reaction = $"{symbolPeriod.Interval.Name} niet gelijk aan {entry.Value}";
                return false;
            }
        }

        reaction = "";
        return true;
    }


    /// <summary>
    /// Zijn de aangevinkte intervallen UP?
    /// </summary>
    //public static bool ValidBarometerConditions(CryptoTradeSide mode, CryptoSignal signal, out string reaction)
    //{
    //    foreach (KeyValuePair<CryptoInterval, decimal> entry in TradingConfig.Config[mode].BarometerOkForInterval)
    //    {
    //        signal.Symbol.Quote?

    //        var symbolPeriod = signal.Symbol.GetSymbolInterval(entry.Key.IntervalPeriod);
    //        if (symbolPeriod.TrendIndicator != entry.Value)
    //        {
    //            if (mode == CryptoTradeSide.Long)
    //                reaction = $"{entry.Key.Name} niet in uptrend";
    //            else
    //                reaction = $"{entry.Key.Name} niet in downtrend";
    //            return false;
    //        }
    //    }

    //    reaction = "";
    //    return true;
    //}

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
        if (!TradingRules.CheckTradingRules(lastCandle1m))
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

        // TODO: Asset management op de exchange implementeren (dit klopt helemaal niet)
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