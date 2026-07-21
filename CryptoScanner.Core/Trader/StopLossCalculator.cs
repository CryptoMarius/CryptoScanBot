using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Trader;

/// <summary>
/// Pure, stateless stop-loss price calculator extracted from <see cref="PositionMonitor"/>.
///
/// Why this exists
/// ───────────────
/// By extracting the decision logic into a pure function with explicit inputs and no
/// dependency on GlobalData or CryptoPosition internals, every branch — signal SL, global SL,
/// fallback, and edge cases — can be covered by fast, deterministic unit tests.
///
/// The signal-SL is always preferred when present, regardless of how many DCA levels have
/// been filled. All DCA orders are placed at once when the entry fills, so the PartCount
/// has no bearing on which SL source to use. The anchor (ExtremeDcaPrice) already ensures
/// the SL sits beyond all DCA levels.
///
/// The calculator does NOT clamp to tick-size or min/max price; the caller is responsible for
/// that (keeps the pure function free of symbol-specific concerns).
/// </summary>
public static class StopLossCalculator
{
    /// <summary>
    /// Inputs for the stop-loss calculation, decoupled from <see cref="CryptoPosition"/>
    /// and <see cref="GlobalData"/> so the function is fully testable in isolation.
    /// </summary>
    public readonly record struct SlInput
    {
        // ── Position state ──────────────────────────────────────────────
        public CryptoTradeSide Side { get; init; }
        /// <summary>Signal-provided SL distance in percent (e.g. 2.5 = 2.5%). Null when the strategy does not provide one.</summary>
        public decimal? SlPercentage { get; init; }
        /// <summary>Actual entry fill price of the position (anchor when no DCA step exists).</summary>
        public decimal EntryPrice { get; init; }
        /// <summary>Price of the most extreme DCA step: lowest buy for long, highest sell for short. Null when no DCA step exists.</summary>
        public decimal? ExtremeDcaPrice { get; init; }

        // ── Global settings ─────────────────────────────────────────────
        /// <summary>Global SL distance in percent (Settings.Trading.StopLossPercentage). 0 = disabled.</summary>
        public decimal GlobalStopLossPercentage { get; init; }
        /// <summary>Global SL limit distance in percent (Settings.Trading.StopLossLimitPercentage).</summary>
        public decimal GlobalStopLossLimitPercentage { get; init; }
    }

    public enum SlSource { None, Signal, Global }

    public readonly record struct SlResult
    {
        public decimal? Stop { get; init; }
        public decimal? Limit { get; init; }
        public SlSource Source { get; init; }
    }


    /// <summary>
    /// Determines which SL source applies and computes stop + limit prices.
    ///
    /// Priority:
    ///   1. Signal-provided SL% — always preferred when present.
    ///   2. Global SL% — when signal SL is not available and <c>GlobalStopLossPercentage &gt; 0</c>.
    ///   3. None — no SL.
    ///
    /// Anchor selection (applies to both sources):
    ///   When <see cref="SlInput.ExtremeDcaPrice"/> is set (DCA orders exist), it is always used
    ///   as anchor so the SL is placed beyond all DCA levels — never between entry and a DCA.
    ///   Otherwise the anchor falls back to <see cref="SlInput.SignalPrice"/> (signal source) or
    ///   <see cref="SlInput.EntryPrice"/> (global source).
    /// </summary>
    public static SlResult Calculate(in SlInput input)
    {
        int multiplier = input.Side == CryptoTradeSide.Long ? +1 : -1;

        // Determine which SL source and percentage to use
        SlSource source;
        decimal slPercent;

        if (input.SlPercentage.HasValue)
        {
            source = SlSource.Signal;
            slPercent = input.SlPercentage.Value;
        }
        else if (input.GlobalStopLossPercentage > 0)
        {
            source = SlSource.Global;
            slPercent = input.GlobalStopLossPercentage;
        }
        else
        {
            return new SlResult
            {
                Stop = null,
                Limit = null,
                Source = SlSource.None
            };
        }

        // Anchor selection:
        //   Signal SL → always anchor on EntryPrice. The strategy computed the SL relative to
        //     the entry; DCAs beyond that SL are not placed, so no conflict is possible.
        //   Global SL → anchor on ExtremeDcaPrice when available, so the SL sits beyond all
        //     placed DCA levels (the user did not express a specific SL distance from entry).
        decimal anchor;
        if (source == SlSource.Signal)
            anchor = input.EntryPrice;
        else if (input.ExtremeDcaPrice.HasValue)
            anchor = input.ExtremeDcaPrice.Value;
        else
            anchor = input.EntryPrice;

        decimal perc = slPercent / 100m;
        decimal stop = anchor - (multiplier * anchor * perc);

        // Limit: signal source uses a 1% buffer beyond the stop; global source uses
        // the configured limit percentage from the same anchor.
        decimal limit;
        if (source == SlSource.Signal)
        {
            decimal limitPerc = 1m / 100m;
            limit = stop - (multiplier * stop * limitPerc);
        }
        else
        {
            decimal limitPctValue = input.GlobalStopLossLimitPercentage;
            if (limitPctValue <= input.GlobalStopLossPercentage)
                limitPctValue = input.GlobalStopLossPercentage + 1m;
            perc = limitPctValue / 100m;
            limit = anchor - (multiplier * anchor * perc);
        }

        return new SlResult
        {
            Stop = stop,
            Limit = limit,
            Source = source
        };
    }
}
