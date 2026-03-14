using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Experiment;

#if DEBUG
/// <summary>
/// BBMA Reentry Long signal (Oma Ally method).
/// Detects when price pulls back into the 510 buy zone (between LWMA5 and LWMA10 on lows)
/// after a bullish CSD crossover (LWMA5 crossed above LWMA10).
/// </summary>
public class SignalBbmaReentryOldLong : SignalBbmaBase
{


    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Wma05Low == null
           || data.CandleData.Wma10Low == null
           || data.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }


    public override string DisplayText()
    {
        return string.Format("wma5.low={0:N8} wma10.low={1:N8}",
            CandleLast.CandleData!.Wma05Low,
            CandleLast.CandleData!.Wma10Low
        );
    }


    /// <summary>
    /// Scans backward to find a bullish CSD crossover:
    /// LWMA5 (lows) crossed above LWMA10 (lows) within the last maxLookback candles.
    /// Returns true when found; candlesAgo indicates how many steps back the crossover occurred.
    /// </summary>
    private bool FindRecentCsd(int maxLookback, out int candlesAgo)
    {
        candlesAgo = 0;
        MyData? later = CandleLast;

        for (int i = 0; i < maxLookback; i++)
        {
            // GetPrevCandle also calls IndicatorsOkay internally
            if (!GetPrevCandle(later, out MyData? earlier))
                return false;

            double laterWma5 = later!.CandleData!.Wma05Low!.Value;
            double laterWma10 = later.CandleData!.Wma10Low!.Value;
            double earlierWma5 = earlier!.CandleData!.Wma05Low!.Value;
            double earlierWma10 = earlier.CandleData!.Wma10Low!.Value;

            // Bullish CSD: LWMA5 was at or below LWMA10, then crossed above
            if (earlierWma5 <= earlierWma10 && laterWma5 > laterWma10)
            {
                candlesAgo = i + 1;
                return true;
            }

            later = earlier;
        }

        return false;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }


        double? wma05Low = CandleLast.CandleData!.Wma05Low;
        double? wma10Low = CandleLast.CandleData!.Wma10Low;

        if (wma05Low == null || wma10Low == null)
            return false;

        // Step 1: Confirm the bullish CSD is currently active (LWMA5 above LWMA10)
        if (wma05Low <= wma10Low)
        {
            ExtraText = "wma5 not above wma10 (no active bullish CSD)";
            return false;
        }

        // Step 2: Find a CSD crossover within the recent past
        if (!FindRecentCsd(15, out int candlesAgo))
        {
            ExtraText = "no recent CSD (bullish crossover) within 15 candles";
            return false;
        }

        // Step 3: Price must be pulling back into the 510 buy zone
        // Buy zone: between LWMA10 (bottom) and LWMA5 (top) on lows
        decimal close = CandleLast.Candle.Close;
        decimal zoneBottom = (decimal)wma10Low;
        decimal zoneTop = (decimal)wma05Low;

        if (close < zoneBottom || close > zoneTop)
        {
            ExtraText = $"not in buy zone [{zoneBottom:N4}-{zoneTop:N4}], close={close:N4}";
            return false;
        }

        ExtraText = $"reentry in 510 zone (CSD {candlesAgo} candles ago)";
        return true;
    }
}
#endif
