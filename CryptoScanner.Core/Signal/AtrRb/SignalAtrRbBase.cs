using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.AtrRb;

/// <summary>
/// Shared base for the AtrRb long/short signals. Adds the optional DLZ / FVG / SMC zone-rejection
/// confluence filter (the three checkboxes in the AtrRb settings), mirroring SignalStoRsiBase, plus the
/// delayed-entry rule: don't enter on the signal candle, wait one candle and enter on the band of that
/// next candle (see <see cref="AllowStepIn"/>).
/// </summary>
public class SignalAtrRbBase : SignalCreateBase
{
    /// <summary>
    /// Delayed entry (per the AtrRb playbook):
    ///   - Wait one candle after the signal candle.
    ///   - Enter at the macro band PRICE of that next candle (wick touch on the signal candle), or — when
    ///     the signal candle's touch was a body break — at the further of the signal close and that band.
    ///   - If a newer AtrRb signal of the same side fires meanwhile, abandon this one (the newer takes over).
    /// The recomputed entry price + SL% are written back onto the signal (SignalPrice / SlPercentage) and
    /// persisted; PositionMonitor then enters at SignalPrice (EntryPriceOverridden). The trader still
    /// decides market vs limit from that price.
    /// </summary>
    /// <summary>
    /// Supersede: when a newer AtrRb signal of the same side exists, this older one is dropped from the
    /// list entirely (GiveUp → true → removed by the monitor) instead of lingering until EntryRemoveTime.
    /// So only the latest band break stays pending — the latest replaces all previous. Otherwise the
    /// standard base GiveUp applies (EntryRemoveTime / a position is already open).
    /// </summary>
    public override bool GiveUp(CryptoSignal signal)
    {
        CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(signal.Interval.IntervalPeriod);
        foreach (CryptoSignal other in symbolInterval.SignalList.ToList())
        {
            if (!ReferenceEquals(other, signal)
                && other.Strategy == signal.Strategy
                && other.Side == signal.Side
                && other.OpenDate > signal.OpenDate)
            {
                ExtraText = "superseded by a newer atrrb signal";
                return true;
            }
        }

        return base.GiveUp(signal);
    }

    public override bool AllowStepIn(CryptoSignal signal)
    {
        CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(signal.Interval.IntervalPeriod);

        // Enter on the candle AFTER the signal candle: wait at least one candle.
        if (CandleLast.Candle.OpenTime.Minutes <= CandleTime.FromDateTime(signal.OpenDate).Minutes)
        {
            ExtraText = "waiting one candle after the signal";
            return false;
        }

        // Place the entry on the band of the current (entry) candle.
        bool isLong = SignalSide == CryptoTradeSide.Long;
        bool gotBand = isLong
            ? AtrRbBandsHelper.TryGetLowerBand(symbolInterval, CandleLast.Candle.OpenTime, out double nextBandD, out double pct)
            : AtrRbBandsHelper.TryGetUpperBand(symbolInterval, CandleLast.Candle.OpenTime, out nextBandD, out pct);
        if (gotBand)
        {
            decimal entry = (decimal)nextBandD;

            // Body break on the signal candle → use the further of (signal close, next band):
            // lower for a long, higher for a short.
            if (signal.Candle.HasValue)
            {
                CryptoCandle sigCandle = signal.Candle.Value;
                bool signalBandOk = isLong
                    ? AtrRbBandsHelper.TryGetLowerBand(symbolInterval, sigCandle.OpenTime, out double sigBandD, out _)
                    : AtrRbBandsHelper.TryGetUpperBand(symbolInterval, sigCandle.OpenTime, out sigBandD, out _);
                if (signalBandOk)
                {
                    decimal sigBand = (decimal)sigBandD;
                    bool bodyBreak = isLong ? sigCandle.Close < sigBand : sigCandle.Close > sigBand;
                    if (bodyBreak)
                        entry = isLong ? Math.Min(sigCandle.Close, entry) : Math.Max(sigCandle.Close, entry);
                }
            }

            signal.SignalPrice = entry.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
            signal.EntryPriceOverridden = true;
            if (GlobalData.Settings.Signal.AtrRb.UseStopLoss)
                signal.SlPercentage = (decimal)pct;
            GlobalData.ThreadSaveObjects?.AddToQueue(signal);
        }

        return base.AllowStepIn(signal);
    }

    /// <summary>
    /// Verifies the optional DLZ / FVG / SMC zone-rejection filters from the AtrRb settings.
    /// When none of the three is enabled the check is skipped (returns true). When one or more are
    /// enabled the candle must have produced a rejection on at least one of the enabled zone types
    /// (OR). The matched zone description is written to <paramref name="zoneInfo"/>; on failure a
    /// "no … rejection" reason is written instead.
    /// </summary>
    protected bool CheckEnabledZoneRejections(out string zoneInfo)
    {
        var settings = GlobalData.Settings.Signal.AtrRb;
        if (!settings.UseDlzZone && !settings.UseFvgZone && !settings.UseSmcZone)
        {
            zoneInfo = "";
            return true;
        }

        if (settings.UseDlzZone && this.WasRejectedAtDlzZone(out string dlzInfo))
        {
            zoneInfo = dlzInfo;
            return true;
        }
        if (settings.UseFvgZone && this.WasRejectedAtFvgZone(out string fvgInfo))
        {
            zoneInfo = fvgInfo;
            return true;
        }
        if (settings.UseSmcZone && this.WasRejectedAtSmcZone(out string smcInfo))
        {
            zoneInfo = smcInfo;
            return true;
        }

        zoneInfo = "no zone rejection (dlz/fvg/smc)";
        return false;
    }
}
