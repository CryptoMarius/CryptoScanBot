using Skender.Stock.Indicators;

namespace CryptoScanner.Core.Signal.Indicators;

/// <summary>
/// One root <see cref="QuoteHub"/> for a symbol+interval plus a cache of every indicator hub built
/// on top of it, keyed by <see cref="IndicatorKey"/>.
/// <para>
/// Two things this buys over the old hard-coded constructor. First, deduplication: an Atr(50)
/// requested by the VBS plugin and an Atr(50) requested by a strategy resolve to the same hub
/// instead of two doing identical work per candle. Second, indicators nobody asks for are simply
/// never created — that is what replaces the <c>#if DEBUG</c> blocks that used to decide, by hand
/// and in a second file, whether Ema50/Wma/Atr14/SuperTrend were computed.
/// </para>
/// <para>
/// Not thread-safe: one registry belongs to one <c>CryptoSymbolInterval</c> and is only touched
/// from the thread feeding that interval's candles, exactly like the hub it replaces.
/// </para>
/// </summary>
public sealed class IndicatorRegistry
{
    private readonly Dictionary<IndicatorKey, object> _hubs = [];
    private readonly int _cacheSize;

    // QuotePartHub instances are themselves chain providers; build them once and only when a
    // WmaLow/WmaHigh is actually requested.
    private QuotePartHub? _lowPart;
    private QuotePartHub? _highPart;

    public QuoteHub QuoteHub { get; }

    public IndicatorRegistry(int cacheSize)
    {
        _cacheSize = cacheSize;
        QuoteHub = new QuoteHub(maxCacheSize: cacheSize);
    }

    /// <summary>Everything built so far — used for diagnostics and by the tests.</summary>
    public IReadOnlyCollection<IndicatorKey> Keys => _hubs.Keys;

    /// <summary>
    /// Build the indicator for <paramref name="key"/>, or return the existing one. Unknown keys
    /// throw rather than returning null: a typo in a plugin's declaration should be loud.
    /// </summary>
    public object GetOrAdd(IndicatorKey key)
    {
        if (_hubs.TryGetValue(key, out object? existing))
            return existing;

        object created = Create(key);
        _hubs[key] = created;
        return created;
    }

    /// <summary>
    /// The already-built hub for <paramref name="key"/>, or null when nobody asked for it.
    /// <see cref="IntervalIndicatorHub.BuildCurrent"/> uses this to fill only the CryptoData
    /// fields that actually have a source.
    /// </summary>
    public T? Find<T>(IndicatorKey key) where T : class
    {
        return _hubs.TryGetValue(key, out object? hub) ? hub as T : null;
    }

    private object Create(IndicatorKey key)
    {
        int p1 = (int)key.P1;
        switch (key.Kind)
        {
            case IndicatorKind.BollingerBands:
                return QuoteHub.ToBollingerBandsHub(p1, key.P2);
            case IndicatorKind.Sma:
                return QuoteHub.ToSmaHub(p1);
            case IndicatorKind.Ema:
                return QuoteHub.ToEmaHub(p1);
            case IndicatorKind.Rsi:
                return QuoteHub.ToRsiHub(p1);
            case IndicatorKind.Macd:
                return QuoteHub.ToMacdHub(p1, (int)key.P2, (int)key.P3);
            case IndicatorKind.Stoch:
                return QuoteHub.ToStochHub(p1, (int)key.P2, (int)key.P3);
            case IndicatorKind.ParabolicSar:
                return QuoteHub.ToParabolicSarHub(key.P1, key.P2);
            case IndicatorKind.Atr:
                return QuoteHub.ToAtrHub(p1);
            case IndicatorKind.WmaLow:
                _lowPart ??= QuoteHub.ToQuotePartHub(CandlePart.Low);
                return _lowPart.ToWmaHub(p1);
            case IndicatorKind.WmaHigh:
                _highPart ??= QuoteHub.ToQuotePartHub(CandlePart.High);
                return _highPart.ToWmaHub(p1);
            case IndicatorKind.SuperTrend:
                return QuoteHub.ToSuperTrendHub(p1, key.P2);
            default:
                throw new NotSupportedException($"IndicatorRegistry cannot build {key}");
        }
    }

    // ── Typed convenience accessors ──────────────────────────────────────
    // Plugins and the hub use these instead of building Skender hubs themselves, so every request
    // goes through the same cache.

    public BollingerBandsHub BollingerBands(int length, double deviation)
        => (BollingerBandsHub)GetOrAdd(IndicatorKey.BollingerBands(length, deviation));

    public SmaHub Sma(int length) => (SmaHub)GetOrAdd(IndicatorKey.Sma(length));

    public EmaHub Ema(int length) => (EmaHub)GetOrAdd(IndicatorKey.Ema(length));

    public RsiHub Rsi(int length) => (RsiHub)GetOrAdd(IndicatorKey.Rsi(length));

    public MacdHub Macd(int fast, int slow, int signal)
        => (MacdHub)GetOrAdd(IndicatorKey.Macd(fast, slow, signal));

    public StochHub Stoch(int length, int smoothD, int smoothK)
        => (StochHub)GetOrAdd(IndicatorKey.Stoch(length, smoothD, smoothK));

    public ParabolicSarHub ParabolicSar(double step, double max)
        => (ParabolicSarHub)GetOrAdd(IndicatorKey.ParabolicSar(step, max));

    public AtrHub Atr(int length) => (AtrHub)GetOrAdd(IndicatorKey.Atr(length));

    public WmaHub WmaLow(int length) => (WmaHub)GetOrAdd(IndicatorKey.WmaLow(length));

    public WmaHub WmaHigh(int length) => (WmaHub)GetOrAdd(IndicatorKey.WmaHigh(length));

    public SuperTrendHub SuperTrend(int lookback, double multiplier)
        => (SuperTrendHub)GetOrAdd(IndicatorKey.SuperTrend(lookback, multiplier));

    /// <summary>
    /// A separate QuoteHub for synthetic series (a plugin feeding hlc3, hlc3², candle range%, …).
    /// Those cannot chain off the price QuoteHub because the plugin supplies its own values, so the
    /// registry just hands out a correctly sized hub instead of every plugin inventing one.
    /// </summary>
    public QuoteHub CreateDerivedHub() => new(maxCacheSize: _cacheSize);
}
