namespace CryptoScanner.Core.Core;

/// Data of pause trading if barometer is out of boundaries
public class CryptoPauseBarometer
{
    public DateTime? Calculated { get; set; }
    public DateTime? Until { get; set; }
    public string? Text { get; set; }
}
