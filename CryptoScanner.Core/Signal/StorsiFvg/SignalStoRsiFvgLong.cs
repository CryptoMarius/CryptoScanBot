using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Storsi;

namespace CryptoScanner.Core.Signal.StorsiFvg;

/// <summary>
/// Combined signal: STORSI (Stochastic + RSI oversold) firing while price is inside a long
/// FVG zone. See <see cref="StorsiDlz.SignalStoRsiDlzLong"/> for the design notes.
/// FVG zones are owned by <see cref="Fvg.SignalFairValueGapLong"/>; this class only reads them.
/// </summary>
public class SignalStoRsiFvgLong : SignalStoRsiLong
{
    public override bool IsSignal()
    {
        ExtraText = "";

        if (!this.IsInsideFvgZone(out string zoneInfo))
        {
            ExtraText = "not inside fvg zone";
            return false;
        }

        if (!base.IsSignal())
            return false;

        ExtraText = $"storsi+{zoneInfo}";
        return true;
    }
}
