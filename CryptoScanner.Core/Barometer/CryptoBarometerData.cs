using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Barometer;

/// The last calculated price or volume barometer values
public class CryptoBarometerData
{
    public CandleTime? PriceDateTime { get; set; } = null;
    public decimal? PriceBarometer { get; set; } = null;

    // The remaining figures of the same measurement, see BarometerResult for what they mean and why
    // they are practically free. PriceBarometer above stays the average, so nothing that reads it
    // changes behaviour. The history of the first four lives in the barometer candles themselves;
    // these fields only hold the latest measurement.
    public decimal? PriceMedian { get; set; } = null;
    public decimal? PricePercentageRising { get; set; } = null;
    public decimal? PriceSpread { get; set; } = null;
    public int? PriceSymbolCount { get; set; } = null;
    public int? PriceOutlierCount { get; set; } = null;

    // How far the typical coin moved regardless of direction, and bitcoin measured against the
    // median coin. The latter stays null on a quote that has no bitcoin pair.
    public decimal? PriceMovement { get; set; } = null;
    public decimal? PriceBitcoinVersusMarket { get; set; } = null;

    // The market trend: the average of the trend percentage (-100..+100) of every coin that took
    // part in the measurement, for the primary and the secondary zigzag settings.
    // <para>
    // This is a different thing from the barometer above it, and the difference is the point. The
    // barometer averages a price CHANGE over an interval, so it is a return; this averages a TREND,
    // which is structural - higher highs and lower lows. A market can be structurally up while the
    // last hour is red, and only these two numbers say so.
    // </para>
    // <para>
    // Not to be confused with SettingsTextual.SymbolTrend (called MarketTrend until 19-09-2026), which tests the trend of
    // ONE coin. That one is really a coin trend; this one is the market.
    // </para>
    public decimal? MarketTrendPrimary { get; set; } = null;
    public decimal? MarketTrendSecondary { get; set; } = null;

    // Experimental, needs another attemp in the future!
    public CandleTime? VolumeDateTime { get; set; } = null;
    public decimal? VolumeBarometer { get; set; } = null;


    public void Clear()
    {
        PriceDateTime = null;
        PriceBarometer = null;

        PriceMedian = null;
        PricePercentageRising = null;
        PriceSpread = null;
        PriceSymbolCount = null;
        PriceOutlierCount = null;
        PriceMovement = null;
        PriceBitcoinVersusMarket = null;

        MarketTrendPrimary = null;
        MarketTrendSecondary = null;

        VolumeDateTime = null;
        VolumeBarometer = null;
    }
}
