using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

/// <summary>
/// Contains FVG or DLZ zones
/// </summary>
public class CryptoSymbolIntervalZones
{
    // Active zones (CloseTime = null)
    public OrderedList<CryptoZone> LongOpen { get; set; } = new(new CompareZoneDescending());
    public OrderedList<CryptoZone> ShortOpen { get; set; } = new(new CompareZoneAscending());
    // Active and closed zones (for display only, kind of polluted?)
    public OrderedList<CryptoZone> LongClosed { get; set; } = new(new CompareZoneDescending());
    public OrderedList<CryptoZone> ShortClosed { get; set; } = new(new CompareZoneAscending());

    public void Add(CryptoZone zone)
    {
        if (zone.CloseTime == null)
        {
            if (zone.Side == CryptoTradeSide.Long)
                LongOpen.Add(zone);
            else
                ShortOpen.Add(zone);
        }
        else
        {
            if (zone.Side == CryptoTradeSide.Long)
                LongClosed.Add(zone);
            else
                ShortClosed.Add(zone);
        }
    }

    public void Reset()
    {
        LongOpen.Clear();
        ShortOpen.Clear();
        LongClosed.Clear();
        ShortClosed.Clear();
    }
}


class CompareZoneAscending : IComparer<CryptoZone>
{
    public int Compare(CryptoZone? zoneA, CryptoZone? zoneB)
    {
        ArgumentNullException.ThrowIfNull(zoneA, nameof(zoneA));
        ArgumentNullException.ThrowIfNull(zoneB, nameof(zoneB));
        return zoneA.Bottom.CompareTo(zoneB.Bottom); // asc via Bottom
    }
}

class CompareZoneDescending : IComparer<CryptoZone>
{
    public int Compare(CryptoZone? zoneA, CryptoZone? zoneB)
    {
        ArgumentNullException.ThrowIfNull(zoneA, nameof(zoneA));
        ArgumentNullException.ThrowIfNull(zoneB, nameof(zoneB));
        return zoneB.Top.CompareTo(zoneA.Top); // desc via Top
    }
}
