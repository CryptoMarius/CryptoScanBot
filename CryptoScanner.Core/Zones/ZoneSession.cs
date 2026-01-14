using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Json;

using System.Text.Json;

namespace CryptoScanner.Core.Zones;

public enum TrendLines
{
    PrimaryTrend,
    SecondaryTrend,
    DlzZoneTrend,
    None,
}


[Serializable]
public class ZoneSession
{
    // symbol
    public string SymbolBase { get; set; } = "BTC";
    public string SymbolQuote { get; set; } = "USDT";
    public string IntervalName { get; set; } = "1h";

    // Period = UtcNow - X candles
    public long MinDate { get; set; }
    public long MaxDate { get; set; }
    public CryptoIntervalPeriod ActiveInterval { get; set; } = CryptoIntervalPeriod.interval1h;

    // hidden
    public bool UseOptimizing { get; set; } = false;
    public decimal Deviation { get; set; } = 1m;

    // trend
    //public TrendType TrendIndicator { get; set; } = TrendType.Primary;
    public TrendType TrendType { get; set; } = TrendType.Primary;
    public bool TrendShowZigZag { get; set; } = false;

    // fib
    public TrendType FibTrend { get; set; } = TrendType.Primary;
    public bool ShowFibRetracement { get; set; } = false;
    public bool ShowFibZigZag { get; set; } = false;

    // options
    public bool ShowPoints { get; set; } = false; // Pivot points
    public bool ShowSignals { get; set; } = false; // Signals from the analyzer
    public bool ShowDlzZones { get; set; } = false; // Dominant Liquidity Zones
    public bool ShowFvgZones { get; set; } = false; // Fear Value Gaps
    public bool ShowDtb { get; set; } = false; // Double Top Double Bottom
    public bool ShowNadarayaWatsonEnvelope { get; set; } = true; // NWE non repainting?
    public bool ShowNadarayaWatsonEnvelopeRepainting { get; set; } = true;
    public bool ShowBollingerBand { get; set; } = false;
    public bool ShowSmaLinesSbm { get; set; } = true;
    //public bool ShowTrendLines { get; set; } = false;

    // misc
    public bool ForceCalculation { get; set; } = false;
    public bool Transparent { get; set; } = false;
}