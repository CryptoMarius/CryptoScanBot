namespace CryptoScanBot.Core.Model;

// Not sure where to put it in the project

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