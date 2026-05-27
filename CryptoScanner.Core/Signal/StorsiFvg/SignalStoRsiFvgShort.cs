using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Storsi;

namespace CryptoScanner.Core.Signal.StorsiFvg;

/// <summary>
/// Combined signal: STORSI (Stochastic + RSI overbought) firing while price is inside a short
/// FVG zone. See <see cref="StorsiDlz.SignalStoRsiDlzLong"/> for the design notes.
/// </summary>
public class SignalStoRsiFvgShort : SignalStoRsiShort
{
    public override bool IsSignal()
    {
        ExtraText = "";

        // Require a rejection wick off the FVG zone, not just "price inside".
        if (!this.WasRejectedAtFvgZone(out string zoneInfo))
        {
            ExtraText = "no fvg rejection";
            return false;
        }

        if (!base.IsSignal())
            return false;

        ExtraText = $"storsi+{zoneInfo}";
        return true;
    }
}
