using CryptoScanBot.Core.Enums;

namespace CryptoScanBot.Core.Model;

// An in-memory class
public class CryptoSymbolInterval
{
    public virtual required CryptoInterval Interval { get; set; }
    public required CryptoIntervalPeriod IntervalPeriod { get; set; }

    // The last synchronized candle with the exchange (without gaps)
    public long? LastCandleSynchronized { get; set; }

    // The last swingpoint we have calculated. If we have a new swinghigh after this
    // point we need to recalculate the zones (we could have a new formed dominant zone)
    public long? LastSwingPointDate { get; set; }

    // The last generated signal
    // TODO: Interesting: We create signals with lots of strategies, but have only place for 1 signal, that does not sound right!
    // Suggestion: CreateSignal will return a list of signals, not sure if this fitts the internal setup, this is not 100%.
    public CryptoSignal? Signal { get; set; }

    // The candles for this interval
    public CryptoCandleList CandleList { get; set; } = [];
}
