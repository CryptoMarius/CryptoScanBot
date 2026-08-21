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

        VolumeDateTime = null;
        VolumeBarometer = null;
    }
}
