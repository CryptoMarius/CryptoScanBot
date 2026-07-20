// CONCEPT — not yet wired into IndicatorEngine / CryptoSymbolInterval.
// Drop-in replacement for IntervalIndicatorHub: strategies declare which indicators they need;
// only those hubs are created and fed. Identical (kind, params) requests share one hub instance.

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Indicators;

using Skender.Stock.Indicators;

namespace CryptoScanner.Analyzers;

// ---------------------------------------------------------------------------
// Key types
// ---------------------------------------------------------------------------

public enum IndicatorKind
{
    Sma,
    Ema,
    BollingerBands,
    Rsi,
    Macd,
    Stoch,
    ParabolicSar,
    Atr,
    // BabaVwap has been migrated to BabaIndicatorExtension (Analyzers plugin).
}

/// <summary>
/// Immutable, hashable identity for one indicator variant. P1–P4 encode the constructor parameters;
/// unused slots stay 0. Record struct = structural equality for free, cheap as dictionary key.
/// </summary>
public readonly record struct IndicatorKey(
    IndicatorKind Kind,
    double P1 = 0, double P2 = 0, double P3 = 0, double P4 = 0)
{
    public static IndicatorKey Sma(int length) => new(IndicatorKind.Sma, length);
    public static IndicatorKey Ema(int length) => new(IndicatorKind.Ema, length);
    public static IndicatorKey Bb(int length, double dev) => new(IndicatorKind.BollingerBands, length, dev);
    public static IndicatorKey Rsi(int length) => new(IndicatorKind.Rsi, length);
    public static IndicatorKey Macd(int fast = 12, int slow = 26, int signal = 9) => new(IndicatorKind.Macd, fast, slow, signal);
    public static IndicatorKey Stoch(int length, int smoothD, int smoothK) => new(IndicatorKind.Stoch, length, smoothD, smoothK);
    public static IndicatorKey Psar(double step = 0.02, double max = 0.2) => new(IndicatorKind.ParabolicSar, step, max);
    public static IndicatorKey Atr(int length) => new(IndicatorKind.Atr, length);
    // BabaVwap key factory has been migrated to BabaIndicatorExtension (Analyzers plugin).
}

// BabaVwapState record has been migrated to BabaIndicatorExtension (Analyzers plugin).

// ---------------------------------------------------------------------------
// Interface: strategy declares its required indicators once
// ---------------------------------------------------------------------------

/// <summary>
/// Strategies that implement this interface declare their indicator dependencies up-front.
/// IndicatorEngine collects the union of all active strategies' requirements and passes them
/// to IndicatorRegistry.EnsureRegistered() before the warm-up; only the requested hubs
/// are created and fed per candle.
/// </summary>
public interface IRequiresIndicators
{
    IEnumerable<IndicatorKey> RequiredIndicators();
}

// ---------------------------------------------------------------------------
// Registry
// ---------------------------------------------------------------------------

/// <summary>
/// Per-symbol+interval indicator hub registry. Replaces the hardcoded <see cref="IntervalIndicatorHub"/>.
///
/// Usage (IndicatorEngine.PrepareViaHub):
///   1. On warm-up: create a new IndicatorRegistry, call EnsureRegistered() with the union of all
///      active strategy requirements, then feed the full candle history via Add().
///   2. Per new candle: call Add(candle), then BuildCurrent() for the CryptoData snapshot.
///
/// Sharing: strategies requesting the same (kind, params) key share one hub instance. Strategies
/// not in the active set never create their hubs — nothing is computed just in case.
/// </summary>
public sealed class IndicatorRegistry
{
    private readonly QuoteHub _quoteHub = new();
    private readonly Dictionary<IndicatorKey, object> _hubs = [];

    // Baba VWAP synthetic hubs have been migrated to BabaIndicatorExtension (Analyzers plugin).


    // -----------------------------------------------------------------------
    // Feed
    // -----------------------------------------------------------------------

    /// <summary>Feed one candle to all registered hubs. Call in ascending open-time order.</summary>
    public void Add(IQuote c)
    {
        _quoteHub.Add(new Quote(c.Timestamp, c.Open, c.High, c.Low, c.Close, c.Volume));

        // Baba hlc3 synthetic feeds have been migrated to BabaIndicatorExtension (Analyzers plugin).
    }


    // -----------------------------------------------------------------------
    // Registration / GetOrAdd
    // -----------------------------------------------------------------------

    /// <summary>
    /// Ensures all keys in <paramref name="keys"/> are registered. Safe to call multiple times;
    /// already-existing hubs are left untouched (no duplicate creation, no data loss).
    /// </summary>
    public void EnsureRegistered(IEnumerable<IndicatorKey> keys)
    {
        foreach (var key in keys)
            EnsureOne(key);
    }

    private void EnsureOne(IndicatorKey key)
    {
        // Already registered — nothing to do.
        if (_hubs.ContainsKey(key))
            return;

        var s = GlobalData.Settings;
        switch (key.Kind)
        {
            case IndicatorKind.Sma:
                Sma((int)key.P1);
                break;
            case IndicatorKind.Ema:
                Ema((int)key.P1);
                break;
            case IndicatorKind.BollingerBands:
                Bb((int)key.P1, key.P2);
                break;
            case IndicatorKind.Rsi:
                Rsi((int)key.P1);
                break;
            case IndicatorKind.Macd:
                Macd((int)key.P1, (int)key.P2, (int)key.P3);
                break;
            case IndicatorKind.Stoch:
                Stoch((int)key.P1, (int)key.P2, (int)key.P3);
                break;
            case IndicatorKind.ParabolicSar:
                Psar(key.P1, key.P2);
                break;
            case IndicatorKind.Atr:
                Atr((int)key.P1);
                break;
                // BabaVwap case has been migrated to BabaIndicatorExtension (Analyzers plugin).
        }
    }


    // -----------------------------------------------------------------------
    // Typed accessors — strategies can also call these directly for one-off needs
    // -----------------------------------------------------------------------

    public SmaHub Sma(int length) => GetOrAdd(IndicatorKey.Sma(length), () => _quoteHub.ToSmaHub(length));
    public EmaHub Ema(int length) => GetOrAdd(IndicatorKey.Ema(length), () => _quoteHub.ToEmaHub(length));
    public BollingerBandsHub Bb(int length, double dev) => GetOrAdd(IndicatorKey.Bb(length, dev), () => _quoteHub.ToBollingerBandsHub(length, dev));
    public RsiHub Rsi(int length) => GetOrAdd(IndicatorKey.Rsi(length), () => _quoteHub.ToRsiHub(length));
    public MacdHub Macd(int fast = 12, int slow = 26, int signal = 9) => GetOrAdd(IndicatorKey.Macd(fast, slow, signal), () => _quoteHub.ToMacdHub(fast, slow, signal));
    public StochHub Stoch(int length, int smoothD, int smoothK) => GetOrAdd(IndicatorKey.Stoch(length, smoothD, smoothK), () => _quoteHub.ToStochHub(length, smoothD, smoothK));
    public ParabolicSarHub Psar(double step = 0.02, double max = 0.2) => GetOrAdd(IndicatorKey.Psar(step, max), () => _quoteHub.ToParabolicSarHub(step, max));
    public AtrHub Atr(int length) => GetOrAdd(IndicatorKey.Atr(length), () => _quoteHub.ToAtrHub(length));

    // BabaVwap accessor has been migrated to BabaIndicatorExtension (Analyzers plugin).


    private TResult GetOrAdd<TResult>(IndicatorKey key, Func<TResult> factory) where TResult : class
    {
        if (_hubs.TryGetValue(key, out var existing))
            return (TResult)existing;
        var result = factory();
        _hubs[key] = result;
        return result;
    }


    // -----------------------------------------------------------------------
    // BuildCurrent — only fills CryptoData fields whose hub was registered
    // -----------------------------------------------------------------------

    /// <summary>
    /// Reads the latest value of every registered hub into a fresh CryptoData.
    /// Fields whose hub was never registered stay null — consistent with the
    /// existing nullable semantics in CryptoData.
    /// </summary>
    public CryptoData BuildCurrent()
    {
        var data = new CryptoData();

        foreach (var (key, hub) in _hubs)
        {
            switch (key.Kind)
            {
                case IndicatorKind.Sma:
                    {
                        var h = (SmaHub)hub;
                        if (h.Results.Count == 0) break;
                        double? v = h.Results[^1].Sma;
                        // Map the period to the fixed named field on CryptoData.
                        switch ((int)key.P1)
                        {
                            case 20: data.Sma20 = v; break;
                            case 50: data.Sma50 = v; break;
                            case 100: data.Sma100 = v; break;
                            case 200: data.Sma200 = v; break;
                                // Non-standard periods have no field in CryptoData yet.
                        }
                        break;
                    }
                case IndicatorKind.BollingerBands:
                    {
                        var h = (BollingerBandsHub)hub;
                        if (h.Results.Count == 0) break;
                        var r = h.Results[^1];
                        data.Sma20 = r.Sma;   // BB basis == Sma20; avoids a separate Sma(20) hub
                        data.BollingerBandsDeviation = 0.5 * (r.UpperBand - r.LowerBand);
                        data.BollingerBandsPercentage = 100 * (r.UpperBand / r.LowerBand - 1);
                        break;
                    }
                case IndicatorKind.Rsi:
                    {
                        var h = (RsiHub)hub;
                        if (h.Results.Count > 0)
                            data.Rsi = h.Results[^1].Rsi;
                        break;
                    }
                case IndicatorKind.Macd:
                    {
                        var h = (MacdHub)hub;
                        if (h.Results.Count == 0) break;
                        var r = h.Results[^1];
                        data.MacdValue = r.Macd;
                        data.MacdSignal = r.Signal;
                        data.MacdHistogram = r.Histogram;
                        break;
                    }
                case IndicatorKind.Stoch:
                    {
                        var h = (StochHub)hub;
                        if (h.Results.Count == 0) break;
                        var r = h.Results[^1];
                        data.StochOscillator = r.Oscillator;
                        data.StochSignal = r.Signal;
                        break;
                    }
                case IndicatorKind.ParabolicSar:
                    {
                        var h = (ParabolicSarHub)hub;
                        if (h.Results.Count > 0 && h.Results[^1].Sar != null)
                            data.PSar = h.Results[^1].Sar;
                        break;
                    }
                    // BabaVwap BuildCurrent case has been migrated to BabaIndicatorExtension (Analyzers plugin).
                    // Ema and Atr have no fixed CryptoData field in the base set;
                    // they are used as sub-components (Atr inside BabaVwap) or for
                    // DEBUG-only fields. Extend here when a strategy needs them in CryptoData.
            }
        }

        return data;
    }
}

// ---------------------------------------------------------------------------
// Example: how SignalBabaBase would declare its requirements
// ---------------------------------------------------------------------------

// public class SignalBabaBase : SignalCreateBase, IRequiresIndicators
// {
//     public IEnumerable<IndicatorKey> RequiredIndicators()
//     {
//         var g = GlobalData.Settings.General;
//         var b = GlobalData.Settings.Signal.Baba;
//         yield return IndicatorKey.Rsi(g.SettingsRsi.Length);              // RSI confluence filter
//         yield return IndicatorKey.BabaVwap(b.Length, b.AtrLength, b.Mult, b.AtrMult);
//     }
// }

// ---------------------------------------------------------------------------
// Example: how IndicatorEngine.PrepareViaHub would bootstrap the registry
// ---------------------------------------------------------------------------

// private static IndicatorRegistry BuildRegistryForActiveStrategies()
// {
//     var registry = new IndicatorRegistry();
//
//     // Collect requirements from every active (enabled) strategy.
//     var keys = RegisterAlgorithms.AlgorithmDefinitionList.Values
//         .SelectMany(def =>
//         {
//             var keys = new List<IndicatorKey>();
//             if (def.AnalyzeLongType  is not null && Activator.CreateInstance(def.AnalyzeLongType)  is IRequiresIndicators l) keys.AddRange(l.RequiredIndicators());
//             if (def.AnalyzeShortType is not null && Activator.CreateInstance(def.AnalyzeShortType) is IRequiresIndicators s) keys.AddRange(s.RequiredIndicators());
//             return keys;
//         });
//
//     registry.EnsureRegistered(keys);
//     return registry;
// }
