using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Bbma;

#if DEBUG
/// <summary>
/// BBMA Reentry Short signal (Oma Ally method).
/// Detects when price pulls back into the 510 sell zone (between LWMA5 and LWMA10 on highs)
/// after a bearish CSD crossover (LWMA5 crossed below LWMA10).
/// </summary>
public class SignalBbmaReentryOldShort : SignalBbmaBase
{


    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Wma05High == null
           || data.CandleData.Wma10High == null
           || data.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }


    public override string DisplayText()
    {
        return string.Format("wma5.high={0:N8} wma10.high={1:N8}",
            CandleLast.CandleData!.Wma05High,
            CandleLast.CandleData!.Wma10High
        );
    }


    /// <summary>
    /// Scans backward to find a bearish CSD crossover:
    /// LWMA5 (highs) crossed below LWMA10 (highs) within the last maxLookback candles.
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

            double laterWma5 = later!.CandleData!.Wma05High!.Value;
            double laterWma10 = later.CandleData!.Wma10High!.Value;
            double earlierWma5 = earlier!.CandleData!.Wma05High!.Value;
            double earlierWma10 = earlier.CandleData!.Wma10High!.Value;

            // Bearish CSD: LWMA5 was at or above LWMA10, then crossed below
            if (earlierWma5 >= earlierWma10 && laterWma5 < laterWma10)
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
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        double? wma05High = CandleLast.CandleData!.Wma05High;
        double? wma10High = CandleLast.CandleData!.Wma10High;

        if (wma05High == null || wma10High == null)
            return false;

        // Step 1: Confirm the bearish CSD is currently active (LWMA5 below LWMA10)
        if (wma05High >= wma10High)
        {
            ExtraText = "wma5 not below wma10 (no active bearish CSD)";
            return false;
        }

        // Step 2: Find a CSD crossover within the recent past
        if (!FindRecentCsd(15, out int candlesAgo))
        {
            ExtraText = "no recent CSD (bearish crossover) within 15 candles";
            return false;
        }

        // Step 3: Price must be pulling back into the 510 sell zone
        // Sell zone: between LWMA5 (bottom) and LWMA10 (top) on highs
        decimal close = CandleLast.Candle.Close;
        decimal zoneBottom = (decimal)wma05High;
        decimal zoneTop = (decimal)wma10High;

        if (close < zoneBottom || close > zoneTop)
        {
            ExtraText = $"not in sell zone [{zoneBottom:N4}-{zoneTop:N4}], close={close:N4}";
            return false;
        }

        ExtraText = $"reentry in 510 zone (CSD {candlesAgo} candles ago)";
        return true;
    }
}
#endif
