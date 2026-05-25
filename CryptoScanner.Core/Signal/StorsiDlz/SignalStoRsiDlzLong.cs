using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Storsi;

namespace CryptoScanner.Core.Signal.StorsiDlz;

/// <summary>
/// Combined signal: STORSI (Stochastic + RSI oversold) firing while price is inside a long
/// DLZ zone. "Inside" means the candle's low has entered the zone and the close has not
/// broken below the zone's floor.
///
/// Inherits the full STORSI pipeline (BB width / Stoch / RSI / trend / AdditionalChecks)
/// from <see cref="SignalStoRsiLong"/> so every existing STORSI setting (Lux 5m, SkipFirstSignal,
/// CheckBollingerBandsCondition, trend filters …) keeps applying. The zone gate runs first
/// because it short-circuits cheaply when price is not inside any zone.
///
/// Zones themselves are owned by <see cref="Dlz.SignalDominantLevelNearLong"/> — this class
/// only reads the precomputed state, it never closes or alarm-flags a zone.
/// </summary>
public class SignalStoRsiDlzLong : SignalStoRsiLong
{
    public override bool IsSignal()
    {
        ExtraText = "";

        // Full STORSI pipeline (BB width, Stoch oversold, RSI oversold, optional trend filters).
        if (!base.IsSignal())
            return false;

        // Cheap gate first: skip candles where price is not inside any DLZ zone.
        if (!this.IsInsideDlzZone(out string zoneInfo))
        {
            ExtraText = "not inside dlz zone";
            return false;
        }

        ExtraText = $"storsi+{zoneInfo}";
        return true;
    }
}
