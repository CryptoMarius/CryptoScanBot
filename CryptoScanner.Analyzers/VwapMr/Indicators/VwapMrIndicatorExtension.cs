using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Indicators;

using Skender.Stock.Indicators;

namespace CryptoScanner.Analyzers.VwapMr.Indicators;

/// <summary>
/// Keeps the day VWAP and the stdev of the close per symbol+interval, and writes the band to the
/// plugin slot of CryptoData. No Skender hubs: the VWAP restarts every day, which no standard hub
/// does, and the stdev is twenty numbers.
/// </summary>
public class VwapMrIndicatorExtension : IIndicatorExtension
{
    private VwapMrBandCalculator? _calculator;
    private VwapMrBandCalculator.BandValue _current;

    public void Init(IndicatorRegistry registry)
    {
        var settings = VwapMrPlugin.Settings;
        _calculator = new VwapMrBandCalculator(settings.StdevLength, settings.Mult);
    }

    public void OnCandleAdded(IQuote candle)
    {
        if (_calculator == null)
            return;
        _current = _calculator.Add(candle.Timestamp, candle.High, candle.Low, candle.Close, candle.Volume);
    }

    public void FillData(CryptoData data)
    {
        // Leave the slot empty while there is no complete day, so the signal can tell "not ready"
        // from a value.
        if (!_current.HasValue)
            return;

        data.SetPluginData(new VwapMrCandleData
        {
            Vwap = _current.Vwap,
            Upper = _current.Upper,
            Lower = _current.Lower,
        });
    }
}
