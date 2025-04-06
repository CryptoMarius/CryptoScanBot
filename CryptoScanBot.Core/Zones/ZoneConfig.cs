using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot.Core.Zones;



public class ZoneConfig
{
    // basic information
    public required Model.CryptoExchange Exchange { get; set; }
    public required CryptoSymbol Symbol { get; set; }
    public required CryptoInterval Interval { get; set; }
    public required CryptoSymbolInterval SymbolInterval { get; set; }

    // indicators
    //public required ZigZagIndicator Indicator { get; set; } // primary
    //public required ZigZagIndicator IndicatorFib { get; set; } // for charting form

    // bool 
    public Dictionary<(TrendType trendType, bool useHighLow), ZigZagIndicator> IndicatorList { get; set; } = [];

    // for charting form
    public List<CryptoSignal> Signals { get; set; } = [];
}