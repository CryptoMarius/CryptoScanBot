using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Storsi;

namespace CryptoScanner.Core.Signal.StorsiDlz;

/// <summary>
/// Combined signal: STORSI (Stochastic + RSI oversold) firing while price is approaching
/// or inside a long DLZ zone.
///
/// Inherits the full STORSI pipeline (BB width / Stoch / RSI / trend / AdditionalChecks)
/// from <see cref="SignalStoRsiLong"/> so every existing STORSI setting (Lux 5m, SkipFirstSignal,
/// CheckBollingerBandsCondition, trend filters …) keeps applying. The only addition is the
/// zone-proximity gate, run first because it short-circuits cheaply when no zone is nearby.
///
/// Zones themselves are owned by <see cref="Dlz.SignalDominantLevelNearLong"/> — this class
/// only reads the precomputed state, it never closes or alarm-flags a zone.
/// </summary>
public class SignalStoRsiDlzLong : SignalStoRsiLong
{
    public override bool IsSignal()
    {
        ExtraText = "";

        // Cheap gate first: most candles are nowhere near a zone, so this eliminates them
        // before we run the indicator-heavy STORSI checks.
        if (!this.IsNearDlzZone(out string zoneInfo))
        {
            ExtraText = "no nearby dlz zone";
            return false;
        }

        // Full STORSI pipeline (BB width, Stoch oversold, RSI oversold, optional trend filters).
        if (!base.IsSignal())
            return false;

        ExtraText = $"storsi+{zoneInfo}";
        return true;
    }
}
