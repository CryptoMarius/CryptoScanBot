using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Storsi;

namespace CryptoScanner.Core.Signal.StorsiDlz;

/// <summary>
/// Combined signal: STORSI.MULTI (Stochastic + RSI overbought with higher-timeframe confirmation)
/// firing while price shows a confirmed rejection off a short DLZ zone. See
/// <see cref="SignalStoRsiMultiDlzLong"/> for the design notes.
/// </summary>
public class SignalStoRsiMultiDlzShort : SignalStoRsiMultiShort
{
    public override bool IsSignal()
    {
        ExtraText = "";

        if (!base.IsSignal())
            return false;

        if (!this.WasRejectedAtDlzZone(out string zoneInfo))
        {
            ExtraText = "no dlz rejection";
            return false;
        }

        ExtraText = $"storsi.multi+{zoneInfo}";
        return true;
    }
}
