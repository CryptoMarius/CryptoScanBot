using Avalonia.Media;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Model;

using static CryptoScanner.Core.Model.CryptoDisplayHelpers;

// Because (multivalue) converters are way to slow.

public partial class CryptoPosition
{
    [Computed] public string IdText => Id.ToString();
    [Computed] public string? AltradyIdText => AltradyPositionId;
    [Computed] public string CreateTimeText => CreateTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    [Computed] public string UpdateTimeText => UpdateTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "";
    [Computed] public string CloseTimeText => CloseTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "";
    [Computed] public string DurationText => this.DurationText();
    [Computed] public string ExchangeText => Exchange.Name;
    [Computed] public string SymbolText => Symbol.Name;
    [Computed] public IBrush SymbolBackground => new SolidColorBrush(Symbol.QuoteData.DisplayColor);
    //[Computed] public string SideText => this.SideText;
    [Computed] public IBrush SideForeground => GetBrushColorSide(Side);
    [Computed] public string IntervalText => Interval.Name;
    //[Computed] public string StrategyText => this.StrategyText;
    [Computed] public IBrush StrategyBackground => GetStrategyBackground(Side, Strategy);
    [Computed] public string StatusText => Status.ToString();
    [Computed] public IBrush StatusForeground => GetPositionStatusColor(Status);
    [Computed] public string InvestedText => (Status == CryptoPositionStatus.Timeout || Status == CryptoPositionStatus.Waiting) ? "-" : Invested.ToString0(Symbol.QuoteData.DisplayFormat);
    [Computed] public string ReturnedText => (Status == CryptoPositionStatus.Timeout || Status == CryptoPositionStatus.Waiting) ? "-" : Returned.ToString0(Symbol.QuoteData.DisplayFormat);
    [Computed] public string CommissionText => (Status == CryptoPositionStatus.Timeout || Status == CryptoPositionStatus.Waiting) ? "-" : Commission.ToString0(Symbol.QuoteData.DisplayFormat);
    [Computed] public string TotalProfitText => (Status == CryptoPositionStatus.Timeout || Status == CryptoPositionStatus.Waiting) ? "-" : Profit.ToString0(Symbol.QuoteData.DisplayFormat);
    [Computed] public string CurrentProfitText => (Status == CryptoPositionStatus.Timeout || Status == CryptoPositionStatus.Waiting) ? "-" : this.CurrentProfit().ToString0(Symbol.QuoteData.DisplayFormat);
    [Computed] public IBrush CurrentProfitForeground => GetBrushColorViaSign((decimal)this.CurrentProfit());
    [Computed] public string CurrentProfitPercentageText => (Status == CryptoPositionStatus.Timeout || Status == CryptoPositionStatus.Waiting) ? "-" : this.CurrentProfitPercentage().ToString("N2");
    [Computed] public IBrush CurrentProfitPercentageForeground => GetBrushColorViaSign((decimal)this.CurrentProfitPercentage());
    [Computed] public string QuantityText => (Status == CryptoPositionStatus.Timeout || Status == CryptoPositionStatus.Waiting) ? "-" : Quantity.ToString0(Symbol.QuantityDisplayFormat);
    [Computed] public string EntryPriceText => (Status == CryptoPositionStatus.Timeout || Status == CryptoPositionStatus.Waiting) ? "-" : EntryPrice.ToString0(Symbol.PriceDisplayFormat);
    [Computed] public string OpenText => (Status == CryptoPositionStatus.Timeout || Status == CryptoPositionStatus.Waiting) ? "-" : (Invested - Returned - Commission).ToString0(Symbol.QuoteData.DisplayFormat);

    [Computed] public string BreakEvenPercentText => this.CurrentBreakEvenPercentage().ToString("N2");
    [Computed] public IBrush BreakEvenPercentForeground => GetBrushColorViaSign((decimal)this.CurrentBreakEvenPercentage());
    [Computed] public string BreakEvenPriceText => (Status == CryptoPositionStatus.Timeout || Status == CryptoPositionStatus.Waiting) ? "-" : BreakEvenPrice.ToString0(Symbol.PriceDisplayFormat);

    [Computed] public string ProfitPriceText => (Status == CryptoPositionStatus.Timeout || Status == CryptoPositionStatus.Waiting) ? "-" : ProfitPrice?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public string CurrentPriceText => Symbol.LastPrice?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public string SignalPriceText => SignalPrice.ToString0(Symbol.PriceDisplayFormat);

    [Computed] public string SignalVolumeText => SignalVolume.ToString("N0");
    [Computed] public IBrush SignalVolumeForeground => GetVolumeColor(Symbol, SignalVolume);
    [Computed] public string PriceChangeText => Last24HoursChange.ToString("N2");
    [Computed] public string TrendPercentagePrimaryText => TrendPercentagePrimary.ToString("N2");
    [Computed] public IBrush TrendPercentagePrimaryForeground => GetBrushColorViaSign(TrendPercentagePrimary);
    [Computed] public string TrendPercentageSecondaryText => TrendPercentageSecondary.ToString("N2");
    [Computed] public IBrush TrendPercentageSecondaryForeground => GetBrushColorViaSign(TrendPercentageSecondary);
    [Computed] public string Last24HoursChangeText => Last24HoursChange.ToString("N2");
    [Computed] public IBrush Last24HoursChangeForeground => GetBrushColorViaSign(Last24HoursChange);
    [Computed] public string LastXDaysEffectiveText => LastXDaysEffective.ToString("N2");
    [Computed] public string AvgBBText => AvgBB.ToString("N2");
    [Computed] public string BbText => BollingerBandsPercentage?.ToString("N2") ?? "";
    [Computed] public string BbLowerText => BollingerBandsLowerBand.ToString0(Symbol.PriceDisplayFormat);
    [Computed] public string BbUpperText => BollingerBandsUpperBand.ToString0(Symbol.PriceDisplayFormat);
    [Computed] public string RsiText => Rsi?.ToString("N2") ?? "";
    [Computed] public IBrush RsiForeground => GetBrushColorRsi(Rsi);
    [Computed] public string LuxIndicator5mText => LuxIndicator5m.ToString("N0");
    [Computed] public IBrush LuxIndicator5mForeground => GetBrushColorViaSign((double)LuxIndicator5m);
    [Computed] public string MacdValueText => MacdValue?.ToString("N5") ?? "";
    [Computed] public IBrush MacdValueForeground => GetBrushColorViaSign(MacdValue);
    [Computed] public string MacdSignalText => MacdSignal?.ToString("N5") ?? "";
    [Computed] public IBrush MacdSignalForeground => GetBrushColorViaSign(MacdSignal);
    [Computed] public string MacdHistogramText => MacdHistogram?.ToString("N2") ?? "";
    [Computed] public IBrush MacdHistogramForeground => GetBrushColorViaSign(MacdHistogram);
    [Computed] public string StochOscillatorText => StochOscillator?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public IBrush StochOscillatorForeground => GetBrushColorStoch(StochOscillator);
    [Computed] public string StochSignalText => StochSignal?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public IBrush StochSignalForeground => GetBrushColorStoch(StochSignal);

    [Computed] public string Sma200Text => Sma200?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public string Sma50Text => Sma50?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public IBrush Sma50Foreground => GetBrushColorSma50(Side, Sma50, Sma200);
    [Computed] public string Sma20Text => Sma20?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public IBrush Sma20Foreground => GetBrushColorSma20(Side, Sma20, Sma50);
    [Computed] public string PSarText => PSar?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public IBrush PSarForeground => GetBrushColorPSar(Side, PSar, Sma20);
    
    [Computed] public string TrendIntervalText => TrendInterval == CryptoTrendIndicator.Bullish ? "up" : "down";
    [Computed] public IBrush TrendIntervalForeground => GetBrushColorTrend(TrendInterval);
    [Computed] public string Trend15mText => Trend15m == CryptoTrendIndicator.Bullish ? "up" : "down";
    [Computed] public IBrush Trend15mForeground => GetBrushColorTrend(Trend15m);
    [Computed] public string Trend30mText => Trend30m == CryptoTrendIndicator.Bullish ? "up" : "down";
    [Computed] public IBrush Trend30mForeground => GetBrushColorTrend(Trend30m);
    [Computed] public string Trend1hText => Trend1h == CryptoTrendIndicator.Bullish ? "up" : "down";
    [Computed] public IBrush Trend1hForeground => GetBrushColorTrend(Trend1h);
    [Computed] public string Trend4hText => Trend4h == CryptoTrendIndicator.Bullish ? "up" : "down";
    [Computed] public IBrush Trend4hForeground => GetBrushColorTrend(Trend4h);
    [Computed] public string Trend1dText => Trend1d == CryptoTrendIndicator.Bullish ? "up" : "down";
    [Computed] public IBrush Trend1dForeground => GetBrushColorTrend(Trend1d);
    
    [Computed] public string Barometer15mText => Barometer15m?.ToString("N2") ?? "";
    [Computed] public IBrush Barometer15mForeground => GetBrushColorViaSign(Barometer15m);
    [Computed] public string Barometer30mText => Barometer30m?.ToString("N2") ?? "";
    [Computed] public IBrush Barometer30mForeground => GetBrushColorViaSign(Barometer30m);
    [Computed] public string Barometer1hText => Barometer1h?.ToString("N2") ?? "";
    [Computed] public IBrush Barometer1hForeground => GetBrushColorViaSign(Barometer1h);
    [Computed] public string Barometer4hText => Barometer4h?.ToString("N2") ?? "";
    [Computed] public IBrush Barometer4hForeground => GetBrushColorViaSign(Barometer4h);
    [Computed] public string Barometer1dText => Barometer1d?.ToString("N2") ?? "";
    [Computed] public IBrush Barometer1dForeground => GetBrushColorViaSign(Barometer1d);
    
    [Computed] public string MinimumEntryText => MinEntry.ToString("N2");
    [Computed] public string PriceMinPercText => PriceMinPerc.ToString("N2");
    [Computed] public string PriceMaxPercText => PriceMaxPerc.ToString("N2");
    [Computed] public string SignalStatusText => GetSignalStatusText(SignalStatus);
    [Computed] public IBrush SignalStatusForeground => GetSignalStatusColor(SignalStatus);
}
