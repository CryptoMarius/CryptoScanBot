using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal.Helpers;

// Detects RSI divergence: price makes a new extreme but RSI does not confirm it.
// Bullish divergence: price lower low, RSI higher low  → reversal up
// Bearish divergence: price higher high, RSI lower high → reversal down

namespace CryptoScanner.Core.Signal.Experiment;

public class SignalRsiDivergence : SignalCreateBase
{
    // Number of candles to look back when searching for the previous swing point
    private const int SwingLookback = 30;

    // Minimum RSI divergence to avoid triggering on noise
    private const double MinRsiDivergence = 3.0;

    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
            || data.Candle.OpenTime == 0
            || data.CandleData == null
            || data.CandleData.Sma20 == null
            || data.CandleData.Rsi == null)
            return false;

        return true;
    }

    public override bool IsSignal()
    {
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.StoRsi.BBMinPercentage, GlobalData.Settings.Signal.StoRsi.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        double? rsiNow = CandleLast.CandleData!.Rsi;
        if (rsiNow == null)
            return false;

        if (SignalSide == CryptoTradeSide.Long)
            return CheckBullishDivergence(rsiNow.Value);
        else
            return CheckBearishDivergence(rsiNow.Value);
    }

    /// <summary>
    /// Bullish divergence: current price makes a lower low than the previous swing low,
    /// but RSI makes a higher low. Suggests selling pressure is weakening.
    /// </summary>
    private bool CheckBullishDivergence(double rsiNow)
    {
        // RSI must be in oversold territory to be meaningful
        if (rsiNow > 45.0)
        {
            ExtraText = $"rsi not oversold: {rsiNow:N1}";
            return false;
        }

        // Find the previous swing low (local price minimum) in recent history
        if (!FindPreviousSwingLow(out MyData? swingLow, out decimal swingLowPrice))
            return false;

        double? rsiAtSwing = swingLow!.CandleData?.Rsi;
        if (rsiAtSwing == null)
            return false;

        decimal priceNow = CandleLast.Candle.Close;

        // Price must make a lower low than the previous swing
        bool priceLowerLow = priceNow < swingLowPrice;

        // RSI must make a higher low than at the previous swing (divergence)
        bool rsiHigherLow = rsiNow > rsiAtSwing.Value + MinRsiDivergence;

        if (!priceLowerLow || !rsiHigherLow)
        {
            ExtraText = $"no divergence: price {priceNow:N2} vs {swingLowPrice:N2}, rsi {rsiNow:N1} vs {rsiAtSwing:N1}";
            return false;
        }

        // Confirmation: current candle must be bullish (reversal starting)
        if (CandleLast.Candle.Close <= CandleLast.Candle.Open)
        {
            ExtraText = $"no bullish candle: rsi {rsiNow:N1} vs {rsiAtSwing:N1}";
            return false;
        }

        ExtraText = $"rsi {rsiAtSwing.Value:N1}→{rsiNow:N1} price {swingLowPrice:N2}→{priceNow:N2}";
        return true;
    }

    /// <summary>
    /// Bearish divergence: current price makes a higher high than the previous swing high,
    /// but RSI makes a lower high. Suggests buying pressure is weakening.
    /// </summary>
    private bool CheckBearishDivergence(double rsiNow)
    {
        // RSI must be in overbought territory to be meaningful
        if (rsiNow < 55.0)
        {
            ExtraText = $"rsi not overbought: {rsiNow:N1}";
            return false;
        }

        // Find the previous swing high (local price maximum) in recent history
        if (!FindPreviousSwingHigh(out MyData? swingHigh, out decimal swingHighPrice))
            return false;

        double? rsiAtSwing = swingHigh!.CandleData?.Rsi;
        if (rsiAtSwing == null)
            return false;

        decimal priceNow = CandleLast.Candle.Close;

        // Price must make a higher high than the previous swing
        bool priceHigherHigh = priceNow > swingHighPrice;

        // RSI must make a lower high (divergence)
        bool rsiLowerHigh = rsiNow < rsiAtSwing.Value - MinRsiDivergence;

        if (!priceHigherHigh || !rsiLowerHigh)
        {
            ExtraText = $"no divergence: price {priceNow:N2} vs {swingHighPrice:N2}, rsi {rsiNow:N1} vs {rsiAtSwing:N1}";
            return false;
        }

        // Confirmation: current candle must be bearish
        if (CandleLast.Candle.Close >= CandleLast.Candle.Open)
        {
            ExtraText = $"no bearish candle: rsi {rsiNow:N1} vs {rsiAtSwing:N1}";
            return false;
        }

        ExtraText = $"rsi {rsiAtSwing.Value:N1}→{rsiNow:N1} price {swingHighPrice:N2}→{priceNow:N2}";
        return true;
    }

    // ── Swing point detection ────────────────────────────────────────────────

    /// <summary>
    /// Walks back through candles to find the most recent local price minimum:
    /// a candle whose close is lower than both its neighbours.
    /// </summary>
    private bool FindPreviousSwingLow(out MyData? swingLow, out decimal swingLowPrice)
    {
        swingLow = null;
        swingLowPrice = decimal.MaxValue;

        // Sliding window: right → pivot → left (right is more recent)
        if (!GetPrevCandle(CandleLast, out MyData? right))
            return false;

        if (!GetPrevCandle(right!, out MyData? pivot))
            return false;

        if (!GetPrevCandle(pivot!, out MyData? left))
            return false;

        for (int i = 0; i < SwingLookback; i++)
        {
            // pivot is a local low if its close is below both neighbours
            if (pivot!.Candle.Close < right!.Candle.Close &&
                pivot.Candle.Close < left!.Candle.Close)
            {
                swingLow = pivot;
                swingLowPrice = pivot.Candle.Close;
                return true;
            }

            // Slide one candle further into the past
            right = pivot;
            pivot = left;
            if (!GetPrevCandle(left!, out left))
                break;
        }

        return false;
    }

    /// <summary>
    /// Walks back through candles to find the most recent local price maximum:
    /// a candle whose close is higher than both its neighbours.
    /// </summary>
    private bool FindPreviousSwingHigh(out MyData? swingHigh, out decimal swingHighPrice)
    {
        swingHigh = null;
        swingHighPrice = decimal.MinValue;

        // Sliding window: right → pivot → left (right is more recent)
        if (!GetPrevCandle(CandleLast, out MyData? right))
            return false;

        if (!GetPrevCandle(right!, out MyData? pivot))
            return false;

        if (!GetPrevCandle(pivot!, out MyData? left))
            return false;

        for (int i = 0; i < SwingLookback; i++)
        {
            // pivot is a local high if its close is above both neighbours
            if (pivot!.Candle.Close > right!.Candle.Close &&
                pivot.Candle.Close > left!.Candle.Close)
            {
                swingHigh = pivot;
                swingHighPrice = pivot.Candle.Close;
                return true;
            }

            // Slide one candle further into the past
            right = pivot;
            pivot = left;
            if (!GetPrevCandle(left!, out left))
                break;
        }

        return false;
    }
}