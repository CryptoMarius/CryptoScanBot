using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.HeikinAshi.Signal;

/// <summary>
/// The Heikin Ashi trend. A long fires on the close of the first green Heikin Ashi candle after a
/// red one while the last closed Heikin Ashi candle of the higher interval is green; a short the
/// other way round. The exit (<see cref="SignalCreateBase.IsExitSignal"/>) leaves on the first
/// Heikin Ashi candle of the other colour or with a wick on the other side.
/// <para>
/// The Heikin Ashi candles are calculated here from the real candles of the interval, over the last
/// <see cref="HistoryCandles"/> candles; nothing is stored. The seed of the first candle has lost its
/// influence long before the last one (it halves with every candle).
/// </para>
/// </summary>
public class HeikinAshiBase : SignalCreateBase
{
    /// <summary>How many real candles the Heikin Ashi candles are calculated over.</summary>
    public const int HistoryCandles = 60;

    /// <summary>
    /// The fewest real candles to calculate over. Below this the seed of the first candle still
    /// weighs too much (after ten candles it is down to a thousandth).
    /// </summary>
    public const int MinimumCandles = 10;

    public override bool IndicatorsOkay(MyData data)
        => data != null
        && data.Candle.OpenTime != 0;

    public override bool HasExitSignal => Settings.ExitOnSignal;


    /// <summary>The settings this instance reads, virtual for a variant with its own settings object.</summary>
    protected virtual HeikinAshiSettings Settings => HeikinAshiPlugin.Settings;


    /// <summary>+1 for a long, -1 for a short: the colour that favours this trade.</summary>
    private int OurColour => SignalSide == CryptoTradeSide.Long ? 1 : -1;


    /// <summary>
    /// The Heikin Ashi candles of this interval up to and including <see cref="SignalCreateBase.CandleLast"/>,
    /// oldest first. False (with ExtraText) when there are too few candles.
    /// </summary>
    protected bool GetCandles(out List<HeikinAshiCandle> candles)
    {
        List<CryptoCandle> real = SymbolInterval.CandleList.GetLastValues(CandleLast.Candle.OpenTime, HistoryCandles, Interval.Duration);
        candles = HeikinAshiCalculator.Calculate(real);
        if (candles.Count < MinimumCandles || candles[^1].OpenTime != CandleLast.Candle.OpenTime)
        {
            ExtraText = $"not enough candles ({candles.Count} of {MinimumCandles})";
            return false;
        }
        return true;
    }


    /// <summary>
    /// The colour of the last Heikin Ashi candle of <paramref name="intervalName"/> that had closed
    /// when the signal candle closed. Null (with ExtraText) when the interval is unknown or there are
    /// too few candles.
    /// </summary>
    protected int? HigherColour(string intervalName)
    {
        if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval) || interval == null)
        {
            ExtraText = $"unknown higher interval {intervalName}";
            return null;
        }

        uint duration = interval.Duration;
        CandleTime momentTime = CandleLast.Candle.OpenTime + Interval.Duration;
        CandleTime lastOpen = (momentTime - 1u).AlignToIntervalMinutes(duration) - duration;
        CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(interval.IntervalPeriod);
        List<CryptoCandle> real = symbolInterval.CandleList.GetLastValues(lastOpen, HistoryCandles, duration);
        List<HeikinAshiCandle> candles = HeikinAshiCalculator.Calculate(real);
        if (candles.Count < MinimumCandles || candles[^1].OpenTime != lastOpen)
        {
            ExtraText = $"not enough {intervalName} candles ({candles.Count} of {MinimumCandles})";
            return null;
        }
        return candles[^1].Colour;
    }


    public override bool IsSignal()
    {
        ExtraText = "";
        HeikinAshiSettings settings = Settings;

        if (!GetCandles(out List<HeikinAshiCandle> candles))
            return false;

        // Cheapest test first: on most candles there is no colour change.
        HeikinAshiCandle last = candles[^1];
        HeikinAshiCandle previous = candles[^2];
        if (last.Colour != OurColour)
        {
            ExtraText = SignalSide == CryptoTradeSide.Long ? "no green candle" : "no red candle";
            return false;
        }
        if (previous.Colour != -OurColour)
        {
            ExtraText = "no colour change";
            return false;
        }
        if (previous.OpenTime + Interval.Duration != last.OpenTime)
        {
            ExtraText = "gap before the signal candle";
            return false;
        }

        string higherText = "";
        string higherName = settings.HigherInterval?.Trim() ?? "";
        if (higherName != "")
        {
            int? colour = HigherColour(higherName);
            if (colour == null)
                return false;
            if (colour != OurColour)
            {
                ExtraText = $"{higherName} candle not {(SignalSide == CryptoTradeSide.Long ? "green" : "red")}";
                return false;
            }
            higherText = $", {higherName} {(SignalSide == CryptoTradeSide.Long ? "green" : "red")}";
        }

        ExtraText = (SignalSide == CryptoTradeSide.Long ? "heikin ashi turned green" : "heikin ashi turned red") + higherText;
        return true;
    }


    /// <summary>
    /// The stop beyond the signal candle as a percentage of the signal price (its close): the
    /// distance to the real low for a long, to the real high for a short. Null when the setting is
    /// off or the close sits on that extreme, which leaves the stop of the trading settings.
    /// </summary>
    public override decimal? OverrideSlPercentage
    {
        get
        {
            if (!Settings.StopBeyondSignalCandle)
                return null;
            decimal close = CandleLast.Candle.Close;
            if (close <= 0)
                return null;
            decimal distance = SignalSide == CryptoTradeSide.Long
                ? close - CandleLast.Candle.Low
                : CandleLast.Candle.High - close;
            if (distance <= 0)
                return null;
            return 100m * distance / close;
        }
    }


    /// <summary>
    /// Out when the last closed Heikin Ashi candle is of the other colour, or, with
    /// ExitOnOppositeWick, has a wick on the other side. Written as a state, so a candle the monitor
    /// did not get to see is not a lost exit: the next candle is asked the same question.
    /// </summary>
    public override bool IsExitSignal()
    {
        ExtraText = "";
        HeikinAshiSettings settings = Settings;
        if (!settings.ExitOnSignal)
            return false;

        if (!GetCandles(out List<HeikinAshiCandle> candles))
            return false;

        HeikinAshiCandle last = candles[^1];
        if (last.Colour == -OurColour)
        {
            ExtraText = SignalSide == CryptoTradeSide.Long ? "heikin ashi turned red" : "heikin ashi turned green";
            return true;
        }

        if (settings.ExitOnOppositeWick)
        {
            bool wick = SignalSide == CryptoTradeSide.Long
                ? last.HasLowerWick(settings.WickMinimumPercentage)
                : last.HasUpperWick(settings.WickMinimumPercentage);
            if (wick)
            {
                ExtraText = SignalSide == CryptoTradeSide.Long ? "heikin ashi wick under the body" : "heikin ashi wick above the body";
                return true;
            }
        }

        ExtraText = "heikin ashi still with the position";
        return false;
    }
}


public class HeikinAshiLong : HeikinAshiBase
{
}


public class HeikinAshiShort : HeikinAshiBase
{
}
