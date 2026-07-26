using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.Vbs.Signal;

/// <summary>
/// Shared base for the VBS long/short signals. Adds the optional DLZ / FVG / SMC zone-rejection
/// confluence filter (the three checkboxes in the VBS settings), mirroring SignalStoRsiBase, plus the
/// delayed-entry rule: don't enter on the signal candle, wait one candle and enter on the band of that
/// next candle (see <see cref="AllowStepIn"/>).
/// </summary>
public class VbsSignalVbs : SignalCreateBase
{
    // Shared last-signal time per symbol+interval — long and short share one cooldown, like the Pine
    // script. Static so both side instances (and rescans) see the same state; this survives the signal
    // leaving the SignalList (a position-bound signal is removed, but the cooldown must still apply).
    //private static readonly ConcurrentDictionary<string, CandleTime> LastSignalTime = new();

    //private string CooldownKey() => $"{Symbol.Name}|{Interval.IntervalPeriod}";

    /// <summary>
    /// True while still within the cooldown window after the last VBS signal on this symbol+interval
    /// (shared long &amp; short): CooldownBars candles must pass before a new signal may fire.
    /// </summary>
    //protected bool InCooldown()
    //{
    //    return false;
    //    //var settings = VbsPlugin.Settings;
    //    //if (!settings.UseCooldown)
    //    //    return false;
    //    //if (!LastSignalTime.TryGetValue(CooldownKey(), out CandleTime last))
    //    //    return false;
    //    //// A candle BEFORE the recorded time means a new/earlier (re)run — stale state from a previous
    //    //// emulator run — so it is NOT a cooldown; the first signal of this run overwrites it again.
    //    //if (CandleLast.Candle.OpenTime.Minutes < last.Minutes)
    //    //    return false;
    //    //uint elapsed = CandleLast.Candle.OpenTime.Minutes - last.Minutes;
    //    //return elapsed < (uint)(settings.CooldownBars * Interval.Duration);
    //}

    ///// <summary>Records that a VBS signal fired on the current candle, starting the cooldown.</summary>
    //protected void MarkSignalFired()
    //{
    //    LastSignalTime[CooldownKey()] = CandleLast.Candle.OpenTime;
    //}

    /// <summary>
    /// Delayed entry (per the VBS playbook):
    ///   - Wait one candle after the signal candle.
    ///   - Enter at the macro band PRICE of that next candle (wick touch on the signal candle), or — when
    ///     the signal candle's touch was a body break — at the further of the signal close and that band.
    ///   - If a newer VBS signal of the same side fires meanwhile, abandon this one (the newer takes over).
    /// The recomputed entry price + SL% are written back onto the signal (SignalPrice / SlPercentage) and
    /// persisted; PositionMonitor then enters at SignalPrice (EntryPriceOverridden). The trader still
    /// decides market vs limit from that price.
    /// </summary>
    /// <summary>
    /// Supersede: when a newer VBS signal of the same side exists, this older one is dropped from the
    /// list entirely (GiveUp → true → removed by the monitor) instead of lingering until EntryRemoveTime.
    /// So only the latest band break stays pending — the latest replaces all previous.
    /// Slide ("glijbaan", experimental): when the slide filter is enabled, a still-pending signal is also
    /// dropped once an efficient one-way slide develops against it (a LONG during a DOWN-slide, a SHORT
    /// during an UP-slide) — so we don't step into a knife that started sliding after the band break.
    /// Otherwise the standard base GiveUp applies (EntryRemoveTime / a position is already open).
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
                ExtraText = "superseded by a newer vbs signal";
                return true;
            }
        }

        return base.GiveUp(signal);
    }

    public override bool AllowStepIn(CryptoSignal signal)
    {
        //CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(signal.Interval.IntervalPeriod);

        // Cluster filter: after the VBS band break, wait until the last 2 candles stay fully
        // inside the Bollinger Bands (no touch). This avoids entering during a cluster of
        // consecutive band breaks — only step in once the extreme move has exhausted.
        //const int requiredInsideBars = 2;
        //MyData? candle = CandleLast;
        //for (int i = 0; i < requiredInsideBars; i++)
        //{
        //    if (candle?.CandleData == null)
        //    {
        //        ExtraText = "not enough candles for bb inside check";
        //        return false;
        //    }

        //    switch (SignalSide)
        //    {
        //        case CryptoTradeSide.Long:
        //            double? lowerBand = candle.CandleData.BollingerBandsLowerBand;
        //            if (!lowerBand.HasValue || (double)candle.Candle.Close <= lowerBand.Value)
        //            {
        //                ExtraText = $"candle {i + 1}/{requiredInsideBars} touches bb.lower (low={(double)candle.Candle.Low:N8} band={lowerBand:N8})";
        //                return false;
        //            }
        //            break;
        //        case CryptoTradeSide.Short:
        //            double? upperBand = candle.CandleData.BollingerBandsUpperBand;
        //            if (!upperBand.HasValue || (double)candle.Candle.Close >= upperBand.Value)
        //            {
        //                ExtraText = $"candle {i + 1}/{requiredInsideBars} touches bb.upper (high={(double)candle.Candle.High:N8} band={upperBand:N8})";
        //                return false;
        //            }
        //            break;
        //    }

        //    if (i < requiredInsideBars - 1)
        //    {
        //        if (!GetPrevCandle(candle, out candle))
        //            return false;
        //    }
        //}

        //// Enter on the candle AFTER the signal candle: wait at least one candle.
        //if (CandleLast.Candle.OpenTime.Minutes <= CandleTime.FromDateTime(signal.OpenDate).Minutes)
        //{
        //    ExtraText = "waiting one candle after the signal";
        //    return false;
        //}

        //// Place the entry on the band of the current (entry) candle.
        //bool isLong = SignalSide == CryptoTradeSide.Long;
        //bool gotBand = isLong
        //    ? VbsBandsHelper.TryGetLowerBand(symbolInterval, CandleLast.Candle.OpenTime, out double nextBandD, out double pct)
        //    : VbsBandsHelper.TryGetUpperBand(symbolInterval, CandleLast.Candle.OpenTime, out nextBandD, out pct);
        //if (gotBand)
        //{
        //    decimal entry = (decimal)nextBandD;

        //    // Entry = the most extreme of the wick, the Close and the band: LOWEST for a long, HIGHEST
        //    // for a short. So we keep going with the highest/lowest price of the signal candle and the
        //    // (next) candle's band.
        //    if (signal.Candle.HasValue)
        //    {
        //        CryptoCandle sigCandle = signal.Candle.Value;
        //        entry = isLong
        //            ? Math.Min(entry, Math.Min(sigCandle.Low, sigCandle.Close))
        //            : Math.Max(entry, Math.Max(sigCandle.High, sigCandle.Close));
        //    }

        //    signal.SignalPrice = entry.Clamp(Symbol.PriceMinimum, Symbol.PriceMaximum, Symbol.PriceTickSize);
        //    signal.EntryPriceOverridden = true;
        //    if (VbsPlugin.Settings.UseStopLoss)
        //        signal.SlPercentage = (decimal)pct;
        //    GlobalData.ThreadSaveObjects?.AddToQueue(signal);
        //}

        return base.AllowStepIn(signal);
    }

}
