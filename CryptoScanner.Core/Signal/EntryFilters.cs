using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

using System.Collections.Concurrent;

namespace CryptoScanner.Core.Signal;

/// <summary>
/// The entry filters that came out of the market-regime study of 13-16-10-2026 (checklist A27-A36):
/// candidates that were measured on the positions of existing runs and never had a setting, so a
/// queue run could not decide them. All off by default; each one is a condition per side.
/// <list type="bullet">
/// <item>BTC move: only step in while the change of BTC over the last N hours lies in a band.</item>
/// <item>Weekdays: only step in on the ticked days (UTC).</item>
/// <item>Market RSI cross: only step in when the market RSI just came out of oversold (long) or
/// overbought (short) within the last N hours.</item>
/// <item>Higher interval: the coin's own RSI 14 on a higher interval in a band, and/or its price
/// above or below that interval's EMA200.</item>
/// <item>Entries per hour (trader): at most N new positions per side within an hour.</item>
/// </list>
/// Every measurement uses candles that had closed at T-1, the "previous minute" convention of the
/// barometer, the market RSI and the market state.
/// </summary>
public static class EntryFilters
{
    public const int RsiLength = 14;
    public const int EmaLength = 200;

    private static readonly ConcurrentDictionary<CryptoTradeSide, List<DateTime>> RecentEntries = new();


    /// <summary>Forget the remembered entries. The emulator calls this at the start of each run.</summary>
    public static void Reset() => RecentEntries.Clear();


    // ─── BTC move ───────────────────────────────────────────────────────────────

    /// <summary>
    /// The % change of BTC of the quote from <paramref name="hours"/> whole hours back to the minute
    /// before <paramref name="momentTime"/>. Null when BTC or its candles are missing.
    /// </summary>
    public static double? BtcMove(CryptoQuoteData quoteData, CandleTime momentTime, int hours)
    {
        var exchange = GlobalData.ActiveExchange;
        if (hours < 1 || exchange == null || !exchange.TryGetSymbolByPair("BTC" + quoteData.Name, out CryptoSymbol? btc))
            return null;

        // The price now: the close of the 1m candle that ended at T-1, else the last whole hour
        CandleTime minuteOpen = momentTime - 2u;
        CandleTime hourOpen = MarketMove.LastHourOpen(momentTime);
        CryptoCandleList hourCandles = btc.GetSymbolInterval(CryptoIntervalPeriod.interval1h).CandleList;
        double now;
        if (btc.GetSymbolInterval(CryptoIntervalPeriod.interval1m).CandleList.TryGetValue(minuteOpen, out CryptoCandle minute))
            now = (double)minute.Close;
        else if (hourCandles.TryGetValue(hourOpen, out CryptoCandle lastHour))
            now = (double)lastHour.Close;
        else
            return null;

        if (!hourCandles.TryGetValue(hourOpen - (uint)(60 * hours), out CryptoCandle then) || then.Close <= 0)
            return null;
        return (now / (double)then.Close - 1.0) * 100.0;
    }


    // ─── Weekdays ───────────────────────────────────────────────────────────────

    /// <summary>The UTC day of the week of the moment, 0 = Sunday .. 6 = Saturday (DayOfWeek).</summary>
    public static int Weekday(CandleTime momentTime) => (int)(momentTime - 1u).ToDateTime().DayOfWeek;


    // ─── Market RSI cross ───────────────────────────────────────────────────────

    /// <summary>
    /// Did the market RSI come out of the zone within the last <paramref name="hours"/> whole hours?
    /// Long: one of those hours was below <paramref name="level"/> and the newest is at or above it.
    /// Short: one was above and the newest is at or below. Null when the market RSI is missing.
    /// </summary>
    public static bool? MarketRsiCrossed(CryptoQuoteData quoteData, CandleTime momentTime, CryptoTradeSide side, decimal level, int hours)
    {
        CandleTime newest = MarketMove.LastHourOpen(momentTime);
        double? now = MarketRsi.HourValue(quoteData, newest);
        if (!now.HasValue)
            return null;
        double limit = (double)level;
        bool outside = side == CryptoTradeSide.Long ? now.Value >= limit : now.Value <= limit;
        if (!outside)
            return false;
        for (int h = 1; h <= Math.Max(1, hours); h++)
        {
            double? before = MarketRsi.HourValue(quoteData, newest - (uint)(60 * h));
            if (before.HasValue && (side == CryptoTradeSide.Long ? before.Value < limit : before.Value > limit))
                return true;
        }
        return false;
    }


    // ─── Higher interval ────────────────────────────────────────────────────────

    /// <summary>
    /// The coin's RSI 14 (Wilder) and its close against the EMA200 on <paramref name="interval"/>,
    /// from the candles of that interval that had closed at T-1. Null when there are too few candles
    /// (the EMA200 part alone needs 200).
    /// </summary>
    public static (double? Rsi, bool? AboveEma200) HigherInterval(CryptoSymbol symbol, CryptoIntervalPeriod interval, CandleTime momentTime)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval);
        uint duration = symbolInterval.Interval.Duration;
        CandleTime lastOpen = (momentTime - 1u).AlignToIntervalMinutes(duration) - duration;
        List<double> closes = symbolInterval.CandleList.GetLastValues(lastOpen, 500, duration).Select(c => (double)c.Close).ToList();

        double? rsi = Rsi(closes, RsiLength);
        bool? above = null;
        if (closes.Count >= EmaLength)
        {
            double[] ema = MarketState.Ema(closes, EmaLength);
            above = closes[^1] > ema[^1];
        }
        return (rsi, above);
    }


    /// <summary>The standard RSI (Wilder smoothing) of the last value, null with too few values.</summary>
    internal static double? Rsi(IReadOnlyList<double> closes, int length)
    {
        if (closes.Count < length + 1)
            return null;
        double gain = 0, loss = 0;
        for (int i = 1; i <= length; i++)
        {
            double change = closes[i] - closes[i - 1];
            gain += Math.Max(change, 0);
            loss += Math.Max(-change, 0);
        }
        gain /= length;
        loss /= length;
        for (int i = length + 1; i < closes.Count; i++)
        {
            double change = closes[i] - closes[i - 1];
            gain = (gain * (length - 1) + Math.Max(change, 0)) / length;
            loss = (loss * (length - 1) + Math.Max(-change, 0)) / length;
        }
        return loss == 0 ? 100.0 : 100.0 - 100.0 / (1.0 + gain / loss);
    }


    // ─── Entries per hour ───────────────────────────────────────────────────────

    /// <summary>Remember that a position was opened on this side at this moment.</summary>
    public static void RecordEntry(CryptoTradeSide side, DateTime moment)
    {
        List<DateTime> list = RecentEntries.GetOrAdd(side, _ => []);
        lock (list)
        {
            list.Add(moment);
            list.RemoveAll(t => t <= moment.AddHours(-1));
        }
    }


    /// <summary>How many positions were opened on this side in the hour before <paramref name="moment"/>.</summary>
    public static int EntriesInLastHour(CryptoTradeSide side, DateTime moment)
    {
        if (!RecentEntries.TryGetValue(side, out List<DateTime>? list))
            return 0;
        lock (list)
        {
            return list.Count(t => t > moment.AddHours(-1) && t <= moment);
        }
    }


    // ─── The checks, per side setting ───────────────────────────────────────────

    /// <summary>
    /// All signal-side entry filters of this file in one call: true when the signal may stay. A
    /// missing measurement never blocks, it is only named in the reaction - the same rule as the
    /// market RSI and the market state.
    /// </summary>
    public static bool Valid(CryptoSymbol symbol, CryptoTradeSide side, CandleTime momentTime, SettingsCompiled settings, out string reaction)
    {
        reaction = "";
        CryptoQuoteData? quoteData = symbol.QuoteData;

        if (settings.WeekdaysActive && !settings.WeekdaysAllowed.Contains(Weekday(momentTime)))
        {
            reaction = $"not on {(DayOfWeek)Weekday(momentTime)}";
            return false;
        }

        if (settings.BtcMoveActive && quoteData != null)
        {
            double? move = BtcMove(quoteData, momentTime, settings.BtcMoveHours);
            if (move.HasValue && !((decimal)move.Value).IsBetween(settings.BtcMoveMinimum, settings.BtcMoveMaximum))
            {
                reaction = $"BTC moved {move.Value:N2}% in {settings.BtcMoveHours}h, outside {settings.BtcMoveMinimum.ToString0("N2")} .. {settings.BtcMoveMaximum.ToString0("N2")}";
                return false;
            }
        }

        if (settings.MarketRsiCrossActive && quoteData != null)
        {
            bool? crossed = MarketRsiCrossed(quoteData, momentTime, side, settings.MarketRsiCrossLevel, settings.MarketRsiCrossHours);
            if (crossed == false)
            {
                reaction = $"market RSI did not come out of {(side == CryptoTradeSide.Long ? "oversold" : "overbought")} ({settings.MarketRsiCrossLevel.ToString0("N0")}) in the last {settings.MarketRsiCrossHours}h";
                return false;
            }
        }

        if (settings.HigherIntervalActive)
        {
            (double? rsi, bool? above) = HigherInterval(symbol, settings.HigherIntervalPeriod, momentTime);
            if (rsi.HasValue && !((decimal)rsi.Value).IsBetween(settings.HigherIntervalRsiMinimum, settings.HigherIntervalRsiMaximum))
            {
                reaction = $"RSI {rsi.Value:N1} on {settings.HigherIntervalName} outside {settings.HigherIntervalRsiMinimum.ToString0("N0")} .. {settings.HigherIntervalRsiMaximum.ToString0("N0")}";
                return false;
            }
            if (above.HasValue && settings.HigherIntervalEma200 != 0 && above.Value != (settings.HigherIntervalEma200 > 0))
            {
                reaction = $"price {(above.Value ? "above" : "below")} the EMA200 on {settings.HigherIntervalName}";
                return false;
            }
        }
        return true;
    }
}
