using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot.Core.Zones;

public class ZoneData
{
    public required CryptoAccount Account { get; set; }
    public required Model.CryptoExchange Exchange { get; set; }
    public required CryptoSymbol Symbol { get; set; }
    public required CryptoInterval Interval { get; set; }
    public required CryptoSymbolInterval SymbolInterval { get; set; }

    public required ZigZagIndicator Indicator { get; set; }
    public required ZigZagIndicator IndicatorFib { get; set; }

    public List<CryptoSignal> Signals { get; set; } = [];
}