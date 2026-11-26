using System.Collections.Concurrent;
using System.Globalization;

using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.ChochFvg.Signal;

/// <summary>
/// Change of character with a fair value gap. Fires on the candle that makes the change of character
/// (the first close beyond the last swing against the trend) when that leg has an untraded fair value
/// gap with its middle in the fib zone. The signal is a limit order on that middle
/// (StrategyRegistration.StandingEntry) with its own stop and take profit and no DCA; the rules are in
/// <see cref="ChochFvgRules"/>. <see cref="WithdrawEntry"/> takes the order off the book when it does
/// not fill in time or the price reaches the target without it.
/// </summary>
public class ChochFvgBase : SignalCreateBase
{
    private decimal? EntryPrice;
    public override decimal? OverrideSignalPrice => EntryPrice;

    private decimal? StopPercentage;
    public override decimal? OverrideSlPercentage => StopPercentage;

    private decimal? TargetPercentage;
    public override decimal? OverrideProfitPercentage => TargetPercentage;

    public override bool WithoutDca => true;

    // The target of the order that is waiting per coin and side, so WithdrawEntry knows when the price
    // went there without a fill. Memory only: after a restart only the expiry withdraws the order.
    private static readonly ConcurrentDictionary<(string Symbol, CryptoTradeSide Side), decimal> WaitingTargets = new();

    /// <summary>Clears the remembered targets, so a next emulator run starts empty.</summary>
    public static void Reset() => WaitingTargets.Clear();

    private static readonly TimeZoneInfo? NewYork = FindNewYork();

    private static TimeZoneInfo? FindNewYork()
    {
        // The IANA name works on every platform with ICU; the Windows name is the fallback
        if (TimeZoneInfo.TryFindSystemTimeZoneById("America/New_York", out TimeZoneInfo? zone))
            return zone;
        if (TimeZoneInfo.TryFindSystemTimeZoneById("Eastern Standard Time", out zone))
            return zone;
        return null;
    }


    /// <summary>Whether the UTC moment lies inside the window given as HH:mm in New York time.</summary>
    public static bool InSessionWindow(DateTime utc, string start, string end, out string reason)
    {
        reason = "";
        if (NewYork == null)
        {
            reason = "time zone New York not found";
            return false;
        }
        if (!TimeOnly.TryParseExact(start.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly from) ||
            !TimeOnly.TryParseExact(end.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly to))
        {
            reason = $"session window {start}-{end} is not HH:mm";
            return false;
        }

        TimeOnly local = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), NewYork));
        bool inside = from <= to
            ? local >= from && local < to
            : local >= from || local < to;
        if (!inside)
            reason = $"outside the session window ({local:HH:mm} New York)";
        return inside;
    }


    public override bool IsSignal()
    {
        ExtraText = "";
        EntryPrice = null;
        StopPercentage = null;
        TargetPercentage = null;

        ChochFvgSettings settings = ChochFvgPlugin.Settings;
        bool isLong = SignalSide == CryptoTradeSide.Long;
        CandleTime closeTime = CandleLast.Candle.OpenTime + Interval.Duration;

        // The candles up to and including the last one, oldest first. Enough history for the two
        // swing highs before the window; a missing candle ends the history there.
        int wanted = 3 * settings.LookbackCandles + 2 * settings.SwingStrength + 3;
        List<ChochFvgBar> bars = new(wanted);
        CandleTime time = CandleLast.Candle.OpenTime;
        for (int i = 0; i < wanted; i++)
        {
            if (!SymbolInterval.CandleList.TryGetValue(time, out CryptoCandle candle))
                break;
            bars.Add(new ChochFvgBar(candle.Open, candle.High, candle.Low, candle.Close));
            if (time < Interval.Duration)
                break;
            time -= Interval.Duration;
        }
        bars.Reverse();

        if (!ChochFvgRules.TryFindSetup(bars, isLong, settings.SwingStrength, settings.LookbackCandles,
            settings.FibZoneStart, settings.FibZoneEnd, settings.RiskReward, out ChochFvgSetup? setup, out string reason,
            settings.MinimumStopPercentage))
        {
            ExtraText = reason;
            return false;
        }

        // Checked after the rules: only a real change of character is worth the time zone conversion
        if (settings.UseSessionWindow && !InSessionWindow(closeTime.ToDateTime(), settings.SessionStart, settings.SessionEnd, out string sessionReason))
        {
            ExtraText = sessionReason;
            return false;
        }

        EntryPrice = setup!.Entry;
        StopPercentage = setup.StopPercentage;
        TargetPercentage = setup.TargetPercentage;
        WaitingTargets[(Symbol.Name, SignalSide)] = setup.Target;

        string swingText = isLong ? "lower high" : "higher low";
        ExtraText = $"change of character through {swingText} {setup.BrokenSwing:0.########}, leg {setup.LegStart:0.########}..{setup.LegEnd:0.########}, "
            + $"limit {setup.Entry:0.########} on the gap middle, stop {setup.Stop:0.########} ({setup.StopPercentage:N2}%), "
            + $"target {setup.Target:0.########} ({setup.TargetPercentage:N2}%)";
        return true;
    }


    /// <summary>
    /// The rules decide the entry themselves; the global entry conditions (watch candles, RSI
    /// recovery and the like) are not part of them.
    /// </summary>
    public override bool AllowStepIn(CryptoSignal signal) => true;


    /// <summary>
    /// The limit stays until it fills, unless it waited EntryExpiryCandles candles or the price reached
    /// the target without it: the move it was meant for is over.
    /// </summary>
    public override bool WithdrawEntry(CryptoPosition position)
    {
        ExtraText = "";
        ChochFvgSettings settings = ChochFvgPlugin.Settings;
        bool isLong = position.Side == CryptoTradeSide.Long;

        DateTime closeTime = (CandleLast.Candle.OpenTime + Interval.Duration).ToDateTime();
        double waited = (closeTime - position.CreateTime).TotalMinutes / Interval.Duration;
        if (waited >= settings.EntryExpiryCandles)
        {
            ExtraText = $"limit not filled within {settings.EntryExpiryCandles} candles";
            WaitingTargets.TryRemove((Symbol.Name, position.Side), out _);
            return true;
        }

        if (WaitingTargets.TryGetValue((Symbol.Name, position.Side), out decimal target))
        {
            bool reached = isLong
                ? CandleLast.Candle.High >= target
                : CandleLast.Candle.Low <= target;
            if (reached)
            {
                ExtraText = $"the price reached the target {target:0.########} without a fill";
                WaitingTargets.TryRemove((Symbol.Name, position.Side), out _);
                return true;
            }
        }

        return false;
    }
}


public class ChochFvgLong : ChochFvgBase
{
}


public class ChochFvgShort : ChochFvgBase
{
}
