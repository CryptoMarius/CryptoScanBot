namespace CryptoScanner.Core.Signal.Helpers;

public static class KeltnerChannelHelper
{
    // TTM Squeeze condition: Bollinger Bands sit fully inside the Keltner Channel.
    // BB.upper <= KC.upper AND BB.lower >= KC.lower. Returns false if any band is missing.
    // Disabled: Keltner fields are commented out in CryptoData.cs; un-comment the fields there
    // AND the keltnerList calculation in IndicatorData.cs to re-enable this helper.
    //public static bool IsKeltnerSqueeze(this MyData data)
    //{
    //#if DEBUG
    //    double? bbUpper = data.CandleData!.BollingerBandsUpperBand;
    //    double? bbLower = data.CandleData!.BollingerBandsLowerBand;
    //    double? kcUpper = data.CandleData!.KeltnerUpperBand;
    //    double? kcLower = data.CandleData!.KeltnerLowerBand;
    //    if (!bbUpper.HasValue || !bbLower.HasValue || !kcUpper.HasValue || !kcLower.HasValue)
    //        return false;
    //    return bbUpper.Value <= kcUpper.Value && bbLower.Value >= kcLower.Value;
    //#else
    //    return false;
    //#endif
    //}
}
