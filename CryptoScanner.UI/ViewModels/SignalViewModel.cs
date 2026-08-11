using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.UI.ViewModels;

public class SignalViewModel
{
    public CryptoSignal Object { get; }

    public SignalViewModel(CryptoSignal signal)
    {
        Object = signal;
    }

    public bool IsInvalid => Object.IsInvalid;

    public string GetCellValue(SignalColumnEnum column)
    {
        return column switch
        {
            SignalColumnEnum.Id => Object.Id.ToString(),
            SignalColumnEnum.Date => FormatDate(),
            SignalColumnEnum.Exchange => Object.Exchange?.Name ?? "",
            SignalColumnEnum.Symbol => Object.Symbol?.Name ?? "",
            SignalColumnEnum.Side => Object.SideText,
            SignalColumnEnum.Interval => Object.Interval?.Name ?? "",
            SignalColumnEnum.Strategy => Object.StrategyText,
            SignalColumnEnum.EventText => Object.EventText ?? "",
            SignalColumnEnum.SignalPrice => Object.SignalPrice.ToString(Object.Symbol?.PriceDisplayFormat ?? "N8"),
            SignalColumnEnum.PriceChange => Object.Last24HoursChange.ToString("N2"),
            SignalColumnEnum.SignalVolume => Object.SignalVolume.ToString("N0"),

            SignalColumnEnum.TrendInterval => FormatTrend(Object.TrendInterval),
            SignalColumnEnum.TrendPercentagePrimary => Object.TrendPercentagePrimary.ToString("N2"),
            SignalColumnEnum.TrendPercentageSecondary => Object.TrendPercentageSecondary.ToString("N2"),
            SignalColumnEnum.Last24HoursChange => Object.Last24HoursChange.ToString("N2"),
            SignalColumnEnum.LastXDaysEffective => Object.LastXDaysEffective.ToString("N2"),

            SignalColumnEnum.BB => Object.BollingerBandsPercentage?.ToString("N2") ?? "-",
            SignalColumnEnum.BbLower => Object.BollingerBandsLowerBand?.ToString(Object.Symbol?.PriceDisplayFormat ?? "N8") ?? "-",
            SignalColumnEnum.BbUpper => Object.BollingerBandsUpperBand?.ToString(Object.Symbol?.PriceDisplayFormat ?? "N8") ?? "-",
            SignalColumnEnum.AvgBB => Object.AvgBB.ToString("N2"),

            SignalColumnEnum.Rsi => Object.Rsi?.ToString("N2") ?? "-",
            SignalColumnEnum.LuxIndicator5m => Object.LuxIndicator5m?.ToString("N0") ?? "-",
            SignalColumnEnum.MacdValue => Object.MacdValue?.ToString("N5") ?? "-",
            SignalColumnEnum.MacdSignal => Object.MacdSignal?.ToString("N5") ?? "-",
            SignalColumnEnum.MacdHistogram => Object.MacdHistogram?.ToString("N2") ?? "-",
            SignalColumnEnum.StochOscillator => Object.StochOscillator?.ToString("N2") ?? "-",
            SignalColumnEnum.StochSignal => Object.StochSignal?.ToString("N2") ?? "-",
            SignalColumnEnum.Sma200 => Object.Sma200?.ToString(Object.Symbol?.PriceDisplayFormat ?? "N8") ?? "-",
            SignalColumnEnum.Sma50 => Object.Sma50?.ToString(Object.Symbol?.PriceDisplayFormat ?? "N8") ?? "-",
            SignalColumnEnum.Sma20 => Object.Sma20?.ToString(Object.Symbol?.PriceDisplayFormat ?? "N8") ?? "-",
            SignalColumnEnum.PSar => Object.PSar?.ToString(Object.Symbol?.PriceDisplayFormat ?? "N8") ?? "-",

            SignalColumnEnum.Trend15m => FormatTrend(Object.Trend15m),
            SignalColumnEnum.Trend30m => FormatTrend(Object.Trend30m),
            SignalColumnEnum.Trend1h => FormatTrend(Object.Trend1h),
            SignalColumnEnum.Trend4h => FormatTrend(Object.Trend4h),
            SignalColumnEnum.Trend1d => FormatTrend(Object.Trend1d),

            SignalColumnEnum.Barometer15m => Object.Barometer15m?.ToString("N2") ?? "-",
            SignalColumnEnum.Barometer30m => Object.Barometer30m?.ToString("N2") ?? "-",
            SignalColumnEnum.Barometer1h => Object.Barometer1h?.ToString("N2") ?? "-",
            SignalColumnEnum.Barometer4h => Object.Barometer4h?.ToString("N2") ?? "-",
            SignalColumnEnum.Barometer1d => Object.Barometer1d?.ToString("N2") ?? "-",

            SignalColumnEnum.MinimumEntry => Object.MinEntry.ToString("N2"),
            _ => "",
        };
    }

    public string GetCellColorClass(SignalColumnEnum column)
    {
        return column switch
        {
            SignalColumnEnum.Side => ColorHelper.GetColorClassSide(Object.Side),
            SignalColumnEnum.PriceChange => ColorHelper.GetColorClassViaSign(Object.Last24HoursChange),
            SignalColumnEnum.Last24HoursChange => ColorHelper.GetColorClassViaSign(Object.Last24HoursChange),

            SignalColumnEnum.TrendInterval => ColorHelper.GetColorClassTrend(Object.TrendInterval),
            SignalColumnEnum.TrendPercentagePrimary => ColorHelper.GetColorClassViaSign((double)Object.TrendPercentagePrimary),
            SignalColumnEnum.TrendPercentageSecondary => ColorHelper.GetColorClassViaSign((double)Object.TrendPercentageSecondary),

            SignalColumnEnum.Rsi => ColorHelper.GetColorClassRsi(Object.Rsi),
            SignalColumnEnum.LuxIndicator5m => ColorHelper.GetColorClassViaSign((double)(Object.LuxIndicator5m ?? 0)),
            SignalColumnEnum.MacdValue => ColorHelper.GetColorClassViaSign(Object.MacdValue),
            SignalColumnEnum.MacdSignal => ColorHelper.GetColorClassViaSign(Object.MacdSignal),
            SignalColumnEnum.MacdHistogram => ColorHelper.GetColorClassViaSign(Object.MacdHistogram),
            SignalColumnEnum.StochOscillator => ColorHelper.GetColorClassStoch(Object.StochOscillator),
            SignalColumnEnum.StochSignal => ColorHelper.GetColorClassStoch(Object.StochSignal),

            SignalColumnEnum.Trend15m => ColorHelper.GetColorClassTrend(Object.Trend15m),
            SignalColumnEnum.Trend30m => ColorHelper.GetColorClassTrend(Object.Trend30m),
            SignalColumnEnum.Trend1h => ColorHelper.GetColorClassTrend(Object.Trend1h),
            SignalColumnEnum.Trend4h => ColorHelper.GetColorClassTrend(Object.Trend4h),
            SignalColumnEnum.Trend1d => ColorHelper.GetColorClassTrend(Object.Trend1d),

            SignalColumnEnum.Barometer15m => ColorHelper.GetColorClassViaSign(Object.Barometer15m),
            SignalColumnEnum.Barometer30m => ColorHelper.GetColorClassViaSign(Object.Barometer30m),
            SignalColumnEnum.Barometer1h => ColorHelper.GetColorClassViaSign(Object.Barometer1h),
            SignalColumnEnum.Barometer4h => ColorHelper.GetColorClassViaSign(Object.Barometer4h),
            SignalColumnEnum.Barometer1d => ColorHelper.GetColorClassViaSign(Object.Barometer1d),

            _ => "",
        };
    }

    public string GetBackgroundStyle()
    {
        if (Object.Symbol?.QuoteData != null)
            return ColorHelper.GetBackgroundStyle(Object.Symbol.QuoteData);
        return "";
    }

    /// <summary>
    /// Per-cell background. Avalonia paints the quote colour behind the Symbol cell and the
    /// configured per-strategy colour behind the Strategy cell (SignalGridView Border bindings).
    /// </summary>
    public string GetBackgroundStyle(SignalColumnEnum column)
    {
        return column switch
        {
            SignalColumnEnum.Symbol => GetBackgroundStyle(),
            SignalColumnEnum.Strategy => GetStrategyBackgroundStyle(),
            _ => "",
        };
    }

    private string GetStrategyBackgroundStyle()
    {
        if (Object.Strategy != null && GlobalData.StrategiesSettings.TryGetValue(Object.Strategy, out var x))
        {
            var c = Object.Side == CryptoTradeSide.Long ? x.strategySettings.ColorLong : x.strategySettings.ColorShort;
            if (c.A == 0)
                return "";
            return $"background-color: rgba({c.R},{c.G},{c.B},{c.A / 255.0:F2})";
        }
        return "";
    }

    private string FormatDate()
    {
        var open = Object.OpenDate.ToLocalTime();
        var close = Object.CloseDate.ToLocalTime();
        if (open.Date == close.Date)
            return $"{open:yyyy-MM-dd HH:mm} - {close:HH:mm}";
        return $"{open:yyyy-MM-dd HH:mm} - {close:MM-dd HH:mm}";
    }

    private static string FormatTrend(CryptoTrendIndicator trend)
    {
        return trend switch
        {
            CryptoTrendIndicator.Bullish => "up",
            CryptoTrendIndicator.Bearish => "down",
            _ => "-",
        };
    }

    private static string FormatTrend(CryptoTrendIndicator? trend)
    {
        if (trend == null)
            return "-";
        return FormatTrend(trend.Value);
    }
}
