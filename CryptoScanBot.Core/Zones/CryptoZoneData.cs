using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot.Core.Zones;

public class CryptoZoneData
{
    public required CryptoAccount Account { get; set; }
    public required Core.Model.CryptoExchange Exchange { get; set; }
    public required CryptoSymbol Symbol { get; set; }
    public required CryptoSymbolInterval SymbolInterval { get; set; }
    public required CryptoInterval Interval { get; set; }

    public required ZigZagIndicator9 Indicator { get; set; }
    public required ZigZagIndicator9 IndicatorFib { get; set; }

    public List<CryptoSignal> Signals { get; set; } = [];
    public List<CryptoPosition> Positions { get; set; } = [];

    // New structure?
    //public required AccountSymbolIntervalData AccountSymbolIntervalData { get; set; }
}