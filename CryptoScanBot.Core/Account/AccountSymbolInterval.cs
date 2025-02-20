using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Account;

public class ZoneDataList
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

    public void ResetZones()
    {
        LongOpen.Clear();
        ShortOpen.Clear();
        LongClosed.Clear();
        ShortClosed.Clear();
    }
}

public class AccountSymbolInterval
{
    // administration (overkill, not really needed)
    public required virtual CryptoInterval Interval { get; set; }
    public required CryptoIntervalPeriod IntervalPeriod { get; set; }

    // zones
    public ZoneDataList DlzZones { get; internal set; } = new();
    public ZoneDataList FvgZones { get; internal set; } = new();

    // ehh?
    public AccountZoneData Zones { get; internal set; } = new();
}