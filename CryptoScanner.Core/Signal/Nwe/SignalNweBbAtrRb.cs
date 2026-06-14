using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Nwe;

/// <summary>
/// Combined NWE×BB + AtrRb strategy. Fires on the current (just-closed) candle when it is the SECOND
/// of an NWE×BB crossover and an AtrRb macro-band hit on the same side, occurring within
/// <see cref="NweBbAtrRbDetector.WindowCandles"/> candles of each other (order irrelevant).
///
/// One class serves both sides (Long/Short) via <see cref="SignalCreateBase.SignalSide"/>, the same way
/// SignalNwe is registered for long and short. Detection is delegated to <see cref="NweBbAtrRbDetector"/>
/// so the live signal and the chart overlay stay identical.
/// </summary>
public class SignalNweBbAtrRb : SignalCreateBase
{
    // How many recent candles to feed the detector. Must comfortably cover the AtrRb EMA/ATR + break
    // lookback, the NWE×BB lookback (~60) and the NWE repaint warm-up, plus the co-occurrence window.
    private const int WindowCandles = 350;

    public override bool IsSignal()
    {
        ExtraText = "";

        List<CryptoCandle> candles = SymbolInterval.CandleList.GetLastNValues(WindowCandles);
        if (candles.Count < 60)
        {
            ExtraText = "insufficient history for nwe.bb × atrrb";
            return false;
        }

        // FiresAt checks only the current (last) candle and gates the expensive NWE behind the cheap
        // AtrRb condition, so most candles cost only O(N) (EMA/ATR) instead of the O(N²) NWE.
        if (NweBbAtrRbDetector.FiresAt(candles, candles.Count - 1, SignalSide))
        {
            ExtraText = $"nwe.bb × atrrb {SignalSide} (within {NweBbAtrRbDetector.WindowCandles} candles)";
            return true;
        }

        return false;
    }
}
