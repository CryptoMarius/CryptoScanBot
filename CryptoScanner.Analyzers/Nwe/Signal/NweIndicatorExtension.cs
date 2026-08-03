using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Model;

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

    public void Init(QuoteHub quoteHub)
    {
        var s = NwePlugin.Settings;
        _repIndicator = new NweIndicator(bandwidth: s.BandWidth, multiplier: s.Multiplication, smoothRepainting: true);
        _npIndicator = new NweIndicator(bandwidth: s.BandWidth, multiplier: s.Multiplication, smoothRepainting: false);
    }

    public void OnCandleAdded(IQuote candle)
    {
        _closes.Add((double)candle.Close);
        _openTimes.Add(CandleTime.FromDateTime(candle.Date));

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

        // Repainting NWE (used by SignalNwe + SignalNweBb)
        if (_repIndicator != null)
        {
            var results = _repIndicator.CalculateCore(closesArr, timesArr, n);
            var last = results[^1];
            if (last.Center != null) data.SetCustom("NweCenter", (double)last.Center.Value);
            if (last.Upper != null) data.SetCustom("NweUpper", (double)last.Upper.Value);
            if (last.Lower != null) data.SetCustom("NweLower", (double)last.Lower.Value);
        }

        // Non-repainting NWE (used by SignalNweNp)
        if (_npIndicator != null)
        {
            var results = _npIndicator.CalculateCore(closesArr, timesArr, n);
            var last = results[^1];
            if (last.Center != null) data.SetCustom("NweNpCenter", (double)last.Center.Value);
            if (last.Upper != null) data.SetCustom("NweNpUpper", (double)last.Upper.Value);
            if (last.Lower != null) data.SetCustom("NweNpLower", (double)last.Lower.Value);
        }
    }
}
