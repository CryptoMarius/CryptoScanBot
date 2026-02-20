using CryptoScanner.Core.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Model;

[Table("Zone")]
public class CryptoZone
{
    [Key]
    public int Id { get; set; }

    public required int ExchangeId { get; set; }
    [Computed]
    public required virtual CryptoExchange Exchange { get; set; }

    public required int SymbolId { get; set; }
    [Computed]
    public required virtual CryptoSymbol Symbol { get; set; }

    public required int IntervalId { get; set; }
    [Computed]
    public required virtual CryptoInterval Interval { get; set; }

    public required CryptoZoneKind Kind { get; set; }
    public required CryptoTradeSide Side { get; set; }
    public required CryptoZoneStrength Strength { get; set; }

    public required CandleTime OpenTime { get; set; } // Zone starts on this date, for limited types of zones
    public required decimal Top { get; set; }
    public required decimal Bottom { get; set; }
    public CandleTime? CloseTime { get; set; } // Zone ends on this date

    // Create a signal when this price triggers (once)
    public CandleTime? AlarmDate { get; set; }

    // Percentage of the zone or other text
    public string Description { get; set; } = "";

    public bool IsValid { get; set; }
}
