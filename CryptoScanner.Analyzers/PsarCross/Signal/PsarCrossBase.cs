using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.PsarCross.Signal;

/// <summary>
/// The parabolic SAR cross. A long fires when the SAR closes under the price after having been
/// above it, a short the other way round. With ExitOnFlipBack the same strategy asks for the
/// position to be closed once the SAR flips back (<see cref="SignalCreateBase.IsExitSignal"/>).
/// <para>
/// Reads the standard 0.02/0.2 SAR from CandleData, which every hub computes. The side is read against the CLOSE of the candle: the SAR itself flips as
/// soon as the high or low touches it, but a closed candle is what the scanner evaluates, so the
/// signal comes at the close of the candle on which the flip happened.
/// </para>
/// </summary>
public class PsarCrossBase : SignalCreateBase
{
    public override bool IndicatorsOkay(MyData data)
        => data != null
        && data.Candle.OpenTime != 0
        && data.CandleData?.PSar != null;

    public override bool HasExitSignal => Settings.ExitOnFlipBack;


    /// <summary>The settings this instance reads, virtual for a variant with its own settings object.</summary>
    protected virtual PsarCrossSettings Settings => PsarCrossPlugin.Settings;


    /// <summary>
    /// Whether the SAR sits on the side of the price that favours this trade: under the close for a
    /// long, above it for a short. Exactly on it counts as neither.
    /// </summary>
    private bool IsOnOurSide(MyData data)
    {
        decimal psar = (decimal)data.CandleData!.PSar!.Value;
        decimal close = data.Candle.Close;
        return SignalSide == CryptoTradeSide.Long ? psar < close : psar > close;
    }


    /// <summary>
    /// Extends <paramref name="candles"/> (newest first, starting at the candle being evaluated)
    /// with older candles until it holds <paramref name="count"/> of them. False when the history is
    /// not there yet - GetPrevCandle also checks that the older candle has its SAR.
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
        PsarCrossSettings settings = Settings;
        int confirm = Math.Max(0, settings.ConfirmationCandles);

        // The candle being evaluated, the confirmation candles behind it, and the candle BEFORE the
        // flip, which has to be on the other side or there was no flip at all.
        List<MyData> candles = new(confirm + 2);
        if (!CollectCandles(confirm + 2, candles))
            return false;

        // Cheapest test first: on most candles the SAR is simply not on our side.
        for (int i = 0; i <= confirm; i++)
        {
            if (!IsOnOurSide(candles[i]))
            {
                ExtraText = i == 0
                    ? (SignalSide == CryptoTradeSide.Long ? "psar not under the price" : "psar not above the price")
                    : $"flip not held for {confirm} candle(s)";
                return false;
            }
        }
        if (IsOnOurSide(candles[confirm + 1]))
        {
            ExtraText = "no flip, the psar was already on this side";
            return false;
        }

        // The distance at the signal candle as a percentage of the price. The SAR is on our side
        // (tested above), so the absolute value reads the same for a long and a short.
        decimal psar = (decimal)CandleLast.CandleData!.PSar!.Value;
        decimal close = CandleLast.Candle.Close;
        decimal distance = close > 0 ? 100m * Math.Abs(close - psar) / close : 0m;
        if (settings.MinimumDistancePercentage > 0 && distance < settings.MinimumDistancePercentage)
        {
            ExtraText = $"psar only {distance:N3}% from the price, {settings.MinimumDistancePercentage}% wanted";
            return false;
        }

        if (!VolumeOkay(settings, candles, out string volumeText))
            return false;

        string direction = SignalSide == CryptoTradeSide.Long ? "under" : "above";
        ExtraText = confirm == 0
            ? $"psar flipped {direction} the price, {distance:N3}% away"
            : $"psar flipped {direction} the price {confirm} candle(s) ago, {distance:N3}% away";
        ExtraText += volumeText;
        return true;
    }


    /// <summary>
    /// The relative volume: the average volume of the recent candles against the average of the
    /// candles before them. The recent candles are left out of the baseline on purpose.
    /// </summary>
    private bool VolumeOkay(PsarCrossSettings settings, List<MyData> candles, out string text)
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
    /// Out when the SAR is against the position. Deliberately "is against" rather than "flipped on
    /// this candle": after a flip back it stays against the position until the next flip, so a
    /// candle the monitor did not get to see (a restart, a skipped candle) is not a lost exit.
    /// </summary>
    public override bool IsExitSignal()
    {
        ExtraText = "";
        PsarCrossSettings settings = Settings;
        if (!settings.ExitOnFlipBack)
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
                    ? "psar still on our side of the price"
                    : $"flip back not held for {confirm} candle(s)";
                return false;
            }
        }

        ExtraText = SignalSide == CryptoTradeSide.Long
            ? "psar flipped back above the price"
            : "psar flipped back under the price";
        return true;
    }
}


public class PsarCrossLong : PsarCrossBase
{
}


public class PsarCrossShort : PsarCrossBase
{
}
