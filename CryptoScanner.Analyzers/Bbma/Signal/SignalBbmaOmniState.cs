using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Indicators;

namespace CryptoScanner.Analyzers.Bbma.Signal;

// DEBUG only, like the rest of BBMA Omni (see SignalBbmaOmniBase).
#if DEBUG

/// <summary>
/// The running TPW forward pass of one side (the MQ5 tpwbuy or tpwsell counter plus what it
/// remembers of the previous bar), and the per-bar states it produced so far.
/// <para>
/// BuildTpwCache fills a fresh one from the oldest candle; IsSignal keeps one per symbol, interval
/// and side in <see cref="OmniIntervalState"/> and only feeds it the candles that closed since the
/// last evaluation. Short leaves <see cref="PrevHigh"/> and <see cref="PrevUpperBand"/> alone (its
/// reset is commented out in the MQ5 source).
/// </para>
/// </summary>
public sealed class TpwRun(Dictionary<CandleTime, SignalBbmaOmniBase.OmniBarState> states)
{
    public Dictionary<CandleTime, SignalBbmaOmniBase.OmniBarState> States { get; } = states;

    public int Count;
    public bool PrevExtremeOwn;
    public bool PrevExtremeOpposite;
    public decimal PrevHigh;
    public decimal PrevUpperBand = decimal.MaxValue; // neutral: first bar never triggers reset

    /// <summary>The newest candle fed to the pass, null when nothing was fed yet.</summary>
    public CandleTime? LastProcessed;

    public TpwRun() : this([])
    {
    }
}


/// <summary>
/// What BBMA Omni keeps for one symbol and interval across evaluations. The strategy instance is
/// created anew for every candle (RegisterAlgorithms.GetAlgorithm), so before this every
/// evaluation that reached the TPW check rebuilt the whole forward pass over all candles of the
/// interval, and every Extreme was computed again on each call.
/// <para>
/// Lives next to the CryptoSymbolInterval in a ConditionalWeakTable, so it goes away with it and
/// Core needs no field for it. Thrown away and started over when the indicator hub of the interval
/// is replaced (warm-up, new emulator run), when the indicator configuration is bumped, or when
/// the BBMA settings object is replaced.
/// </para>
/// </summary>
public sealed class OmniIntervalState
{
    private static readonly ConditionalWeakTable<CryptoSymbolInterval, OmniIntervalState> Table = new();

    private object? _hub;
    private int _configVersion;
    private BbmaSettings? _settings;

    public ConcurrentDictionary<CandleTime, bool> ExtremeBuy { get; private set; } = new();
    public ConcurrentDictionary<CandleTime, bool> ExtremeSell { get; private set; } = new();
    public TpwRun TpwBuy { get; private set; } = new();
    public TpwRun TpwSell { get; private set; } = new();

    /// <summary>The state of <paramref name="symbolInterval"/>, renewed when what it was built on changed.</summary>
    public static OmniIntervalState For(CryptoSymbolInterval symbolInterval)
    {
        OmniIntervalState state = Table.GetValue(symbolInterval, _ => new OmniIntervalState());
        lock (state)
        {
            if (!ReferenceEquals(state._hub, symbolInterval.IndicatorHub)
                || state._configVersion != IndicatorConfiguration.Version
                || !ReferenceEquals(state._settings, BbmaPlugin.Settings))
            {
                state.ResetLocked();
                state._hub = symbolInterval.IndicatorHub;
                state._configVersion = IndicatorConfiguration.Version;
                state._settings = BbmaPlugin.Settings;
            }
            state.TrimLocked(symbolInterval);
        }
        return state;
    }

    /// <summary>Start over (time went back: a new emulator run on the same symbol).</summary>
    public void Reset()
    {
        lock (this)
            ResetLocked();
    }

    // New objects instead of Clear: an evaluation that still holds the old ones keeps working on them.
    private void ResetLocked()
    {
        ExtremeBuy = new();
        ExtremeSell = new();
        TpwBuy = new();
        TpwSell = new();
    }

    // The indicator data is trimmed from the old side (CandleTools.CleanCandleDataAsync), and a
    // candle older than the first one left can never be asked for again (GetPrevCandle fails on it).
    // Without this the dictionaries grow for the whole run.
    private void TrimLocked(CryptoSymbolInterval symbolInterval)
    {
        int limit = 2 * symbolInterval.Data.Count + 64;
        if (ExtremeBuy.Count + ExtremeSell.Count <= 2 * limit)
            return;

        CandleTime first;
        lock (symbolInterval.Data)
        {
            if (symbolInterval.Data.Count == 0)
                return;
            first = symbolInterval.Data.Keys.First();
        }

        foreach (CandleTime key in ExtremeBuy.Keys)
        {
            if (key < first)
                ExtremeBuy.TryRemove(key, out _);
        }
        foreach (CandleTime key in ExtremeSell.Keys)
        {
            if (key < first)
                ExtremeSell.TryRemove(key, out _);
        }
        // The TPW states are trimmed by SignalBbmaOmniBase.UpdateSharedTpwCache, under the lock of
        // their run. Taking that lock here, inside the lock of this state, would invert the order
        // IsSignal takes them in (run first, then this state through the Extreme cache).
    }
}
#endif
