using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Storsi;

namespace CryptoScanner.Core.Signal.StorsiSmc;

/// <summary>
/// Combined signal: STORSI.MULTI (Stochastic + RSI oversold with higher-timeframe confirmation)
/// firing while price shows a confirmed rejection off a long SMC Order Block (demand zone).
/// Inherits the full STORSI multi-timeframe pipeline from <see cref="SignalStoRsiMultiLong"/>
/// so the higher-TF confirmation, AdditionalChecks and trend filters all keep applying.
/// </summary>
public class SignalStoRsiMultiSmcLong : SignalStoRsiMultiLong
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

        ExtraText = $"storsi.multi+{zoneInfo}";
        return true;
    }
}
