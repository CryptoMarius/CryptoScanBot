using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.CoreTests.Core;

[TestClass()]
public class IntervalToolsTestsStartOfIntervalCandle2 : TestBase
{
    
    [TestMethod]
    public void StartOfIntervalCandle2Test10MinutesBack()
    {
        InitTestSession();

        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        DateTime sourceDate = new(2025, 10, 06, 19, 38, 0, 0, DateTimeKind.Utc);
        long sourceUnix = CandleTools.GetUnixTime(sourceDate, sourceInterval.Duration);

        // should fall back on the previous day because the day candle aint finished
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval10m];
        long targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = CandleTools.GetUnixDate(targetUnix);

        DateTime expectedDate = new(2025, 10, 06, 19, 20, 0, 0, DateTimeKind.Utc);
        long expectedUnix = CandleTools.GetUnixTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");
    }


    [TestMethod]
    public void StartOfIntervalCandle2Test20MinutesBack()
    {
        InitTestSession();

        DateTime sourceDate = new(2025, 10, 06, 19, 38, 0, 0, DateTimeKind.Utc);
        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        long sourceUnix = CandleTools.GetUnixTime(sourceDate, sourceInterval.Duration);


        // should fall back on ..
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval30m];
        long targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = CandleTools.GetUnixDate(targetUnix);

        DateTime expectedDate = new(2025, 10, 06, 19, 00, 0, 0, DateTimeKind.Utc);
        long expectedUnix = CandleTools.GetUnixTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");
    }


    [TestMethod]
    public void StartOfIntervalCandle2Test1HourBack()
    {
        InitTestSession();

        DateTime sourceDate = new(2025, 10, 06, 19, 38, 0, 0, DateTimeKind.Utc);
        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        long sourceUnix = CandleTools.GetUnixTime(sourceDate, sourceInterval.Duration);


        // should fall back on ..
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];
        long targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = CandleTools.GetUnixDate(targetUnix);

        DateTime expectedDate = new(2025, 10, 06, 18, 00, 0, 0, DateTimeKind.Utc);
        long expectedUnix = CandleTools.GetUnixTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");

    }


    [TestMethod]
    public void StartOfIntervalCandle2Test2HoursBack()
    {
        InitTestSession();

        DateTime sourceDate = new(2025, 10, 06, 19, 38, 0, 0, DateTimeKind.Utc);
        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        long sourceUnix = CandleTools.GetUnixTime(sourceDate, sourceInterval.Duration);


        // should fall back on ..
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2h];
        long targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = CandleTools.GetUnixDate(targetUnix);

        DateTime expectedDate = new(2025, 10, 06, 16, 00, 0, 0, DateTimeKind.Utc);
        long expectedUnix = CandleTools.GetUnixTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");
    }



    [TestMethod]
    public void StartOfIntervalCandle2Test7()
    {
        InitTestSession();

        DateTime sourceDate = new(2025, 10, 06, 19, 38, 0, 0, DateTimeKind.Utc);
        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        long sourceUnix = CandleTools.GetUnixTime(sourceDate, sourceInterval.Duration);


        // should fall back on ..
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2h];
        long targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = CandleTools.GetUnixDate(targetUnix);

        DateTime expectedDate = new(2025, 10, 06, 16, 00, 0, 0, DateTimeKind.Utc);
        long expectedUnix = CandleTools.GetUnixTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");
    }


    [TestMethod]
    public void StartOfIntervalCandle2Test1DayBack()
    {
        InitTestSession();

        DateTime sourceDate = new(2025, 10, 06, 19, 38, 0, 0, DateTimeKind.Utc);
        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        long sourceUnix = CandleTools.GetUnixTime(sourceDate, sourceInterval.Duration);

        // should fall back on the previous day because the day candle aint finished
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1d];
        long targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = CandleTools.GetUnixDate(targetUnix);

        DateTime expectedDate = new(2025, 10, 05, 00, 00, 0, 0, DateTimeKind.Utc);
        long expectedUnix = CandleTools.GetUnixTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");
    }



    [TestMethod]
    public void StartOfIntervalCandle2Test1DayBack2()
    {
        InitTestSession();

        DateTime sourceDate = new(2025, 10, 06, 00, 0, 0, 0, DateTimeKind.Utc);
        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        long sourceUnix = CandleTools.GetUnixTime(sourceDate, sourceInterval.Duration);

        // should fall back on the previous day because the day candle aint finished
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1d];
        long targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = CandleTools.GetUnixDate(targetUnix);

        DateTime expectedDate = new(2025, 10, 05, 00, 00, 0, 0, DateTimeKind.Utc);
        long expectedUnix = CandleTools.GetUnixTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");
    }


    [TestMethod]
    public void StartOfIntervalCandle2Test1DayBack3()
    {
        InitTestSession();

        DateTime sourceDate = new(2025, 10, 06, 23, 59, 0, 0, DateTimeKind.Utc);
        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        long sourceUnix = CandleTools.GetUnixTime(sourceDate, sourceInterval.Duration);

        // should fall back on the previous day because the day candle aint finished
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1d];
        long targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = CandleTools.GetUnixDate(targetUnix);

        DateTime expectedDate = new(2025, 10, 06, 00, 00, 0, 0, DateTimeKind.Utc);
        long expectedUnix = CandleTools.GetUnixTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");
    }



    [TestMethod]
    public void StartOfIntervalCandle2Test1DayBack4()
    {
        InitTestSession();

        DateTime sourceDate = new(2025, 10, 07, 00, 00, 0, 0, DateTimeKind.Utc);
        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        long sourceUnix = CandleTools.GetUnixTime(sourceDate, sourceInterval.Duration);

        // should fall back on the previous day because the day candle aint finished
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1d];
        long targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = CandleTools.GetUnixDate(targetUnix);

        DateTime expectedDate = new(2025, 10, 06, 00, 00, 0, 0, DateTimeKind.Utc);
        long expectedUnix = CandleTools.GetUnixTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");
    }


    [TestMethod]
    public void StartOfIntervalCandle2Test1DayBack5()
    {
        InitTestSession();

        DateTime sourceDate = new(2025, 10, 07, 00, 01, 0, 0, DateTimeKind.Utc);
        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        long sourceUnix = CandleTools.GetUnixTime(sourceDate, sourceInterval.Duration);

        // should fall back on the previous day because the day candle aint finished
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1d];
        long targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = CandleTools.GetUnixDate(targetUnix);

        DateTime expectedDate = new(2025, 10, 06, 00, 00, 0, 0, DateTimeKind.Utc);
        long expectedUnix = CandleTools.GetUnixTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");
    }
}