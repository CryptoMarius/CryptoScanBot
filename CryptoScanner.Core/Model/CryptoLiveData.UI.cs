using Avalonia.Media;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Model;

using static CryptoScanner.Core.Model.CryptoDisplayHelpers;

// Because (multivalue) converters are way to slow.

public partial class CryptoLiveData
{
    [Computed]
    public string DateText
    {
        get
        {
            var closeData = Candle.Date.AddMinutes(Interval.Duration);
            return Candle.Date.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + " - " + closeData.ToLocalTime().ToString("HH:mm");
        }
    }

    [Computed] public string ExchangeText => Symbol.Exchange.Name;
    [Computed] public string SymbolText => Symbol.Name;
    [Computed] public IBrush SymbolBackground => new SolidColorBrush(Symbol.QuoteData.DisplayColor);
    [Computed] public string IntervalText => Interval.Name;

    [Computed] public string PriceText => Candle.Close.ToString0(Symbol.PriceDisplayFormat);

    [Computed] public string VolumeText => Symbol.Volume.ToString("N0");
    [Computed] public IBrush VolumeForeground => GetVolumeColor(Symbol, Symbol.Volume);

    [Computed] public string BBText => CandleData?.BollingerBandsPercentage?.ToString("N2") ?? "";
    [Computed] public string BbLowerText => CandleData?.BollingerBandsLowerBand?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public string BbUpperText => CandleData?.BollingerBandsUpperBand?.ToString0(Symbol.PriceDisplayFormat) ?? "";

    [Computed] public string RsiText => CandleData?.Rsi?.ToString("N2") ?? "";
    [Computed] public IBrush RsiForeground => GetBrushColorRsi(CandleData?.Rsi);

    [Computed] public string LuxIndicator5mText => CandleData?.Lux5mValue.ToString("N0") ?? "";
    [Computed] public IBrush LuxIndicator5mForeground => GetBrushColorViaSign((double)(CandleData?.Lux5mValue ?? 0));

    [Computed] public string MacdValueText => CandleData?.MacdValue?.ToString("N5") ?? "";
    [Computed] public IBrush MacdValueForeground => GetBrushColorViaSign(CandleData?.MacdValue);

    [Computed] public string MacdSignalText => CandleData?.MacdSignal?.ToString("N5") ?? "";
    [Computed] public IBrush MacdSignalForeground => GetBrushColorViaSign(CandleData?.MacdSignal);

    [Computed] public string MacdHistogramText => CandleData?.MacdHistogram?.ToString("N2") ?? "";
    [Computed] public IBrush MacdHistogramForeground => GetBrushColorViaSign(CandleData?.MacdHistogram);

    [Computed] public string StochOscillatorText => CandleData?.StochOscillator?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public IBrush StochOscillatorForeground => GetBrushColorStoch(CandleData?.StochOscillator);

    [Computed] public string StochSignalText => CandleData?.StochSignal?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public IBrush StochSignalForeground => GetBrushColorStoch(CandleData?.StochSignal);

    [Computed] public string Sma200Text => CandleData?.Sma200?.ToString0(Symbol.PriceDisplayFormat) ?? "";

    [Computed] public string Sma50Text => CandleData?.Sma50?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public IBrush Sma50Foreground => GetBrushColorSma50(CryptoTradeSide.Long, CandleData?.Sma50, CandleData?.Sma200);

    [Computed] public string Sma20Text => CandleData?.Sma20?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public IBrush Sma20Foreground => GetBrushColorSma20(CryptoTradeSide.Long, CandleData?.Sma20, CandleData?.Sma50);

    [Computed] public string PSarText => CandleData?.PSar?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public IBrush PSarForeground => GetBrushColorPSar(CryptoTradeSide.Long, CandleData?.PSar, CandleData?.Sma20);

    [Computed] public string FundingRateText => Symbol.FundingRate.ToString("N2") ?? "";

}