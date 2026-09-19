using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.Mac.Signal;

/// <summary>
/// MAC - the trend cloud, with three entries, each of which is a different idea:
/// <list type="bullet">
/// <item>the BREAKOUT: the candle closes through the last confirmed pivot level, with the cloud
/// pointing the way of the trade;</item>
/// <item>the CLOUD CROSS: the fast EMA closes on the other side of the second one;</item>
/// <item>the SPRINGBOARD BOUNCE: inside a running trend the price dips to the fast EMA and closes
/// back on our side of it.</item>
/// </list>
/// Each has its own switch, so a run measures them apart or together.
/// <para>
/// Every filter on top of those triggers is off by default, so the bare rule is measured first and
/// each filter after it on its own. See <see cref="MacSettings"/>.
/// </para>
/// <para>
/// The checks run cheapest first. Everything that reads the candle in hand comes before the single
/// step back to the previous candle, the volume average walks furthest and therefore runs last, and
/// most candles never get past the cloud.
/// </para>
/// </summary>
public class MacBase : SignalCreateBase
{
    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null || data.Candle.OpenTime == 0 || data.CandleData == null)
            return false;
        MacCandleData? mac = data.CandleData.GetPluginData<MacCandleData>();
        return mac?.EmaFast != null && mac.EmaSecond != null
            && mac.SmaMedium != null && mac.SmaSlow != null;
    }

    public override bool HasExitSignal => Settings.ExitOnCloudFlip;


    /// <summary>
    /// The settings this instance reads. Virtual so a variant that derives from this strategy can
    /// hand back its OWN settings object, which keeps the two tunable apart from each other.
    /// </summary>
    protected virtual MacSettings Settings => MacPlugin.Settings;


    /// <summary>
    /// Whether the cloud points the way this trade wants: the fast ema above the second one for a
    /// long, under it for a short. Exactly on it counts as neither.
    /// </summary>
    private bool IsCloudOnOurSide(MacCandleData mac)
    {
        double fast = mac.EmaFast!.Value;
        double second = mac.EmaSecond!.Value;
        return SignalSide == CryptoTradeSide.Long ? fast > second : fast < second;
    }


    /// <summary>The height of the whole cloud - top line to bottom line - as a percentage of the price.</summary>
    private static decimal CloudWidthPercentage(MacCandleData mac, decimal close)
    {
        if (close <= 0)
            return 0m;
        double? top = mac.CloudTop;
        double? bottom = mac.CloudBottom;
        if (top == null || bottom == null)
            return 0m;
        return 100m * (decimal)(top.Value - bottom.Value) / close;
    }


    public override bool IsSignal()
    {
        ExtraText = "";
        MacSettings settings = Settings;
        if (!settings.EntryOnBreakout && !settings.EntryOnCloudCross && !settings.EntryOnSpringboard)
        {
            ExtraText = "no entry trigger is switched on";
            return false;
        }

        MacCandleData? mac = CandleLast.CandleData!.GetPluginData<MacCandleData>();
        if (mac?.EmaFast == null || mac.EmaSecond == null || mac.SmaSlow == null)
        {
            ExtraText = "the cloud is not there yet";
            return false;
        }

        // Cheapest test first: on most candles the cloud simply points the other way. It holds for
        // both triggers - a cross ends with the cloud on our side, a breakout wants it there too.
        if (!IsCloudOnOurSide(mac))
        {
            ExtraText = SignalSide == CryptoTradeSide.Long
                ? "cloud points down, a long wants the fast ema above the second one"
                : "cloud points up, a short wants the fast ema under the second one";
            return false;
        }

        decimal close = CandleLast.Candle.Close;
        decimal cloudWidth = CloudWidthPercentage(mac, close);
        if (settings.MinimumCloudWidthPercentage > 0 && cloudWidth < settings.MinimumCloudWidthPercentage)
        {
            ExtraText = $"cloud only {cloudWidth:N2}% wide, {settings.MinimumCloudWidthPercentage}% wanted";
            return false;
        }

        // The slow line: how much weight the trend itself deserves. One lookup, so it sits here.
        string slopeText = "";
        if (settings.MinimumSlowLineSlopePercentage > 0)
        {
            if (mac.SlowSlopePercentage == null)
            {
                ExtraText = "the slow line has no slope yet";
                return false;
            }
            decimal slope = (decimal)mac.SlowSlopePercentage.Value;
            decimal wanted = settings.MinimumSlowLineSlopePercentage;
            bool okay = SignalSide == CryptoTradeSide.Long ? slope >= wanted : slope <= -wanted;
            if (!okay)
            {
                ExtraText = SignalSide == CryptoTradeSide.Long
                    ? $"slow line only {slope:N2}% over {settings.SlowLineLookbackCandles} candles, {wanted}% wanted"
                    : $"slow line {slope:N2}% over {settings.SlowLineLookbackCandles} candles, -{wanted}% wanted";
                return false;
            }
            slopeText = $", slow line {slope:N2}%";
        }

        string rsiText = "";
        if (settings.UseRsiFilter && !RsiOkay(settings, out rsiText))
            return false;

        // One step back, shared by everything below: the cross needs the cloud of the previous
        // candle, the breakout its close, and the widening test both.
        if (!GetPrevCandle(CandleLast, out MyData? candlePrev) || candlePrev == null)
            return false;
        MacCandleData? macPrev = candlePrev.CandleData!.GetPluginData<MacCandleData>();
        decimal closePrev = candlePrev.Candle.Close;

        // The triggers. Either one is enough; the reason of the one that did not fire is kept for
        // the log, so a candle that missed by a hair still says why.
        string trigger = "";
        string missed = "";
        if (settings.EntryOnCloudCross)
        {
            if (CloudCrossed(mac, macPrev, out string crossReason))
            {
                trigger = SignalSide == CryptoTradeSide.Long
                    ? "cloud crossed up"
                    : "cloud crossed down";
            }
            else
            {
                missed = crossReason;
            }
        }
        if (trigger.Length == 0 && settings.EntryOnSpringboard)
        {
            if (BouncedOffTheFastLine(mac, candlePrev, out string bounceReason))
                trigger = "bounced off the fast line";
            else
                missed = missed.Length > 0 ? $"{missed}; {bounceReason}" : bounceReason;
        }
        if (trigger.Length == 0 && settings.EntryOnBreakout)
        {
            if (BrokeTheLevel(settings, mac, close, closePrev, out string levelText, out string breakReason))
                trigger = levelText;
            else
                missed = missed.Length > 0 ? $"{missed}; {breakReason}" : breakReason;
        }
        if (trigger.Length == 0)
        {
            ExtraText = missed;
            return false;
        }

        string cloudText = "";
        if (settings.RequireCloudWidening)
        {
            if (macPrev?.EmaFast == null || macPrev.EmaSecond == null || macPrev.SmaSlow == null)
            {
                ExtraText = "no cloud on the previous candle to compare against";
                return false;
            }
            decimal widthPrev = CloudWidthPercentage(macPrev, closePrev);
            if (cloudWidth <= widthPrev)
            {
                ExtraText = $"cloud narrowing, {widthPrev:N2}% to {cloudWidth:N2}%";
                return false;
            }
            cloudText = $", widening from {widthPrev:N2}%";
        }

        // Last, because it is the only test that walks a stretch of candles.
        string volumeText = "";
        if (settings.UseVolumeFilter && !VolumeOkay(settings, out volumeText))
            return false;

        ExtraText = $"{trigger}, cloud {cloudWidth:N2}% wide{cloudText}{slopeText}{rsiText}{volumeText}";
        return true;
    }


    /// <summary>
    /// The cloud cross. The cloud is already known to be on our side at the candle in hand, so the
    /// cross is there when it was NOT on our side at the candle before.
    /// </summary>
    private bool CloudCrossed(MacCandleData mac, MacCandleData? macPrev, out string reason)
    {
        if (macPrev?.EmaFast == null || macPrev.EmaSecond == null || macPrev.SmaSlow == null)
        {
            reason = "no cloud on the previous candle to cross from";
            return false;
        }
        if (IsCloudOnOurSide(macPrev))
        {
            reason = "no cross, the cloud already pointed this way";
            return false;
        }
        reason = "";
        return true;
    }


    /// <summary>
    /// The springboard bounce: inside a trend that is already running, the price dips to the fast
    /// EMA and closes back on our side of it. The previous candle has to be on that side too, which
    /// is what separates a dip into the line from a first crossing of it.
    /// </summary>
    private bool BouncedOffTheFastLine(MacCandleData mac, MyData candlePrev, out string reason)
    {
        double fast = mac.EmaFast!.Value;
        decimal close = CandleLast.Candle.Close;
        bool reached = SignalSide == CryptoTradeSide.Long
            ? (double)CandleLast.Candle.Low <= fast
            : (double)CandleLast.Candle.High >= fast;
        if (!reached)
        {
            reason = "the candle did not reach the fast line";
            return false;
        }

        bool closedBack = SignalSide == CryptoTradeSide.Long ? (double)close > fast : (double)close < fast;
        if (!closedBack)
        {
            reason = SignalSide == CryptoTradeSide.Long
                ? "the candle closed under the fast line instead of bouncing off it"
                : "the candle closed above the fast line instead of bouncing off it";
            return false;
        }

        // The trend has to have been there before this candle, or this is a crossing dressed up as
        // a bounce. The fast line of the PREVIOUS candle is the one to measure that against.
        MacCandleData? macPrev = candlePrev.CandleData!.GetPluginData<MacCandleData>();
        if (macPrev?.EmaFast == null)
        {
            reason = "no fast line on the previous candle";
            return false;
        }
        decimal closePrev = candlePrev.Candle.Close;
        bool wasOnOurSide = SignalSide == CryptoTradeSide.Long
            ? (double)closePrev > macPrev.EmaFast.Value
            : (double)closePrev < macPrev.EmaFast.Value;
        if (!wasOnOurSide)
        {
            reason = "the price was not above the fast line before the dip";
            return false;
        }

        reason = "";
        return true;
    }


    /// <summary>
    /// The break through the last confirmed pivot level. The level sits at least PivotRightCandles
    /// candles back, so it can never be the candle in hand, and the previous close has to be on the
    /// other side of it - otherwise a market trading above its resistance signals on every candle.
    /// </summary>
    private bool BrokeTheLevel(MacSettings settings, MacCandleData mac, decimal close, decimal closePrev,
        out string text, out string reason)
    {
        text = "";

        // Applies to the breakout only: at a cloud cross the price sits on the cloud by definition.
        if (settings.RequirePriceOutsideCloud)
        {
            double edge = SignalSide == CryptoTradeSide.Long ? mac.CloudTop!.Value : mac.CloudBottom!.Value;
            bool outside = SignalSide == CryptoTradeSide.Long ? (double)close > edge : (double)close < edge;
            if (!outside)
            {
                reason = SignalSide == CryptoTradeSide.Long
                    ? $"close {close:N8} is not above the cloud ({edge:N8})"
                    : $"close {close:N8} is not under the cloud ({edge:N8})";
                return false;
            }
        }

        double? level = SignalSide == CryptoTradeSide.Long ? mac.PivotHigh : mac.PivotLow;
        int age = SignalSide == CryptoTradeSide.Long ? mac.PivotHighAge : mac.PivotLowAge;
        if (level == null)
        {
            reason = SignalSide == CryptoTradeSide.Long
                ? "no pivot high to break through yet"
                : "no pivot low to break through yet";
            return false;
        }
        if (settings.PivotMaximumAgeCandles > 0 && age > settings.PivotMaximumAgeCandles)
        {
            reason = $"the level is {age} candles old, {settings.PivotMaximumAgeCandles} is the limit";
            return false;
        }

        decimal levelPrice = (decimal)level.Value;
        decimal buffer = levelPrice * settings.BreakoutBufferPercentage / 100m;
        decimal breakPrice = SignalSide == CryptoTradeSide.Long ? levelPrice + buffer : levelPrice - buffer;
        bool through = SignalSide == CryptoTradeSide.Long ? close > breakPrice : close < breakPrice;
        if (!through)
        {
            reason = SignalSide == CryptoTradeSide.Long
                ? $"close {close:N8} did not clear the resistance at {breakPrice:N8}"
                : $"close {close:N8} did not clear the support at {breakPrice:N8}";
            return false;
        }

        bool wasThrough = SignalSide == CryptoTradeSide.Long ? closePrev > breakPrice : closePrev < breakPrice;
        if (wasThrough)
        {
            reason = "the level was already broken on the previous candle";
            return false;
        }

        reason = "";
        text = SignalSide == CryptoTradeSide.Long
            ? $"broke the resistance at {levelPrice:N8} ({age} candles old)"
            : $"broke the support at {levelPrice:N8} ({age} candles old)";
        return true;
    }


    /// <summary>
    /// The volume condition: the signal candle has to trade at VolumeMultiplier times the average
    /// volume of the VolumeAverageCandles candles BEFORE it. The signal candle is left out of that
    /// average on purpose - a spike that is part of its own average is a smaller spike.
    /// </summary>
    private bool VolumeOkay(MacSettings settings, out string text)
    {
        text = "";
        int length = Math.Max(1, settings.VolumeAverageCandles);
        decimal sum = 0m;
        MyData? walk = CandleLast;
        for (int i = 0; i < length; i++)
        {
            if (!GetPrevCandle(walk, out walk) || walk == null)
            {
                ExtraText = $"not enough candles for the volume average ({i} of {length})";
                return false;
            }
            sum += walk.Candle.Volume;
        }

        decimal average = sum / length;
        if (average <= 0)
        {
            ExtraText = "no volume to compare against";
            return false;
        }

        decimal ratio = CandleLast.Candle.Volume / average;
        if (ratio < settings.VolumeMultiplier)
        {
            ExtraText = $"volume {ratio:N2}x the average, {settings.VolumeMultiplier}x wanted";
            return false;
        }

        text = $", volume {ratio:N2}x";
        return true;
    }


    /// <summary>
    /// The RSI has to agree with the break: at or above the minimum for a long, at or below the
    /// maximum for a short. An RSI that is not there yet is a no, said out loud.
    /// </summary>
    private bool RsiOkay(MacSettings settings, out string text)
    {
        text = "";
        double? rsi = CandleLast.CandleData!.Rsi;
        if (rsi == null)
        {
            ExtraText = "rsi not available yet";
            return false;
        }

        if (SignalSide == CryptoTradeSide.Long && (decimal)rsi.Value < settings.RsiLongMinimum)
        {
            ExtraText = $"rsi {rsi.Value:N1} under the minimum of {settings.RsiLongMinimum}";
            return false;
        }
        if (SignalSide == CryptoTradeSide.Short && (decimal)rsi.Value > settings.RsiShortMaximum)
        {
            ExtraText = $"rsi {rsi.Value:N1} above the maximum of {settings.RsiShortMaximum}";
            return false;
        }

        text = $", rsi {rsi.Value:N1}";
        return true;
    }


    /// <summary>
    /// Out when the cloud is against the position. Deliberately "is against" rather than "flipped on
    /// this candle": after a flip the cloud stays against the position until it turns back, so a
    /// candle the monitor did not get to see (a restart, a skipped candle) is not a lost exit.
    /// </summary>
    public override bool IsExitSignal()
    {
        ExtraText = "";
        MacSettings settings = Settings;
        if (!settings.ExitOnCloudFlip)
            return false;

        int confirm = Math.Max(0, settings.ExitConfirmationCandles);
        MyData? walk = CandleLast;
        for (int i = 0; i <= confirm; i++)
        {
            if (i > 0 && (!GetPrevCandle(walk, out walk) || walk == null))
                return false;

            MacCandleData? mac = walk!.CandleData!.GetPluginData<MacCandleData>();
            if (mac?.EmaFast == null || mac.EmaSecond == null || mac.SmaSlow == null)
            {
                ExtraText = "the cloud is not there";
                return false;
            }
            if (IsCloudOnOurSide(mac))
            {
                ExtraText = i == 0
                    ? "cloud still points our way"
                    : $"flip not held for {confirm} candle(s)";
                return false;
            }
        }

        ExtraText = SignalSide == CryptoTradeSide.Long
            ? "cloud flipped down"
            : "cloud flipped up";
        return true;
    }
}


public class MacLong : MacBase
{
}


public class MacShort : MacBase
{
}
