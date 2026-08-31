using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.CandlePattern.Signal;

/// <summary>
/// Fires on any of the candlestick reversal shapes named in the settings. Long and short share
/// everything; the side only decides how the shape is read, which is the point - a hammer and a
/// hanging man are the same candle.
/// <para>
/// No indicators. This exists to answer one question: does reacting to these shapes work as well as
/// any other reasonable strategy? Adding a filter first would leave that unanswered, so anything
/// extra belongs in a later variation, not in the measurement.
/// </para>
/// </summary>
public class CandlePatternBase : SignalCreateBase
{
    public override bool IndicatorsOkay(MyData data)
        => data != null && data.Candle.OpenTime != 0;


    public override bool IsSignal()
    {
        ExtraText = "";
        CandlePatternStrategySettings settings = CandlePatternPlugin.Settings;

        // Nothing ticked in the settings. Said out loud, because a strategy that produces nothing is
        // the most expensive thing to diagnose in this codebase.
        if (settings.Patterns.Count == 0)
        {
            ExtraText = "no pattern selected";
            return false;
        }

        if (!GetPrevCandle(CandleLast!, out MyData? previous))
            return false;

        // Only the three-candle shapes read this far back, and asking for a candle that is not there
        // would drop every signal at the start of a run. Missing simply means those shapes cannot
        // match, which CandlePatternHelper already handles - a two-candle shape in the same list
        // still fires.
        GetPrevCandle(previous, out MyData? before);

        if (!CandlePatternHelper.MatchesAny(settings.Patterns, SignalSide, CandleLast!.Candle,
                previous!.Candle, before?.Candle, settings.Shape,
                nameof(settings.Patterns), out CryptoCandlePattern matched))
        {
            ExtraText = $"no {string.Join("/", settings.Patterns)}";
            return false;
        }

        if (!PrecededByAMoveTheOtherWay(settings, previous))
            return false;

        ExtraText = $"{matched}";
        return true;
    }


    /// <summary>
    /// Whether price actually moved against the trade over the candles before the pattern. Measured
    /// from the close BEFORE the pattern started, so the pattern's own candles are not counted as
    /// the move they are supposed to reverse.
    /// </summary>
    private bool PrecededByAMoveTheOtherWay(CandlePatternStrategySettings settings, MyData? previous)
    {
        if (settings.PrecedingCandles <= 0)
            return true;

        MyData? walk = previous;
        for (int step = 0; step < settings.PrecedingCandles; step++)
        {
            if (!GetPrevCandle(walk, out walk))
            {
                ExtraText = "not enough candles for the preceding move";
                return false;
            }
        }

        decimal from = walk!.Candle.Close;
        decimal to = CandleLast!.Candle.Close;
        if (from <= 0)
            return false;

        // Positive when price moved AGAINST the trade over those candles: down before a long, up
        // before a short. That is what makes it a reversal rather than a continuation.
        decimal moved = 100m * (SignalSide == CryptoTradeSide.Long ? from - to : to - from) / from;
        if (moved < settings.PrecedingPercentage || moved <= 0)
        {
            ExtraText = $"price did not run against the trade first ({moved:N2}%)";
            return false;
        }

        return true;
    }
}


public class CandlePatternLong : CandlePatternBase
{
}


public class CandlePatternShort : CandlePatternBase
{
}
