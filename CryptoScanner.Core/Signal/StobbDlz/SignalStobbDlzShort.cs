using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Stobb;

namespace CryptoScanner.Core.Signal.StobbDlz;

/// <summary>
/// Combined signal: STOBB (overbought Bollinger Band tag with Stoch) firing while price is
/// inside a short DLZ zone. See <see cref="SignalStobbDlzLong"/> for the design notes.
/// </summary>
public class SignalStobbDlzShort : SignalStobbShort
{
    public override bool IsSignal()
    {
        ExtraText = "";

        // Require a rejection wick off the DLZ zone, not just "price inside".
        if (!this.WasRejectedAtDlzZone(out string zoneInfo))
        {
            ExtraText = "no dlz rejection";
            return false;
        }

        if (!base.IsSignal())
            return false;

        ExtraText = $"stobb+{zoneInfo}";
        return true;
    }
}
