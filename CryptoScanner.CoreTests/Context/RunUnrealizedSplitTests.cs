using CryptoScanner.Core.Context;

namespace CryptoScanner.CoreTests.Context;

/// <summary>
/// The unrealised PnL of an older run goes to the side that still has open positions; with open
/// positions on both sides it cannot be split and stays empty.
/// </summary>
[TestClass]
public class RunUnrealizedSplitTests
{
    // Digest columns trimmed to the ones the split reads; side 0 = long, 1 = short, status 1 = open.
    private static string Digest(params string[] rows) =>
        "{\"v\":1,\"cols\":[\"sym\",\"side\",\"profit\",\"invested\",\"status\"],\"rows\":[" + string.Join(",", rows) + "]}";

    [TestMethod]
    public void OnlyLongOpen_AllToLong()
    {
        string digest = Digest("[1,0,-100.5,100,1]", "[2,1,3.2,100,2]");

        Assert.AreEqual((-41.63m, 0m), RunUnrealizedSplit.Split(digest, 1, -41.63m));
    }

    [TestMethod]
    public void OnlyShortOpen_AllToShort()
    {
        string digest = Digest("[1,1,99.9,100,1]", "[2,0,3.2,100,2]");

        Assert.AreEqual((0m, 12.5m), RunUnrealizedSplit.Split(PositionDigest.Pack(digest), 1, 12.5m));
    }

    [TestMethod]
    public void BothSidesOpen_Empty()
    {
        string digest = Digest("[1,0,-100.5,100,1]", "[2,1,99.9,100,1]");

        Assert.IsNull(RunUnrealizedSplit.Split(digest, 2, 1.58m));
    }

    [TestMethod]
    public void UnfilledEntryDoesNotCount()
    {
        // The short is still open but never bought anything: only the long carries value
        string digest = Digest("[1,0,-100.5,100,1]", "[2,1,0,0,1]");

        Assert.AreEqual((-7.58m, 0m), RunUnrealizedSplit.Split(digest, 2, -7.58m));
    }

    [TestMethod]
    public void NothingOpen_Zero()
    {
        Assert.AreEqual((0m, 0m), RunUnrealizedSplit.Split(Digest("[1,0,3.2,100,2]"), 0, 0m));
        Assert.AreEqual((0m, 0m), RunUnrealizedSplit.Split(null, 0, 0m));
        Assert.IsNull(RunUnrealizedSplit.Split(null, 3, -5m));
    }
}
