using CryptoScanner.Core.Context;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The "adverse" column of the position digest (appended 03-10-2026): how far the price went against
/// a position at its worst, so a report can say what sat under water and for how long after the
/// positions themselves are dropped.
/// </summary>
[TestClass]
public class PositionDigestAdverseTests
{
    [TestMethod]
    public void ALong_ReadsTheLowestLow()
    {
        var row = new PositionDigest.DigestRow { Side = (int)CryptoTradeSide.Long, PriceMinPerc = "-12.346", PriceMaxPerc = "30" };
        Assert.AreEqual(-12.35, PositionDigest.Adverse(row));
    }


    /// <summary>A short goes against you when the price rises, so its highest high counts, as a negative number.</summary>
    [TestMethod]
    public void AShort_ReadsTheHighestHigh_AsANegativeNumber()
    {
        var row = new PositionDigest.DigestRow { Side = (int)CryptoTradeSide.Short, PriceMinPerc = "-30", PriceMaxPerc = "149.2" };
        Assert.AreEqual(-149.2, PositionDigest.Adverse(row));
    }


    [TestMethod]
    public void NothingMeasured_IsNull_NotZero()
    {
        var row = new PositionDigest.DigestRow { Side = (int)CryptoTradeSide.Long, PriceMinPerc = null };
        Assert.IsNull(PositionDigest.Adverse(row));
    }


    [TestMethod]
    public void TheColumnIsAppended_SoOlderDigestsKeepTheirLayout()
    {
        //Assert.AreEqual("event", CryptoPositionDigest.Columns[^2]);
        //Assert.AreEqual("adverse", CryptoPositionDigest.Columns[^1]);
        // "funding" was appended behind it on 24-11-2026
        //Assert.AreEqual("event", CryptoPositionDigest.Columns[^3]);
        //Assert.AreEqual("adverse", CryptoPositionDigest.Columns[^2]);
        //Assert.AreEqual("funding", CryptoPositionDigest.Columns[^1]);
        // "price", "sl" and "openPnl" were appended behind it on 26-11-2026
        Assert.AreEqual("event", CryptoPositionDigest.Columns[^6]);
        Assert.AreEqual("adverse", CryptoPositionDigest.Columns[^5]);
        Assert.AreEqual("funding", CryptoPositionDigest.Columns[^4]);
        Assert.AreEqual("price", CryptoPositionDigest.Columns[^3]);
        Assert.AreEqual("sl", CryptoPositionDigest.Columns[^2]);
        Assert.AreEqual("openPnl", CryptoPositionDigest.Columns[^1]);
    }
}
