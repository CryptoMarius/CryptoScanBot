using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Model;

// An idea for liquidation levels

[Table("Zone")]
public class CryptoZone
{
    [Key]
    public int Id { get; set; }

    public int Direction { get; set; } // todo.. downwards or upwards long or short?
    public decimal Top { get; set; }
    public decimal Bottom { get; set; }

    // Create a signal when this price triggers (once)
    public decimal AlarmPrice { get; set; }
    public DateTime AlarmDate { get; set; }

    // Remove the zone when this price has been triggered (once)
    public decimal ExpirationPrice { get; set; }
    public DateTime ExpirationDate { get; set; }
}
