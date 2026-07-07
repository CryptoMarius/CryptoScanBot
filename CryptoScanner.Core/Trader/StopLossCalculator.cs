using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Trader;

/// <summary>
/// Pure, stateless stop-loss price calculator extracted from <see cref="PositionMonitor"/>.
///
/// Why this exists
/// ───────────────
/// In July 2026 a refactoring of CalculateSlPrices added an <c>!ActiveDca</c> guard to the
/// signal-SL branch. The change was functionally correct in theory (a pending DCA invalidates
/// the signal anchor) but caused a severe regression: the signal SL was abandoned too early,
/// falling back to the global SL and dramatically shifting trade outcomes. The bug went
/// undetected because CalculateSlPrices was a private method buried inside PositionMonitor
/// with no unit-test coverage.
///
/// By extracting the decision logic into a pure function with explicit inputs and no
/// dependency on GlobalData or CryptoPosition internals, every branch — signal SL, global SL,
/// fallback, and edge cases — can be covered by fast, deterministic unit tests.
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
        /// <summary>Number of DCA parts that have been filled (Invested > 0).</summary>
        public int PartCount { get; init; }
        /// <summary>True when a DCA order has been placed but not yet filled.</summary>
        public bool ActiveDca { get; init; }
        /// <summary>Price at which the signal fired (anchor for signal-SL).</summary>
        public decimal SignalPrice { get; init; }
        /// <summary>Average entry price of the position (anchor when no DCA step exists).</summary>
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
    ///   1. Signal-provided SL% — only when <c>PartCount == 0</c> (no DCA filled yet).
    ///      Anchored on <see cref="SlInput.SignalPrice"/>.
    ///   2. Global SL% — when signal SL does not apply and <c>GlobalStopLossPercentage &gt; 0</c>.
    ///      Anchored on the most extreme DCA step price, or <see cref="SlInput.EntryPrice"/> when
    ///      no DCA step exists.
    ///   3. None — no SL.
    /// </summary>
    public static SlResult Calculate(in SlInput input)
    {
        int multiplier = input.Side == CryptoTradeSide.Long ? +1 : -1;

        // Priority 1: signal-provided SL
        if (input.SlPercentage.HasValue && input.PartCount == 0)
        {
            decimal perc = input.SlPercentage.Value / 100m;
            decimal stop = input.SignalPrice - (multiplier * input.SignalPrice * perc);

            // 1% buffer for the limit beyond the stop
            decimal limitPerc = 1m / 100m;
            decimal limit = stop - (multiplier * stop * limitPerc);

            return new SlResult { Stop = stop, Limit = limit, Source = SlSource.Signal };
        }

        // Priority 2: global SL
        if (input.GlobalStopLossPercentage > 0)
        {
            decimal anchor = input.ExtremeDcaPrice ?? input.EntryPrice;

            decimal perc = input.GlobalStopLossPercentage / 100m;
            decimal stop = anchor - (multiplier * anchor * perc);

            // Limit must be beyond the stop; fall back to stop + 1% if misconfigured
            decimal limitPctValue = input.GlobalStopLossLimitPercentage;
            if (limitPctValue <= input.GlobalStopLossPercentage)
                limitPctValue = input.GlobalStopLossPercentage + 1m;
            perc = limitPctValue / 100m;
            decimal limit = anchor - (multiplier * anchor * perc);

            return new SlResult { Stop = stop, Limit = limit, Source = SlSource.Global };
        }

        return new SlResult { Stop = null, Limit = null, Source = SlSource.None };
    }
}
