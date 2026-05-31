using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Stobb;

namespace CryptoScanner.Core.Signal.StobbDlz;

/// <summary>
/// Combined signal: STOBB.MULTI (oversold Bollinger Band tag with higher-timeframe confirmation)
/// firing while price shows a confirmed rejection off a long DLZ zone. Inherits the full
/// STOBB multi-timeframe pipeline from <see cref="SignalStobbMultiLong"/>. See
/// <see cref="SignalStobbDlzLong"/> for the single-TF version.
/// </summary>
public class SignalStobbMultiDlzLong : SignalStobbMultiLong
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
