using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Stobb;

namespace CryptoScanner.Core.Signal.StobbFvg;

/// <summary>
/// Combined signal: STOBB.MULTI (oversold Bollinger Band tag with higher-timeframe confirmation)
/// firing while price shows a confirmed rejection off a long FVG zone. Inherits the full
/// STOBB multi-timeframe pipeline from <see cref="SignalStobbMultiLong"/>. See
/// <see cref="SignalStobbFvgLong"/> for the single-TF version.
/// </summary>
public class SignalStobbMultiFvgLong : SignalStobbMultiLong
{
    public override bool IsSignal()
    {
        ExtraText = "";

        if (!this.WasRejectedAtFvgZone(out string zoneInfo))
        {
            ExtraText = "no fvg rejection";
            return false;
        }

        if (!base.IsSignal())
            return false;

        ExtraText = $"stobb.multi+{zoneInfo}";
        return true;
    }
}
