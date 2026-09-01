using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.UI.ViewModels;

public class PositionViewModel
{
    public CryptoPosition Object { get; }

    public PositionViewModel(CryptoPosition position)
    {
        Object = position;
    }

    public string GetCellValue(PositionColumnEnum column)
    {
        return column switch
        {
            PositionColumnEnum.Id => Object.Id.ToString(),
            PositionColumnEnum.AltradyId => Object.AltradyPositionId ?? "",
            PositionColumnEnum.CreateTime => Object.CreateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
            PositionColumnEnum.UpdateTime => Object.UpdateTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "",
            PositionColumnEnum.CloseTime => Object.CloseTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "",
            PositionColumnEnum.Duration => Object.DurationText(),
            PositionColumnEnum.Exchange => Object.Exchange.Name,
            PositionColumnEnum.Symbol => Object.Symbol.PairName,
            PositionColumnEnum.Interval => Object.Interval?.Name ?? "",
            PositionColumnEnum.Side => Object.SideText,
            PositionColumnEnum.Strategy => Object.StrategyText,
            PositionColumnEnum.Status => Object.Status.ToString(),

            PositionColumnEnum.Invested => FormatMoney(Object.Invested),
            PositionColumnEnum.Returned => FormatMoney(Object.Returned),
            PositionColumnEnum.Commission => FormatMoney(Object.Commission),
            PositionColumnEnum.BreakEvenPrice => Object.BreakEvenPrice.ToString0(Object.Symbol.PriceDisplayFormat),
            PositionColumnEnum.BreakEvenPercent => IsInactiveStatus() ? "" : (Object.CurrentBreakEvenPercentage() - 100).ToString0("N2"),
            PositionColumnEnum.Quantity => Object.Status == CryptoPositionStatus.Timeout ? "-" : Object.Quantity.ToString0(),
            PositionColumnEnum.Open => IsInactiveStatus() ? "-" : (Object.Invested - Object.Returned - Object.Commission).ToString(Object.Symbol.QuoteData.DisplayFormat),
            PositionColumnEnum.CurrentProfit => IsInactiveStatus() ? "-" : Object.CurrentProfit().ToString(Object.Symbol.QuoteData.DisplayFormat),
            PositionColumnEnum.CurrentProfitPercentage => IsInactiveStatus() ? "-" : Object.CurrentProfitPercentage().ToString("N2") + "%",
            PositionColumnEnum.Parts => Object.PartCountText(),
            PositionColumnEnum.EntryPrice => Object.EntryPrice?.ToString0(Object.Symbol.PriceDisplayFormat) ?? "",
            PositionColumnEnum.ProfitPrice => Object.ProfitPrice?.ToString0(Object.Symbol.PriceDisplayFormat) ?? "",
            PositionColumnEnum.FundingRate => Object.Symbol.FundingRate.ToString0(),
            PositionColumnEnum.QuantityTick => Object.Symbol.QuantityTickSize.ToString0(),
            PositionColumnEnum.RemainingDust => IsInactiveStatus() ? "-" : Object.RemainingDust.ToString0(),
            PositionColumnEnum.RemainingDustValue => FormatDustValue(),

            PositionColumnEnum.SignalDate => Object.SignalEventTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
            PositionColumnEnum.SignalPrice => Object.SignalPrice.ToString0(Object.Symbol.PriceDisplayFormat),
            PositionColumnEnum.EventText => Object.EventText ?? "",
            PositionColumnEnum.SignalVolume => Object.SignalVolume.ToString0("N0"),

            PositionColumnEnum.TrendInterval => Object.TrendInterval == CryptoTrendIndicator.Bullish ? "up" : "down",
            PositionColumnEnum.TrendPercentagePrimary => Object.TrendPercentagePrimary.ToString("N2"),
            PositionColumnEnum.TrendPercentageSecondary => Object.TrendPercentageSecondary.ToString("N2"),
            PositionColumnEnum.Last24HoursChange => Object.Last24HoursChange.ToString("N2"),
            PositionColumnEnum.LastXDaysEffective => Object.LastXDaysEffective.ToString("N2"),

            PositionColumnEnum.BB => Object.BollingerBandsPercentage?.ToString("N2") ?? "",
            PositionColumnEnum.BbUpper => Object.BollingerBandsUpperBand.ToString0(Object.Symbol.PriceDisplayFormat),
            PositionColumnEnum.BbLower => Object.BollingerBandsLowerBand.ToString0(Object.Symbol.PriceDisplayFormat),
            PositionColumnEnum.AvgBB => Object.AvgBB.ToString("N2"),
            PositionColumnEnum.RangeIndex => Object.BandRangeIndex?.ToString("N2") ?? "",
            PositionColumnEnum.RangeCount => Object.BandRangeCount?.ToString() ?? "",

            PositionColumnEnum.Rsi => Object.Rsi?.ToString("N2") ?? "",
            PositionColumnEnum.LuxIndicator5m => Object.LuxIndicator5m?.ToString("N0") ?? "",
            PositionColumnEnum.MacdValue => Object.MacdValue?.ToString("N5") ?? "",
            PositionColumnEnum.MacdSignal => Object.MacdSignal?.ToString("N5") ?? "",
            PositionColumnEnum.MacdHistogram => Object.MacdHistogram?.ToString("N2") ?? "",
            PositionColumnEnum.StochOscillator => Object.StochOscillator?.ToString0("N2") ?? "",
            PositionColumnEnum.StochSignal => Object.StochSignal?.ToString0("N2") ?? "",
            PositionColumnEnum.Sma200 => Object.Sma200?.ToString0(Object.Symbol.PriceDisplayFormat) ?? "",
            PositionColumnEnum.Sma50 => Object.Sma50?.ToString0(Object.Symbol.PriceDisplayFormat) ?? "",
            PositionColumnEnum.Sma20 => Object.Sma20?.ToString0(Object.Symbol.PriceDisplayFormat) ?? "",
            PositionColumnEnum.PSar => Object.PSar?.ToString0(Object.Symbol.PriceDisplayFormat) ?? "",

            PositionColumnEnum.Trend15m => Object.Trend15m == CryptoTrendIndicator.Bullish ? "up" : "down",
            PositionColumnEnum.Trend30m => Object.Trend30m == CryptoTrendIndicator.Bullish ? "up" : "down",
            PositionColumnEnum.Trend1h => Object.Trend1h == CryptoTrendIndicator.Bullish ? "up" : "down",
            PositionColumnEnum.Trend4h => Object.Trend4h == CryptoTrendIndicator.Bullish ? "up" : "down",
            PositionColumnEnum.Trend1d => Object.Trend1d == CryptoTrendIndicator.Bullish ? "up" : "down",

            PositionColumnEnum.Barometer15m => Object.Barometer15m?.ToString("N2") ?? "",
            PositionColumnEnum.Barometer30m => Object.Barometer30m?.ToString("N2") ?? "",
            PositionColumnEnum.Barometer1h => Object.Barometer1h?.ToString("N2") ?? "",
            PositionColumnEnum.Barometer4h => Object.Barometer4h?.ToString("N2") ?? "",
            PositionColumnEnum.Barometer1d => Object.Barometer1d?.ToString("N2") ?? "",

            PositionColumnEnum.MinimumEntry => Object.MinEntry.ToString("N2"),

            _ => "",
        };
    }

    public string GetCellColorClass(PositionColumnEnum column)
    {
        return column switch
        {
            PositionColumnEnum.Side => ColorHelper.GetColorClassSide(Object.Side),
            PositionColumnEnum.RangeIndex => ColorHelper.GetColorClassBandRangeIndex(Object.BandRangeIndex),
            PositionColumnEnum.Status => ColorHelper.GetColorClassPositionStatus(Object.Status),
            PositionColumnEnum.CurrentProfit => ColorHelper.GetColorClassViaSign(Object.CurrentProfit()),
            PositionColumnEnum.CurrentProfitPercentage => ColorHelper.GetColorClassViaSign(Object.CurrentProfit()),
            PositionColumnEnum.BreakEvenPercent => ColorHelper.GetColorClassViaSign(Object.CurrentBreakEvenPercentage() - 100),
            PositionColumnEnum.TrendInterval => ColorHelper.GetColorClassTrend(Object.TrendInterval),
            PositionColumnEnum.TrendPercentagePrimary => ColorHelper.GetColorClassViaSign(Object.TrendPercentagePrimary),
            PositionColumnEnum.TrendPercentageSecondary => ColorHelper.GetColorClassViaSign(Object.TrendPercentageSecondary),
            PositionColumnEnum.Last24HoursChange => ColorHelper.GetColorClassViaSign(Object.Last24HoursChange),
            PositionColumnEnum.Rsi => ColorHelper.GetColorClassRsi(Object.Rsi),
            PositionColumnEnum.LuxIndicator5m => ColorHelper.GetColorClassViaSign((double)(Object.LuxIndicator5m ?? 0)),
            PositionColumnEnum.MacdValue => ColorHelper.GetColorClassViaSign(Object.MacdValue),
            PositionColumnEnum.MacdSignal => ColorHelper.GetColorClassViaSign(Object.MacdSignal),
            PositionColumnEnum.MacdHistogram => ColorHelper.GetColorClassViaSign(Object.MacdHistogram),
            PositionColumnEnum.StochOscillator => ColorHelper.GetColorClassStoch(Object.StochOscillator),
            PositionColumnEnum.StochSignal => ColorHelper.GetColorClassStoch(Object.StochSignal),
            PositionColumnEnum.Trend15m => ColorHelper.GetColorClassTrend(Object.Trend15m),
            PositionColumnEnum.Trend30m => ColorHelper.GetColorClassTrend(Object.Trend30m),
            PositionColumnEnum.Trend1h => ColorHelper.GetColorClassTrend(Object.Trend1h),
            PositionColumnEnum.Trend4h => ColorHelper.GetColorClassTrend(Object.Trend4h),
            PositionColumnEnum.Trend1d => ColorHelper.GetColorClassTrend(Object.Trend1d),
            PositionColumnEnum.Barometer15m => ColorHelper.GetColorClassViaSign(Object.Barometer15m),
            PositionColumnEnum.Barometer30m => ColorHelper.GetColorClassViaSign(Object.Barometer30m),
            PositionColumnEnum.Barometer1h => ColorHelper.GetColorClassViaSign(Object.Barometer1h),
            PositionColumnEnum.Barometer4h => ColorHelper.GetColorClassViaSign(Object.Barometer4h),
            PositionColumnEnum.Barometer1d => ColorHelper.GetColorClassViaSign(Object.Barometer1d),
            _ => "",
        };
    }

    public string GetBackgroundStyle(PositionColumnEnum column)
    {
        return column switch
        {
            PositionColumnEnum.Symbol => ColorHelper.GetBackgroundStyle(Object.Symbol.QuoteData),
            PositionColumnEnum.Strategy => GetStrategyBackgroundStyle(),
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

    private bool IsInactiveStatus()
    {
        return Object.Status == CryptoPositionStatus.Timeout || Object.Status == CryptoPositionStatus.Waiting;
    }

    private string FormatMoney(decimal value)
    {
        if (IsInactiveStatus())
            return "-";
        return value.ToString(Object.Symbol.QuoteData.DisplayFormat);
    }

    private string FormatDustValue()
    {
        if (Object.Symbol.LastPrice == null)
            return (Object.RemainingDust * Object.ProfitPrice).ToString0(Object.Symbol.QuoteData.DisplayFormat);
        return (Object.RemainingDust * Object.Symbol.LastPrice).ToString0(Object.Symbol.QuoteData.DisplayFormat);
    }

    // Which market inside the exchange, shown as a coloured badge behind the name
    public string MarketLabel => Object.Symbol.MarketLabel;
}
