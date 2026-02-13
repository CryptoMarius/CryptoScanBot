using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Barometer;

/// The last calculated price or volume barometer values
public class CryptoBarometerData
{
    public CandleTime? PriceDateTime { get; set; } = null;
    public decimal? PriceBarometer { get; set; } = null;

    // Experimental, needs another attemp in the future!
    public CandleTime? VolumeDateTime { get; set; } = null;
    public decimal? VolumeBarometer { get; set; } = null;


    public void Clear()
    {
        PriceDateTime = null;
        PriceBarometer = null;

        VolumeDateTime = null;
        VolumeBarometer = null;
    }
}
