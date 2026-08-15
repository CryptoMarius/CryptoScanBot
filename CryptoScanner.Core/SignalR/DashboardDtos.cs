namespace CryptoScanner.Core.SignalR;

/// <summary>
/// Single barometer data point (one per minute).
/// </summary>
public class BarometerPointDto
{
    public DateTime Time { get; set; }
    public decimal Value { get; set; }
}

/// <summary>
/// Full barometer graph history returned by GetBarometerGraph().
/// </summary>
public class BarometerGraphDto
{
    public string Quote { get; set; } = "";
    public string Interval { get; set; } = "";
    public List<BarometerPointDto> Points { get; set; } = [];

    /// <summary>True once the scanner has finished loading candles (ApplicationStatus == Running).</summary>
    public bool Ready { get; set; }

    /// <summary>Live "done / total (symbol)" candle-load progress while starting up; empty once ready.</summary>
    public string Progress { get; set; } = "";
}

/// <summary>
/// Current barometer summary values (1h, 4h, 1d) as shown on the info panel.
/// </summary>
public class BarometerValuesDto
{
    public string Quote { get; set; } = "";
    public decimal Barometer1h { get; set; }
    public decimal Barometer4h { get; set; }
    public decimal Barometer1d { get; set; }
    public string BarometerTime { get; set; } = "";

    /// <summary>
    /// Market breadth per interval: the percentage of symbols that rose, on a 0..100 scale. The
    /// barometer above is an average and cannot tell a broad rise apart from a few coins carrying
    /// the whole move; this can. See BarometerResult for the other figures of the same measurement.
    /// </summary>
    public decimal Rising1h { get; set; }
    public decimal Rising4h { get; set; }
    public decimal Rising1d { get; set; }

    /// <summary>Number of symbols the most recent measurement was based on.</summary>
    public int SymbolCount { get; set; }

    /// <summary>True once the scanner has finished loading candles (ApplicationStatus == Running).</summary>
    public bool Ready { get; set; }

    /// <summary>Live "done / total (symbol)" candle-load progress while starting up; empty once ready.</summary>
    public string Progress { get; set; } = "";
}

/// <summary>
/// A single market indicator (TradingView symbol, exchange symbol, or Fear and Greed).
/// </summary>
public class MarketIndicatorDto
{
    public string Type { get; set; } = "";
    public string Symbol { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal? Price { get; set; }
    public double? Volume { get; set; }
}

/// <summary>
/// Symbol price + volume for an exchange symbol shown in the info bar.
/// </summary>
public class SymbolPriceDto
{
    public string Symbol { get; set; } = "";
    public decimal? Price { get; set; }
    public double? Volume { get; set; }
}

/// <summary>
/// Ticker counts and scanner statistics.
/// </summary>
public class TickerStatsDto
{
    public int KlineTickerCount { get; set; }
    public int ScannerExecuteCount { get; set; }
    public int ScannerSignalCount { get; set; }
    public string ScannerPositionCount { get; set; } = "";
}

/// <summary>
/// Complete periodic dashboard update pushed every minute.
/// </summary>
public class DashboardUpdateDto
{
    public BarometerPointDto? LatestBarometerPoint { get; set; }
    public BarometerValuesDto? BarometerValues { get; set; }
    public List<MarketIndicatorDto> MarketIndicators { get; set; } = [];
    public List<SymbolPriceDto> SymbolPrices { get; set; } = [];
    public TickerStatsDto? TickerStats { get; set; }
}
