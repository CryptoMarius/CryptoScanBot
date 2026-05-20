#if DEBUG
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Squeeze;

// Squeeze.Brk (long): classic TTM Squeeze breakout. The bands were compressed
// (squeeze) for at least N candles in the lookback window, the squeeze has now
// released (current candle is NOT in squeeze), and momentum kicks up via a fresh
// Stoch %K-over-%D cross. We ride the released energy in the cross direction.
public class SignalSqueezeBrkLong : SignalSqueezeBase
{
    public override bool IsSignal()
    {
        ExtraText = "";
        var settings = GlobalData.Settings.Signal.Squeeze;

        // ---- Cheap checks first ----

        // BB width sanity (drop pathological cases).
        if (!CandleLast.CheckBollingerBandsWidth(settings.BBMinPercentage, settings.BBMaxPercentage))
        {
            ExtraText = $"bb.width out of range {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // Squeeze must have RELEASED — current candle should no longer be a squeeze.
        if (CandleLast.IsKeltnerSqueeze())
        {
            ExtraText = "still in squeeze (no release yet)";
            return false;
        }

        // Momentum kick: current %K > %D (bullish state on this bar).
        if (CandleLast.CandleData!.StochOscillator <= CandleLast.CandleData!.StochSignal)
        {
            ExtraText = "stoch %K not above %D";
            return false;
        }

        // ---- prev candle for fresh cross ----
        if (!GetPrevCandle(CandleLast, out MyData? prev) || prev == null)
            return false;
        if (prev.CandleData!.StochOscillator > prev.CandleData!.StochSignal)
        {
            ExtraText = "no fresh stoch bull cross";
            return false;
        }

        // ---- Walk back: prior squeeze must have built up ----
        ScanSqueeze(settings.BrkReleaseLookback, out int squeezeCount, out int lastSqueezeOffset);
        if (squeezeCount < settings.BrkReleaseMinCandles)
        {
            ExtraText = $"only {squeezeCount} squeeze candles in last {settings.BrkReleaseLookback} (need {settings.BrkReleaseMinCandles})";
            return false;
        }

        ExtraText = $"squeeze {squeezeCount}/{settings.BrkReleaseLookback} candles, released {lastSqueezeOffset} ago";

        // ---- Trend filter last ----
        if (settings.CheckTrendPrimaryDirection && !CheckTrendPrimary(settings.TrendPrimaryDirectionCount))
            return false;
        if (settings.CheckTrendSecondaryDirection && !CheckTrendSecondary(settings.TrendSecondaryDirectionCount))
            return false;

        return true;
    }
}
#endif
