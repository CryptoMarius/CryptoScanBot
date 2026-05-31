using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Stobb;

namespace CryptoScanner.Core.Signal.StobbDlz;

/// <summary>
/// Combined signal: STOBB.MULTI (overbought Bollinger Band tag with higher-timeframe confirmation)
/// firing while price shows a confirmed rejection off a short DLZ zone. See
/// <see cref="SignalStobbMultiDlzLong"/> for the design notes.
/// </summary>
public class SignalStobbMultiDlzShort : SignalStobbMultiShort
{
    public override bool IsSignal()
    {
        ExtraText = "";

        if (!this.WasRejectedAtDlzZone(out string zoneInfo))
        {
            ExtraText = "no dlz rejection";
            return false;
        }

        if (!base.IsSignal())
            return false;

        ExtraText = $"stobb.multi+{zoneInfo}";
        return true;
    }
}
