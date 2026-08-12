using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Indicators;

using Skender.Stock.Indicators;

namespace CryptoScanner.Analyzers.Nwe.Signal;

/// <summary>
/// Plugs NWE into the IntervalIndicatorHub so that center/upper/lower values
/// are precomputed once per candle — signal strategies read the result from
/// CryptoData instead of recalculating the O(window * effectiveRange) kernel.
/// </summary>
public class NweIndicatorExtension : IIndicatorExtension
{
    private NweIndicator? _repIndicator;
    private NweIndicator? _npIndicator;

    private readonly List<double> _closes = new(600);
    private readonly List<CandleTime> _openTimes = new(600);

    public void Init(IndicatorRegistry registry)
    {
        // NWE runs its own kernel over a rolling close buffer, so it needs no hub from the registry.
        var s = NwePlugin.Settings;
        _repIndicator = new NweIndicator(bandwidth: s.BandWidth, multiplier: s.Multiplication, smoothRepainting: true);
        _npIndicator = new NweIndicator(bandwidth: s.BandWidth, multiplier: s.Multiplication, smoothRepainting: false);
    }

    public void OnCandleAdded(IQuote candle)
    {
        _closes.Add((double)candle.Close);
        _openTimes.Add(CandleTime.FromDateTime(candle.Timestamp));

        int maxBuffer = (_repIndicator?.Length ?? 500) + 50;
        if (_closes.Count > maxBuffer)
        {
            int trim = _closes.Count - (_repIndicator?.Length ?? 500);
            _closes.RemoveRange(0, trim);
            _openTimes.RemoveRange(0, trim);
        }
    }

    public void FillData(CryptoData data)
    {
        int n = _closes.Count;
        if (n < 1)
            return;

        var closesArr = _closes.ToArray();
        var timesArr = _openTimes.ToArray();
        var nweData = new NweCandleData();

        // Repainting NWE (used by SignalNwe + SignalNweBb)
        if (_repIndicator != null)
        {
            var results = _repIndicator.CalculateCore(closesArr, timesArr, n);
            var last = results[^1];
            if (last.Center != null) nweData.Center = (double)last.Center.Value;
            if (last.Upper != null) nweData.Upper = (double)last.Upper.Value;
            if (last.Lower != null) nweData.Lower = (double)last.Lower.Value;
        }

        // Non-repainting NWE (used by SignalNweNp)
        if (_npIndicator != null)
        {
            var results = _npIndicator.CalculateCore(closesArr, timesArr, n);
            var last = results[^1];
            if (last.Center != null) nweData.NpCenter = (double)last.Center.Value;
            if (last.Upper != null) nweData.NpUpper = (double)last.Upper.Value;
            if (last.Lower != null) nweData.NpLower = (double)last.Lower.Value;
        }

        data.SetPluginData(nweData);
    }
}
