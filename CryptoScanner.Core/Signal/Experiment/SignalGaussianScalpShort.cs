using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Experiment;

/// <summary>
/// GaussianScalp Short — mirror of SignalGaussianScalpLong.
///
/// Signal fires when the filtered Gaussian output just flipped bearish (goShort),
/// meaning the current bar fell while the previous bar was a local peak,
/// AND the previously confirmed direction was bullish (contsw == +1).
///
/// Additional filters (applied before the Gaussian calculation):
///   - RSI(30) &lt; 50 (momentum confirmation)
///   - MACD(24/52/9) histogram &lt; 0 (trend validation)
/// </summary>
public class SignalGaussianScalpShort : SignalGaussianScalpBase
{
    public override bool IndicatorsOkay(MyData data)
    {
        if ((data == null)
           || data.Candle.OpenTime == 0
           || (data.CandleData == null)
            || (data.CandleData.Rsi30 == null)
            || (data.CandleData.MacdHistogram24 == null)
            )
            return false;

        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // --- RSI(30) < 50: momentum confirmation ---
        double rsi30 = CandleLast.CandleData.Rsi30!.Value;
        if (rsi30 >= 50)
        {
            ExtraText = $"RSI30 {rsi30:N1} >= 50";
            return false;
        }

        // --- MACD(24/52/9) histogram < 0: trend validation ---
        double macdHist = CandleLast.CandleData.MacdHistogram24!.Value;
        if (macdHist >= 0)
        {
            ExtraText = $"MACD24 hist {macdHist:N6} >= 0";
            return false;
        }

        // --- Gaussian filter: goShort signal ---
        if (!ComputeSignal(out _, out bool goShort))
        {
            ExtraText = "insufficient history for Gaussian filter";
            return false;
        }

        if (!goShort)
        {
            ExtraText = "no Gaussian goShort signal";
            return false;
        }

        ExtraText = $"G↓ RSI30={rsi30:N1}";
        return true;
    }


    /// <summary>
    /// Allow step-in only when the candle that follows the signal is convincingly bearish:
    /// close must be below the signal candle's close (price actually moved down).
    /// </summary>
    public override bool AllowStepIn(CryptoSignal signal)
    {
        if (!GetPrevCandle(CandleLast, out MyData? signalCandle))
            return false;

        // Current candle close must be below the signal candle's close
        if (CandleLast.Candle.Close >= signalCandle!.Candle.Close)
        {
            ExtraText = $"price not moving down: {CandleLast.Candle.Close:N8} >= {signalCandle.Candle.Close:N8}";
            return false;
        }

        // Current candle must be a bearish candle (close < open)
        if (CandleLast.Candle.Close >= CandleLast.Candle.Open)
        {
            ExtraText = "no bearish confirmation candle";
            return false;
        }

        return true;
    }


    /// <summary>
    /// Give up when the setup has not triggered within 2 candles after the signal,
    /// or when RSI(30) has risen back above 50 (momentum invalidated).
    /// </summary>
    public override bool GiveUp(CryptoSignal signal)
    {
        // Abandon after 2 candles without entry
        if (CandleTime.FromDateTime(signal.CloseDate).Minutes + 2 * Interval.Duration < CandleLast.Candle.OpenTime.Minutes)
        {
            ExtraText = "give up after 2 candles";
            return true;
        }

        // RSI(30) rose above 50 — bearish momentum lost
        double? rsi30 = CandleLast.CandleData.Rsi30;
        if (rsi30 >= 50)
        {
            ExtraText = $"RSI30 {rsi30:N1} risen above 50";
            return true;
        }

        // MACD histogram went positive — trend reversed
        double? macdHist = CandleLast.CandleData.MacdHistogram24;
        if (macdHist >= 0)
        {
            ExtraText = $"MACD24 hist {macdHist:N6} turned positive";
            return true;
        }

        return false;
    }
}
