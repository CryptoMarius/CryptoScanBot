using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Stobb;

namespace CryptoScanner.Core.Signal.StobbSmc;

/// <summary>
/// Combined signal: STOBB.MULTI (overbought Bollinger Band tag with higher-timeframe confirmation)
/// firing while price shows a confirmed rejection off a short SMC Order Block (supply zone).
/// See <see cref="SignalStobbMultiSmcLong"/> for the design notes.
/// </summary>
public class SignalStobbMultiSmcShort : SignalStobbMultiShort
{
    public override bool IsSignal()
    {
        ExtraText = "";

        if (!this.WasRejectedAtSmcZone(out string zoneInfo))
        {
            ExtraText = "no smc rejection";
            return false;
        }

        if (!base.IsSignal())
            return false;

        ExtraText = $"stobb.multi+{zoneInfo}";
        return true;
    }
}
