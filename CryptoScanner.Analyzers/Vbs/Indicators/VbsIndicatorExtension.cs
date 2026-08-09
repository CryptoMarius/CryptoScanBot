using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Indicators;

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
    private QuoteHub? _vbsSrcHub;
    private QuoteHub? _vbsSqHub;
    private QuoteHub? _rangeHub;
    private VwmaHub? _vpsVwmaSrc;
    private VwmaHub? _vpsVwmaSq;
    private SmaHub? _rangeSma;
    private AtrHub? _atrVpsSl;
    private double _vbsMult;
    private double _acsFactor;

    public void Init(IndicatorRegistry registry)
    {
        var vbs = VbsPlugin.Settings;

        // Through the registry, so an Atr(Length) requested elsewhere is the same hub instead of a
        // second one doing identical work on every candle.
        _atrVpsSl = registry.Atr(vbs.Length);

        // The VWAP band needs hlc3 and hlc3 squared, which are values this plugin produces itself —
        // they cannot chain off the price hub, hence a derived hub per series.
        _vbsSrcHub = registry.CreateDerivedHub();
        _vbsSqHub = registry.CreateDerivedHub();
        _vpsVwmaSrc = _vbsSrcHub.ToVwmaHub(vbs.Length);
        _vpsVwmaSq = _vbsSqHub.ToVwmaHub(vbs.Length);
        _vbsMult = vbs.Mult;

        // ACS (Average Candle Size): SMA of the per-candle range% = (high-low)/close*100, over AcsLength.
        _rangeHub = registry.CreateDerivedHub();
        _rangeSma = _rangeHub.ToSmaHub(vbs.AcsLength);
        _acsFactor = vbs.AcsFactor;
    }

    public void OnCandleAdded(IQuote candle)
    {
        if (_vbsSrcHub == null)
            return;
        decimal hlc3 = (candle.High + candle.Low + candle.Close) / 3m;
        _vbsSrcHub.Add(new Quote(candle.Timestamp, 0m, 0m, 0m, hlc3, candle.Volume));
        _vbsSqHub!.Add(new Quote(candle.Timestamp, 0m, 0m, 0m, hlc3 * hlc3, candle.Volume));

        // Candle range% carried as the Close of a synthetic quote so the SMA hub averages it.
        decimal rangePct = candle.Close != 0m ? (candle.High - candle.Low) / candle.Close * 100m : 0m;
        _rangeHub!.Add(new Quote(candle.Timestamp, 0m, 0m, 0m, rangePct, 0m));
    }

    public void FillData(CryptoData data)
    {
        var vbsData = new VbsCandleData();
        bool any = false;

        if (_atrVpsSl?.Results.Count > 0 && _atrVpsSl.Results[^1].Atr != null)
        {
            vbsData.AtrSl = _atrVpsSl.Results[^1].Atr;
            any = true;
        }

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
                vbsData.Basis = mean.Value;
                vbsData.Upper = mean.Value + pad;
                vbsData.Lower = mean.Value - pad;
                vbsData.VwStdev = vwStdev;
                any = true;
            }
        }

        // ACS% = AcsFactor * SMA(range%, AcsLength). Drives the stop-loss (SL = entry -/+ Acs%).
        if (_rangeSma?.Results.Count > 0 && _rangeSma.Results[^1].Sma is double sma)
        {
            vbsData.Acs = _acsFactor * sma;
            any = true;
        }

        // Nothing computed yet during warm-up — leave the slot empty instead of attaching an
        // all-null object, so a strategy can tell "not ready" from "ready but zero".
        if (any)
            data.SetPluginData(vbsData);
    }
}
