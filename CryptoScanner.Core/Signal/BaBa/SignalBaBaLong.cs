using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.BaBa;

/// <summary>
/// "baba" algorithm — fires a (long) alert when price hits the macro lower band of the
/// BaBa Bands construction, i.e. the exact moment the chart prints its lower-band percentage
/// label. The reported percentage matches the chart label.
/// </summary>
public class SignalBaBaLong : SignalCreateBase
{
    public override bool IsSignal()
    {
        ExtraText = "";

        if (!BaBaBandsHelper.IsLowerBandBreak(SymbolInterval, CandleLast.Candle.OpenTime, out double pctDeviation, out _))
        {
            ExtraText = "no lower band break";
            return false;
        }

        ExtraText = $"BaBa lower band hit {pctDeviation:N2}%";
        return true;
    }
}
