using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.CoreTests.Core;

[TestClass()]
public class IntervalToolsTestsStartOfIntervalCandle2 : TestBase
{

    [TestMethod]
    public void StartOfIntervalCandle2Test10MinutesBack()
    {
        InitTestSession();

        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        DateTime sourceDate = new(2025, 10, 06, 19, 38, 0, 0, DateTimeKind.Utc);
        CandleTime sourceUnix = CandleTime.AlignFromDateTime(sourceDate, sourceInterval.Duration);

        // should fall back on the previous day because the day candle aint finished
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval10m];
        CandleTime targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = targetUnix.ToDateTime();

        DateTime expectedDate = new(2025, 10, 06, 19, 20, 0, 0, DateTimeKind.Utc);
        CandleTime expectedUnix = CandleTime.AlignFromDateTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");
    }


    [TestMethod]
    public void StartOfIntervalCandle2Test20MinutesBack()
    {
        InitTestSession();

        DateTime sourceDate = new(2025, 10, 06, 19, 38, 0, 0, DateTimeKind.Utc);
        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        CandleTime sourceUnix = CandleTime.AlignFromDateTime(sourceDate, sourceInterval.Duration);


        // should fall back on ..
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval30m];
        CandleTime targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = targetUnix.ToDateTime();

        DateTime expectedDate = new(2025, 10, 06, 19, 00, 0, 0, DateTimeKind.Utc);
        CandleTime expectedUnix = CandleTime.AlignFromDateTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");
    }


    [TestMethod]
    public void StartOfIntervalCandle2Test1HourBack()
    {
        InitTestSession();

        DateTime sourceDate = new(2025, 10, 06, 19, 38, 0, 0, DateTimeKind.Utc);
        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        CandleTime sourceUnix = CandleTime.AlignFromDateTime(sourceDate, sourceInterval.Duration);


        // should fall back on ..
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];
        CandleTime targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = targetUnix.ToDateTime();

        DateTime expectedDate = new(2025, 10, 06, 18, 00, 0, 0, DateTimeKind.Utc);
        CandleTime expectedUnix = CandleTime.AlignFromDateTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");

    }


    [TestMethod]
    public void StartOfIntervalCandle2Test2HoursBack()
    {
        InitTestSession();

        DateTime sourceDate = new(2025, 10, 06, 19, 38, 0, 0, DateTimeKind.Utc);
        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        CandleTime sourceUnix = CandleTime.AlignFromDateTime(sourceDate, sourceInterval.Duration);


        // should fall back on ..
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2h];
        CandleTime targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = targetUnix.ToDateTime();

        DateTime expectedDate = new(2025, 10, 06, 16, 00, 0, 0, DateTimeKind.Utc);
        CandleTime expectedUnix = CandleTime.AlignFromDateTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");
    }



    [TestMethod]
    public void StartOfIntervalCandle2Test7()
    {
        InitTestSession();

        DateTime sourceDate = new(2025, 10, 06, 19, 38, 0, 0, DateTimeKind.Utc);
        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        CandleTime sourceUnix = CandleTime.AlignFromDateTime(sourceDate, sourceInterval.Duration);


        // should fall back on ..
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval2h];
        CandleTime targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = targetUnix.ToDateTime();

        DateTime expectedDate = new(2025, 10, 06, 16, 00, 0, 0, DateTimeKind.Utc);
        CandleTime expectedUnix = CandleTime.AlignFromDateTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");
    }


    [TestMethod]
    public void StartOfIntervalCandle2Test1DayBack()
    {
        InitTestSession();

        DateTime sourceDate = new(2025, 10, 06, 19, 38, 0, 0, DateTimeKind.Utc);
        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        CandleTime sourceUnix = CandleTime.AlignFromDateTime(sourceDate, sourceInterval.Duration);

        // should fall back on the previous day because the day candle aint finished
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1d];
        CandleTime targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = targetUnix.ToDateTime();

        DateTime expectedDate = new(2025, 10, 05, 00, 00, 0, 0, DateTimeKind.Utc);
        CandleTime expectedUnix = CandleTime.AlignFromDateTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");
    }



    [TestMethod]
    public void StartOfIntervalCandle2Test1DayBack2()
    {
        InitTestSession();

        DateTime sourceDate = new(2025, 10, 06, 00, 0, 0, 0, DateTimeKind.Utc);
        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        CandleTime sourceUnix = CandleTime.AlignFromDateTime(sourceDate, sourceInterval.Duration);

        // should fall back on the previous day because the day candle aint finished
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1d];
        CandleTime targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = targetUnix.ToDateTime();

        DateTime expectedDate = new(2025, 10, 05, 00, 00, 0, 0, DateTimeKind.Utc);
        CandleTime expectedUnix = CandleTime.AlignFromDateTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");
    }


    [TestMethod]
    public void StartOfIntervalCandle2Test1DayBack3()
    {
        InitTestSession();

        DateTime sourceDate = new(2025, 10, 06, 23, 59, 0, 0, DateTimeKind.Utc);
        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        CandleTime sourceUnix = CandleTime.AlignFromDateTime(sourceDate, sourceInterval.Duration);

        // should fall back on the previous day because the day candle aint finished
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1d];
        CandleTime targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = targetUnix.ToDateTime();

        DateTime expectedDate = new(2025, 10, 06, 00, 00, 0, 0, DateTimeKind.Utc);
        CandleTime expectedUnix = CandleTime.AlignFromDateTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");
    }



    [TestMethod]
    public void StartOfIntervalCandle2Test1DayBack4()
    {
        InitTestSession();

        DateTime sourceDate = new(2025, 10, 07, 00, 00, 0, 0, DateTimeKind.Utc);
        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        CandleTime sourceUnix = CandleTime.AlignFromDateTime(sourceDate, sourceInterval.Duration);

        // should fall back on the previous day because the day candle aint finished
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1d];
        CandleTime targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = targetUnix.ToDateTime();

        DateTime expectedDate = new(2025, 10, 06, 00, 00, 0, 0, DateTimeKind.Utc);
        CandleTime expectedUnix = CandleTime.AlignFromDateTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");
    }


    [TestMethod]
    public void StartOfIntervalCandle2Test1DayBack5()
    {
        InitTestSession();

        DateTime sourceDate = new(2025, 10, 07, 00, 01, 0, 0, DateTimeKind.Utc);
        CryptoInterval sourceInterval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1m];
        CandleTime sourceUnix = CandleTime.AlignFromDateTime(sourceDate, sourceInterval.Duration);

        // should fall back on the previous day because the day candle aint finished
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1d];
        CandleTime targetUnix = IntervalTools.StartOfIntervalCandle2(sourceUnix, sourceInterval.Duration, intervalTarget.Duration);
        DateTime targetDate = targetUnix.ToDateTime();

        DateTime expectedDate = new(2025, 10, 06, 00, 00, 0, 0, DateTimeKind.Utc);
        CandleTime expectedUnix = CandleTime.AlignFromDateTime(expectedDate, intervalTarget.Duration);
        Assert.AreEqual(expectedUnix, targetUnix, "Target date");
    }
}