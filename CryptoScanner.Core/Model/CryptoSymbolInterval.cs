using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Model;

public class CryptoSymbolInterval
{
    // Administration (not really needed but it is what is is)
    public required virtual CryptoInterval Interval { get; set; }
    public required CryptoIntervalPeriod IntervalPeriod { get; set; }

    // The last synchronized candle with the exchange (without gaps)
    public long? LastCandleSynchronized { get; set; }

    // The candles for this interval
    public CryptoCandleList CandleList { get; set; } = [];

    // The signals generated for this interval
    public List<CryptoSignal> SignalList { get; set; } = [];

    // All the calculated zones
    public CryptoSymbolIntervalZones DlzZones { get; internal set; } = new();
    public CryptoSymbolIntervalZones FvgZones { get; internal set; } = new();

    // Zone administration (calculation and distances)
    public CryptoSymbolIntervalZoneCalc DlzAdmin { get; internal set; } = new();

    // For display in the symbol grid
    // These are the closest DLZ zones (calculated from all the zones)
    public CryptoZoneDistance DlzZoneDistance { get; } = new();

    // Primary and Secondary trend data
    public CryptoTrendData TrendPrimary = new();
    public CryptoTrendData TrendSecondary = new();
}