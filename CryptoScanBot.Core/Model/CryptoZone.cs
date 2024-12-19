using CryptoScanBot.Core.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Model;

public enum CryptoZoneKind
{
    DominantLevel = 1, // Or LiquidityZone..?
}

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

    public required CryptoZoneKind Kind { get; set; }
    public required CryptoTradeSide Side { get; set; }

    // Created on..
    public required DateTime CreateTime { get; set; }

    public long? OpenTime { get; set; } // Zone starts on this date, for limited types of zones
    public required decimal Top { get; set; }
    public required decimal Bottom { get; set; }
    public long? CloseTime { get; set; } // Zone ends on this date

    // Create a signal when this price triggers (once)
    public DateTime? AlarmDate { get; set; }

    public long? LastSignalDate { get; set; }

    // Percentage of the zone or other text
    public string Description { get; set; } = "";

    public bool IsValid { get; set; }
}
