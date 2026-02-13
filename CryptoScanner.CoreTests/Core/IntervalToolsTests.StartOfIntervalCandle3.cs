using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.CoreTests.Core;

[TestClass()]
public class IntervalToolsTestsStartOfIntervalCandle3 : TestBase
{
     [TestMethod]
    public void StartOfIntervalCandle3Test1()
    {
        InitTestSession();

        CryptoInterval intervalSource = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval10m];
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

        DateTime now = new(2024, 12, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        CandleTime sourceStart = CandleTime.AlignFromDateTime(now, intervalSource.Duration);

        for (int i = 0; i < 6; i++)
        {
            CandleTime sourceStartLoop = sourceStart + i * intervalSource.Duration;
            (bool targetComplete, CandleTime targetStart) = IntervalTools.StartOfIntervalCandle3(sourceStartLoop, intervalSource.Duration, intervalTarget.Duration);

            Assert.AreEqual(targetStart, sourceStart, "Target date");
            Assert.AreEqual(targetComplete, i == 5, "Target complete");
        }
    }


    [TestMethod]
    public void StartOfIntervalCandle3Test2()
    {
        InitTestSession();

        CryptoInterval intervalSource = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval10m];
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

        // Same as the first test, but the startdate is shifted 5 minutes
        DateTime now = new(2024, 12, 1, 0, 5, 0, 0, DateTimeKind.Utc);
        CandleTime sourceStart = CandleTime.AlignFromDateTime(now, intervalSource.Duration);

        for (int i = 0; i < 6; i++)
        {
            //???????????????? I dont get it at this moment
            // Time already strange because of the AlignFromDateTime (those 5m are gone!)

            CandleTime sourceStartLoop = sourceStart + i * intervalSource.Duration;
            DateTime sourceStartLoopDate = sourceStartLoop.ToDateTime();
            (bool targetIncomplete, CandleTime targetStart) = IntervalTools.StartOfIntervalCandle3(sourceStartLoop, intervalSource.Duration, intervalTarget.Duration);

            Assert.AreEqual(targetStart, sourceStart, "Target date");
            Assert.AreEqual(targetIncomplete, i == 5, "Target complete");
        }
    }

}