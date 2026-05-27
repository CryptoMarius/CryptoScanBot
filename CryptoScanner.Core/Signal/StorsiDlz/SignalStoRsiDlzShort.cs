using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Storsi;

namespace CryptoScanner.Core.Signal.StorsiDlz;

/// <summary>
/// Combined signal: STORSI (Stochastic + RSI overbought) firing while price is inside a short
/// DLZ zone. See <see cref="SignalStoRsiDlzLong"/> for the design notes.
/// </summary>
public class SignalStoRsiDlzShort : SignalStoRsiShort
{
    public override bool IsSignal()
    {
        ExtraText = "";

        if (!base.IsSignal())
            return false;

        // Require a rejection wick off the DLZ zone, not just "price inside".
        if (!this.WasRejectedAtDlzZone(out string zoneInfo))
        {
            ExtraText = "no dlz rejection";
            return false;
        }

        ExtraText = $"storsi+{zoneInfo}";
        return true;
    }
}
