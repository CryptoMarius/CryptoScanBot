using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Stobb;

namespace CryptoScanner.Core.Signal.StobbFvg;

/// <summary>
/// Combined signal: STOBB (oversold Bollinger Band tag with Stoch) firing while price is
/// inside a long FVG zone. See <see cref="StobbDlz.SignalStobbDlzLong"/> for the design notes.
/// FVG zones are owned by <see cref="Fvg.SignalFairValueGapLong"/>; this class only reads them.
/// </summary>
public class SignalStobbFvgLong : SignalStobbLong
{
    public override bool IsSignal()
    {
        ExtraText = "";

        if (!this.IsInsideFvgZone(out string zoneInfo))
        {
            ExtraText = "not inside fvg zone";
            return false;
        }

        if (!base.IsSignal())
            return false;

        ExtraText = $"stobb+{zoneInfo}";
        return true;
    }
}
