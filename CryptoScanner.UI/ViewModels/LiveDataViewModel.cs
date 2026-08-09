using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.UI.ViewModels;

public class LiveDataViewModel
{
    public CryptoLiveData Object { get; }

    public LiveDataViewModel(CryptoLiveData liveData)
    {
        Object = liveData;
    }

    public string GetCellValue(LiveDataColumnEnum column)
    {
        return column switch
        {
            LiveDataColumnEnum.Date => FormatDate(),
            LiveDataColumnEnum.Exchange => Object.Symbol.Exchange.Name,
            LiveDataColumnEnum.Symbol => Object.Symbol.Name,
            LiveDataColumnEnum.Volume => Object.Symbol.Volume.ToString("N0"),
            LiveDataColumnEnum.Interval => Object.Interval.Name,
            LiveDataColumnEnum.Price => Object.Candle.Close.ToString0(Object.Symbol.PriceDisplayFormat),
            LiveDataColumnEnum.BB => Object.CandleData?.BollingerBandsPercentage?.ToString("N2") ?? "",
            LiveDataColumnEnum.BbLower => Object.CandleData?.BollingerBandsLowerBand?.ToString0(Object.Symbol.PriceDisplayFormat) ?? "",
            LiveDataColumnEnum.BbUpper => Object.CandleData?.BollingerBandsUpperBand.ToString0(Object.Symbol.PriceDisplayFormat) ?? "",
            LiveDataColumnEnum.Rsi => Object.CandleData?.Rsi.ToString0("N2") ?? "",
            LiveDataColumnEnum.LuxIndicator5m => Object.CandleData?.Lux5mValue?.ToString("N0") ?? "",
            LiveDataColumnEnum.MacdValue => Object.CandleData?.MacdValue?.ToString("N5") ?? "",
            LiveDataColumnEnum.MacdSignal => Object.CandleData?.MacdSignal?.ToString("N5") ?? "",
            LiveDataColumnEnum.MacdHistogram => Object.CandleData?.MacdHistogram?.ToString("N2") ?? "",
            LiveDataColumnEnum.StochOscillator => Object.CandleData?.StochOscillator?.ToString("N2") ?? "",
            LiveDataColumnEnum.StochSignal => Object.CandleData?.StochSignal?.ToString("N2") ?? "",
            LiveDataColumnEnum.Sma200 => Object.CandleData?.Sma200?.ToString0(Object.Symbol.PriceDisplayFormat) ?? "",
            LiveDataColumnEnum.Sma50 => Object.CandleData?.Sma50?.ToString0(Object.Symbol.PriceDisplayFormat) ?? "",
            LiveDataColumnEnum.Sma20 => Object.CandleData?.Sma20?.ToString0(Object.Symbol.PriceDisplayFormat) ?? "",
            LiveDataColumnEnum.PSar => Object.CandleData?.PSar?.ToString0(Object.Symbol.PriceDisplayFormat) ?? "",
            LiveDataColumnEnum.FundingRate => Object.Symbol.FundingRate.ToString0(),
            _ => "",
        };
    }

    public string GetCellColorClass(LiveDataColumnEnum column)
    {
        return column switch
        {
            LiveDataColumnEnum.Volume => ColorHelper.GetVolumeColorClass(Object.Symbol, (double)Object.Symbol.Volume),
            LiveDataColumnEnum.Rsi => ColorHelper.GetColorClassRsi(Object.CandleData?.Rsi),
            LiveDataColumnEnum.LuxIndicator5m => ColorHelper.GetColorClassViaSign((double)(Object.CandleData?.Lux5mValue ?? 0)),
            LiveDataColumnEnum.MacdValue => ColorHelper.GetColorClassViaSign(Object.CandleData?.MacdValue),
            LiveDataColumnEnum.MacdSignal => ColorHelper.GetColorClassViaSign(Object.CandleData?.MacdSignal),
            LiveDataColumnEnum.MacdHistogram => ColorHelper.GetColorClassViaSign(Object.CandleData?.MacdHistogram),
            LiveDataColumnEnum.StochOscillator => ColorHelper.GetColorClassStoch(Object.CandleData?.StochOscillator),
            LiveDataColumnEnum.StochSignal => ColorHelper.GetColorClassStoch(Object.CandleData?.StochSignal),
            LiveDataColumnEnum.Sma50 => GetSma50ColorClass(),
            LiveDataColumnEnum.Sma20 => GetSma20ColorClass(),
            LiveDataColumnEnum.PSar => GetPSarColorClass(),
            _ => "",
        };
    }

    public string GetBackgroundStyle(LiveDataColumnEnum column)
    {
        return column switch
        {
            LiveDataColumnEnum.Symbol => ColorHelper.GetBackgroundStyle(Object.Symbol.QuoteData),
            _ => "",
        };
    }

    private string FormatDate()
    {
        var open = Object.Candle.Date.ToLocalTime();
        var close = Object.Candle.Date.AddSeconds(Object.Interval.Duration).ToLocalTime();
        return $"{open:yyyy-MM-dd HH:mm} - {close:HH:mm}";
    }

    private string GetSma50ColorClass()
    {
        if (Object.CandleData?.Sma50 == null || Object.CandleData?.Sma200 == null)
            return "";
        return Object.CandleData.Sma50 > Object.CandleData.Sma200
            ? ColorHelper.Green : ColorHelper.Red;
    }

    private string GetSma20ColorClass()
    {
        if (Object.CandleData?.Sma20 == null || Object.CandleData?.Sma50 == null)
            return "";
        return Object.CandleData.Sma20 > Object.CandleData.Sma50
            ? ColorHelper.Green : ColorHelper.Red;
    }

    private string GetPSarColorClass()
    {
        if (Object.CandleData?.PSar == null || Object.CandleData?.Sma20 == null)
            return "";
        return Object.CandleData.PSar > Object.CandleData.Sma20
            ? ColorHelper.Red : ColorHelper.Green;
    }
}
