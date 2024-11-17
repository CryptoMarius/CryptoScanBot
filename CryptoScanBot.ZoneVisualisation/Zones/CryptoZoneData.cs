using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot.ZoneVisualisation.Zones;

public class CryptoZoneData
{
    public required Core.Model.CryptoExchange Exchange { get; set; }
    public required CryptoSymbol Symbol { get; set; }
    public required CryptoSymbolInterval SymbolInterval { get; set; }
    public required CryptoInterval Interval { get; set; }

    public required ZigZagIndicator9 Indicator { get; set; }
    public required ZigZagIndicator9 IndicatorFib { get; set; }

    // New structure?
    //public required AccountSymbolIntervalData AccountSymbolIntervalData { get; set; }
}