using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Trader;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Intern;

public static class PositionTools
{

    /// <summary>
    /// Retourneer de part met id=x
    /// </summary>
    public static CryptoPositionPart? FindPositionPart(CryptoPosition position, int Id)
    {
        foreach (CryptoPositionPart part in position.PartList.Values.ToList())
        {
            if (part.Id == Id)
                return part;
        }
        return null;
    }

    /// <summary>
    /// Retourneer een openstaande TP
    /// </summary>
    public static CryptoPositionStep? FindPositionPartStep(CryptoPositionPart part, CryptoOrderSide side, bool closed)
    {
        foreach (CryptoPositionStep step in part.StepList.Values.ToList())
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

    public static CryptoPositionStep? FindOpenStep(CryptoPosition position, CryptoOrderSide side, CryptoPartPurpose purpose)
    {
        foreach (CryptoPositionPart part in position.PartList.Values.ToList())
        {
            if (!part.CloseTime.HasValue && part.Purpose == purpose)
            {
                foreach (CryptoPositionStep step in part.StepList.Values.ToList())
                {
                    if (!step.CloseTime.HasValue && step.Side == side)
                    {
                        return step;
                    }
                }
            }
        }
        return null;
    }

    public static CryptoPosition CreatePosition(CryptoAccount tradeAccount, CryptoSymbol symbol, CryptoSignalStrategy strategy, CryptoTradeSide side, 
        CryptoSymbolInterval symbolInterval, DateTime currentDate)
    {
        CryptoPosition position = new()
        {
            Account = tradeAccount,
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

    public static void AddSignalProperties(CryptoPosition position, CryptoSignal signal)
    {
        position.SignalEventTime = signal.CloseDate;
        // Copy common indicator values
        position.AssignValues(signal);
        // Reset the statistics though
        position.PriceMin = signal.SignalPrice;
        position.PriceMax = signal.SignalPrice;
        position.PriceMinPerc = 0;
        position.PriceMaxPerc = 0;
    }

    public static CryptoPositionPart ExtendPosition(CryptoDatabase database, CryptoPosition position, CryptoPartPurpose purpose, CryptoInterval interval,
        CryptoSignalStrategy strategy, CryptoEntryOrDcaStrategy stepInMethod, decimal signalPrice, DateTime currentDate, bool manualOrder = false)
    {
        CryptoPositionPart part = new()
        {
            Purpose = purpose,
            PartNumber = position.PartList.Count, //position.PartCount + 1, // 
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

        if (purpose == CryptoPartPurpose.Dca)
            position.ActiveDca = true;

        position.UpdateTime = part.CreateTime;
        database.Connection.Update<CryptoPosition>(position);

        // Nieuwe parts kunnen hierdoor via de cooldown worden uitgesteld
        position.Symbol.LastTradeDate = currentDate;

        GlobalData.AddTextToLogTab($"{position.Symbol.Name} {purpose} {stepInMethod} plaatsen op {signalPrice.ToString0(position.Symbol.PriceDisplayFormat)}");
        return part;
    }


    static public CryptoPositionStep CreatePositionStep(CryptoPosition position, CryptoPositionPart part, 
        TradeParams tradeParams, CryptoTrailing trailing = CryptoTrailing.None)
    {
        CryptoPositionStep step = new()
        {
            PositionId = position.Id,
            PositionPartId = part.Id,

            CancelInProgress = false,
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

        if (position.UpdateTime == null || step.CreateTime > position.UpdateTime)
            position.UpdateTime = step.CreateTime;
        return step;
    }

    public static void AddPosition(CryptoAccount tradeAccount, CryptoPosition position)
    {
        position.Account = tradeAccount;
        if (GlobalData.ExchangeListId.TryGetValue(position.ExchangeId, out Model.CryptoExchange? exchange))
        {
            position.Exchange = exchange;
            if (exchange.SymbolListId.TryGetValue(position.SymbolId, out CryptoSymbol? symbol))
            {
                position.Symbol = symbol;
                if (GlobalData.IntervalListId.TryGetValue((int)position.IntervalId!, out CryptoInterval? interval))
                    position.Interval = interval;

                tradeAccount.Data.PositionList.TryAdd(symbol.Name, position);
            }
        }
    }


    static public void AddPositionPart(CryptoPosition position, CryptoPositionPart part)
    {
        position.PartList.TryAdd(part.Id, part);
        part.Position = position; // parent
        part.Exchange = position.Exchange;
        part.Symbol = position.Symbol;
    }


    static public void AddPositionPartStep(CryptoPositionPart part, CryptoPositionStep step)
    {
        part.StepList.TryAdd(step.Id, step);

        // OrderId index aanvullen
        if (step.OrderId != null && step.OrderId != "")
            part.Position.StepOrderList.TryAdd(step.OrderId, step);
        if (step.Order2Id != null && step.Order2Id != "") 
            part.Position.StepOrderList.TryAdd(step.Order2Id, step);
    }


    static public void AddPositionClosed(CryptoPosition position)
    {
        if (GlobalData.TradeAccountList.TryGetValue(position.TradeAccountId, out CryptoAccount? tradeAccount))
        {
            position.Account = tradeAccount;
            if (GlobalData.ExchangeListId.TryGetValue(position.ExchangeId, out Model.CryptoExchange? exchange))
            {
                position.Exchange = exchange;
                if (exchange.SymbolListId.TryGetValue(position.SymbolId, out CryptoSymbol? symbol))
                {
                    position.Symbol = symbol;
                    if (GlobalData.IntervalListId.TryGetValue((int)position.IntervalId!, out CryptoInterval? interval))
                        position.Interval = interval!;

                    GlobalData.PositionsClosed.Add(position);
                }
            }
        }
    }


    static public void RemovePosition(CryptoAccount tradeAccount, CryptoPosition position, bool addToClosed)
    {
        if (tradeAccount.Data.PositionList.TryGetValue(position.Symbol.Name, out CryptoPosition? positionFound))
        {
            tradeAccount.Data.PositionList.Remove(positionFound.Symbol.Name);

            if (addToClosed)
            {
                if (GlobalData.PositionsClosed.Count != 0)
                    GlobalData.PositionsClosed.Add(position);
                else
                    GlobalData.PositionsClosed.Insert(0, position);
            }
        }
    }


    public static void LoadPosition(CryptoDatabase database, CryptoPosition position)
    {
        // De parts
        string sql = string.Format("select * from positionpart where PositionId={0} order by Id", position.Id);
        foreach (CryptoPositionPart part in database.Connection.Query<CryptoPositionPart>(sql))
        {
            if (part.IntervalId.HasValue && GlobalData.IntervalListId.TryGetValue((int)position.IntervalId!, out CryptoInterval? interval))
               part.Interval = interval!;
            AddPositionPart(position, part);
        }

        // De steps
        sql = string.Format("select * from positionstep where PositionId={0} order by Id", position.Id);
        foreach (CryptoPositionStep step in database.Connection.Query<CryptoPositionStep>(sql))
        {
            if (position.PartList.TryGetValue(step.PositionPartId, out CryptoPositionPart? part))
                AddPositionPartStep(part, step);
        }
    }


    public static CryptoPosition? HasPosition(CryptoAccount tradeAccount, CryptoSymbol symbol)
    {
        if (tradeAccount.Data.PositionList.TryGetValue(symbol.Name, out CryptoPosition? position))
        {
            return position;
        }
        return null;
    }


    /// <summary>
    /// Is er een positie open (dan wel signalen maken voor deze munt)
    /// </summary>
    public static bool HasPosition(CryptoSymbol symbol)
    {
        foreach (CryptoAccount tradeAccount in GlobalData.TradeAccountList.Values.ToList())
        {
            if (tradeAccount.Data.PositionList.TryGetValue(symbol.Name, out var _))
                return true;
        }

        return false;
    }


    /// <summary>
    /// Zijn de aangevinkte intervallen UP?
    /// </summary>
    public static bool ValidTrendConditions(CryptoAccount tradeAccount, string symbolName, Dictionary<CryptoIntervalPeriod, CryptoTrendIndicator> trend, out string reaction)
    {
        foreach (KeyValuePair<CryptoIntervalPeriod, CryptoTrendIndicator> entry in trend)
        {
            AccountSymbolIntervalData accountSymbolIntervalData = tradeAccount.Data.GetSymbolTrendData(symbolName, entry.Key);
            if (accountSymbolIntervalData.TrendIndicator != entry.Value)
            {
                reaction = $"trend op de {accountSymbolIntervalData.Interval.Name} niet gelijk aan {entry.Value}";
                return false;
            }
        }

        reaction = "";
        return true;
    }


    public static bool ValidMarketTrendConditions(CryptoAccount tradeAccount, CryptoSymbol symbol, List<(decimal minValue, decimal maxValue)> marketTrend, out string reaction)
    {
        if (marketTrend.Count != 0)
        {
            AccountSymbolData accountSymbolData = tradeAccount.Data.GetSymbolData(symbol.Name);
            if (!accountSymbolData.MarketTrendPercentage.HasValue)
            {
                reaction = $"Markettrend {symbol.Name} is niet berekend";
                return false;
            }

            foreach ((decimal minValue, decimal maxValue) in marketTrend)
            {
                decimal trendPercentage = (decimal)accountSymbolData.MarketTrendPercentage;
                if (!trendPercentage.IsBetween(minValue, maxValue))
                {
                    string minValueStr = minValue.ToString0("N2");
                    if (minValue == decimal.MinValue)
                        minValueStr = "-maxint";
                    string maxValueStr = maxValue.ToString0("N2");
                    if (maxValue == decimal.MaxValue)
                        maxValueStr = "+maxint";
                    reaction = $"Markettrend {symbol.Name} {accountSymbolData.MarketTrendPercentage?.ToString("N2")} niet tussen {minValueStr} en {maxValueStr}";
                    return false;
                }
            }
        }

        reaction = "";
        return true;
    }

}