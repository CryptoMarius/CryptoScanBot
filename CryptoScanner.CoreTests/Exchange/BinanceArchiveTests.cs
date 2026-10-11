using CryptoScanner.Core.Enums;

namespace CryptoScanner.Core.Exchange.Binance.Tests;

/// <summary>
/// The offline half of the Binance download archive: which files cover a window, and how a csv row
/// becomes a kline. The download itself is not tested here (it needs the network).
/// </summary>
[TestClass]
public class BinanceArchiveTests
{
    private static DateTime Utc(int year, int month, int day, int hour = 0, int minute = 0)
        => new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    [TestMethod]
    public void PlanPieces_EndedMonthsMonthly_CurrentMonthDaily_StopsBeforeToday()
    {
        DateTime now = Utc(2026, 10, 11, 14, 30);
        var pieces = BinanceArchive.PlanPieces(Utc(2026, 7, 15, 10, 0), Utc(2026, 10, 11, 14, 0), now);

        // July, August, September as months, then October 1..10 as days; today (the 11th) is left to the api
        Assert.HasCount(3 + 10, pieces);
        Assert.IsTrue(pieces[0].Monthly);
        Assert.AreEqual(Utc(2026, 7, 1), pieces[0].Start);
        Assert.IsFalse(pieces[0].Recent);
        Assert.IsFalse(pieces[1].Recent);   // August: two months back
        Assert.IsTrue(pieces[2].Recent);    // September: may not be published yet
        Assert.IsFalse(pieces[3].Monthly);
        Assert.AreEqual(Utc(2026, 10, 1), pieces[3].Start);
        Assert.AreEqual(Utc(2026, 10, 11), pieces[^1].End);
    }

    [TestMethod]
    public void PlanPieces_WindowInsideAnEndedMonth_IsThatMonth()
    {
        var pieces = BinanceArchive.PlanPieces(Utc(2026, 3, 5), Utc(2026, 3, 20), Utc(2026, 10, 11));

        Assert.HasCount(1, pieces);
        Assert.IsTrue(pieces[0].Monthly);
        Assert.AreEqual(Utc(2026, 3, 1), pieces[0].Start);
        Assert.AreEqual(Utc(2026, 4, 1), pieces[0].End);
    }

    [TestMethod]
    public void PlanPieces_OnlyToday_Nothing()
    {
        var pieces = BinanceArchive.PlanPieces(Utc(2026, 10, 11, 2, 0), Utc(2026, 10, 11, 14, 0), Utc(2026, 10, 11, 14, 30));

        Assert.HasCount(0, pieces);
    }

    [TestMethod]
    public void ParseCsv_PerpetualWithHeader_Milliseconds()
    {
        string csv =
            "open_time,open,high,low,close,volume,close_time,quote_volume,count,taker_buy_volume,taker_buy_quote_volume,ignore\n" +
            "1759276800000,114000.10,114050.00,113990.00,114020.50,12.345,1759276859999,1407654.32,1500,6.1,695432.1,0\n";

        var klines = BinanceArchive.ParseCsv(new StringReader(csv));

        Assert.HasCount(1, klines);
        Assert.AreEqual(Utc(2025, 10, 1), klines[0].OpenTime);
        Assert.AreEqual(114000.10m, klines[0].Open);
        Assert.AreEqual(114020.50m, klines[0].Close);
        Assert.AreEqual(1407654.32m, klines[0].QuoteVolume);
    }

    [TestMethod]
    public void ParseCsv_SpotWithoutHeader_Microseconds()
    {
        string csv = "1759276800000000,4100.5,4101,4099.25,4100,10,1759276859999999,41003.5,99,5,20501,0\n";

        var klines = BinanceArchive.ParseCsv(new StringReader(csv));

        Assert.HasCount(1, klines);
        Assert.AreEqual(Utc(2025, 10, 1), klines[0].OpenTime);
        Assert.AreEqual(41003.5m, klines[0].QuoteVolume);
    }

    [TestMethod]
    public void ArchiveIntervalName_UpToADay()
    {
        Assert.AreEqual("1m", BinanceArchive.ArchiveIntervalName(CryptoIntervalPeriod.interval1m));
        Assert.AreEqual("1d", BinanceArchive.ArchiveIntervalName(CryptoIntervalPeriod.interval1d));
        Assert.IsNull(BinanceArchive.ArchiveIntervalName(CryptoIntervalPeriod.interval1w));
    }
}
