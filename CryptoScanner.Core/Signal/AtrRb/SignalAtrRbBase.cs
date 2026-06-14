using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.AtrRb;

/// <summary>
/// Shared base for the AtrRb long/short signals. Adds the optional DLZ / FVG / SMC zone-rejection
/// confluence filter (the three checkboxes in the AtrRb settings), mirroring SignalStoRsiBase.
/// </summary>
public class SignalAtrRbBase : SignalCreateBase
{
    /// <summary>
    /// Verifies the optional DLZ / FVG / SMC zone-rejection filters from the AtrRb settings.
    /// When none of the three is enabled the check is skipped (returns true). When one or more are
    /// enabled the candle must have produced a rejection on at least one of the enabled zone types
    /// (OR). The matched zone description is written to <paramref name="zoneInfo"/>; on failure a
    /// "no … rejection" reason is written instead.
    /// </summary>
    protected bool CheckEnabledZoneRejections(out string zoneInfo)
    {
        var settings = GlobalData.Settings.Signal.AtrRb;
        if (!settings.UseDlzZone && !settings.UseFvgZone && !settings.UseSmcZone)
        {
            zoneInfo = "";
            return true;
        }

        if (settings.UseDlzZone && this.WasRejectedAtDlzZone(out string dlzInfo))
        {
            zoneInfo = dlzInfo;
            return true;
        }
        if (settings.UseFvgZone && this.WasRejectedAtFvgZone(out string fvgInfo))
        {
            zoneInfo = fvgInfo;
            return true;
        }
        if (settings.UseSmcZone && this.WasRejectedAtSmcZone(out string smcInfo))
        {
            zoneInfo = smcInfo;
            return true;
        }

        zoneInfo = "no zone rejection (dlz/fvg/smc)";
        return false;
    }
}
