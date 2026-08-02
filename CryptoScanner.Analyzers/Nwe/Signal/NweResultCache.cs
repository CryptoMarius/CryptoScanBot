using System.Runtime.CompilerServices;

using CryptoScanner.Core.Model;

namespace CryptoScanner.Analyzers.Nwe.Signal;

/// <summary>
/// Per-CandleList cache for NWE indicator results.
/// Avoids recalculating the O(window^2) kernel when multiple signal strategies
/// (NWE long/short, NweBb long/short) query the same interval within one tick.
/// </summary>
internal static class NweResultCache
{
    private sealed class CacheSlot
    {
        public CacheEntry? Repainting;
        public CacheEntry? NonRepainting;
    }

    private sealed class CacheEntry
    {
        public long LastOpenTimeMinutes;
        public int Count;
        public double Bandwidth;
        public decimal Multiplier;
        public List<NweIndicator.NweResult> Results = [];
    }

    private static readonly ConditionalWeakTable<CryptoCandleList, CacheSlot> Cache = new();

    public static List<NweIndicator.NweResult> GetOrCalculate(
        CryptoCandleList candles,
        double bandwidth,
        decimal multiplier,
        bool smoothRepainting)
    {
        var slot = Cache.GetValue(candles, static _ => new CacheSlot());

        ref var entryRef = ref smoothRepainting ? ref slot.Repainting : ref slot.NonRepainting;

        long lastMinutes = candles.LastCandle.OpenTime.Minutes;
        int count = candles.Count;

        var entry = entryRef;
        if (entry != null
            && entry.LastOpenTimeMinutes == lastMinutes
            && entry.Count == count
            && entry.Bandwidth == bandwidth
            && entry.Multiplier == multiplier)
        {
            return entry.Results;
        }

        var indicator = new NweIndicator(
            bandwidth: bandwidth,
            multiplier: multiplier,
            smoothRepainting: smoothRepainting);
        var results = indicator.Calculate(candles);

        entryRef = new CacheEntry
        {
            LastOpenTimeMinutes = lastMinutes,
            Count = count,
            Bandwidth = bandwidth,
            Multiplier = multiplier,
            Results = results,
        };

        return results;
    }
}
