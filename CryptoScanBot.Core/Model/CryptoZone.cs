using CryptoScanBot.Core.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Model;

[Table("Zone")]
public class CryptoZone
{
    [Key]
    public int Id { get; set; }

    public int AccountId { get; set; }
    [Computed]
    public required virtual CryptoAccount Account { get; set; }

    public required int ExchangeId { get; set; }
    [Computed]
    public required virtual CryptoExchange Exchange { get; set; }

    public required int SymbolId { get; set; }
    [Computed]
    public required virtual CryptoSymbol Symbol { get; set; }

    public required CryptoTradeSide Side { get; set; }
    public required CryptoSignalStrategy Strategy { get; set; }

    public required decimal Top { get; set; }
    public required decimal Bottom { get; set; }

    // Create a signal when this price triggers (once)
    public decimal AlarmPrice { get; set; } // obsolete, just for debugging
    public DateTime? AlarmDate { get; set; }

    // Remove the zone when this price has been triggered (once)
    public decimal? ExpirationPrice { get; set; }
    public DateTime? ExpirationDate { get; set; }

}
