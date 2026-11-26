using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Indicators;

using Skender.Stock.Indicators;

namespace CryptoScanner.Analyzers.ImpulseBox.Indicators;

/// <summary>
/// Keeps the impulse box detector per symbol+interval and writes what it saw on the last candle to
/// the plugin slot of CryptoData. No Skender hubs: the detector is a state machine of its own and
/// keeps its own ATR.
/// </summary>
public class ImpulseBoxIndicatorExtension : IIndicatorExtension
{
    private ImpulseBoxDetector? _detector;
    private List<ImpulseBoxEvent>? _events;

    public void Init(IndicatorRegistry registry)
    {
        _detector = new ImpulseBoxDetector(ImpulseBoxPlugin.Settings);
    }

    public void OnCandleAdded(IQuote candle)
    {
        if (_detector == null)
            return;
        _events = _detector.Add((double)candle.Open, (double)candle.High, (double)candle.Low, (double)candle.Close);
    }

    public void FillData(CryptoData data)
    {
        // Leave the slot empty while the ATR has no value, so the signal can tell "not ready" apart
        if (_detector == null || !_detector.IsReady)
            return;

        data.SetPluginData(new ImpulseBoxCandleData
        {
            Events = _events,
            Status = _detector.Status,
        });
    }
}
