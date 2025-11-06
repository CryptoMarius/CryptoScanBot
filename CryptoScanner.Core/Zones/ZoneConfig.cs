using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.Core.Zones;

public class ZoneConfig
{
    // basic information
    public required Model.CryptoExchange Exchange { get; set; }
    public required CryptoSymbol Symbol { get; set; }
    public required CryptoInterval Interval { get; set; }
    public required CryptoSymbolInterval SymbolInterval { get; set; }

    // indicators
    public Dictionary<(TrendType trendType, bool useHighLow), ZigZagIndicator> IndicatorList { get; set; } = [];

    // for charting form
    public List<CryptoSignal> Signals { get; set; } = [];
}