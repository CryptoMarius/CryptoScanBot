using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.Core.Signal;

/// <summary>
/// The "what if" bookkeeping of a signal: how far the price moved for and against the signal
/// price since the signal fired, and whether a virtual position taken at the signal price would
/// have reached its stop or one of the take-profit levels by now. It is a statistic for the signal
/// grid, nothing in the trader reads it. Both UIs call <see cref="Update"/> once a minute for every
/// signal still on the grid and persist the signal when it reports a change.
/// <para>
/// PriceMin/PriceMax hold the lowest low and highest high of the 1m candles since the signal
/// fired (the last price of the ticker when the symbol has no 1m candles); PriceMinPerc and
/// PriceMaxPerc are those extremes as a percentage of the signal price, 0..100 scale, negative
/// below the signal price.
/// </para>
/// <para>
/// SignalStatus starts at Run and only ever moves forward: Lost when the stop price was reached
/// before any take-profit level, otherwise the highest take-profit level reached so far (Tp1..Tp5).
/// Once a level is reached a later stop no longer counts: the trader would already have taken
/// (part of) the profit there. When both are reached within the same minute the stop wins, the
/// conservative reading of a whipsaw. The levels are the signal's own TpPercentage when the
/// strategy set one (a single level), otherwise the trader's TpList; the stop is SlPercentage,
/// otherwise the trader's StopLossPercentage. A percentage of 0 disables that check. Both are
/// placed from the signal price with the same log-mirrored placement as the trader
/// (<see cref="PricePlacement"/>), so a long and a short need the same move. The trader itself
/// measures its take-profit from the break-even price, which includes the fees, so a real position
/// reaches a level a fraction later than this statistic.
/// </para>
/// </summary>
public static class SignalStatistics
{
    // Tp1..Tp5 in CryptoSignalStatus; a TpList with more levels stops counting here.
    public const int MaxTpLevel = 5;

    /// <summary>
    /// Bring the statistics of a signal up to date with the latest 1m candle. Returns true when a
    /// value changed, so the caller knows the signal has to be saved.
    /// </summary>
    public static bool Update(CryptoSignal signal)
    {
        if (signal.SignalPrice <= 0)
            return false;
        if (!TryGetPriceRange(signal, out decimal low, out decimal high))
            return false;

        bool changed = UpdateExtremes(signal, low, high);

        CryptoSignalStatus status = CalculateStatus(signal);
        if (status != signal.SignalStatus)
        {
            signal.SignalStatus = status;
            changed = true;
        }
        return changed;
    }

    /// <summary>
    /// The same extremes for a position, fed by the trader with the 1m candle it is handling,
    /// for as long as the position is open (live and emulator alike). Measured from the signal
    /// price, like the signal, so the two grids show the same kind of number. Nothing is saved
    /// here: the values ride along with the next save of the position, at the latest when it
    /// closes. Only the extremes, a position has a real outcome of its own instead of a status.
    /// </summary>
    public static bool UpdatePosition(CryptoPosition position, decimal low, decimal high)
    {
        if (position.SignalPrice <= 0 || low <= 0 || high <= 0)
            return false;
        return UpdateExtremes(position, low, high);
    }

    private static bool UpdateExtremes(CryptoData2 data, decimal low, decimal high)
    {
        bool changed = false;

        // A signal or position from before the statistics were (re)introduced has 0 in both,
        // start from the current candle rather than keep 0 as the minimum forever.
        if (data.PriceMin == 0 || low < data.PriceMin)
        {
            data.PriceMin = low;
            data.PriceMinPerc = Percentage(data, low);
            changed = true;
        }
        if (high > data.PriceMax)
        {
            data.PriceMax = high;
            data.PriceMaxPerc = Percentage(data, high);
            changed = true;
        }
        return changed;
    }

    /// <summary>The grid text for a status: run, sl, tp1..tp5.</summary>
    public static string GetStatusText(CryptoSignalStatus status)
    {
        return status switch
        {
            CryptoSignalStatus.Run => "run",
            CryptoSignalStatus.Lost => "sl",
            CryptoSignalStatus.Tp1 => "tp1",
            CryptoSignalStatus.Tp2 => "tp2",
            CryptoSignalStatus.Tp3 => "tp3",
            CryptoSignalStatus.Tp4 => "tp4",
            CryptoSignalStatus.Tp5 => "tp5",
            _ => "",
        };
    }

    /// <summary>True for every take-profit level, false for Run and Lost.</summary>
    public static bool IsWin(CryptoSignalStatus status)
    {
        return status >= CryptoSignalStatus.Tp1;
    }

    private static float Percentage(CryptoData2 data, decimal price)
    {
        return (float)(100 * (price / data.SignalPrice - 1));
    }

    /// <summary>
    /// The low and high to measure against: the last 1m candle, but only when that candle opened
    /// at or after the close of the signal candle (a candle from before the signal fired says
    /// nothing about it). Without such a candle the ticker's last price stands in for both.
    /// </summary>
    private static bool TryGetPriceRange(CryptoSignal signal, out decimal low, out decimal high)
    {
        low = 0;
        high = 0;

        CryptoSymbolInterval symbolInterval = signal.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
        // LastCandle is kept by CryptoCandleList under its own lock, no need to walk the list.
        CryptoCandle candle = symbolInterval.CandleList.LastCandle;
        if (candle.OpenTime != 0 && candle.OpenTime.ToDateTime() >= signal.CloseDate)
        {
            low = candle.Low;
            high = candle.High;
            return low > 0 && high > 0;
        }

        if (signal.Symbol.LastPrice is decimal lastPrice && lastPrice > 0)
        {
            low = lastPrice;
            high = lastPrice;
            return true;
        }
        return false;
    }

    private static CryptoSignalStatus CalculateStatus(CryptoSignal signal)
    {
        CryptoSignalStatus status = signal.SignalStatus;
        if (status == CryptoSignalStatus.Lost)
            return status;

        bool isLong = signal.Side == CryptoTradeSide.Long;

        // The stop only counts while nothing was won yet
        if (status == CryptoSignalStatus.Run)
        {
            decimal slPercentage = signal.SlPercentage ?? GlobalData.Settings.Trading.StopLossPercentage;
            if (slPercentage > 0)
            {
                decimal stopPrice = PricePlacement.Adverse(signal.Side, signal.SignalPrice, slPercentage);
                bool stopReached = isLong ? signal.PriceMin <= stopPrice : signal.PriceMax >= stopPrice;
                if (stopReached)
                    return CryptoSignalStatus.Lost;
            }
        }

        // The highest take-profit level reached, counted from the first level upwards
        List<CryptoTpEntry> levels = GetTpLevels(signal);
        int reached = 0;
        for (int index = 0; index < levels.Count && index < MaxTpLevel; index++)
        {
            decimal tpPercentage = levels[index].Percentage;
            if (tpPercentage <= 0)
                break;
            decimal tpPrice = PricePlacement.Favorable(signal.Side, signal.SignalPrice, tpPercentage);
            bool levelReached = isLong ? signal.PriceMax >= tpPrice : signal.PriceMin <= tpPrice;
            if (!levelReached)
                break;
            reached = index + 1;
        }

        if (reached > 0)
        {
            CryptoSignalStatus tpStatus = (CryptoSignalStatus)((int)CryptoSignalStatus.Tp1 + reached - 1);
            if (tpStatus > status)
                return tpStatus;
        }
        return status;
    }

    /// <summary>
    /// Same choice as TradeTools.EffectiveTpList makes for a position: the signal's own single
    /// level when the strategy set one, otherwise the trader's grid.
    /// </summary>
    private static List<CryptoTpEntry> GetTpLevels(CryptoSignal signal)
    {
        if (signal.TpPercentage is decimal tpPercentage && tpPercentage > 0)
            return [new CryptoTpEntry { Factor = 100m, Percentage = tpPercentage }];
        return GlobalData.Settings.Trading.TpList;
    }
}
