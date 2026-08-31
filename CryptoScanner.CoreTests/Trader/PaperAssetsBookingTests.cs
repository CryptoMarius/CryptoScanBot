using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.CoreTests;

using Dapper;

namespace CryptoScanner.Core.Trader.Tests;

/// <summary>
/// Booking a balance by hand from the paper-assets screen - the way to compose a starting position
/// without throwing everything away, and the only way to get a coin into the list that is not in it.
/// </summary>
[TestClass]
public class PaperAssetsBookingTests : TestBase
{
    private static CryptoDatabase Arrange()
    {
        InitTestSession();
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;

        CryptoDatabase database = new();
        database.Open();
        CreateTestSymbol(database);
        DeleteAllPositionRelatedStuff(database);
        return database;
    }


    /// <summary>A coin that is not in the list is booked, and it lands in capitals.</summary>
    [TestMethod]
    public void ACoinThatIsNotInTheListIsBooked()
    {
        using CryptoDatabase database = Arrange();

        Assert.IsTrue(PaperAssetsEditor.Add(GlobalData.ActiveExchange, " btc ", 0.1m));

        Assert.IsTrue(GlobalData.ActiveExchange!.Data.AssetList.TryGetValue("BTC", out CryptoAsset? asset),
            "trimmed and in capitals, or it lands next to the real balance");
        Assert.AreEqual(0.1m, asset!.Total);
        Assert.AreEqual(0.1m, asset.Free, "nothing is reserved on it");
    }


    /// <summary>A coin that IS in the list is set to the amount, exactly like a correction.</summary>
    [TestMethod]
    public void ACoinThatIsAlreadyThereIsSetToTheAmount()
    {
        using CryptoDatabase database = Arrange();

        PaperAssetsEditor.Add(GlobalData.ActiveExchange, "BTC", 0.1m);
        PaperAssetsEditor.Add(GlobalData.ActiveExchange, "BTC", 0.25m);

        Assert.AreEqual(0.25m, GlobalData.ActiveExchange!.Data.AssetList["BTC"].Total, "set, not added up");
    }


    /// <summary>
    /// The booking is a movement in and out of the account, so it belongs in the ledger - without it
    /// the capital line would read the amount as a very good day.
    /// </summary>
    [TestMethod]
    public void TheBookingIsRecordedAsAManualCorrection()
    {
        using CryptoDatabase database = Arrange();
        database.Connection.Execute("delete from AssetAdjustment");

        PaperAssetsEditor.Add(GlobalData.ActiveExchange, "ADA", 1000m);

        int corrections = database.Connection.ExecuteScalar<int>(
            "select count(*) from AssetAdjustment where Name = 'ADA' and Reason = @reason",
            new { reason = (int)CryptoAssetAdjustmentReason.ManualCorrection });
        Assert.AreEqual(1, corrections);
    }


    /// <summary>Nothing usable, nothing booked.</summary>
    [TestMethod]
    public void AnEmptyCoinOrAnAmountOfZeroIsRefused()
    {
        using CryptoDatabase database = Arrange();

        Assert.IsFalse(PaperAssetsEditor.Add(GlobalData.ActiveExchange, "", 100m), "no coin");
        Assert.IsFalse(PaperAssetsEditor.Add(GlobalData.ActiveExchange, "   ", 100m), "still no coin");
        Assert.IsFalse(PaperAssetsEditor.Add(GlobalData.ActiveExchange, "BTC", 0m), "no amount");
        Assert.IsFalse(PaperAssetsEditor.Add(GlobalData.ActiveExchange, "BTC", -1m), "and not a negative one");

        Assert.AreEqual(0, GlobalData.ActiveExchange!.Data.AssetList.Count);
    }
}
