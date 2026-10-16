using System.Globalization;

using CryptoScanner.Core.Const;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Barometer;

/// <summary>
/// The days of history behind the slow figures of the barometer graph.
/// <para>
/// The barometer writes a candle per minute and the graph used to show the last seven hours of them.
/// For the market trend, the market state and its parts and the market RSI that is a flat line: they
/// look back 72 hours or more, the market state is only calculated once per whole hour, and seven
/// hours of that hardly moves. The only reason to have their graph at all is to see the market turn,
/// and that takes days.
/// </para>
/// <para>
/// So the two symbols that hold them ($BMX and $BMS) keep <see cref="Constants.BarometerHistoryDays"/>
/// days instead of <see cref="Constants.BarometerGraphHours"/> hours, but only the candle of every
/// whole hour once it is older than those hours. That is 24 candles per day per interval list, next
/// to the minutes that were already kept. $BMP keeps its seven hours: nothing it holds is drawn over
/// days. The figures of $BMX that do follow the interval (movement, bitcoin against the market) ride
/// along in the hourly candles but are never read from them - their graph stays at seven hours.
/// </para>
/// <para>
/// Not in the emulator: a replay keeps its barometer series in its own candle store and has no use for
/// a graph, so it keeps the window it always had.
/// </para>
/// </summary>
public static class BarometerHourHistory
{
    /// <summary>One point per whole hour in the part older than the minute window.</summary>
    public const uint HourMinutes = 60;


    /// <summary>
    /// Whether this barometer symbol keeps the days of hourly history. False for every ordinary coin,
    /// for $BMP, and for everything in the emulator.
    /// </summary>
    public static bool KeepsHourHistory(CryptoSymbol symbol)
    {
        if (GlobalData.IsEmulatorMode)
            return false;
        return symbol.Base == Constants.SymbolNameBarometerExtra || symbol.Base == Constants.SymbolNameBarometerState;
    }


    /// <summary>Whether the graph of this figure shows days with a point per hour instead of hours
    /// with a point per minute. Exactly the figures that do not depend on the interval.</summary>
    public static bool DrawsDays(BarometerGraphValue value)
    {
        return !BarometerCandleFields.UsesInterval(value);
    }


    /// <summary>The oldest minute that is still kept per minute. Older candles are thinned to whole
    /// hours. The same boundary <see cref="CandleTools.GetCandleFetchStart"/> gives $BMP.</summary>
    public static CandleTime MinuteWindowStart(DateTime utcNow)
    {
        return CandleTime.AlignFromDateTime(utcNow, 1) - (uint)(Constants.BarometerGraphHours * 60);
    }


    /// <summary>The oldest minute of the hourly history.</summary>
    public static CandleTime HistoryStart(DateTime utcNow)
    {
        return CandleTime.AlignFromDateTime(utcNow, 1) - (uint)(Constants.BarometerHistoryDays * 24 * 60);
    }


    /// <summary>Thin the part of a candle list that is older than the minute window down to whole hours.</summary>
    public static int Thin(CryptoCandleList candles, DateTime utcNow)
    {
        return candles.ThinBefore(MinuteWindowStart(utcNow), HourMinutes);
    }


    /// <summary>
    /// The ranges of candles.db that are kept for a symbol with hourly history: the minute window as one
    /// range, and every whole hour before it as a range of a single minute. Anything else is a minute
    /// that was thinned away in memory, and keeping it on disk would load it back at the next start.
    /// </summary>
    public static IEnumerable<(CandleTime Start, CandleTime End)> GetKeepRanges(DateTime utcNow)
    {
        CandleTime minuteStart = MinuteWindowStart(utcNow);
        CandleTime hour = HistoryStart(utcNow).AlignToIntervalMinutes(HourMinutes);
        while (hour < minuteStart)
        {
            yield return (hour, hour);
            hour += HourMinutes;
        }
        yield return (minuteStart, CandleTime.AlignFromDateTime(utcNow, 1));
    }


    /// <summary>
    /// The candles the graph of <paramref name="value"/> draws, oldest first. A figure that follows
    /// the interval gets the minutes of the last <see cref="Constants.BarometerGraphHours"/> hours,
    /// exactly as before. A slow figure gets the whole hours of the last
    /// <see cref="Constants.BarometerHistoryDays"/> days, plus the newest candle so the line ends at
    /// the value the scanner works with right now.
    /// </summary>
    public static List<CryptoCandle> ReadGraphCandles(CryptoCandleList candles, BarometerGraphValue value)
    {
        if (!DrawsDays(value))
            return candles.GetLastNValues(Constants.BarometerGraphHours * 60, 1);

        if (!candles.TryGetLastCandle(out CryptoCandle last))
            return [];

        CandleTime lastHour = last.OpenTime.AlignToIntervalMinutes(HourMinutes);
        CandleTime firstHour = lastHour - (uint)((Constants.BarometerHistoryDays * 24 - 1) * HourMinutes);
        List<CryptoCandle> result = candles.GetRange(firstHour, lastHour, HourMinutes);
        if (last.OpenTime != lastHour)
            result.Add(last);
        return result;
    }


    /// <summary>
    /// The vertical lines of the graph and their labels, between <paramref name="firstUtc"/> and
    /// <paramref name="lastUtc"/> (both UTC). Every whole hour with the bare local hour as label for
    /// the graph of hours; every local midnight with the day for the graph of days. Shared by both
    /// dashboards so their axes cannot drift apart.
    /// </summary>
    public static List<(DateTime Utc, string Label)> GetAxisMarks(DateTime firstUtc, DateTime lastUtc, bool days)
    {
        List<(DateTime, string)> result = [];
        if (lastUtc <= firstUtc)
            return result;

        if (!days)
        {
            DateTime hour = new(lastUtc.Year, lastUtc.Month, lastUtc.Day, lastUtc.Hour, 0, 0, DateTimeKind.Utc);
            while (hour > firstUtc)
            {
                result.Add((hour, hour.ToLocalTime().Hour.ToString(CultureInfo.InvariantCulture)));
                hour = hour.AddHours(-1);
            }
        }
        else
        {
            // Local midnight, so the line sits where the reader's day changes. Walked in local time
            // and converted back, which also keeps a daylight saving switch on its own midnight.
            DateTime lastLocal = DateTime.SpecifyKind(lastUtc, DateTimeKind.Utc).ToLocalTime();
            DateTime midnight = lastLocal.Date;
            while (true)
            {
                DateTime midnightUtc = DateTime.SpecifyKind(midnight, DateTimeKind.Local).ToUniversalTime();
                if (midnightUtc <= firstUtc)
                    break;
                if (midnightUtc < lastUtc)
                    result.Add((midnightUtc, midnight.ToString("ddd d", CultureInfo.InvariantCulture)));
                midnight = midnight.AddDays(-1);
            }
        }
        return result;
    }
}
