using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Experiment;

/// <summary>
/// Long (bullish) variant of the BbWickSma strategy.
/// All five conditions must be met:
///   0. The Bollinger Band width is within the configured range (not too narrow, not too wide).
///   1. The high-low price range over the last LookbackCandles candles is at least MinPriceRangePercentage.
///   2. Within the last LookbackCandles candles: a wick poked below the lower Bollinger Band
///      (price bounced off the bottom, signalling bullish pressure).
///   3. The SMA20 slope is currently positive (rising momentum).
///   4. Within the last LookbackCandles candles: the close crossed from below to above the SMA50
///      (bullish SMA50 breakout confirms the trend shift).
/// </summary>
public class SignalBbWickSmaLong : SignalBbWickSmaBase
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

        if (!HadWickBelowBb(LookbackCandles))
        {
            ExtraText = $"no wick below lower BB in last {LookbackCandles} candles";
            return false;
        }

        if (!IsSma20SlopePositive())
        {
            ExtraText = "SMA20 slope is not positive";
            return false;
        }

        if (!HadCrossAboveSma50(LookbackCandles))
        {
            ExtraText = $"no cross above SMA50 in last {LookbackCandles} candles";
            return false;
        }

        return true;
    }
}
