namespace CryptoScanBot.Core.Barometer;

/// The last calculated price or volume barometer values
public class BarometerData
{
    public long? PriceDateTime { get; set; } = null;
    public decimal? PriceBarometer { get; set; } = null;

    // Experimental, needs another attemp in the future!
    public long? VolumeDateTime { get; set; } = null;
    public decimal? VolumeBarometer { get; set; } = null;


    public void Clear()
    {
        PriceDateTime = null;
        PriceBarometer = null;

        VolumeDateTime = null;
        VolumeBarometer = null;
    }
}
