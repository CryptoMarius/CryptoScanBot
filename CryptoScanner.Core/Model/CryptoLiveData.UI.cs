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
            var closeData = Candle.Date.AddSeconds(Interval.Duration);
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

    [Computed] public string BbText => Candle.CandleData?.BollingerBandsPercentage?.ToString("N2") ?? "";
    [Computed] public string BbLowerText => Candle.CandleData?.BollingerBandsLowerBand?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public string BbUpperText => Candle.CandleData?.BollingerBandsUpperBand?.ToString0(Symbol.PriceDisplayFormat) ?? "";

    [Computed] public string RsiText => Candle.CandleData?.Rsi?.ToString("N2") ?? "";
    [Computed] public IBrush RsiForeground => GetBrushColorRsi(Candle.CandleData?.Rsi);

    [Computed] public string LuxIndicator5mText => Candle.CandleData?.Lux5mValue.ToString("N0") ?? "";
    [Computed] public IBrush LuxIndicator5mForeground => GetBrushColorViaSign((double)(Candle.CandleData?.Lux5mValue ?? 0));

    [Computed] public string MacdValueText => Candle.CandleData?.MacdValue?.ToString("N5") ?? "";
    [Computed] public IBrush MacdValueForeground => GetBrushColorViaSign(Candle.CandleData?.MacdValue);

    [Computed] public string MacdSignalText => Candle.CandleData?.MacdSignal?.ToString("N5") ?? "";
    [Computed] public IBrush MacdSignalForeground => GetBrushColorViaSign(Candle.CandleData?.MacdSignal);

    [Computed] public string MacdHistogramText => Candle.CandleData?.MacdHistogram?.ToString("N2") ?? "";
    [Computed] public IBrush MacdHistogramForeground => GetBrushColorViaSign(Candle.CandleData?.MacdHistogram);

    [Computed] public string StochOscillatorText => Candle.CandleData?.StochOscillator?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public IBrush StochOscillatorForeground => GetBrushColorStoch(Candle.CandleData?.StochOscillator);

    [Computed] public string StochSignalText => Candle.CandleData?.StochSignal?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public IBrush StochSignalForeground => GetBrushColorStoch(Candle.CandleData?.StochSignal);

    [Computed] public string Sma200Text => Candle.CandleData?.Sma200?.ToString0(Symbol.PriceDisplayFormat) ?? "";

    [Computed] public string Sma50Text => Candle.CandleData?.Sma50?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public IBrush Sma50Foreground => GetBrushColorSma50(CryptoTradeSide.Long, Candle.CandleData?.Sma50, Candle.CandleData?.Sma200);

    [Computed] public string Sma20Text => Candle.CandleData?.Sma20?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public IBrush Sma20Foreground => GetBrushColorSma20(CryptoTradeSide.Long, Candle.CandleData?.Sma20, Candle.CandleData?.Sma50);

    [Computed] public string PSarText => Candle.CandleData?.PSar?.ToString0(Symbol.PriceDisplayFormat) ?? "";
    [Computed] public IBrush PSarForeground => GetBrushColorPSar(CryptoTradeSide.Long, Candle.CandleData?.PSar, Candle.CandleData?.Sma20);
}