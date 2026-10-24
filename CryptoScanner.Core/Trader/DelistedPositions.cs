using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Trader;

/// <summary>
/// Closes an open position whose symbol the exchange no longer trades (open point 120).
/// <para>
/// The hourly symbol refresh puts a delisted or settled symbol on status 0, which takes it out of
/// the subscriptions, the candle fetch and PositionMonitor.NewCandleArrivedAsync. A position on it
/// froze: still Trading, its orders open, its stake locked in the paper assets, and even the manual
/// exit could not help because a paper order only fills on a new candle. STORJUSDT on Binance
/// Perpetual was settled on 26-08-2026 09:00 UTC; any position on it would have stayed open forever.
/// </para>
/// <para>
/// The close takes the door of the manual exit (<see cref="ManualExit"/>): ExitRequested aims the
/// take profit one tick through the last price, or cancels a waiting entry. The fill then runs on a
/// flat candle at the last known price, the closest the scanner has to the settlement price. Two
/// candles, because an order is stamped with the close of the candle it was placed on and may only
/// fill from the next one on (PaperTrading.CheckStepAgainstCandleLocked).
/// </para>
/// <para>
/// Only in our own administration. Nothing goes to Altrady (<see cref="CryptoPosition.Delisted"/>
/// stops the close ThreadCheckFinishedPosition would send): the exchange settled the position there
/// already. With real trading nothing is closed either, an order on a settled market would only be
/// refused; the user gets a message to check the exchange.
/// </para>
/// </summary>
public static class DelistedPositions
{
    /// <summary>
    /// Whether the scanner closes this position itself: still open, its symbol on status 0, and a
    /// trading mode in which the position lives in the paper administration.
    /// </summary>
    public static bool ShouldClose(CryptoPosition position)
    {
        if (GlobalData.IsEmulatorMode)
            return false;
        if (position.CloseTime.HasValue)
            return false;
        if (position.Symbol is null || position.Symbol.Status != 0)
            return false;
        return GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.PaperTrade
            || GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.PaperTradingAndAltrady;
    }


    /// <summary>
    /// The price the position is closed at: the last price the scanner saw, or the close of the
    /// newest 1m candle when there is no last price. Null when there is neither.
    /// </summary>
    public static decimal? ClosingPrice(CryptoSymbol symbol)
    {
        if (symbol.LastPrice.HasValue && symbol.LastPrice.Value > 0)
            return symbol.LastPrice.Value;

        var symbolPeriod = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
        if (symbolPeriod.CandleList.TryGetLastCandle(out CryptoCandle candle) && candle.Close > 0)
            return candle.Close;
        return null;
    }


    /// <summary>
    /// The closing price for a position whose symbol has nothing in memory: after a restart a
    /// status 0 symbol gets no candles loaded and no last price. First the newest stored 1h candle
    /// of the last 90 days, then the price of the newest fill of the position itself.
    /// </summary>
    private static (decimal? Price, string Source) FallbackClosingPrice(CryptoPosition position)
    {
        CryptoSymbol symbol = position.Symbol;
        try
        {
            if (GlobalData.IntervalListPeriod.TryGetValue(CryptoIntervalPeriod.interval1h, out CryptoInterval? interval))
            {
                CandleTime to = CandleTime.AlignFromDateTime(GlobalData.Clock.UtcNow, interval.Duration);
                CandleTime from = to - (uint)(90 * 24 * 60);
                using var db = new CandleDatabase(symbol.Exchange);
                db.Open();
                List<CryptoCandle> stored = CandleDatabase.LoadCandlesInRange(db.Connection, symbol, interval, from.Minutes, to.Minutes);
                if (stored.Count > 0 && stored[^1].Close > 0)
                    return (stored[^1].Close, "the newest stored candle");
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, $"DelistedPositions {symbol.Name} reading the candle database");
        }

        CryptoTrade? newest = position.TradeList.Values.OrderBy(t => t.TradeTime).LastOrDefault();
        if (newest != null && newest.Price > 0)
            return (newest.Price, "the price of the newest fill of the position");
        return (null, "");
    }


    /// <summary>
    /// A 1m candle around the given price, not added to any candle list. It reaches one tick past
    /// the price on both sides: a paper limit order only fills when the price goes THROUGH it, and a
    /// target that sat exactly on the last price is left there by HandlePosition (it tolerates a
    /// difference of one tick), so a candle flat on the price would never fill it.
    /// </summary>
    public static CryptoCandle FlatCandle(CryptoSymbol symbol, CandleTime openTime, decimal price)
    {
        decimal tick = symbol.PriceTickSize > 0 ? symbol.PriceTickSize : 0m;
        return new CryptoCandle
        {
            TickDecimals = symbol.PriceDecimals,
            OpenTime = openTime,
            Open = price,
            High = price + tick,
            Low = price - tick > 0 ? price - tick : price,
            Close = price,
            Volume = 0,
        };
    }


    /// <summary>
    /// Walk the open positions of the active exchange and close the ones on a delisted symbol.
    /// Called by the hourly refresh right after the symbols were read again.
    /// </summary>
    public static async Task CloseAsync()
    {
        if (GlobalData.IsEmulatorMode)
            return;
        Model.CryptoExchange? exchange = GlobalData.ActiveExchange;
        if (exchange is null)
            return;

        foreach (CryptoPosition position in exchange.Data.PositionList.Values.ToList())
        {
            if (position.Symbol is null || position.Symbol.Status != 0 || position.CloseTime.HasValue)
                continue;

            if (GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.RealTrading)
            {
                GlobalData.AddTextToLogTab($"{position.Symbol.Name} {position.SideText} position {position.Id} is on a symbol the exchange no longer trades (delisted), check the position at the exchange");
                continue;
            }

            if (!ShouldClose(position))
                continue;

            try
            {
                await CloseAsync(exchange, position);
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, $"DelistedPositions {position.Symbol.Name}");
                GlobalData.AddErrorToLogTab($"{position.Symbol.Name} closing the delisted position {position.Id} failed: {error.Message}");
            }
        }
    }


    private static async Task CloseAsync(Model.CryptoExchange exchange, CryptoPosition position)
    {
        CryptoSymbol symbol = position.Symbol;

        // Already handled in an earlier round and still open: say so instead of repeating the close
        // message every hour
        bool firstAttempt = !position.Delisted;

        decimal? price = ClosingPrice(symbol);
        string source = "the last price";
        if (!price.HasValue)
            (price, source) = FallbackClosingPrice(position);
        if (!price.HasValue)
        {
            GlobalData.AddTextToLogTab($"{symbol.Name} {position.SideText} position {position.Id} is on a delisted symbol but there is no price to close it at");
            return;
        }

        position.Delisted = true;
        position.ExitRequested = true;
        position.ForceCheckPosition = true;
        if (firstAttempt)
            GlobalData.AddTextToLogTab($"{symbol.Name} {position.SideText} position {position.Id} is closed because the symbol was delisted, at {source} {price.Value.ToString(symbol.PriceDisplayFormat)}");
        else
            GlobalData.AddTextToLogTab($"{symbol.Name} {position.SideText} position {position.Id} on a delisted symbol is still open, trying the close again at {source} {price.Value.ToString(symbol.PriceDisplayFormat)}");

        // Two closed minutes: the first places the exit order (stamped with its close), the second
        // fills it. A fill is stamped with the close of its candle, so filling on the minute that is
        // still running would put the close time up to a minute in the future.
        CandleTime current = CandleTime.AlignFromDateTime(GlobalData.Clock.UtcNow, 1);
        CryptoCandle placeCandle = FlatCandle(symbol, current - 2u, price.Value);
        CryptoCandle fillCandle = FlatCandle(symbol, current - 1u, price.Value);

        // The same lock ThreadCheckFinishedPosition takes, so the hourly position check cannot run
        // HandlePosition on this position at the same time (that is how parts get doubled)
        await position.ProcessPositionSemaphore.WaitAsync();
        try
        {
            using (PositionMonitor positionMonitor = new(symbol, placeCandle))
                await positionMonitor.CheckThePosition(position);

            // A waiting entry was cancelled above and the position is gone already
            if (position.CloseTime.HasValue || !PositionTools.IsListed(exchange, position))
                return;

            using PositionMonitor fillMonitor = new(symbol, fillCandle);
            await PaperTrading.PaperTradingCheckOrders(fillMonitor.Database, exchange, symbol, fillCandle);
        }
        finally
        {
            position.ProcessPositionSemaphore.Release();
        }
    }
}
