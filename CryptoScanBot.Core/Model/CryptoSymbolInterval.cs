using CryptoScanBot.Core.Enums;

namespace CryptoScanBot.Core.Model;

// An in-memory class
public class CryptoSymbolInterval
{
    public virtual required CryptoInterval Interval { get; set; }
    public required CryptoIntervalPeriod IntervalPeriod { get; set; }

    // The last synchronized candle with the exchange (without gaps)
    public long? LastCandleSynchronized { get; set; }

    // The last signals generated for this interval
    public List<CryptoSignal> SignalList { get; set; } = [];

    // The candles for this interval
    public CryptoCandleList CandleList { get; set; } = [];
}
