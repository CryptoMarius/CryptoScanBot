using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Model;

using Skender.Stock.Indicators;

namespace CryptoScanner.Analyzers.Baba.Indicators;

/// <summary>
/// Registers the Baba VWAP band hubs on the shared QuoteHub: two synthetic
/// hubs (hlc3 and hlc3^2) for the volume-weighted variance, plus ATR hubs
/// for the pad term and the stop-loss %. Writes the band values to the
/// dedicated CryptoData fields (BabaBasis/Upper/Lower/VwStdev/AtrBaba/BabaAtrSl).
/// </summary>
public class BabaIndicatorExtension : IIndicatorExtension
{
    private const int HubCacheSize = 300;

    private QuoteHub? _babaSrcHub;
    private QuoteHub? _babaSqHub;
    private VwmaHub? _babaVwmaSrc;
    private VwmaHub? _babaVwmaSq;
    private AtrHub? _atrBaba;
    private AtrHub? _atrBabaSl;
    private double _babaMult;
    private double _babaAtrMult;

    public void Init(QuoteHub quoteHub)
    {
        var baba = BabaPlugin.Settings;
        _atrBaba = quoteHub.ToAtrHub(baba.AtrLength);
        _atrBabaSl = quoteHub.ToAtrHub(baba.Length);

        _babaSrcHub = new QuoteHub(maxCacheSize: HubCacheSize);
        _babaSqHub = new QuoteHub(maxCacheSize: HubCacheSize);
        _babaVwmaSrc = _babaSrcHub.ToVwmaHub(baba.Length);
        _babaVwmaSq = _babaSqHub.ToVwmaHub(baba.Length);
        _babaMult = baba.Mult;
        _babaAtrMult = baba.AtrMult;
    }

    public void OnCandleAdded(IQuote candle)
    {
        if (_babaSrcHub == null)
            return;
        decimal hlc3 = (candle.High + candle.Low + candle.Close) / 3m;
        _babaSrcHub.Add(new Quote(candle.Timestamp, 0m, 0m, 0m, hlc3, candle.Volume));
        _babaSqHub!.Add(new Quote(candle.Timestamp, 0m, 0m, 0m, hlc3 * hlc3, candle.Volume));
    }

    public void FillData(CryptoData data)
    {
        if (_atrBaba?.Results.Count > 0 && _atrBaba.Results[^1].Atr != null)
            data.AtrBaba = _atrBaba.Results[^1].Atr;
        if (_atrBabaSl?.Results.Count > 0 && _atrBabaSl.Results[^1].Atr != null)
            data.BabaAtrSl = _atrBabaSl.Results[^1].Atr;

        var babaSrc = _babaVwmaSrc?.Results;
        var babaSq = _babaVwmaSq?.Results;
        if (babaSrc?.Count > 0 && babaSq?.Count > 0)
        {
            double? mean = babaSrc[^1].Vwma;
            double? second = babaSq[^1].Vwma;
            if (mean.HasValue && second.HasValue)
            {
                double variance = second.Value - mean.Value * mean.Value;
                double vwStdev = variance > 0 ? Math.Sqrt(variance) : 0;
                double pad = _babaMult * vwStdev + _babaAtrMult * (data.AtrBaba ?? 0);
                data.BabaBasis = mean.Value;
                data.BabaUpper = mean.Value + pad;
                data.BabaLower = mean.Value - pad;
                data.BabaVwStdev = vwStdev;
            }
        }
    }
}
