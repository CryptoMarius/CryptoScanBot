using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.CoreTests;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Trader.Tests;

/// <summary>
/// The Settings.Trading.UseAssetManagement switch, and the two rules that came with it: an entry is
/// sized against the FREE balance instead of the total, and a new position must leave room for the
/// DCA levels behind it.
/// <para>
/// The switch exists because a run that cannot open a position for lack of money answers a different
/// question than a run that takes every signal. Both are worth measuring, so both have to be
/// reachable from the same build - and a run has to say afterwards which of the two it was, which is
/// why the emulator writes the setting into its snapshot.
/// </para>
/// </summary>
[TestClass]
public class AssetManagementSwitchTests : TestBase
{
    private static (CryptoDatabase database, CryptoSymbol symbol, CryptoAsset assetQuote) Arrange(decimal total)
    {
        InitTestSession();
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;

        CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        DeleteAllPositionRelatedStuff(database);

        CryptoAsset assetQuote = new() { Name = symbol.Quote, Total = total, Free = total, Locked = 0 };
        GlobalData.ActiveExchange!.Data.AssetList.TryAdd(assetQuote.Name, assetQuote);

        // Assert on whatever is IN the list, not on the object we just built: TryAdd keeps the
        // existing entry when a coin is already there.
        assetQuote = GlobalData.ActiveExchange!.Data.AssetList[assetQuote.Name];
        assetQuote.Total = total;
        assetQuote.Locked = 0;
        assetQuote.Free = total;

        database.Connection.Insert(assetQuote);
        return (database, symbol, assetQuote);
    }


    /// <summary>
    /// Put an open buy order on the book, which is the only way to get a locked amount: Locked is
    /// DERIVED from the orders that are open right now, so writing it straight into the asset is
    /// undone by the first recalculation.
    /// </summary>
    private static void ReserveWithAnOpenOrder(CryptoDatabase database, CryptoSymbol symbol, decimal price, decimal quantity)
    {
        DateTime startTime = DateTime.UtcNow.AddHours(-48);
        CryptoPosition position = PositionTools.CreatePosition(symbol, "stobb", CryptoTradeSide.Long, "Test",
            symbol.Data.SymbolIntervalList[0], startTime);
        database.Connection.Insert(position);
        PositionTools.AddPosition(position);

        CryptoPositionPart part = PositionTools.ExtendPosition(database, position, CryptoPartPurpose.Entry,
            position.Symbol.Data.SymbolIntervalList[0].Interval, "Test", price, startTime);

        TradeParams tradeParams = CreateTradeParams(database, startTime, CryptoOrderSide.Buy,
            CryptoOrderType.Limit, price, quantity);
        CryptoPositionStep step = PositionTools.CreatePositionStep(position, part, tradeParams);
        database.Connection.Insert<CryptoPositionStep>(step);
        PositionTools.AddPositionPartStep(part, step);
    }


    private bool _savedUseAssetManagement;
    private decimal _savedStartCapital;
    private List<CryptoDcaEntry> _savedDcaList = [];

    [TestInitialize]
    public void SaveSettings()
    {
        _savedUseAssetManagement = GlobalData.Settings.Trading.UseAssetManagement;
        _savedStartCapital = GlobalData.Settings.Trading.PaperAssetStartCapital;
        _savedDcaList = GlobalData.Settings.Trading.DcaList;

        // No DCA levels unless a test asks for them - the reservation is what most of these tests
        // are NOT about, and another test class may have left a list behind.
        GlobalData.Settings.Trading.DcaList = [];
        GlobalData.Settings.Trading.UseAssetManagement = true;
    }

    [TestCleanup]
    public void RestoreSettings()
    {
        GlobalData.Settings.Trading.UseAssetManagement = _savedUseAssetManagement;
        GlobalData.Settings.Trading.PaperAssetStartCapital = _savedStartCapital;
        GlobalData.Settings.Trading.DcaList = _savedDcaList;
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The DCA reservation
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The factor is a PERCENTAGE of the entry amount, not a multiplier: 100/200/400 on a 100 entry
    /// costs 100 + 200 + 400 = 700 on top of it. Reading those numbers as multipliers is an old
    /// trap - a data folder from before the change buys 2% instead of 2x - so it is pinned here.
    /// </summary>
    [TestMethod]
    public void TheDcaReservationAddsUpTheFactorsAsPercentages()
    {
        GlobalData.Settings.Trading.DcaList =
        [
            new CryptoDcaEntry { Percentage = 1.5m, Factor = 100m },
            new CryptoDcaEntry { Percentage = 3.0m, Factor = 200m },
            new CryptoDcaEntry { Percentage = 4.5m, Factor = 400m },
        ];

        Assert.AreEqual(700m, AssetTools.GetDcaReservation(100m));
    }

    /// <summary>Without DCA levels there is nothing to put aside.</summary>
    [TestMethod]
    public void TheDcaReservationIsZeroWithoutLevels()
    {
        Assert.AreEqual(0m, AssetTools.GetDcaReservation(100m));
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Asset management ON
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// An entry that fits on its own is still refused when the DCA levels behind it do not, because
    /// the trader puts every level on the book the moment the entry fills. Letting it through buys a
    /// position that cannot be defended: the drop comes, and the orders meant to catch it are refused.
    /// </summary>
    [TestMethod]
    public void ANewPositionNeedsRoomForItsDcaLevelsAsWell()
    {
        var (database, symbol, _) = Arrange(total: 500m);
        symbol.QuoteData!.EntryAmount = 100m;
        symbol.QuoteData.EntryPercentage = 0;
        GlobalData.Settings.Trading.DcaList =
        [
            new CryptoDcaEntry { Percentage = 1.5m, Factor = 200m },
            new CryptoDcaEntry { Percentage = 3.0m, Factor = 400m },
        ];

        // 100 entry + 200 + 400 = 700 against a free balance of 500
        var result = AssetTools.CheckAvailableAssets(GlobalData.ActiveExchange!, symbol, reserveForDca: true);

        Assert.IsFalse(result.success, "entry plus DCA levels do not fit in 500");
        StringAssert.Contains(result.reaction, "dca's");
    }

    /// <summary>
    /// The same situation is fine for a position that is already open: those DCA orders are on the
    /// book already, so their money is in the locked amount and counting it again would refuse a
    /// perfectly ordinary top-up.
    /// </summary>
    [TestMethod]
    public void AddingToAnOpenPositionDoesNotReserveForDcaLevels()
    {
        var (database, symbol, _) = Arrange(total: 500m);
        symbol.QuoteData!.EntryAmount = 100m;
        symbol.QuoteData.EntryPercentage = 0;
        GlobalData.Settings.Trading.DcaList =
        [
            new CryptoDcaEntry { Percentage = 1.5m, Factor = 200m },
            new CryptoDcaEntry { Percentage = 3.0m, Factor = 400m },
        ];

        var result = AssetTools.CheckAvailableAssets(GlobalData.ActiveExchange!, symbol);

        Assert.IsTrue(result.success, $"a 100 entry fits in 500 ({result.reaction})");
        Assert.AreEqual(100m, result.entryQuoteAsset);
    }

    /// <summary>
    /// An entry percentage is taken from what is FREE, not from the total. The difference is the
    /// money that open orders already claimed: spending it would be spending it twice.
    /// </summary>
    [TestMethod]
    public void AnEntryPercentageIsTakenFromTheFreeBalance()
    {
        var (database, symbol, _) = Arrange(total: 1000m);
        ReserveWithAnOpenOrder(database, symbol, price: 100m, quantity: 6m); // 600 on the book
        symbol.QuoteData!.EntryAmount = 0;
        symbol.QuoteData.EntryPercentage = 10;

        var result = AssetTools.CheckAvailableAssets(GlobalData.ActiveExchange!, symbol);

        Assert.IsTrue(result.success, result.reaction);
        Assert.AreEqual(40m, result.entryQuoteAsset, "10% of the 400 that is free, not of the 1000 total");
    }

    /// <summary>
    /// A percentage of a shrinking balance keeps producing ever smaller entries. The entry AMOUNT is
    /// the floor under that: below it the entry is not taken at all.
    /// </summary>
    [TestMethod]
    public void AnEntryBelowTheConfiguredAmountIsRefused()
    {
        var (database, symbol, _) = Arrange(total: 200m);
        symbol.QuoteData!.EntryAmount = 50m;   // the floor
        symbol.QuoteData.EntryPercentage = 10; // 10% of 200 = 20, below the floor

        var result = AssetTools.CheckAvailableAssets(GlobalData.ActiveExchange!, symbol);

        Assert.IsFalse(result.success, "20 is below the 50 floor");
        StringAssert.Contains(result.reaction, "minimum entry amount");
    }

    /// <summary>
    /// The floor only applies when a percentage is in use. A plain fixed amount IS the entry, so
    /// comparing it against itself would refuse every entry there is.
    /// </summary>
    [TestMethod]
    public void AFixedEntryAmountIsNotItsOwnFloor()
    {
        var (database, symbol, _) = Arrange(total: 200m);
        symbol.QuoteData!.EntryAmount = 50m;
        symbol.QuoteData.EntryPercentage = 0;

        var result = AssetTools.CheckAvailableAssets(GlobalData.ActiveExchange!, symbol);

        Assert.IsTrue(result.success, result.reaction);
        Assert.AreEqual(50m, result.entryQuoteAsset);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Asset management OFF
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// With the switch off nothing is refused for lack of money - not even on a balance that is
    /// spent down to nothing. That is the whole point: the run shows what the strategy does without
    /// a capital limit.
    /// </summary>
    [TestMethod]
    public void WithAssetManagementOffAnEmptyBalanceStillEnters()
    {
        var (database, symbol, _) = Arrange(total: 0m);
        GlobalData.Settings.Trading.UseAssetManagement = false;
        GlobalData.Settings.Trading.PaperAssetStartCapital = 10000m;
        symbol.QuoteData!.EntryAmount = 100m;
        symbol.QuoteData.EntryPercentage = 0;

        var result = AssetTools.CheckAvailableAssets(GlobalData.ActiveExchange!, symbol, reserveForDca: true);

        Assert.IsTrue(result.success, $"nothing may be refused for lack of money ({result.reaction})");
        Assert.AreEqual(100m, result.entryQuoteAsset);
    }

    /// <summary>
    /// The entry is then the plain entry AMOUNT, whatever the balance is doing and whatever
    /// percentage is filled in beside it - there is nothing to take a percentage of once the balance
    /// may run negative, and every entry the same size is what makes such a run readable.
    /// </summary>
    [TestMethod]
    public void WithAssetManagementOffTheEntryIsThePlainAmount()
    {
        var (database, symbol, _) = Arrange(total: 250m);
        ReserveWithAnOpenOrder(database, symbol, price: 100m, quantity: 2m); // 200 on the book, 50 free
        GlobalData.Settings.Trading.UseAssetManagement = false;
        symbol.QuoteData!.EntryAmount = 100m;
        symbol.QuoteData.EntryPercentage = 2; // ignored in this mode

        var result = AssetTools.CheckAvailableAssets(GlobalData.ActiveExchange!, symbol);

        Assert.IsTrue(result.success, result.reaction);
        Assert.AreEqual(100m, result.entryQuoteAsset, "the entry amount, not 2% of anything");
    }

    /// <summary>
    /// A quote coin that only has a percentage cannot trade in this mode, and says so. Quietly
    /// entering with a number nobody chose is how a run ends up unexplainable.
    /// </summary>
    [TestMethod]
    public void WithAssetManagementOffAPercentageOnlyQuoteCoinIsRefused()
    {
        var (database, symbol, _) = Arrange(total: 1000m);
        GlobalData.Settings.Trading.UseAssetManagement = false;
        symbol.QuoteData!.EntryAmount = 0;
        symbol.QuoteData.EntryPercentage = 2;

        var result = AssetTools.CheckAvailableAssets(GlobalData.ActiveExchange!, symbol);

        Assert.IsFalse(result.success);
        StringAssert.Contains(result.reaction, "No entry amount given");
    }

    /// <summary>
    /// The bookkeeping itself keeps running with the switch off - that is what the assets screen and
    /// any equity curve built on it need - and the balance is allowed to go under zero. Flooring it
    /// at zero would hide exactly the number such a run is meant to show.
    /// </summary>
    [TestMethod]
    public void WithAssetManagementOffTheBalanceMayGoNegative()
    {
        var (database, symbol, assetQuote) = Arrange(total: 100m);
        GlobalData.Settings.Trading.UseAssetManagement = false;

        // Buy 1 @ 250 on a balance of 100
        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, CryptoTradeSide.Long, CryptoOrderSide.Buy,
            CryptoOrderStatus.Filled, 1m, 250m, "entry-filled");

        assetQuote = GlobalData.ActiveExchange!.Data.AssetList[symbol.Quote];
        Assert.AreEqual(-150m, assetQuote.Total, "100 - 250, booked as it happened");
        Assert.AreEqual(-150m, assetQuote.Free, "nothing is reserved, so free follows total");
    }

    /// <summary>
    /// With the switch ON the same overspend cannot arise through the front door, but the floor that
    /// used to catch it stays in place - a balance is never handed to the trader as a negative number
    /// while asset management is what guards the entries.
    /// </summary>
    [TestMethod]
    public void WithAssetManagementOnTheBalanceIsStillFlooredAtZero()
    {
        var (database, symbol, assetQuote) = Arrange(total: 100m);
        GlobalData.Settings.Trading.UseAssetManagement = true;

        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, CryptoTradeSide.Long, CryptoOrderSide.Buy,
            CryptoOrderStatus.Filled, 1m, 250m, "entry-filled");

        // Total went to zero, and an asset with nothing in it and nothing reserved is dropped
        bool stillThere = GlobalData.ActiveExchange!.Data.AssetList.TryGetValue(symbol.Quote, out CryptoAsset? quote);
        Assert.IsTrue(!stillThere || quote!.Total == 0m, "the balance must not be handed out as a negative number");
    }
}
