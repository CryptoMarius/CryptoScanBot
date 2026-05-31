using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Storsi;

namespace CryptoScanner.Core.Signal.StorsiDlz;

/// <summary>
/// Combined signal: STORSI.MULTI (Stochastic + RSI oversold with higher-timeframe confirmation)
/// firing while price shows a confirmed rejection off a long DLZ zone. Inherits the full
/// STORSI multi-timeframe pipeline from <see cref="SignalStoRsiMultiLong"/> so the higher-TF
/// confirmation, AdditionalChecks and trend filters all keep applying. See
/// <see cref="SignalStoRsiDlzLong"/> for the single-TF version.
/// </summary>
public class SignalStoRsiMultiDlzLong : SignalStoRsiMultiLong
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
