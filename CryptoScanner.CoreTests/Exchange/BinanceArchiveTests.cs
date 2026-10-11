using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

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

    private static CryptoCandleList Candles3m(DateTime from, DateTime until)
    {
        CryptoCandleList candles = [];
        for (CandleTime time = CandleTime.FromDateTime(from); time < CandleTime.FromDateTime(until); time += 3u)
            candles.Add(time, new CryptoCandle { OpenTime = time });
        return candles;
    }

    [TestMethod]
    public void IsPiecePresent_MonthBuiltFrom1m_IsSkipped()
    {
        // 3m built from 1m from 30-12 23:50, while the 3m window starts at 23:00
        DateTime from = Utc(2025, 12, 30, 23, 0);
        DateTime until = Utc(2026, 10, 1);
        CryptoCandleList candles = Candles3m(Utc(2025, 12, 30, 23, 51), until);
        var pieces = BinanceArchive.PlanPieces(from, until, Utc(2026, 10, 11));

        // December misses its first candles and has to come in; January is complete
        Assert.IsFalse(BinanceArchive.IsPiecePresent(candles, 3, pieces[0], from, until));
        Assert.IsTrue(BinanceArchive.IsPiecePresent(candles, 3, pieces[1], from, until));
    }

    [TestMethod]
    public void IsPiecePresent_OneCandleMissing_IsDownloaded()
    {
        DateTime from = Utc(2026, 1, 1);
        DateTime until = Utc(2026, 2, 1);
        CryptoCandleList candles = Candles3m(from, until);
        candles.Remove(CandleTime.FromDateTime(Utc(2026, 1, 15, 12, 0)));
        var pieces = BinanceArchive.PlanPieces(from, until, Utc(2026, 10, 11));

        Assert.IsFalse(BinanceArchive.IsPiecePresent(candles, 3, pieces[0], from, until));
    }

    [TestMethod]
    public void MoreMissingThan_WarmUpBeforeBuiltPart_IsNotEnoughForTheArchive()
    {
        // 3m: 500 candles of warm-up missing in front of nine months that were built from 1m
        DateTime from = Utc(2025, 12, 30, 23, 0);
        DateTime until = Utc(2026, 10, 1);
        CryptoCandleList candles = Candles3m(Utc(2025, 12, 31, 0, 0).AddMinutes(1500), until);

        Assert.IsFalse(CandleBase.MoreMissingThan(candles, 3, CandleTime.FromDateTime(from), CandleTime.FromDateTime(until), 5000));
        Assert.IsTrue(CandleBase.MoreMissingThan(candles, 3, CandleTime.FromDateTime(from), CandleTime.FromDateTime(until), 400));
    }

    [TestMethod]
    public void MoreMissingThan_NothingPresent_GoesToTheArchive()
    {
        DateTime from = Utc(2026, 1, 1);
        DateTime until = Utc(2026, 10, 1);

        Assert.IsTrue(CandleBase.MoreMissingThan([], 1, CandleTime.FromDateTime(from), CandleTime.FromDateTime(until), 5000));
    }

    [TestMethod]
    public void ArchiveIntervalName_UpToADay()
    {
        Assert.AreEqual("1m", BinanceArchive.ArchiveIntervalName(CryptoIntervalPeriod.interval1m));
        Assert.AreEqual("1d", BinanceArchive.ArchiveIntervalName(CryptoIntervalPeriod.interval1d));
        Assert.IsNull(BinanceArchive.ArchiveIntervalName(CryptoIntervalPeriod.interval1w));
    }
}
