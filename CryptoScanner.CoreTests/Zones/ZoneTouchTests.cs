using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.CoreTests.Zones;

/// <summary>
/// Tests for <see cref="ZoneTools.Touches"/>, the test behind "only react to a reversal pattern
/// when it happens in a zone". Everything is fabricated, so no data files and no exchange.
/// </summary>
[TestClass]
public class ZoneTouchTests
{
    private static CryptoZone Zone(CryptoTradeSide side, decimal bottom, decimal top,
        uint openMinutes = 100, CandleTime? closeTime = null) => new()
        {
            // Touches only reads side, band, open and close time; the rest is required by the model.
            Kind = CryptoZoneKind.DominantLevel,
            Strength = CryptoZoneStrength.Strong,
            ExchangeId = 1,
            Exchange = null!,
            SymbolId = 1,
            Symbol = null!,
            IntervalId = 1,
            Interval = null!,
            Side = side,
            Bottom = bottom,
            Top = top,
            OpenTime = new CandleTime(openMinutes),
            CloseTime = closeTime,
            IsValid = true,
        };

    private static CryptoCandle Candle(decimal low, decimal high, uint openMinutes = 200) => new()
    {
        // FIRST, and not a detail: CryptoCandle stores prices as whole ticks, so the price setters
        // read TickDecimals. Left at zero the tick size is 1 and a low of 100,5 comes back as 100.
        TickDecimals = 4,
        OpenTime = new CandleTime(openMinutes),
        Open = low,
        High = high,
        Low = low,
        Close = high,
    };


    [TestMethod]
    public void CandleInsideTheZone_Touches()
    {
        Assert.IsTrue(ZoneTools.Touches(Zone(CryptoTradeSide.Long, 90m, 100m),
            Candle(92m, 98m), CryptoTradeSide.Long));
    }


    [TestMethod]
    public void OnlyTheWickReachesIn_StillTouches()
    {
        // The three zone strategies count a wick as a touch, and this has to agree with them:
        // a hammer that pokes into the zone and closes above it is exactly the case worth catching.
        Assert.IsTrue(ZoneTools.Touches(Zone(CryptoTradeSide.Long, 90m, 100m),
            Candle(99m, 120m), CryptoTradeSide.Long));
    }


    [TestMethod]
    public void CandleAboveTheZone_DoesNotTouch()
    {
        Assert.IsFalse(ZoneTools.Touches(Zone(CryptoTradeSide.Long, 90m, 100m),
            Candle(101m, 110m), CryptoTradeSide.Long));
    }


    [TestMethod]
    public void ToleranceWidensTheBand()
    {
        var zone = Zone(CryptoTradeSide.Long, 90m, 100m);
        var candle = Candle(100.5m, 110m);           // half a point above the top

        Assert.IsFalse(ZoneTools.Touches(zone, candle, CryptoTradeSide.Long));
        Assert.IsTrue(ZoneTools.Touches(zone, candle, CryptoTradeSide.Long, 1m));   // 1% of 100 = 101
    }


    [TestMethod]
    public void WrongSide_DoesNotTouch()
    {
        // A long pattern in a supply zone is not the setup; the sides have to agree.
        Assert.IsFalse(ZoneTools.Touches(Zone(CryptoTradeSide.Short, 90m, 100m),
            Candle(92m, 98m), CryptoTradeSide.Long));
    }


    [TestMethod]
    public void ClosedZone_DoesNotTouch()
    {
        Assert.IsFalse(ZoneTools.Touches(
            Zone(CryptoTradeSide.Long, 90m, 100m, closeTime: new CandleTime(150)),
            Candle(92m, 98m), CryptoTradeSide.Long));
    }


    [TestMethod]
    public void ZoneFromTheFuture_DoesNotTouch()
    {
        // The look-ahead guard: a zone detected after this candle may not be read, or a replay
        // would use knowledge it could not have had and stop being reproducible.
        Assert.IsFalse(ZoneTools.Touches(Zone(CryptoTradeSide.Long, 90m, 100m, openMinutes: 300),
            Candle(92m, 98m, openMinutes: 200), CryptoTradeSide.Long));
    }


    [TestMethod]
    public void ZoneOpeningOnTheSameCandle_Touches()
    {
        Assert.IsTrue(ZoneTools.Touches(Zone(CryptoTradeSide.Long, 90m, 100m, openMinutes: 200),
            Candle(92m, 98m, openMinutes: 200), CryptoTradeSide.Long));
    }
}
