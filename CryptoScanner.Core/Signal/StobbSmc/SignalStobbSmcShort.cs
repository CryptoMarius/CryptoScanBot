using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Stobb;

namespace CryptoScanner.Core.Signal.StobbSmc;

/// <summary>
/// Combined signal: STOBB (overbought Bollinger Band tag with Stoch) firing while price shows
/// a confirmed rejection off a short SMC Order Block (supply zone). See
/// <see cref="SignalStobbSmcLong"/> for the design notes.
/// </summary>
public class SignalStobbSmcShort : SignalStobbShort
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

        ExtraText = $"stobb+{zoneInfo}";
        return true;
    }
}
