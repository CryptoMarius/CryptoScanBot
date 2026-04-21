using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Experiment;

/// <summary>
/// GaussianScalp Long — uses the STD-Filtered N-Pole Gaussian Filter [Loxx] for trend detection.
///
/// Signal fires when the filtered Gaussian output just flipped bullish (goLong),
/// meaning the current bar rose while the previous bar was a local trough,
/// AND the previously confirmed direction was bearish (contsw == -1).
///
/// Additional filters (applied before the Gaussian calculation):
///   - RSI(30) > 50 (momentum confirmation)
///   - MACD(24/52/9) histogram > 0 (trend validation)
/// </summary>
public class SignalGaussianScalpLong : SignalGaussianScalpBase
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

        // --- RSI(30) > 50: momentum confirmation ---
        double rsi30 = CandleLast.CandleData.Rsi30!.Value;
        if (rsi30 <= 50)
        {
            ExtraText = $"RSI30 {rsi30:N1} <= 50";
            return false;
        }

        // --- MACD(24/52/9) histogram > 0: trend validation ---
        double macdHist = CandleLast.CandleData.MacdHistogram24!.Value;
        if (macdHist <= 0)
        {
            ExtraText = $"MACD24 hist {macdHist:N6} <= 0";
            return false;
        }

        // --- Gaussian filter: goLong signal ---
        if (!ComputeSignal(out bool goLong, out _))
        {
            ExtraText = "insufficient history for Gaussian filter";
            return false;
        }

        if (!goLong)
        {
            ExtraText = "no Gaussian goLong signal";
            return false;
        }

        ExtraText = $"G↑ RSI30={rsi30:N1}";
        return true;
    }


    /// <summary>
    /// Allow step-in only when the candle that follows the signal is convincingly bullish:
    /// close must be above the signal candle's close (price actually moved up).
    /// </summary>
    public override bool AllowStepIn(CryptoSignal signal)
    {
        if (!GetPrevCandle(CandleLast, out MyData? signalCandle))
            return false;

        // Current candle close must be above the signal candle's close
        if (CandleLast.Candle.Close <= signalCandle!.Candle.Close)
        {
            ExtraText = $"price not moving up: {CandleLast.Candle.Close:N8} <= {signalCandle.Candle.Close:N8}";
            return false;
        }

        // Current candle must be a bullish candle (close > open)
        if (CandleLast.Candle.Close <= CandleLast.Candle.Open)
        {
            ExtraText = "no bullish confirmation candle";
            return false;
        }

        return true;
    }


    /// <summary>
    /// Give up when the setup has not triggered within 2 candles after the signal,
    /// or when RSI(30) has dropped back below 50 (momentum invalidated).
    /// </summary>
    public override bool GiveUp(CryptoSignal signal)
    {
        // Abandon after 2 candles without entry
        if (CandleTime.FromDateTime(signal.CloseDate).Minutes + 2 * Interval.Duration < CandleLast.Candle.OpenTime.Minutes)
        {
            ExtraText = "give up after 2 candles";
            return true;
        }

        // RSI(30) dropped below 50 — momentum lost
        double? rsi30 = CandleLast.CandleData.Rsi30;
        if (rsi30 <= 50)
        {
            ExtraText = $"RSI30 {rsi30:N1} dropped below 50";
            return true;
        }

        // MACD histogram went negative — trend reversed
        double? macdHist = CandleLast.CandleData.MacdHistogram24;
        if (macdHist <= 0)
        {
            ExtraText = $"MACD24 hist {macdHist:N6} turned negative";
            return true;
        }

        return false;
    }
}
