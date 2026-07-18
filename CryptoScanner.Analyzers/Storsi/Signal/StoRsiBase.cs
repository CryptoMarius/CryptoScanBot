using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Storsi.Signal;

// WGHM - Wave Generation High Momentum
// Shared base for all StoRsi variants (single + multi, long + short).
//
// Inherits directly from SignalCreateBase, not from SignalSbmBase, because StoRsi does not
// share any of the SBM-specific pipeline checks (MACD recovery, MA-percentage filters,
// MA-crossings). Going through SbmBase would silently re-introduce those checks via
// inherited AdditionalChecks/GiveUp.
public class StoRsiBase : SignalCreateBase
{
    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Rsi == null
           || data.CandleData.StochSignal == null
           || data.CandleData.StochOscillator == null
           || data.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }

    /// <summary>
    /// Verifies the optional DLZ / FVG / SMC zone-rejection filters from the STORSI settings.
    /// When none of the three is enabled the check is skipped (returns true).
    /// When one or more are enabled the candle must have produced a rejection wick on at
    /// least one of the enabled zone types (OR). The matched zone description is written to
    /// <paramref name="zoneInfo"/>; on failure a "no … rejection" reason is written instead.
    /// </summary>
    protected bool CheckEnabledZoneRejections(out string zoneInfo)
    {
        var settings = StoRsiPlugin.Settings;
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

    public override bool GiveUp(CryptoSignal signal)
    {
        if (base.GiveUp(signal))
            return true;

        switch (SignalSide)
        {
            case CryptoTradeSide.Long:
                if (CandleLast?.Candle.Close > (decimal?)CandleLast?.CandleData?.Sma20)
                {
                    ExtraText = "Close above sma20";
                    return true;
                }
                break;
            case CryptoTradeSide.Short:
                if (CandleLast!.Candle.Close < (decimal?)CandleLast!.CandleData?.Sma20)
                {
                    ExtraText = "Close below sma20";
                    return true;
                }
                break;
        }

        ExtraText = "";
        return false;
    }
}
