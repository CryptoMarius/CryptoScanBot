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

        if (!this.IsInsideDlzZone(out string zoneInfo))
        {
            ExtraText = "not inside dlz zone";
            return false;
        }

        ExtraText = $"storsi+{zoneInfo}";
        return true;
    }
}
