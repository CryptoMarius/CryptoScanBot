using CryptoSbmScanner.Context;
using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Exchange;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Trader;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Intern;

public static class PositionTools
{
#if TRADEBOT

    public static bool ValidTradeAccount(CryptoTradeAccount tradeAccount, CryptoSymbol symbol)
    {
        // De exchanges moet uiteraard matchen
        if (symbol.ExchangeId == GlobalData.Settings.General.ExchangeId
            && tradeAccount.ExchangeId == GlobalData.Settings.General.ExchangeId) 
        {
            // Niet echt super, enumeratie oid hiervoor in het leven roepen, werkt verder wel
            if (tradeAccount.TradeAccountType == CryptoTradeAccountType.BackTest && GlobalData.BackTest)
                return true;
            if (tradeAccount.TradeAccountType == CryptoTradeAccountType.PaperTrade && GlobalData.Settings.Trading.TradeViaPaperTrading)
                return true;
            if (tradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading && GlobalData.Settings.Trading.TradeViaExchange)
                return true;
        }
        return false;
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


    public static CryptoPosition CreatePosition(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoSignalStrategy strategy, CryptoTradeSide side, 
        CryptoSymbolInterval symbolInterval, DateTime currentDate)
    {
        CryptoPosition position = new()
        {
            TradeAccount = tradeAccount,
            TradeAccountId = tradeAccount.Id,
            CreateTime = currentDate,
            UpdateTime = currentDate,
            Data = symbol.Name, // mag vervallen (maar is ook best handig)
            Symbol = symbol,
            SymbolId = symbol.Id,
            Exchange = symbol.Exchange,
            ExchangeId = symbol.ExchangeId,
            Interval = symbolInterval.Interval,
            IntervalId = symbolInterval.Interval.Id,
            Status = CryptoPositionStatus.Waiting,
            Strategy = strategy,
            ActiveDca = false,
            PartCount = 0,
            Side = side,
        };
        return position;
    }


    public static void ExtendPosition(CryptoDatabase database, CryptoPosition position, CryptoPartPurpose purpose, CryptoInterval interval,
        CryptoSignalStrategy strategy, CryptoEntryOrProfitMethod stepInMethod, decimal signalPrice, DateTime currentDate, bool manualOrder = false)
    {
        CryptoPositionPart part = new()
        {
            Purpose = purpose,
            PartNumber = position.Parts.Count,
            Strategy = strategy,
            Interval = interval,
            IntervalId = interval.Id,
            EntryMethod = stepInMethod,
            SignalPrice = signalPrice,
            CreateTime = currentDate,
            PositionId = position.Id,
            Symbol = position.Symbol,
            SymbolId = position.Symbol.Id,
            Exchange = position.Symbol.Exchange,
            ExchangeId = position.Symbol.ExchangeId,
            ManualOrder = manualOrder,
        };

        database.Connection.Insert<CryptoPositionPart>(part);
        AddPositionPart(position, part);

        position.PartCount += 1;
        position.ActiveDca = true;
        position.UpdateTime = part.CreateTime;
        database.Connection.Update<CryptoPosition>(position);

        // Nieuwe parts kunnen hierdoor via de cooldown worden uitgesteld
        position.Symbol.LastTradeDate = currentDate;

        GlobalData.AddTextToLogTab($"{position.Symbol.Name} {purpose} {stepInMethod} plaatsen op {signalPrice.ToString0(position.Symbol.PriceDisplayFormat)}");
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

                tradeAccount.PositionList.TryAdd(symbol.Name, []);
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


    static public void RemovePosition(CryptoTradeAccount tradeAccount, CryptoPosition position, bool addToClosed)
    {
        if (tradeAccount.PositionList.TryGetValue(position.Symbol.Name, out var positionList))
        {
            if (positionList.ContainsKey(position.Id))
            {
                positionList.Remove(position.Id);

                if (addToClosed)
                {
                    if (GlobalData.PositionsClosed.Count != 0)
                        GlobalData.PositionsClosed.Add(position);
                    else
                        GlobalData.PositionsClosed.Insert(0, position);
                }
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
            PositionTools.AddPositionPart(position, part);
        }

        // De steps
        sql = string.Format("select * from positionstep where PositionId={0} order by Id", position.Id);
        foreach (CryptoPositionStep step in database.Connection.Query<CryptoPositionStep>(sql))
        {
            if (position.Parts.TryGetValue(step.PositionPartId, out CryptoPositionPart part))
                AddPositionPartStep(part, step);
        }
    }


    public static CryptoPosition HasPosition(CryptoTradeAccount tradeAccount, CryptoSymbol symbol)
    {
        if (tradeAccount.PositionList.TryGetValue(symbol.Name, out var positionList))
        {
            foreach (var position in positionList.Values.ToList())
            {
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

#endif

    /// <summary>
    /// Zijn de aangevinkte intervallen UP?
    /// </summary>
    public static bool ValidTrendConditions(CryptoSymbol symbol, Dictionary<CryptoIntervalPeriod, CryptoTrendIndicator> trend, out string reaction)
    {
        foreach (KeyValuePair<CryptoIntervalPeriod, CryptoTrendIndicator> entry in trend)
        {
            var symbolPeriod = symbol.GetSymbolInterval(entry.Key);
            if (symbolPeriod.TrendIndicator != entry.Value)
            {
                reaction = $"trend op de {symbolPeriod.Interval.Name} niet gelijk aan {entry.Value}";
                return false;
            }
        }

        reaction = "";
        return true;
    }


    public static bool ValidMarketTrendConditions(CryptoSymbol symbol, List<(decimal minValue, decimal maxValue)> marketTrend, out string reaction)
    {
        if (marketTrend.Count != 0)
        {
            if (!symbol.TrendPercentage.HasValue)
            {
                reaction = $"Markettrend {symbol.Name} is niet berekend";
                return false;
            }

            foreach ((decimal minValue, decimal maxValue) entry in marketTrend)
            {
                decimal trendPercentage = (decimal)symbol.TrendPercentage;
                if (!trendPercentage.IsBetween(entry.minValue, entry.maxValue))
                {
                    string minValueStr = entry.minValue.ToString0("N2");
                    if (entry.minValue == decimal.MinValue)
                        minValueStr = "-maxint";
                    string maxValueStr = entry.maxValue.ToString0("N2");
                    if (entry.maxValue == decimal.MaxValue)
                        maxValueStr = "+maxint";
                    reaction = $"Markettrend {symbol.Name} {symbol.TrendPercentage?.ToString("N2")} niet tussen {minValueStr} en {maxValueStr}";
                    return false;
                }
            }
        }

        reaction = "";
        return true;
    }

}