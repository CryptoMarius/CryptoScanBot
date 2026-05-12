using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Experiment;

/// <summary>
/// Short (bearish) variant of the BbWickSma strategy.
/// All five conditions must be met:
///   0. The Bollinger Band width is within the configured range (not too narrow, not too wide).
///   1. The high-low price range over the last LookbackCandles candles is at least MinPriceRangePercentage.
///   2. Within the last LookbackCandles candles: a wick poked above the upper Bollinger Band
///      (price was rejected at the top, signalling bearish pressure).
///   3. The SMA20 slope is currently negative (declining momentum).
///   4. Within the last LookbackCandles candles: the close crossed from above to below the SMA50
///      (bearish SMA50 breakout confirms the trend shift).
/// </summary>
public class SignalBbWickSmaShort : SignalBbWickSmaBase
{
    public override bool IsSignal()
    {
        ExtraText = "";

        // The BB width must be at least the configured minimum percentage
        if (!CandleLast!.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // The high-low range over the lookback window must be sufficient
        double priceRange = GetPriceRangePercentage(LookbackCandles);
        if (priceRange < MinPriceRangePercentage)
        {
            ExtraText = $"price range {priceRange:N2}% < {MinPriceRangePercentage:N2}%";
            return false;
        }

        if (!HadWickAboveBb(LookbackCandles))
        {
            ExtraText = $"no wick above upper BB in last {LookbackCandles} candles";
            return false;
        }

        if (!IsSma20SlopeNegative())
        {
            ExtraText = "SMA20 slope is not negative";
            return false;
        }

        if (!HadCrossBelowSma50(LookbackCandles))
        {
            ExtraText = $"no cross below SMA50 in last {LookbackCandles} candles";
            return false;
        }

        return true;
    }
}
