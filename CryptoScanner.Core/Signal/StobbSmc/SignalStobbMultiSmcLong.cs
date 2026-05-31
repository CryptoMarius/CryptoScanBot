using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Stobb;

namespace CryptoScanner.Core.Signal.StobbSmc;

/// <summary>
/// Combined signal: STOBB.MULTI (oversold Bollinger Band tag with higher-timeframe confirmation)
/// firing while price shows a confirmed rejection off a long SMC Order Block (demand zone).
/// Inherits the full STOBB multi-timeframe pipeline from <see cref="SignalStobbMultiLong"/>.
/// </summary>
public class SignalStobbMultiSmcLong : SignalStobbMultiLong
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
