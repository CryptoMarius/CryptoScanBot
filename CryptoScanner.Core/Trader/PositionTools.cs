using CryptoScanner.Core.Context;
using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Messages;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Trader;

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

    public static CryptoPositionStep? FindOpenStep(CryptoPosition position,
        CryptoOrderSide side, CryptoPartPurpose purpose)
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

    public static CryptoPosition CreatePosition(CryptoSymbol symbol, string? strategyName, CryptoTradeSide side,
        string? eventText, CryptoSymbolInterval symbolInterval, DateTime currentDate)
    {
        CryptoPosition position = new()
        {
            CreateTime = currentDate,
            UpdateTime = currentDate,
            Symbol = symbol,
            SymbolId = symbol.Id,
            Exchange = symbol.Exchange,
            ExchangeId = symbol.ExchangeId,
            Interval = symbolInterval.Interval,
            IntervalId = symbolInterval.Interval.Id,
            Status = CryptoPositionStatus.Waiting,
            Strategy = strategyName ?? "",   // the column is NOT NULL
            ActiveDca = false,
            EventText = eventText,
            PartCount = 0,
            Side = side,
            EmulatorRunId = GlobalData.CurrentEmulatorRunId,
        };
        return position;
    }

    public static void AddSignalProperties(CryptoPosition position, CryptoSignal signal)
    {
        position.SignalId = signal.Id;
        position.SignalEventTime = signal.CloseDate;
        position.AssignValues(signal); // Copy common indicator values

        // Reset the statistics (AssignValues copied those of the signal, the position starts its own)
        position.PriceMinPerc = 0;
        position.PriceMaxPerc = 0;
        position.PriceMin = signal.SignalPrice;
        position.PriceMax = signal.SignalPrice;

        // Forward any per-signal SL override to the position (persisted; see CryptoPosition).
        position.SlPercentage = signal.SlPercentage;
        // Forward any per-signal TP override to the position (single TP at this distance).
        position.TpPercentage = signal.TpPercentage;
        // Forward any per-signal DCA ladder to the position (placed instead of Settings.Trading.DcaList).
        position.DcaLevels = signal.DcaLevels;
    }

    /// <summary>
    /// The 1-based sequence number a new part of the given Purpose would get if created right now -
    /// "Entry 1", "Dca 1/2/3...", "TP 1/2/3...". PartList is in creation order (sorted by Id), so
    /// counting existing same-purpose parts reproduces the DcaList/TpList configuration order.
    /// TradeTools.CalculateProfitAndBreakEvenPrice applies the same rule when it renumbers parts.
    /// </summary>
    public static int NextPartNumber(CryptoPosition position, CryptoPartPurpose purpose)
    {
        return position.PartList.Values.Count(p => p.Purpose == purpose) + 1;
    }

    public static CryptoPositionPart ExtendPosition(CryptoDatabase database,
        CryptoPosition position, CryptoPartPurpose purpose, CryptoInterval interval,
        string? strategyName, //CryptoEntryOrDcaStrategy stepInMethod,
        decimal signalPrice, DateTime currentDate, bool manualOrder = false)
    {
        CryptoPositionPart part = new()
        {
            Position = position,
            Purpose = purpose,
            PartNumber = NextPartNumber(position, purpose),
            Strategy = strategyName ?? "",   // the column is NOT NULL
            Interval = interval,
            IntervalId = interval.Id,
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

        GlobalData.AddTextToLogTab($"{position.Symbol.Name} {purpose} placing {signalPrice.ToString0(position.Symbol.PriceDisplayFormat)}");
        return part;
    }


    public static CryptoPositionStep CreatePositionStep(CryptoPosition position, CryptoPositionPart part,
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

    /// <summary>
    /// Loads all open positions from the database into the in-memory PositionList at startup.
    /// Must be called before CheckOpenPositions() so the trading engine can manage them
    /// without depending on the UI positions tab being opened first.
    /// </summary>
    public static void LoadOpenPositionsFromDatabase(CryptoDatabase database)
    {
        string sql = "select * from position where exchangeid=@exchangeid and closetime is null and status < 2";
        foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql, new { exchangeid = GlobalData.ActiveExchange!.Id }))
        {
            // Skip positions already in memory (e.g. loaded by the UI tab before this call). By id:
            // a coin can hold several positions (Trading.MaxPositionsPerCoin).
            if (GlobalData.ActiveExchange.Data.PositionList.Values.Any(p => p.Id == position.Id))
                continue;

            AddPosition(position);
            if (position.Symbol != null)
                LoadPosition(database, position);
        }
    }


    /// <summary>
    /// Register the position in the position list of its exchange and return the instance that is
    /// in that list afterwards. A position for the same symbol that was already in memory keeps its
    /// place, so a position just read from the database is then a detached copy and the live one is
    /// returned instead. Callers that keep a reference (the grids) must use what comes back: a
    /// detached copy stops following the trader and freezes on the values of that moment.
    /// </summary>
    public static CryptoPosition AddPosition(CryptoPosition position)
    {
        if (GlobalData.ExchangeListId.TryGetValue(position.ExchangeId, out Model.CryptoExchange? exchange))
        {
            position.Exchange = exchange;
            if (exchange.SymbolListId.TryGetValue(position.SymbolId, out CryptoSymbol? symbol))
            {
                position.Symbol = symbol;
                if (GlobalData.IntervalListId.TryGetValue((int)position.IntervalId!, out CryptoInterval? interval))
                    position.Interval = interval;

                return exchange.Data.PositionList.GetOrAdd(PositionListKey(position), position);
            }
        }
        return position;
    }


    /// <summary>
    /// The separator between the symbol name and the position id in the key of a parallel position.
    /// Not a character a symbol name uses (a product sits behind a dot, see CryptoProduct.Separator).
    /// </summary>
    public const char ParallelKeySeparator = '#';


    /// <summary>
    /// Whether this position belongs to a strategy that may hold several positions on one coin
    /// (StrategyRegistration.ParallelPositions).
    /// </summary>
    public static bool IsParallel(CryptoPosition position)
        => RegisterAlgorithms.HasParallelPositions(position.Strategy);


    /// <summary>
    /// The key of the position in ExchangeData.PositionList: "symbol#id" for every position. Until
    /// 24-10-2026 a position of an ordinary strategy was filed under the bare symbol name, which left
    /// room for one per coin; since Trading.MaxPositionsPerCoin a coin can hold several, so nothing
    /// looks a position up by its key any more. Code that wants the positions of a coin asks
    /// <see cref="PositionsOf"/>, <see cref="OrdinaryPositionsOf"/> or <see cref="HasPosition(Model.CryptoExchange, CryptoSymbol)"/>.
    /// </summary>
    public static string PositionListKey(CryptoPosition position)
        => $"{position.Symbol.Name}{ParallelKeySeparator}{position.Id}";


    /// <summary>
    /// How many positions may be open on one coin (Trading.MaxPositionsPerCoin, never below 1).
    /// </summary>
    public static int MaxPositionsPerCoin => Math.Max(1, GlobalData.Settings.Trading.MaxPositionsPerCoin);


    /// <summary>
    /// The open positions of the ordinary strategies on this symbol (not those of a strategy with
    /// parallel positions), oldest first. With Trading.MaxPositionsPerCoin at 1 this is the one
    /// position the coin has, or nothing.
    /// </summary>
    public static List<CryptoPosition> OrdinaryPositionsOf(Model.CryptoExchange exchange, CryptoSymbol symbol)
    {
        List<CryptoPosition> list = [];
        foreach (var item in exchange.Data.PositionList)
        {
            if (ReferenceEquals(item.Value.Symbol, symbol) && !IsParallel(item.Value))
                list.Add(item.Value);
        }
        if (list.Count > 1)
            list.Sort((a, b) => a.Id.CompareTo(b.Id));
        return list;
    }


    /// <summary>
    /// Every open position on this symbol: those of the ordinary strategies first, then the parallel
    /// ones, both in the order of their id. Empty when there is none.
    /// </summary>
    public static List<CryptoPosition> PositionsOf(Model.CryptoExchange exchange, CryptoSymbol symbol)
    {
        List<CryptoPosition> list = OrdinaryPositionsOf(exchange, symbol);
        List<CryptoPosition>? parallel = null;
        foreach (var item in exchange.Data.PositionList)
        {
            if (ReferenceEquals(item.Value.Symbol, symbol) && IsParallel(item.Value))
                (parallel ??= []).Add(item.Value);
        }
        if (parallel != null)
            list.AddRange(parallel.OrderBy(p => p.Id));
        return list;
    }


    /// <summary>
    /// The open parallel positions of one strategy on this symbol (see <see cref="PositionsOf"/>).
    /// </summary>
    public static List<CryptoPosition> ParallelPositionsOf(Model.CryptoExchange exchange, CryptoSymbol symbol, string strategy)
        => [.. PositionsOf(exchange, symbol).Where(p => p.Strategy == strategy && IsParallel(p))];


    /// <summary>
    /// Whether any position is open on this symbol, an ordinary or a parallel one.
    /// </summary>
    public static bool HasAnyPosition(Model.CryptoExchange exchange, CryptoSymbol symbol)
    {
        foreach (var item in exchange.Data.PositionList)
        {
            if (ReferenceEquals(item.Value.Symbol, symbol))
                return true;
        }
        return false;
    }


    /// <summary>
    /// Whether this very position (not just one on the same symbol) is still in the position list.
    /// </summary>
    public static bool IsListed(Model.CryptoExchange exchange, CryptoPosition position)
        => ListedKey(exchange, position) != null;


    /// <summary>
    /// The key this very position is listed under, or null. Searched by reference rather than
    /// computed, so a position that was put in the list some other way (a test) is found as well.
    /// </summary>
    public static string? ListedKey(Model.CryptoExchange exchange, CryptoPosition position)
    {
        if (exchange.Data.PositionList.TryGetValue(PositionListKey(position), out CryptoPosition? listed)
            && ReferenceEquals(listed, position))
            return PositionListKey(position);
        foreach (var item in exchange.Data.PositionList)
        {
            if (ReferenceEquals(item.Value, position))
                return item.Key;
        }
        return null;
    }


    /// <summary>
    /// The open position an order belongs to. The position holding the order id answers first.
    /// Without one, a coin with a single ordinary position answers with that position, exactly as
    /// before Trading.MaxPositionsPerCoin; with several, only the order id can tell them apart.
    /// </summary>
    public static CryptoPosition? PositionOfOrder(Model.CryptoExchange exchange, CryptoSymbol symbol, string? orderId)
    {
        if (!string.IsNullOrEmpty(orderId))
        {
            foreach (var item in exchange.Data.PositionList)
            {
                if (ReferenceEquals(item.Value.Symbol, symbol) && item.Value.StepOrderList.ContainsKey(orderId))
                    return item.Value;
            }
        }
        List<CryptoPosition> ordinary = OrdinaryPositionsOf(exchange, symbol);
        return ordinary.Count == 1 ? ordinary[0] : null;
    }


    public static void AddPositionPart(CryptoPosition position, CryptoPositionPart part)
    {
        position.PartList.TryAdd(part.Id, part);
        part.Position = position; // parent
        part.Exchange = position.Exchange;
        part.Symbol = position.Symbol;
    }


    public static void AddPositionPartStep(CryptoPositionPart part, CryptoPositionStep step)
    {
        part.StepList.TryAdd(step.Id, step);

        // OrderId index aanvullen
        if (step.OrderId != null && step.OrderId != "")
            part.Position.StepOrderList.TryAdd(step.OrderId, step);
        if (step.Order2Id != null && step.Order2Id != "")
            part.Position.StepOrderList.TryAdd(step.Order2Id, step);
    }


    public static void RemovePosition(Model.CryptoExchange activeExchange, CryptoPosition position, bool addToClosed)
    {
        string? key = ListedKey(activeExchange, position);
        if (key != null)
        {
            position.Symbol.ClearSignals();
            activeExchange.Data.PositionList.TryRemove(key, out _);

            if (addToClosed)
            {
                // Send the position to the closed positions ViewModel
                GlobalData.SendMvvmMessage(new PositionIsClosedMessage(position));
                GlobalData.PositionClosed?.Invoke(position);
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


    /// <summary>
    /// Remove one position and everything that hangs off it from the database.
    ///
    /// <para>
    /// The orders and the trades go FIRST. They are only reachable through PositionStep.OrderId and
    /// Order2Id, so once the steps are gone there is no way left to find them - which is exactly what
    /// used to happen: deleting a position left its orders and trades behind for good and those two
    /// tables only ever grew. The emulator has cleaned up after itself this way per run for a while
    /// (EmulatorDb.DeleteRuns); this is the same recipe for one position.
    /// </para>
    /// </summary>
    public static void DeleteFromDatabase(CryptoDatabase database, CryptoPosition position)
    {
        using var transaction = database.BeginTransaction();

        // Order2Id is the second order of a step (a stop order and the limit order behind it), and it
        // is null on most steps - hence the union rather than one list.
        const string orderIds =
            "select OrderId from PositionStep where PositionId = @id " +
            "union select Order2Id from PositionStep where Order2Id is not null and PositionId = @id";

        database.Connection.Execute($"delete from [Trade] where OrderId in ({orderIds})", new { id = position.Id }, transaction);
        database.Connection.Execute($"delete from [Order] where OrderId in ({orderIds})", new { id = position.Id }, transaction);
        database.Connection.Execute("delete from PositionStep where PositionId = @id", new { id = position.Id }, transaction);
        database.Connection.Execute("delete from PositionPart where PositionId = @id", new { id = position.Id }, transaction);
        database.Connection.Execute("delete from Position where Id = @id", new { id = position.Id }, transaction);

        transaction.Commit();
    }


    /// <summary>
    /// Remove every position and everything that hangs off it from the database.
    ///
    /// <para>
    /// The orders and trades are emptied outright instead of being looked up per position: paper
    /// trading is the only thing that writes them (see <see cref="PaperTrading"/>), so with every
    /// position gone there is nothing left that could still belong to one. That also clears whatever
    /// earlier deletes orphaned, back when the two tables were not cleaned up at all.
    /// </para>
    /// </summary>
    public static void DeleteAllFromDatabase(CryptoDatabase database)
    {
        using var transaction = database.BeginTransaction();

        database.Connection.Execute("delete from [Trade]", transaction);
        database.Connection.Execute("delete from [Order]", transaction);
        database.Connection.Execute("delete from PositionStep", transaction);
        database.Connection.Execute("delete from PositionPart", transaction);
        database.Connection.Execute("delete from Position", transaction);

        transaction.Commit();
    }


    /// <summary>
    /// The oldest open position of an ordinary strategy on this symbol (see <see cref="OrdinaryPositionsOf"/>),
    /// or null. With Trading.MaxPositionsPerCoin at 1 the only one there is.
    /// </summary>
    public static CryptoPosition? HasPosition(Model.CryptoExchange activeExchange, CryptoSymbol symbol)
    {
        List<CryptoPosition> positions = OrdinaryPositionsOf(activeExchange, symbol);
        return positions.Count > 0 ? positions[0] : null;
    }


    /// <summary>
    /// Is er een positie open (dan wel signalen maken voor deze munt)
    /// </summary>
    public static bool HasPosition(CryptoSymbol symbol)
        => HasPosition(GlobalData.ActiveExchange!, symbol) != null;


    /// <summary>
    /// Zijn de aangevinkte intervallen UP?
    /// </summary>
    public static bool ValidTrendConditions(CryptoSymbol symbol, CryptoInterval intervalBase, TrendType trendType,
        Dictionary<CryptoIntervalPeriod, CryptoTrendIndicator> trend, out string reaction)
    {
        CryptoTrendData symbolTrend = trendType == TrendType.Primary ? symbol.Data.TrendPrimary : symbol.Data.TrendSecondary;

        foreach (KeyValuePair<CryptoIntervalPeriod, CryptoTrendIndicator> entry in trend)
        {
            var interval = GlobalData.IntervalListPeriod[entry.Key];
            if (interval.IntervalPeriod >= intervalBase.IntervalPeriod)
            {
                CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(entry.Key);
                CryptoTrendData intervalTrend = trendType == TrendType.Primary ? symbolInterval.TrendPrimary : symbolInterval.TrendSecondary;

                if (intervalTrend.Trend != entry.Value)
                {
                    reaction = $"trend op de {interval.Name} niet gelijk aan {entry.Value}";
                    return false;
                }
            }
        }

        reaction = "";
        return true;
    }


    public static bool ValidSymbolTrendConditions(CryptoSymbol symbol, TrendType trendType,
        List<(decimal minValue, decimal maxValue)> marketTrend, out string reaction)
    {
        if (marketTrend.Count != 0)
        {
            CryptoTrendData symbolTrend = trendType == TrendType.Primary ? symbol.Data.TrendPrimary : symbol.Data.TrendSecondary;
            string trendLabel = trendType == TrendType.Primary ? "Markettrend(P)" : "Markettrend(S)";

            if (!symbolTrend.Percentage.HasValue)
            {
                reaction = $"{trendLabel} {symbol.Name} is not calculated";
                return false;
            }

            foreach ((decimal minValue, decimal maxValue) in marketTrend)
            {
                decimal trendPercentage = (decimal)symbolTrend.Percentage;
                if (!trendPercentage.IsBetween(minValue, maxValue))
                {
                    string minValueStr = minValue.ToString0("N2");
                    if (minValue == decimal.MinValue)
                        minValueStr = "-maxint";
                    string maxValueStr = maxValue.ToString0("N2");
                    if (maxValue == decimal.MaxValue)
                        maxValueStr = "+maxint";
                    reaction = $"{trendLabel} {symbol.Name} {symbolTrend.Percentage?.ToString("N2")} not between {minValueStr} and {maxValueStr}";
                    return false;
                }
            }
        }

        reaction = "";
        return true;
    }

    /// <summary>
    /// Is the market RSI outside the band in which this side skips a signal? Always true when the
    /// filter is off, and also when there is no market RSI (too few coins with enough candles): a
    /// missing measurement does not stop trading, it is only said in the reaction.
    /// </summary>
    public static bool ValidMarketRsiConditions(CryptoSymbol symbol, CandleTime momentTime, SettingsCompiled settings,
        out string reaction)
    {
        reaction = "";
        if (!settings.MarketRsiActive || symbol.QuoteData == null)
            return true;

        double? marketRsi = MarketRsi.Calculate(symbol.QuoteData, momentTime, out int symbolCount);
        if (marketRsi == null)
        {
            reaction = $"Market RSI not available ({symbolCount} coins)";
            return true;
        }

        decimal value = (decimal)marketRsi.Value;
        if (value.IsBetween(settings.MarketRsiMinimum, settings.MarketRsiMaximum))
        {
            reaction = $"Market RSI {value:N1} between {settings.MarketRsiMinimum.ToString0("N1")} and {settings.MarketRsiMaximum.ToString0("N1")}";
            return false;
        }
        return true;
    }

    /// <summary>
    /// Does the market state of the quote lie in the band in which this side may step in? Always true
    /// when the filter is off, and also when there is no market state (too few coins, or too few BTC
    /// candles): a missing measurement does not stop trading, it is only said in the reaction.
    /// </summary>
    public static bool ValidMarketStateConditions(CryptoSymbol symbol, CandleTime momentTime, SettingsCompiled settings,
        out string reaction)
    {
        reaction = "";
        if (!settings.MarketStateActive || symbol.QuoteData == null)
            return true;

        MarketStateValue? value = MarketState.Calculate(symbol.QuoteData, momentTime, settings.MarketStateInterval);
        if (value == null)
        {
            reaction = "Market state not available";
            return true;
        }

        decimal score = (decimal)value.Score;
        if (!score.IsBetween(settings.MarketStateMinimum, settings.MarketStateMaximum))
        {
            reaction = $"Market state {score:N0} outside {settings.MarketStateMinimum.ToString0("N0")} .. {settings.MarketStateMaximum.ToString0("N0")}"
                + $" (BTC {value.BtcPart:N0}, {value.Breadth:N0}% above EMA50, market 72h {value.Market72h:N1}%)";
            return false;
        }
        return true;
    }

}
