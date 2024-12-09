using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.CoreTests.Intern;

[TestClass()]
public class IntervalToolsTests : TestBase
{
    [TestMethod]
    public void StartOfIntervalCandle3Test1()
    {
        InitTestSession();

        CryptoInterval intervalSource = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval10m];
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

        DateTime now = new(2024, 12, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        long sourceStart = CandleTools.GetUnixTime(now, intervalSource.Duration);

        for (int i = 0; i < 6; i++)
        {
            long sourceStartLoop = sourceStart + i * intervalSource.Duration;
            (bool targetComplete, long targetStart) = IntervalTools.StartOfIntervalCandle3(sourceStartLoop, intervalSource.Duration, intervalTarget.Duration);

            Assert.AreEqual(targetStart, sourceStart, "Target date");
            Assert.AreEqual(targetComplete, i == 5, "Target complete");
        }
    }


    [TestMethod]
    public void StartOfIntervalCandle3Test2()
    {
        // Same as the first test, but the startdate is shifted 5 minutes

        InitTestSession();

        CryptoInterval intervalSource = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval10m];
        CryptoInterval intervalTarget = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

        DateTime now = new(2024, 12, 1, 0, 5, 0, 0, DateTimeKind.Utc);
        long sourceStart = CandleTools.GetUnixTime(now, intervalSource.Duration);

        for (int i = 0; i < 6; i++)
        {
            long sourceStartLoop = sourceStart + i * intervalSource.Duration;
            (bool targetIncomplete, long targetStart) = IntervalTools.StartOfIntervalCandle3(sourceStartLoop, intervalSource.Duration, intervalTarget.Duration);

            Assert.AreEqual(targetStart, sourceStart, "Target date");
            Assert.AreEqual(targetIncomplete, i == 5, "Target complete");
        }
    }
}