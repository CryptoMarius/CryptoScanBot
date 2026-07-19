using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

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
    public CryptoIntervalPeriod ActiveInterval { get; set; } = CryptoIntervalPeriod.interval1h;

    // Period = UtcNow - X candles
    public CandleTime MinDate { get; set; }
    public CandleTime MaxDate { get; set; }

    // hidden
    public bool UseOptimizing { get; set; } = false;
    //public decimal Deviation { get; set; } = 1m; now fixed

    // trend
    public TrendType TrendType { get; set; } = TrendType.Primary;
    public bool TrendShowZigZag { get; set; } = false;

    // fib
    public TrendType FibTrend { get; set; } = TrendType.Primary;
    public bool ShowFibRetracement { get; set; } = false;
    public bool ShowFibZigZag { get; set; } = false;

    // Indicators
    public bool ShowDlzZones { get; set; } = false; // Dominant Liquidity Zones
    public bool ShowFvgZones { get; set; } = false; // Fear Value Gaps
    public bool ShowSmcZones { get; set; } = false; // SMC Order Blocks (skeleton implementation)
    public bool ShowDtb { get; set; } = false; // Double Top Double Bottom
    public bool ShowNwe { get; set; } = true; // NWE non repainting?
    public bool ShowNweRepainting { get; set; } = false;
    public bool ShowPSar { get; set; } = false;
    public bool ShowBollingerBand { get; set; } = true;
    public bool ShowKeltnerChannel { get; set; } = false;
    // ShowAtrRbBands, ShowBabaBands, ShowBreBands, ShowSlide and ShowBbma have been migrated
    // to PluginOverlayStates (plugin chart overlays managed by PluginManager).
    public bool ShowSmaLinesSbm { get; set; } = false;
    public bool ShowStoch { get; set; } = false;
    public bool ShowRsi { get; set; } = false;
    public bool ShowLux { get; set; } = false; // RSI Multi Length [LuxAlgo], 5m
    public bool ShowMacd { get; set; } = false;
    public bool ShowVolume { get; set; } = false;

    // Plugin overlay toggles (keyed by IChartOverlay.GroupKey)
    public Dictionary<string, bool> PluginOverlayStates { get; set; } = [];

    // options
    public bool ShowCandles { get; set; } = true; // focus on other stuff then candles
    public bool ShowPoints { get; set; } = false; // Pivot points (debug)
    public bool ShowSignals { get; set; } = false; // Signals from the analyzer
    public bool ShowPositions { get; set; } = false; // Positions from the trader

    // misc
    public bool ForceCalculation { get; set; } = false;
    public bool Transparent { get; set; } = false;
}