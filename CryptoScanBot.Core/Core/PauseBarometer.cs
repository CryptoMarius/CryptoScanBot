namespace CryptoScanBot.Core.Core;

/// Data of pause trading if barometer is out of boundaries
public class PauseBarometer
{
    public DateTime? Calculated { get; set; }
    public DateTime? Until { get; set; }
    public string? Text { get; set; }
}
