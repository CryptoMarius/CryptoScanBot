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

    /// <summary>
    /// Whether the position monitor asks this strategy for an exit at all. Every way out has to be
    /// named here: a way out that is switched on while this says no is never asked, the run comes
    /// back identical to one without it, and the setting reads as "measured and worthless" when it
    /// was never measured. That happened on 21-09-2026 to the second line crossing.
    /// </summary>
    public override bool HasExitSignal => Settings.ExitOnCloudFlip || Settings.ExitOnSecondLineCross;


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
        if (!settings.EntryOnOpenMarker && !settings.EntryOnCrossMarker
            && !settings.EntryOnCloseMarker && !settings.EntryOnBreakMarker)
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

        // The line cross is settled first, because it is the one trigger that fires BEFORE the
        // cloud turns: the second line gives way to the third while the fast line is still on the
        // other side. Asking for the cloud first would make it unreachable.
        MyData? candlePrev = null;
        MacCandleData? macPrev = null;
        bool lineCrossed = false;
        bool closeCrossedSecond = false;
        if ((settings.EntryOnCrossMarker || settings.EntryOnCloseMarker)
            && GetPrevCandle(CandleLast, out candlePrev) && candlePrev != null)
        {
            macPrev = candlePrev.CandleData!.GetPluginData<MacCandleData>();
            if (settings.EntryOnCrossMarker)
                lineCrossed = SecondLineCrossedTheThird(mac, macPrev);
            if (settings.EntryOnCloseMarker)
                closeCrossedSecond = CloseCrossedTheSecondLine(mac, macPrev, candlePrev);
        }

        // Cheapest test first: on most candles the cloud simply points the other way. It holds for
        // both triggers - a cross ends with the cloud on our side, a breakout wants it there too.
        //
        // The two CROSSINGS are exempt, and for the same reason: both fire while the cloud still
        // points the other way. The line cross runs ahead of the cloud, and the close crossing the
        // second line is the very marker the strategy draws to CLOSE the opposite position - by
        // definition the cloud is then still pointing that opposite way.
        if (!lineCrossed && !closeCrossedSecond && !IsCloudOnOurSide(mac))
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
        if (candlePrev == null && (!GetPrevCandle(CandleLast, out candlePrev) || candlePrev == null))
            return false;
        macPrev ??= candlePrev.CandleData!.GetPluginData<MacCandleData>();
        decimal closePrev = candlePrev.Candle.Close;

        // The triggers. Either one is enough; the reason of the one that did not fire is kept for
        // the log, so a candle that missed by a hair still says why.
        string trigger = "";
        string missed = "";
        if (settings.EntryOnCrossMarker)
        {
            if (lineCrossed)
            {
                trigger = SignalSide == CryptoTradeSide.Long
                    ? "second line crossed over the third"
                    : "second line crossed under the third";
            }
            else
            {
                missed = "no line cross, the second line stayed on its side of the third";
            }
        }
        if (trigger.Length == 0 && settings.EntryOnCloseMarker)
        {
            if (closeCrossedSecond)
            {
                trigger = SignalSide == CryptoTradeSide.Long
                    ? "close crossed over the second line"
                    : "close crossed under the second line";
            }
            else
            {
                missed = missed.Length > 0
                    ? $"{missed}; the close stayed on its side of the second line"
                    : "the close stayed on its side of the second line";
            }
        }
        if (trigger.Length == 0 && settings.EntryOnOpenMarker)
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
        if (trigger.Length == 0 && settings.EntryOnBreakMarker)
        {
            if (BrokeTheLevel(settings, mac, out string levelText, out string breakReason))
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
    /// The second line crossing the third: EMA(40) through SMA(50), above it for a long and under
    /// it for a short. It is the earliest event in the cloud - the second line gives way before the
    /// fast line does - and the strategy draws it as its own marker.
    /// </summary>
    private bool SecondLineCrossedTheThird(MacCandleData mac, MacCandleData? macPrev)
    {
        if (mac.EmaSecond == null || mac.SmaMedium == null
            || macPrev?.EmaSecond == null || macPrev.SmaMedium == null)
            return false;
        double second = mac.EmaSecond.Value;
        double medium = mac.SmaMedium.Value;
        double secondPrev = macPrev.EmaSecond.Value;
        double mediumPrev = macPrev.SmaMedium.Value;
        return SignalSide == CryptoTradeSide.Long
            ? secondPrev <= mediumPrev && second > medium
            : secondPrev >= mediumPrev && second < medium;
    }


    /// <summary>
    /// The close crossing the second line the way this signal wants to trade.
    /// <para>
    /// This is the SAME crossing the exit reads, taken from the other side: the marker the strategy
    ///  draws as "Close Long" is the close falling through the second line, and entering
    /// a SHORT on it is entering on what closes a long. So a long entry wants the close to cross
    /// UP through that line, which is the strategy's Close Short.
    /// </para>
    /// <para>
    /// The three guards the EXIT carries - the cloud pointing the way of the position, the close on
    /// that side of the slow line, the cloud stacked - are NOT repeated here. They exist to stop an
    /// exit firing against a trend that is still running, which is the opposite of what an entry on
    /// this crossing is for. What does apply is everything <see cref="IsSignal"/> asks of every
    /// trigger: the cloud width, the slope of the slow line, and the rest.
    /// </para>
    /// </summary>
    private bool CloseCrossedTheSecondLine(MacCandleData mac, MacCandleData? macPrev,
        MyData? candlePrev)
    {
        if (macPrev?.EmaSecond == null || mac.EmaSecond == null || candlePrev == null)
            return false;

        double close = (double)CandleLast.Candle.Close;
        double closePrev = (double)candlePrev.Candle.Close;
        return SignalSide == CryptoTradeSide.Long
            ? closePrev <= macPrev.EmaSecond.Value && close > mac.EmaSecond.Value
            : closePrev >= macPrev.EmaSecond.Value && close < mac.EmaSecond.Value;
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
    /// The Breakout and Breakdown markers: the close beyond the level with the wick past the
    /// hundred candles before it, the third of them at most since the position opened. All of the
    /// counting is done in the indicator, so there is nothing to walk here.
    /// <para>
    /// A second reading stood here until 25 September 2026 - fire on the candle whose close crosses
    /// the level, with a buffer and a maximum age around it. That reading is simply wrong: it lands
    /// on its own candle far less often, and now that the rule is known exactly there is no
    /// reason to keep it. The buffer and the age went with it.
    /// </para>
    /// </summary>
    private bool BrokeTheLevel(MacSettings settings, MacCandleData mac, out string text,
        out string reason)
    {
        text = "";

        // Applies to the break only: at a cloud cross the price sits on the cloud by definition.
        if (settings.RequirePriceOutsideCloud)
        {
            decimal close = CandleLast.Candle.Close;
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

        int rank = SignalSide == CryptoTradeSide.Long ? mac.BreakoutRank : mac.BreakdownRank;
        int allowed = Math.Max(1, settings.BreakoutEntriesPerRun);
        if (rank == 0)
        {
            reason = "this candle is not one the rule marks";
            return false;
        }
        if (rank > allowed)
        {
            reason = $"this is number {rank} of the position, {allowed} is the limit";
            return false;
        }

        reason = "";
        text = SignalSide == CryptoTradeSide.Long
            ? $"break number {rank} beyond the resistance"
            : $"break number {rank} beyond the support";
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
    /// <summary>
    /// The close crossing back through the second line against the position: down through it for a
    /// long, up through it for a short. The previous candle has to have been on the other side, so
    /// this is the crossing itself and not the state that follows it.
    /// </summary>
    private bool CrossedTheSecondLine()
    {
        MacCandleData? mac = CandleLast.CandleData!.GetPluginData<MacCandleData>();
        if (mac?.EmaSecond == null)
            return false;
        if (!GetPrevCandle(CandleLast, out MyData? candlePrev) || candlePrev == null)
            return false;
        MacCandleData? macPrev = candlePrev.CandleData!.GetPluginData<MacCandleData>();
        if (macPrev?.EmaSecond == null)
            return false;

        double close = (double)CandleLast.Candle.Close;
        double closePrev = (double)candlePrev.Candle.Close;
        bool crossed = SignalSide == CryptoTradeSide.Long
            ? closePrev >= macPrev.EmaSecond.Value && close < mac.EmaSecond.Value
            : closePrev <= macPrev.EmaSecond.Value && close > mac.EmaSecond.Value;
        if (!crossed)
            return false;

        // Two conditions that belong to the crossing, both measured against the marker the strategy
        //  draws for it, over 629 crossings on four coins.
        //
        // The cloud has to still point the way of the position. Without it the same crossing fires
        // on BOTH sides - a long and a short exit on one candle - which is why this fired 314 times
        // against the 134 the strategy draws.
        if (mac.EmaFast == null)
            return false;
        bool cloudWithUs = SignalSide == CryptoTradeSide.Long
            ? mac.EmaFast.Value > mac.EmaSecond.Value
            : mac.EmaFast.Value < mac.EmaSecond.Value;
        if (!cloudWithUs)
            return false;

        // And the cloud has to be STACKED the way of the position, all the way down: the second
        // line on our side of the third AND the third on our side of the slow one. Together with
        // the fast line over the second that is the FULL stack - fast > second > medium > slow for
        // a long - and that turns out to be the whole rule.
        //
        // A close falling back through the second line only means something while the lines
        // behind it are still in order. Once the third line has given way, the trend it was part of
        // has gone and the crossing is noise.
        //
        // What stood here until 25 September 2026 asked instead that the CLOSE was still on the
        // position's side of the slow line. That is a near miss of the same idea: over 4282
        // crossings it turns away one exit in twenty that the stack keeps, and lets about as many
        // through that the stack does not.
        if (mac.SmaMedium == null || mac.SmaSlow == null)
            return false;
        bool cloudStacked = SignalSide == CryptoTradeSide.Long
            ? mac.EmaSecond.Value > mac.SmaMedium.Value && mac.SmaMedium.Value > mac.SmaSlow.Value
            : mac.EmaSecond.Value < mac.SmaMedium.Value && mac.SmaMedium.Value < mac.SmaSlow.Value;
        if (!cloudStacked)
            return false;

        ExtraText = SignalSide == CryptoTradeSide.Long
            ? $"close {close:N8} crossed under the second line ({mac.EmaSecond.Value:N8})"
            : $"close {close:N8} crossed over the second line ({mac.EmaSecond.Value:N8})";
        return true;
    }


    public override bool IsExitSignal()
    {
        ExtraText = "";
        MacSettings settings = Settings;
        // Two ways out, and they stack: whichever fires first ends the position.
        if (settings.ExitOnSecondLineCross && CrossedTheSecondLine())
            return true;
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
