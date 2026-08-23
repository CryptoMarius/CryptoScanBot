using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Zones;

/// <summary>
/// SMC (Smart Money Concepts) supply/demand detector — base + expansion model. Intended to be
/// visualised in the chart window and finetuned later. NOT yet wired into the periodic
/// zone-calculation pipeline; the chart window calls <see cref="Detect"/> directly when the
/// user toggles the "SMC zones" option.
///
/// Anatomy modelled (classic supply/demand: RBR / DBR / RBD / DBD):
///   1. BASE      — a short cluster of small "consolidation" candles (range below average).
///                  This IS the zone: the area price is expected to react to on a later return.
///   2. EXPANSION — a strong impulsive candle leaving the base (range well above average).
///                  Its size grades the zone's strength; its direction sets the side:
///                    impulse UP   after a base → DEMAND zone (Long,  bounce up expected)
///                    impulse DOWN after a base → SUPPLY zone (Short, rejection down expected)
///
/// A zone is only created at a base→expansion transition (the impulse candle's predecessor must
/// itself be a base candle). That naturally dedupes long trend runs and only marks the genuine
/// departure points, which is what keeps the chart readable.
///
/// The zone price band is the full base range [min low, max high of the base candles]; OpenTime
/// is the first base candle. (A tighter proximal/distal body-based band is a later refinement.)
///
/// What's INTENTIONALLY still missing (to add later):
///   - Mitigation tracking (CE 50% touch) and TouchCount — we only do a hard break invalidation
///   - Liquidity sweep filter (only zones that swept BSL/SSL first)
///   - Premium/Discount tagging using a Fib midpoint of the dominant leg
///   - BOS/CHoCH structure-event linkage
///   - DB persistence — these zones live in <see cref="CryptoSymbolIntervalSmc.Zones"/> only
///   - Per-zone realtime invalidation through <see cref="ZoneInvalidation"/>
/// </summary>
public static class ZoneSmc
{
    // All tuning knobs live in Settings.Signal.ZonesSmc (appsettings.json) so they can be
    // finetuned without a rebuild — see SettingsSignalStrategySmc.

    /// <summary>
    /// Recompute the SMC supply/demand zones for one (symbol, interval) from scratch and store
    /// them in <see cref="CryptoSymbolIntervalSmc.Zones"/>. The new list is diffed against the
    /// existing one on (Side, OpenTime, Bottom, Top); unchanged zones keep their DB Id and
    /// AlarmDate, modified zones are queued for UPDATE, fresh zones for INSERT, and old zones
    /// that no longer appear are queued for DELETE — mirroring the DLZ persistence flow.
    /// Cheap enough to call from the chart toggle or the zone worker directly.
    /// Concurrency: this method does NOT take <see cref="CryptoSymbolData.ZoneLock"/> itself
    /// (the ChartWindow already holds it during its draw pipeline; <c>SemaphoreSlim</c> is not
    /// re-entrant so taking it again here would deadlock). Callers in background contexts —
    /// see <see cref="RebuildAllZonesForActiveExchange"/> and SignalPrepare — acquire the
    /// lock themselves before calling.
    /// </summary>
    /// <summary>
    /// Recompute (or incrementally extend) the SMC zones for one (symbol, interval). Dispatches to
    /// <see cref="DetectFull"/> the first time it runs for this (symbol, interval) — or whenever the
    /// AverageWindow/BaseMaxCandles settings changed since the cursor was built — and to
    /// <see cref="DetectIncremental"/> on every later call. The full scan is O(candle history); the
    /// incremental scan is O(new candles since the last call), which is what makes repeated calls on
    /// a long-running emulator/live session cheap instead of getting slower as history accumulates.
    /// </summary>
    public static void Detect(CryptoSymbol symbol, CryptoInterval interval)
    {
        var settings = GlobalData.Settings.Signal.ZonesSmc;
        int averageWindow = Math.Max(2, settings.AverageWindow);
        decimal baseMaxRangeFactor = settings.BaseMaxRangeFactor;
        decimal expansionMinRangeFactor = settings.ExpansionMinRangeFactor;
        decimal expansionBodyFraction = settings.ExpansionBodyFraction;
        decimal strongExpansionFactor = settings.StrongExpansionFactor;
        int baseMaxCandles = Math.Max(1, settings.BaseMaxCandles);
        int maxBlocksPerInterval = Math.Max(1, settings.MaxBlocksPerInterval);
        bool requireOppositeBaseColor = settings.RequireOppositeBaseColor;

        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        bool cacheValid = symbolInterval.Smc.ProcessedCandleMarker != null
            && symbolInterval.Smc.CachedAverageWindow == averageWindow
            && symbolInterval.Smc.CachedBaseMaxCandles == baseMaxCandles;

        if (cacheValid)
        {
            DetectIncremental(symbol, interval, symbolInterval, averageWindow, baseMaxRangeFactor,
                expansionMinRangeFactor, expansionBodyFraction, strongExpansionFactor, baseMaxCandles,
                maxBlocksPerInterval, requireOppositeBaseColor);
        }
        else
        {
            DetectFull(symbol, interval, symbolInterval, averageWindow, baseMaxRangeFactor,
                expansionMinRangeFactor, expansionBodyFraction, strongExpansionFactor, baseMaxCandles,
                maxBlocksPerInterval, requireOppositeBaseColor);
            symbolInterval.Smc.CachedAverageWindow = averageWindow;
            symbolInterval.Smc.CachedBaseMaxCandles = baseMaxCandles;
        }
    }


    /// <summary>
    /// Full historical (re)scan — the original (unchanged) algorithm. Only runs once per (symbol,
    /// interval): the first call ever, or after a settings change invalidates the incremental cursor.
    /// Throws away and rebuilds the entire SmcZones list, diffed against the DB by
    /// <see cref="ReconcileWithDatabase"/>.
    /// </summary>
    private static void DetectFull(CryptoSymbol symbol, CryptoInterval interval, CryptoSymbolInterval symbolInterval,
        int averageWindow, decimal baseMaxRangeFactor, decimal expansionMinRangeFactor, decimal expansionBodyFraction,
        decimal strongExpansionFactor, int baseMaxCandles, int maxBlocksPerInterval, bool requireOppositeBaseColor)
    {
        // Snapshot to avoid enumerating a live collection while the scanner adds candles.
        symbolInterval.CandleList.Lock();
        List<CryptoCandle> candles;
        try
        {
            candles = [.. symbolInterval.CandleList.Values];
        }
        finally
        {
            symbolInterval.CandleList.Unlock();
        }

        List<CryptoZone> zones = [];
        // Per-zone OpenTime of the impulse candle. Mitigation/touch counting must start AFTER
        // the impulse, not after the (earlier) base start — otherwise the other base candles
        // and the impulse candle's own wick would already burn the zone's freshness.
        List<CandleTime> impulseTimes = [];

        // Need at least a full average window plus one impulse candle to do anything useful.
        // Cursor deliberately stays null so the next call retries the full scan again.
        if (candles.Count < averageWindow + 2)
        {
            ReconcileWithDatabase(symbolInterval, zones);
            return;
        }

        // Prefix sums of candle range (High-Low) for O(1) trailing averages.
        // prefix[k] = sum of ranges of candles[0..k-1].
        decimal[] prefix = new decimal[candles.Count + 1];
        for (int k = 0; k < candles.Count; k++)
            prefix[k + 1] = prefix[k] + (candles[k].High - candles[k].Low);

        // Scan for base→expansion transitions. Start at averageWindow so every candle has a
        // full trailing window to measure against.
        for (int i = averageWindow; i < candles.Count; i++)
        {
            decimal avgRange = AverageRange(prefix, i, averageWindow);
            if (avgRange <= 0)
                continue;

            CryptoCandle impulse = candles[i];
            decimal range = impulse.High - impulse.Low;
            decimal body = Math.Abs(impulse.Close - impulse.Open);

            // 1) Is candle i an expansion (impulsive leg-out)?
            bool isExpansion = range >= expansionMinRangeFactor * avgRange
                && body >= expansionBodyFraction * range;
            if (!isExpansion)
                continue;

            // 2) Does it depart from a base? The immediately preceding candle must be small.
            int b = i - 1;
            if (b < 0 || (candles[b].High - candles[b].Low) > baseMaxRangeFactor * avgRange)
                continue;

            // 2b) Classical ICT/SMC tightening: the last base candle must have the OPPOSITE
            // color of the impulse — i.e. a bearish candle right before a bullish BOS for a
            // long zone, and vice versa. Dojis on the base candle are rejected. Opt-in via
            // settings.RequireOppositeBaseColor; default behaviour stays the broader
            // supply/demand (color-agnostic) base detection.
            if (requireOppositeBaseColor)
            {
                bool impulseUp = impulse.Close >= impulse.Open;
                var baseCandle = candles[b];
                bool baseIsBearish = baseCandle.Close < baseCandle.Open;
                bool baseIsBullish = baseCandle.Close > baseCandle.Open;
                if (impulseUp ? !baseIsBearish : !baseIsBullish)
                    continue;
            }

            // 3) Walk back over the consecutive small candles to capture the whole base.
            int baseEnd = b;          // last (newest) base candle, adjacent to the impulse
            int baseStart = b;        // will move backwards
            int collected = 1;
            while (baseStart - 1 >= 0
                && collected < baseMaxCandles
                && (candles[baseStart - 1].High - candles[baseStart - 1].Low) <= baseMaxRangeFactor * avgRange)
            {
                baseStart--;
                collected++;
            }

            // 4) Base price band = full range across the base candles.
            decimal top = decimal.MinValue;
            decimal bottom = decimal.MaxValue;
            for (int j = baseStart; j <= baseEnd; j++)
            {
                if (candles[j].High > top)
                    top = candles[j].High;
                if (candles[j].Low < bottom)
                    bottom = candles[j].Low;
            }

            // 5) Direction + strength from the expansion.
            bool up = impulse.Close >= impulse.Open;
            CryptoTradeSide side = up ? CryptoTradeSide.Long : CryptoTradeSide.Short;
            CryptoZoneStrength strength = range >= strongExpansionFactor * avgRange
                ? CryptoZoneStrength.Strong
                : CryptoZoneStrength.Weak;

            zones.Add(BuildZone(symbol, interval, candles[baseStart].OpenTime, top, bottom,
                side, strength, interval.Name));
            impulseTimes.Add(impulse.OpenTime);
        }

        // Mitigation + touch-counting + break invalidation in one pass per zone.
        ApplyMitigationAndInvalidation(zones, impulseTimes, candles);

        // Trim to the newest N so the chart doesn't get overwhelmed on long histories.
        // Trimmed-out zones are dropped from the new list, so the diff below will mark
        // them for deletion in the DB as well — keeping memory and DB in sync.
        if (zones.Count > maxBlocksPerInterval)
        {
            zones.Sort((a, b) => a.OpenTime.Minutes.CompareTo(b.OpenTime.Minutes));
            zones.RemoveRange(0, zones.Count - maxBlocksPerInterval);
        }

        ReconcileWithDatabase(symbolInterval, zones);

        // Candles is never empty here (the early-return above already handled that case), so the
        // cursor can safely advance to the last candle actually scanned.
        symbolInterval.Smc.ProcessedCandleMarker = candles[^1].OpenTime;
    }


    /// <summary>
    /// Incremental SMC scan: only looks at candles that arrived since
    /// <see cref="CryptoSymbolIntervalSmc.ProcessedCandleMarker"/>, plus a small bounded lookback window
    /// (averageWindow/baseMaxCandles) for context — never the full candle history. Base→expansion
    /// classification is causal (only looks at the trailing window and i-1), so a candle's
    /// classification never changes once later candles arrive — replaying it is genuinely unnecessary,
    /// not just an approximation.
    /// New zones are appended straight to the live <see cref="CryptoSymbolIntervalSmc.Zones"/> list and
    /// queued for DB insert immediately (no full diff needed — see ZoneThreadCalculate's load-once
    /// guard, the in-memory list is already authoritative). Mitigation/touch/break bookkeeping for
    /// existing open zones continues from each zone's own <see cref="CryptoZone.InsideExcursion"/> /
    /// <see cref="CryptoZone.MitigationStartTime"/> state instead of replaying their whole history.
    /// </summary>
    private static void DetectIncremental(CryptoSymbol symbol, CryptoInterval interval, CryptoSymbolInterval symbolInterval,
        int averageWindow, decimal baseMaxRangeFactor, decimal expansionMinRangeFactor, decimal expansionBodyFraction,
        decimal strongExpansionFactor, int baseMaxCandles, int maxBlocksPerInterval, bool requireOppositeBaseColor)
    {
        CryptoCandle lastCandle = symbolInterval.CandleList.LastCandle;
        if (lastCandle.OpenTime == 0)
            return; // no candles at all (yet) — leave the cursor as-is and retry next call

        CandleTime latestTime = lastCandle.OpenTime;
        CandleTime cursor = symbolInterval.Smc.ProcessedCandleMarker!.Value;
        if (latestTime <= cursor)
            return; // nothing new since the previous call

        // Bounded local window: enough lookback for the trailing average + base walk-back, plus
        // every candle that arrived since the cursor. Built via direct key lookups, never by
        // enumerating/copying the full candle history.
        int lookback = Math.Max(averageWindow, baseMaxCandles) + 1;
        CandleTime windowStart = cursor - (lookback * interval.Duration);

        List<CryptoCandle> window = symbolInterval.CandleList.GetRange(windowStart, latestTime, interval.Duration);

        int firstNewIndex = window.FindIndex(c => c.OpenTime.Minutes > cursor.Minutes);
        if (firstNewIndex < 0 || window.Count < averageWindow + 2)
            return; // not enough history yet (very young symbol/interval) — try again next call

        decimal[] prefix = new decimal[window.Count + 1];
        for (int k = 0; k < window.Count; k++)
            prefix[k + 1] = prefix[k] + (window[k].High - window[k].Low);

        // Same base→expansion scan as DetectFull, but starting at the first new candle instead of
        // averageWindow — everything before it was already classified (and can't retroactively change).
        List<CryptoZone> createdZones = [];
        for (int i = Math.Max(firstNewIndex, averageWindow); i < window.Count; i++)
        {
            decimal avgRange = AverageRange(prefix, i, averageWindow);
            if (avgRange <= 0)
                continue;

            CryptoCandle impulse = window[i];
            decimal range = impulse.High - impulse.Low;
            decimal body = Math.Abs(impulse.Close - impulse.Open);

            bool isExpansion = range >= expansionMinRangeFactor * avgRange
                && body >= expansionBodyFraction * range;
            if (!isExpansion)
                continue;

            int b = i - 1;
            if (b < 0 || (window[b].High - window[b].Low) > baseMaxRangeFactor * avgRange)
                continue;

            if (requireOppositeBaseColor)
            {
                bool impulseUp = impulse.Close >= impulse.Open;
                var baseCandle = window[b];
                bool baseIsBearish = baseCandle.Close < baseCandle.Open;
                bool baseIsBullish = baseCandle.Close > baseCandle.Open;
                if (impulseUp ? !baseIsBearish : !baseIsBullish)
                    continue;
            }

            int baseEnd = b;
            int baseStart = b;
            int collected = 1;
            while (baseStart - 1 >= 0
                && collected < baseMaxCandles
                && (window[baseStart - 1].High - window[baseStart - 1].Low) <= baseMaxRangeFactor * avgRange)
            {
                baseStart--;
                collected++;
            }

            decimal top = decimal.MinValue;
            decimal bottom = decimal.MaxValue;
            for (int j = baseStart; j <= baseEnd; j++)
            {
                if (window[j].High > top)
                    top = window[j].High;
                if (window[j].Low < bottom)
                    bottom = window[j].Low;
            }

            bool up = impulse.Close >= impulse.Open;
            CryptoTradeSide side = up ? CryptoTradeSide.Long : CryptoTradeSide.Short;
            CryptoZoneStrength strength = range >= strongExpansionFactor * avgRange
                ? CryptoZoneStrength.Strong
                : CryptoZoneStrength.Weak;

            CryptoZone zone = BuildZone(symbol, interval, window[baseStart].OpenTime, top, bottom,
                side, strength, interval.Name);
            zone.MitigationStartTime = impulse.OpenTime;
            createdZones.Add(zone);
        }

        foreach (var zone in createdZones)
        {
            symbolInterval.Smc.Zones.Add(zone);
            GlobalData.ThreadSaveObjects!.AddToQueue(zone);
        }

        // Mitigation/touch/break: only feed the candles that are genuinely new to every zone that is
        // still open — closed zones are dead and never need another candle, so they're skipped
        // entirely (this is also why the list doesn't need splitting into open/closed sub-lists the
        // way DLZ/FVG do: zone count here is capped by maxBlocksPerInterval, so a linear scan is cheap).
        List<CryptoCandle> newCandles = window.GetRange(firstNewIndex, window.Count - firstNewIndex);
        foreach (var zone in symbolInterval.Smc.Zones)
        {
            if (zone.CloseTime != null || zone.MitigationStartTime == null)
                continue;

            foreach (var c in newCandles)
            {
                if (c.OpenTime.Minutes <= zone.MitigationStartTime.Value.Minutes)
                    continue;
                if (ApplyMitigationStep(zone, c))
                {
                    // Broke this tick — the CloseTime change must reach the DB.
                    GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                    break;
                }
            }
        }

        // Trim to the newest N, mirroring DetectFull's trim but applied to the live cumulative list.
        if (symbolInterval.Smc.Zones.Count > maxBlocksPerInterval)
        {
            symbolInterval.Smc.Zones.Sort((a, b) => a.OpenTime.Minutes.CompareTo(b.OpenTime.Minutes));
            int excess = symbolInterval.Smc.Zones.Count - maxBlocksPerInterval;
            for (int k = 0; k < excess; k++)
            {
                var zone = symbolInterval.Smc.Zones[k];
                if (zone.Id > 0)
                {
                    zone.Id *= -1;
                    GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                }
            }
            symbolInterval.Smc.Zones.RemoveRange(0, excess);
        }

        symbolInterval.Smc.ProcessedCandleMarker = latestTime;
    }

    /// <summary>
    /// Diff the freshly built <paramref name="newZones"/> against the existing
    /// <see cref="CryptoSymbolIntervalSmc.Zones"/> and queue insert/update/delete via
    /// <see cref="ThreadSaveObjects"/>. Matching is on (Side, OpenTime, Bottom, Top), same as
    /// DLZ/FVG. Matched zones keep their DB Id and AlarmDate so alarms are NOT re-fired after
    /// a restart. After the diff the SmcZones reference is atomically replaced.
    /// </summary>
    private static void ReconcileWithDatabase(CryptoSymbolInterval symbolInterval, List<CryptoZone> newZones)
    {
        var oldList = symbolInterval.Smc.Zones;
        DatabaseStatistics statistics = new();
        SortedList<(CryptoTradeSide, CandleTime?, decimal, decimal), CryptoZone> oldZones = [];
        ZoneTools.CreateZoneIndex(oldList, oldZones, statistics);

        foreach (var zone in newZones)
        {
            if (oldZones.TryGetValue((zone.Side, zone.OpenTime, zone.Bottom, zone.Top), out CryptoZone? zoneInDb))
            {
                // Reuse persisted Id and AlarmDate so restarts don't re-fire alarms.
                zone.Id = zoneInDb.Id;
                zone.AlarmDate = zoneInDb.AlarmDate;
                bool zoneExistsInDatabase = zoneInDb.Id > 0;

                oldZones.Remove((zone.Side, zone.OpenTime, zone.Bottom, zone.Top));

                // Nothing important changed — skip the DB write.
                if (zoneInDb.CloseTime == zone.CloseTime && zoneInDb.Description == zone.Description &&
                    zoneInDb.IsValid == zone.IsValid && zoneInDb.Strength == zone.Strength)
                {
                    statistics.Untouched++;
                    continue;
                }

                if (zoneExistsInDatabase)
                {
                    statistics.Modified++;
                    GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                }
                else
                {
                    statistics.Inserted++;
                    GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                }
            }
            else
            {
                statistics.Inserted++;
                GlobalData.ThreadSaveObjects!.AddToQueue(zone);
            }
        }

        // Old zones the rebuilder did not reproduce (out of candle window, trimmed, or settings
        // changed) are deleted from the DB. Mirrors ZoneTools.DeleteRemainingZones.
        foreach (var zone in oldZones.Values)
        {
            if (zone.Id > 0)
            {
                zone.Id *= -1;
                statistics.Deleted++;
                GlobalData.ThreadSaveObjects!.AddToQueue(zone);
            }
        }

        statistics.Total = newZones.Count;

        // Atomic swap so readers (signal classes, chart) always see a complete list.
        symbolInterval.Smc.Zones = newZones;
    }

    /// <summary>
    /// One-shot startup rebuild: run <see cref="Detect"/> for every (symbol, interval)
    /// configured in <see cref="SettingsSignalStrategySmc.IntervalList"/>. Two reasons we
    /// need this even though <see cref="ZoneDlz.LoadAllZones"/> already loaded the persisted
    /// zones into memory:
    ///   1. After a fresh deploy (no SMC rows in the DB yet) the in-memory list is empty
    ///      for every symbol — this populates the DB for the first time.
    ///   2. <see cref="CryptoZone.TouchCount"/> and <see cref="CryptoZone.IsMitigated"/> are
    ///      <c>[Computed]</c> — they are NOT persisted, so they would all read as 0/false
    ///      after a restart until the next interval boundary runs Detect. This refreshes
    ///      them immediately.
    /// Designed to be fired and forgotten from startup; the diff inside Detect suppresses
    /// no-op DB writes so this is cheap on subsequent restarts.
    /// </summary>
    public static void RebuildAllZonesForActiveExchange()
    {
        if (GlobalData.ActiveExchange == null)
            return;

        var intervalNames = GlobalData.Settings.Signal.ZonesSmc.IntervalList;
        if (intervalNames.Count == 0)
            return;

        int rebuilt = 0;
        foreach (var symbol in GlobalData.ActiveExchange.SymbolListName.Values.ToList())
        {
            if (!symbol.QuoteData!.FetchCandles || symbol.Status != 1 || symbol.IsBarometerSymbol())
                continue;

            // Hold ZoneLock for the whole symbol so all its intervals see a consistent
            // candle/zone snapshot and we don't race the DLZ worker mid-symbol.
            symbol.Data.ZoneLock.Wait();
            try
            {
                foreach (string intervalName in intervalNames)
                {
                    if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
                        continue;

                    try
                    {
                        Detect(symbol, interval);
                        rebuilt++;
                    }
                    catch (Exception error)
                    {
                        ScannerLog.Logger.Error(error, $"SMC startup rebuild failed for {symbol.Name} {interval.Name}");
                    }
                }
            }
            finally
            {
                symbol.Data.ZoneLock.Release();
            }
        }

        GlobalData.AddTextToLogTab($"SMC startup rebuild done ({rebuilt} (symbol, interval) combinations)");
    }


    /// <summary>
    /// Mean candle range (High-Low) over the trailing averageWindow candles ending just
    /// before index i, using the prefix-sum table for O(1) lookup.
    /// </summary>
    private static decimal AverageRange(decimal[] prefix, int i, int averageWindow)
    {
        int start = i - averageWindow;
        decimal sum = prefix[i] - prefix[start];
        return sum / averageWindow;
    }

    private static CryptoZone BuildZone(CryptoSymbol symbol, CryptoInterval interval,
        CandleTime openTime, decimal top, decimal bottom, CryptoTradeSide side,
        CryptoZoneStrength strength, string description)
    {
        return new CryptoZone
        {
            ExchangeId = symbol.ExchangeId,
            Exchange = symbol.Exchange,
            SymbolId = symbol.Id,
            Symbol = symbol,
            IntervalId = interval.Id,
            Interval = interval,
            Kind = CryptoZoneKind.OrderBlock,
            Side = side,
            Strength = strength,
            OpenTime = openTime,
            Top = top,
            Bottom = bottom,
            IsValid = true,
            Description = description,
        };
    }

    /// <summary>
    /// Single backward-looking pass per zone that fills three things from the candles AFTER
    /// the impulse candle (NOT after the base start — the base candles themselves and the
    /// impulse wick would otherwise be miscounted as touches and burn the zone's freshness
    /// before it has even been retested):
    ///
    ///   • TouchCount  — number of separate excursions in which price reached the zone's 50%
    ///                   midpoint (Consequent Encroachment / CE). A "touch" only counts once
    ///                   per excursion: price must first LEAVE the zone again (back past the
    ///                   proximal edge) before a return can count as the next touch. This is
    ///                   the supply/demand "freshness" gauge: 0 = fresh, 1 = tested, 2+ = used.
    ///   • IsMitigated — true as soon as TouchCount >= 1 (price has reached CE at least once).
    ///   • CloseTime   — set when price BREAKS the zone: a close beyond the distal edge
    ///                   (below the bottom for a demand zone, above the top for a supply zone).
    ///                   Counting stops at the break — a broken zone is dead.
    ///
    /// Note on entry vs mitigation: CE (50%) is used for the freshness bookkeeping here, NOT
    /// as an entry trigger. Entry happens at the PROXIMAL edge (zone.Top for demand,
    /// zone.Bottom for supply) so a shallow bounce that only dips a few percent into a large
    /// zone is not missed. That entry logic will live in the (future) signal class; this
    /// method only records the analytics.
    /// </summary>
    private static void ApplyMitigationAndInvalidation(List<CryptoZone> zones, List<CandleTime> impulseTimes, List<CryptoCandle> candles)
    {
        for (int zi = 0; zi < zones.Count; zi++)
        {
            var zone = zones[zi];
            // Start counting AFTER the impulse candle. Stored on the zone (not just a local var) so
            // DetectIncremental can resume this same zone's bookkeeping on a later call without
            // replaying candles it already accounted for.
            zone.MitigationStartTime = impulseTimes[zi];

            // candles is in OpenTime ascending order (CandleList is a SortedList).
            for (int k = 0; k < candles.Count; k++)
            {
                CryptoCandle c = candles[k];
                if (c.OpenTime.Minutes <= zone.MitigationStartTime.Value.Minutes)
                    continue;

                if (ApplyMitigationStep(zone, c))
                    break; // broken — dead, stop feeding it candles
            }
        }
    }


    /// <summary>
    /// Apply a single candle to a single zone's CE mitigation / touch-count / break bookkeeping.
    /// Shared by <see cref="ApplyMitigationAndInvalidation"/> (the full historical scan, looped over
    /// every candle once) and <see cref="DetectIncremental"/> (looped over only the newly-arrived
    /// candles, continuing from <see cref="CryptoZone.InsideExcursion"/> instead of restarting).
    /// Returns true once the zone has just been broken (CloseTime set) so the caller can stop.
    /// </summary>
    private static bool ApplyMitigationStep(CryptoZone zone, CryptoCandle c)
    {
        decimal ce = (zone.Top + zone.Bottom) / 2m; // 50% midpoint (Consequent Encroachment)

        if (zone.Side == CryptoTradeSide.Short)
        {
            // Supply zone: price approaches from BELOW (proximal = Bottom, distal = Top).
            // Break first — a close above the top kills the zone.
            if (c.Close > zone.Top)
            {
                zone.CloseTime = c.OpenTime;
                return true;
            }

            // CE touch: a wick reaching up to the 50% midpoint.
            if (!zone.InsideExcursion && c.High >= ce)
            {
                zone.TouchCount++;
                zone.IsMitigated = true;
                zone.InsideExcursion = true;
            }
            // Excursion ends once price drops back below the proximal edge (left the zone).
            else if (zone.InsideExcursion && c.High < zone.Bottom)
            {
                zone.InsideExcursion = false;
            }
        }
        else
        {
            // Demand zone: price approaches from ABOVE (proximal = Top, distal = Bottom).
            // Break first — a close below the bottom kills the zone.
            if (c.Close < zone.Bottom)
            {
                zone.CloseTime = c.OpenTime;
                return true;
            }

            // CE touch: a wick reaching down to the 50% midpoint.
            if (!zone.InsideExcursion && c.Low <= ce)
            {
                zone.TouchCount++;
                zone.IsMitigated = true;
                zone.InsideExcursion = true;
            }
            // Excursion ends once price rises back above the proximal edge (left the zone).
            else if (zone.InsideExcursion && c.Low > zone.Top)
            {
                zone.InsideExcursion = false;
            }
        }

        return false;
    }
}
