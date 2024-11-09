using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot.Core.Zones;

public class CryptoZoneData
{
    public required Model.CryptoExchange Exchange { get; set; }
    public required CryptoSymbol Symbol { get; set; }
    public required CryptoSymbolInterval SymbolInterval { get; set; }
    public required CryptoInterval Interval { get; set; }
    public required ZigZagIndicator9 Indicator { get; set; }
}