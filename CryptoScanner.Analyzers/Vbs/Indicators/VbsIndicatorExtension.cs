using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Model;

using Skender.Stock.Indicators;

namespace CryptoScanner.Analyzers.Vbs.Indicators;

/// <summary>
/// Registers the VWAP band hubs on the shared QuoteHub: two synthetic
/// hubs (hlc3 and hlc3^2) for the volume-weighted variance, plus ATR hubs
/// for the pad term and the stop-loss %. Writes the band values to the
/// dedicated CryptoData fields
/// </summary>
public class VbsIndicatorExtension : IIndicatorExtension
{
    private const int HubCacheSize = 300;

    private QuoteHub? _vbsSrcHub;
    private QuoteHub? _vbsSqHub;
    private VwmaHub? _vpsVwmaSrc;
    private VwmaHub? _vpsVwmaSq;
    private AtrHub? _atrVpsSl;
    private double _vbsMult;

    public void Init(QuoteHub quoteHub)
    {
        var vbs = VbsPlugin.Settings;
        _atrVpsSl = quoteHub.ToAtrHub(vbs.Length);

        _vbsSrcHub = new QuoteHub(maxCacheSize: HubCacheSize);
        _vbsSqHub = new QuoteHub(maxCacheSize: HubCacheSize);
        _vpsVwmaSrc = _vbsSrcHub.ToVwmaHub(vbs.Length);
        _vpsVwmaSq = _vbsSqHub.ToVwmaHub(vbs.Length);
        _vbsMult = vbs.Mult;
    }

    public void OnCandleAdded(IQuote candle)
    {
        if (_vbsSrcHub == null)
            return;
        decimal hlc3 = (candle.High + candle.Low + candle.Close) / 3m;
        _vbsSrcHub.Add(new Quote(candle.Timestamp, 0m, 0m, 0m, hlc3, candle.Volume));
        _vbsSqHub!.Add(new Quote(candle.Timestamp, 0m, 0m, 0m, hlc3 * hlc3, candle.Volume));
    }

    public void FillData(CryptoData data)
    {
        if (_atrVpsSl?.Results.Count > 0 && _atrVpsSl.Results[^1].Atr != null)
            data.VbsAtrSl = _atrVpsSl.Results[^1].Atr;

        var vbsSrc = _vpsVwmaSrc?.Results;
        var vbsSq = _vpsVwmaSq?.Results;
        if (vbsSrc?.Count > 0 && vbsSq?.Count > 0)
        {
            double? mean = vbsSrc[^1].Vwma;
            double? second = vbsSq[^1].Vwma;
            if (mean.HasValue && second.HasValue)
            {
                double variance = second.Value - mean.Value * mean.Value;
                double vwStdev = variance > 0 ? Math.Sqrt(variance) : 0;
                double pad = _vbsMult * vwStdev;
                data.VbsBasis = mean.Value;
                data.VbsUpper = mean.Value + pad;
                data.VbsLower = mean.Value - pad;
                data.VbsVwStdev = vwStdev;
            }
        }
    }
}
