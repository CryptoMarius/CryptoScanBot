using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Zones;

/// <summary>
/// What both charts (the Avalonia chart window and the Photino chart page) show of the SMC order
/// blocks: brought up to date when the overlay is drawn, only the zones around the current price,
/// and the interval with the touch count as caption. One place, so the two charts cannot drift.
/// <para>
/// Declutter: per interval the zones the price sits inside, plus the nearest
/// <see cref="NearbyZonesPerSide"/> above and below. This avoids the "wirwar" of dozens of
/// overlapping historical blocks (especially the big ones that span the whole chart).
/// </para>
/// </summary>
public static class SmcChartSelection
{
    // How many zones to show on each side (above / below) of the current price, PER interval.
    // Broken (invalidated) zones are intentionally included so you can look back at past structure.
    public const int NearbyZonesPerSide = 5;

    /// <summary>
    /// Run ZoneSmc.Detect for every SMC interval, so the chart shows current zones. The detector
    /// mutates the live zone list in place, which is not safe while the engine's own Detect runs
    /// on a background thread: without <paramref name="zoneLockAlreadyHeld"/> the zone lock is only
    /// taken when it is free (Wait(0)); otherwise Detect is skipped and the chart shows whatever the
    /// list already holds, the next redraw retries.
    /// </summary>
    public static void Detect(CryptoSymbol symbol, bool zoneLockAlreadyHeld)
    {
        bool tookLock = !zoneLockAlreadyHeld && symbol.Data.ZoneLock.Wait(0);
        if (!zoneLockAlreadyHeld && !tookLock)
            return;
        try
        {
            foreach (string intervalName in GlobalData.Settings.Signal.ZonesSmc.IntervalList)
            {
                if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
                    ZoneSmc.Detect(symbol, interval);
            }
        }
        finally
        {
            if (tookLock)
                symbol.Data.ZoneLock.Release();
        }
    }

    /// <summary>The zones to draw, over all SMC intervals.</summary>
    public static List<CryptoZone> Select(CryptoSymbol symbol)
    {
        List<CryptoZone> result = [];
        decimal? currentPrice = CurrentPrice(symbol);
        foreach (string intervalName in GlobalData.Settings.Signal.ZonesSmc.IntervalList)
        {
            if (!GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
                continue;

            // A copy: ZoneSmc mutates the list in place
            List<CryptoZone> zones = [.. symbol.Data.Get(interval.IntervalPeriod).Smc.Zones];
            result.AddRange(SelectSurroundingZones(zones, currentPrice));
        }
        return result;
    }

    /// <summary>
    /// Interval + freshness: the touch count (CE touches) is appended, so a quick glance tells fresh
    /// (no number) from tested ("2x"). Colour already encodes demand vs supply.
    /// </summary>
    public static string Caption(CryptoZone zone)
        => zone.TouchCount > 0 ? $"{zone.Interval.Name} {zone.TouchCount}x" : zone.Interval.Name;

    /// <summary>
    /// Best-effort "current price" for proximity filtering: the symbol's live LastPrice, or the most
    /// recent candle close across the symbol's intervals as a fallback (e.g. when LastPrice is not
    /// populated in a backtest/emulator run).
    /// </summary>
    public static decimal? CurrentPrice(CryptoSymbol symbol)
    {
        if (symbol.LastPrice.HasValue && symbol.LastPrice.Value > 0)
            return symbol.LastPrice.Value;

        decimal? best = null;
        CandleTime newest = CandleTime.MinValue;
        foreach (var symbolInterval in symbol.Data.SymbolIntervalList)
        {
            var last = symbolInterval.CandleList.LastCandle;
            if (last.OpenTime != 0 && last.OpenTime >= newest)
            {
                newest = last.OpenTime;
                best = last.Close;
            }
        }
        return best;
    }

    /// <summary>
    /// Keep only the zones surrounding the current price: every zone the price sits inside, plus
    /// the nearest <see cref="NearbyZonesPerSide"/> above and below. When no price is available
    /// (shouldn't normally happen) all zones are returned unfiltered.
    /// </summary>
    public static IEnumerable<CryptoZone> SelectSurroundingZones(List<CryptoZone> zones, decimal? currentPrice)
    {
        if (currentPrice == null || zones.Count == 0)
            return zones;

        decimal price = currentPrice.Value;

        List<CryptoZone> inside = [];
        List<CryptoZone> above = []; // entirely above price (Bottom > price) → resistance
        List<CryptoZone> below = []; // entirely below price (Top < price)    → support

        foreach (var zone in zones)
        {
            if (price >= zone.Bottom && price <= zone.Top)
                inside.Add(zone);
            else if (zone.Bottom > price)
                above.Add(zone);
            else
                below.Add(zone);
        }

        // Nearest first: above by lowest Bottom, below by highest Top.
        above.Sort((a, b) => a.Bottom.CompareTo(b.Bottom));
        below.Sort((a, b) => b.Top.CompareTo(a.Top));

        List<CryptoZone> result = [.. inside];
        result.AddRange(above.Take(NearbyZonesPerSide));
        result.AddRange(below.Take(NearbyZonesPerSide));
        return result;
    }
}
