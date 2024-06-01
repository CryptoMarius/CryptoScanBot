using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot.Core.Model;

// An in-memory class
public class CryptoSymbolInterval
{
    public virtual CryptoInterval? Interval { get; set; }

    public CryptoIntervalPeriod IntervalPeriod { get; set; }

    // The last collected candle (synchonized with exchange)
    public long? LastCandleSynchronized { get; set; }

    // The last calculated trend & generated date
    public CryptoTrendIndicator TrendIndicator { get; set; }
    public DateTime? TrendInfoDate { get; set; }
    public long? TrendInfoUnix { get; set; }
    // Caching ZigZag indicator because of emulator speed
    public ZigZagIndicatorCache? ZigZagCache { get; set; }

    // The last generated signal
    // TODO: Interesting: We create signals with lots of strategies, but have only place for 1 signal, that does not sound right..
    public CryptoSignal? Signal { get; set; }

    // The candles for this interval
    public SortedList<long, CryptoCandle> CandleList { get; set; } = [];
}
