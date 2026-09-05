using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.MacdCross.Signal;

/// <summary>
/// The MACD crossover. A long fires when the MACD line closes above its signal line after having
/// been under it, a short the other way round. With ExitOnCrossBack the same strategy asks for the
/// position to be closed once the lines cross back - the first strategy to use
/// <see cref="SignalCreateBase.IsExitSignal"/>.
/// <para>
/// Reads the standard 12/26/9 MACD from CandleData, which every hub computes, and ADX(14), which
/// the plugin declares. The checks run cheapest first: the cross itself needs a handful of candles,
/// the ADX filters one value plus a short walk, the volume filter the longest walk - and most
/// candles never get past the first test.
/// </para>
/// </summary>
public class MacdCrossBase : SignalCreateBase
{
    public override bool IndicatorsOkay(MyData data)
        => data != null
        && data.Candle.OpenTime != 0
        && data.CandleData?.MacdValue != null
        && data.CandleData.MacdSignal != null;

    public override bool HasExitSignal => Settings.ExitOnCrossBack;


    /// <summary>
    /// The settings this instance reads. Virtual so a variant that derives from this strategy can
    /// hand back its OWN settings object (a MacdCrossSettings subclass), which keeps the two
    /// strategies tunable apart from each other instead of sharing one set of values.
    /// </summary>
    protected virtual MacdCrossSettings Settings => MacdCrossPlugin.Settings;


    /// <summary>
    /// Whether the MACD line sits on the side of the signal line that favours this trade: above it
    /// for a long, under it for a short. Exactly on it counts as neither.
    /// </summary>
    private bool IsOnOurSide(MyData data)
    {
        double macd = data.CandleData!.MacdValue!.Value;
        double signal = data.CandleData.MacdSignal!.Value;
        return SignalSide == CryptoTradeSide.Long ? macd > signal : macd < signal;
    }


    /// <summary>
    /// Extends <paramref name="candles"/> (newest first, starting at the candle being evaluated)
    /// with older candles until it holds <paramref name="count"/> of them. Called more than once
    /// with a growing count, so the walk is never done twice for the same candle. False when the
    /// history is not there yet, which at the start of a run it is not - GetPrevCandle also checks
    /// that the older candle has its MACD, so a warming-up hub says no here too.
    /// </summary>
    protected bool CollectCandles(int count, List<MyData> candles)
    {
        if (candles.Count == 0)
            candles.Add(CandleLast);
        MyData? walk = candles[^1];
        while (candles.Count < count)
        {
            if (!GetPrevCandle(walk, out walk) || walk == null)
            {
                ExtraText = $"not enough candles ({candles.Count} of {count})";
                return false;
            }
            candles.Add(walk);
        }
        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";
        MacdCrossSettings settings = Settings;
        int confirm = Math.Max(0, settings.ConfirmationCandles);

        // The candle being evaluated, the confirmation candles behind it, and one more: the candle
        // BEFORE the cross, which has to be on the other side or there was no cross at all.
        List<MyData> candles = new(confirm + 2);
        if (!CollectCandles(confirm + 2, candles))
            return false;

        // Cheapest test first: on most candles the lines are simply not on our side.
        for (int i = 0; i <= confirm; i++)
        {
            if (!IsOnOurSide(candles[i]))
            {
                ExtraText = i == 0
                    ? "macd not on our side of the signal line"
                    : $"cross not held for {confirm} candle(s)";
                return false;
            }
        }
        if (IsOnOurSide(candles[confirm + 1]))
        {
            ExtraText = "no cross, the lines were already on this side";
            return false;
        }

        // The cross candle is the oldest one on our side. The zero-line filter reads the MACD line
        // THERE, not at the signal candle: what it asks is where the cross happened.
        if (settings.RequireCrossBeyondZeroLine)
        {
            double macdAtCross = candles[confirm].CandleData!.MacdValue!.Value;
            bool beyond = SignalSide == CryptoTradeSide.Long ? macdAtCross < 0 : macdAtCross > 0;
            if (!beyond)
            {
                ExtraText = SignalSide == CryptoTradeSide.Long
                    ? "cross above the zero line, a long wants it under"
                    : "cross under the zero line, a short wants it above";
                return false;
            }
        }

        // The separation at the signal candle as a percentage of the price. The lines are on our
        // side (tested above), so the absolute value reads the same for a long and a short.
        double macd = CandleLast.CandleData!.MacdValue!.Value;
        double signal = CandleLast.CandleData.MacdSignal!.Value;
        decimal close = CandleLast.Candle.Close;
        decimal distance = close > 0 ? 100m * (decimal)Math.Abs(macd - signal) / close : 0m;
        if (settings.MinimumDistancePercentage > 0 && distance < settings.MinimumDistancePercentage)
        {
            ExtraText = $"lines only {distance:N3}% apart, {settings.MinimumDistancePercentage}% wanted";
            return false;
        }

        // The pre-selection: is the coin moving, and is the move young? Only asked once the cross is
        // there, so the walks below run on the few candles that get this far.
        if (!TrendStrengthOkay(settings, candles, out string trendText))
            return false;
        if (!VolumeOkay(settings, candles, out string volumeText))
            return false;

        // The last word goes to the variant, if there is one: it walks the furthest back, so it runs
        // once everything above has already said yes.
        if (!ExtraFiltersOkay(candles, out string variantText))
            return false;

        string direction = SignalSide == CryptoTradeSide.Long ? "above" : "under";
        ExtraText = confirm == 0
            ? $"macd crossed {direction} the signal line, {distance:N3}% apart"
            : $"macd crossed {direction} the signal line {confirm} candle(s) ago, {distance:N3}% apart";
        ExtraText += trendText + volumeText + variantText;
        return true;
    }


    /// <summary>
    /// A hook for a strategy that derives from this one: an extra test, run last because every
    /// filter above it is cheaper. Returning false means no signal, and the override writes its own
    /// reason into <see cref="SignalCreateBase.ExtraText"/>; returning true may add a phrase to
    /// <paramref name="text"/>, which is appended to the signal's text.
    /// <para>
    /// <paramref name="candles"/> is the list the checks above already walked, newest first, and can
    /// be extended further back with <see cref="CollectCandles"/> - so the walk is never done twice
    /// for the same candle.
    /// </para>
    /// </summary>
    protected virtual bool ExtraFiltersOkay(List<MyData> candles, out string text)
    {
        text = "";
        return true;
    }


    /// <summary>
    /// The ADX filters: a minimum at the signal candle, and the "young trend" test - somewhere in
    /// the last N candles the ADX has to have been under a threshold, so the cross is the start of
    /// a move and not its tail. Both off is the default and costs nothing. An ADX that is not there
    /// yet (the hub is warming up, or the filter is switched on in a profile whose plugin did not
    /// declare it) is a no, said out loud.
    /// </summary>
    private bool TrendStrengthOkay(MacdCrossSettings settings, List<MyData> candles, out string text)
    {
        text = "";
        if (settings.AdxMinimum <= 0 && settings.AdxRecentlyBelow <= 0)
            return true;

        double? adx = CandleLast.CandleData!.Adx14;
        if (adx == null)
        {
            ExtraText = "adx not available yet";
            return false;
        }
        if (settings.AdxMinimum > 0 && (decimal)adx.Value < settings.AdxMinimum)
        {
            ExtraText = $"adx {adx.Value:N1} under the minimum of {settings.AdxMinimum}";
            return false;
        }

        if (settings.AdxRecentlyBelow > 0)
        {
            int within = Math.Max(1, settings.AdxRecentlyWithinCandles);
            if (!CollectCandles(within, candles))
                return false;

            double lowest = double.MaxValue;
            for (int i = 0; i < within; i++)
            {
                double? earlier = candles[i].CandleData!.Adx14;
                if (earlier != null && earlier.Value < lowest)
                    lowest = earlier.Value;
            }
            if ((decimal)lowest >= settings.AdxRecentlyBelow)
            {
                ExtraText = $"adx did not come from under {settings.AdxRecentlyBelow} in the last "
                    + $"{within} candle(s), lowest {lowest:N1}";
                return false;
            }
        }

        text = $", adx {adx.Value:N1}";
        return true;
    }


    /// <summary>
    /// The relative volume: the average volume of the recent candles against the average of the
    /// candles before them. The recent candles are left out of the baseline on purpose - a spike
    /// that is part of its own average is a smaller spike.
    /// </summary>
    private bool VolumeOkay(MacdCrossSettings settings, List<MyData> candles, out string text)
    {
        text = "";
        if (settings.RelativeVolumeMinimum <= 0)
            return true;

        int recent = Math.Max(1, settings.RelativeVolumeCandles);
        int baseline = Math.Max(1, settings.RelativeVolumeAverageCandles);
        if (!CollectCandles(recent + baseline, candles))
            return false;

        decimal recentSum = 0m;
        for (int i = 0; i < recent; i++)
            recentSum += candles[i].Candle.Volume;
        decimal baselineSum = 0m;
        for (int i = recent; i < recent + baseline; i++)
            baselineSum += candles[i].Candle.Volume;

        decimal baselineAverage = baselineSum / baseline;
        if (baselineAverage <= 0)
        {
            ExtraText = "no volume to compare against";
            return false;
        }

        decimal ratio = recentSum / recent / baselineAverage;
        if (ratio < settings.RelativeVolumeMinimum)
        {
            ExtraText = $"volume {ratio:N2}x the average, {settings.RelativeVolumeMinimum}x wanted";
            return false;
        }

        text = $", volume {ratio:N2}x";
        return true;
    }


    /// <summary>
    /// Out when the lines are against the position. Deliberately "are against" rather than "crossed
    /// back on this candle": after a cross back they stay against the position until the next cross,
    /// so a candle the monitor did not get to see (a restart, a skipped candle) is not a lost exit.
    /// </summary>
    public override bool IsExitSignal()
    {
        ExtraText = "";
        MacdCrossSettings settings = Settings;
        if (!settings.ExitOnCrossBack)
            return false;

        int confirm = Math.Max(0, settings.ExitConfirmationCandles);
        List<MyData> candles = new(confirm + 1);
        if (!CollectCandles(confirm + 1, candles))
            return false;

        for (int i = 0; i <= confirm; i++)
        {
            if (IsOnOurSide(candles[i]))
            {
                ExtraText = i == 0
                    ? "macd still on our side of the signal line"
                    : $"cross back not held for {confirm} candle(s)";
                return false;
            }
        }

        ExtraText = SignalSide == CryptoTradeSide.Long
            ? "macd crossed back under the signal line"
            : "macd crossed back above the signal line";
        return true;
    }
}


public class MacdCrossLong : MacdCrossBase
{
}


public class MacdCrossShort : MacdCrossBase
{
}
