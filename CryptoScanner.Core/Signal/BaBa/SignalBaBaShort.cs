using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.BaBa;

/// <summary>
/// "baba" algorithm — fires a (short) alert when price hits the macro upper band of the
/// BaBa Bands construction, i.e. the exact moment the chart prints its upper-band percentage
/// label. The reported percentage matches the chart label.
/// </summary>
public class SignalBaBaShort : SignalCreateBase
{
    public override bool IsSignal()
    {
        ExtraText = "";

        if (!BaBaBandsHelper.IsUpperBandBreak(SymbolInterval, CandleLast.Candle.OpenTime, out double pctDeviation, out _))
        {
            ExtraText = "no upper band break";
            return false;
        }

        ExtraText = $"BaBa upper band hit {pctDeviation:N2}%";
        return true;
    }
}
