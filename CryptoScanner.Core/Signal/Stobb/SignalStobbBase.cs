using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Sbm;

namespace CryptoScanner.Core.Signal.Stobb;

public class SignalStobbBase : SignalSbmBase
{
    /// <summary>
    /// Verifies the optional DLZ / FVG / SMC zone-rejection filters from the STOBB settings.
    /// When none of the three is enabled the check is skipped (returns true).
    /// When one or more are enabled the candle must have produced a rejection wick on at
    /// least one of the enabled zone types (OR). The matched zone description is written to
    /// <paramref name="zoneInfo"/>; on failure a "no … rejection" reason is written instead.
    /// </summary>
    protected bool CheckEnabledZoneRejections(out string zoneInfo)
    {
        var settings = GlobalData.Settings.Signal.Stobb;
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


    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Sma20 == null
           || data.CandleData.StochSignal == null
           || data.CandleData.StochOscillator == null
           || data.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }


    public override bool GiveUp(CryptoSignal signal)
    {
        if (base.GiveUp(signal))
            return true;

        switch (SignalSide)
        {
            case Enums.CryptoTradeSide.Long:
                if (CandleLast?.Candle.Close > (decimal?)CandleLast?.CandleData?.Sma20)
                {
                    ExtraText = "Close above sma20";
                    return true;
                }
                break;
            case Enums.CryptoTradeSide.Short:
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
