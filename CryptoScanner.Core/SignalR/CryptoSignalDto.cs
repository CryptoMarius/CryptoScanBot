using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.SignalR;

/// <summary>
/// Lightweight DTO for broadcasting signals over SignalR (no navigation properties / DB annotations).
/// </summary>
public class CryptoSignalDto
{
    public int Id { get; set; }
    public string Exchange { get; set; } = "";
    public string Symbol { get; set; } = "";
    public string Interval { get; set; } = "";
    public string Side { get; set; } = "";
    public string Strategy { get; set; } = "";
    public decimal SignalPrice { get; set; }
    public double SignalVolume { get; set; }
    public DateTime OpenDate { get; set; }
    public DateTime CloseDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public bool IsInvalid { get; set; }
    public string? EventText { get; set; }
    public decimal? SlPercentage { get; set; }
    public float Last24HoursChange { get; set; }

    // Barometers
    public float? Barometer15m { get; set; }
    public float? Barometer30m { get; set; }
    public float? Barometer1h { get; set; }
    public float? Barometer4h { get; set; }
    public float? Barometer1d { get; set; }

    // Trends
    public string? Trend15m { get; set; }
    public string? Trend30m { get; set; }
    public string? Trend1h { get; set; }
    public string? Trend4h { get; set; }
    public string? Trend1d { get; set; }

    public static CryptoSignalDto FromSignal(CryptoSignal signal)
    {
        return new CryptoSignalDto
        {
            Id = signal.Id,
            Exchange = signal.Exchange.Name,
            Symbol = signal.Symbol.Name,
            Interval = signal.Interval.Name,
            Side = signal.Side.ToString().ToLowerInvariant(),
            Strategy = signal.StrategyText,
            SignalPrice = signal.SignalPrice,
            SignalVolume = signal.SignalVolume,
            OpenDate = signal.OpenDate,
            CloseDate = signal.CloseDate,
            ExpirationDate = signal.ExpirationDate,
            IsInvalid = signal.IsInvalid,
            EventText = signal.EventText,
            SlPercentage = signal.SlPercentage,
            Last24HoursChange = signal.Last24HoursChange,
            Barometer15m = signal.Barometer15m,
            Barometer30m = signal.Barometer30m,
            Barometer1h = signal.Barometer1h,
            Barometer4h = signal.Barometer4h,
            Barometer1d = signal.Barometer1d,
            Trend15m = signal.Trend15m?.ToString(),
            Trend30m = signal.Trend30m?.ToString(),
            Trend1h = signal.Trend1h?.ToString(),
            Trend4h = signal.Trend4h?.ToString(),
            Trend1d = signal.Trend1d?.ToString(),
        };
    }
}
