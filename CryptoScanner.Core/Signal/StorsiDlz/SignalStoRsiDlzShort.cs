using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Storsi;

namespace CryptoScanner.Core.Signal.StorsiDlz;

/// <summary>
/// Combined signal: STORSI (Stochastic + RSI overbought) firing while price is approaching
/// or inside a short DLZ zone. See <see cref="SignalStoRsiDlzLong"/> for the design notes.
/// </summary>
public class SignalStoRsiDlzShort : SignalStoRsiShort
{
    public override bool IsSignal()
    {
        ExtraText = "";

        if (!this.IsNearDlzZone(out string zoneInfo))
        {
            ExtraText = "no nearby dlz zone";
            return false;
        }

        if (!base.IsSignal())
            return false;

        ExtraText = $"storsi+{zoneInfo}";
        return true;
    }
}
