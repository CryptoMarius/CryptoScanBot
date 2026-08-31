using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.CoreTests;

using Dapper;

namespace CryptoScanner.Core.Trader.Tests;

/// <summary>
/// Undoing what a position did to the balances, the way deleting a position from the database does it.
/// <para>
/// Every test here books its fills through <see cref="PaperAssets.Change"/> and
/// <see cref="PaperAssets.BookCommission"/> - the same road the trader takes - and then checks that
/// the reversal puts the balance back exactly where it started. Recomputing the amounts by hand in
/// the test would only prove that two copies of the same sum agree with each other.
/// </para>
/// <para>
/// The short is the case that made this necessary. Its proceeds land on the balance at the entry
/// while the coins it owes are only ever derived from the open position, so deleting one without a
/// reversal leaves the proceeds behind and lets the debt evaporate.
/// </para>
/// </summary>
[TestClass]
public class PositionReversalTests : TestBase
{
    private static readonly DateTime Moment = new(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);

    private const decimal StartCapital = 10000m;

    private static CryptoDatabase Arrange(out CryptoSymbol symbol)
    {
        InitTestSession();
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;
        GlobalData.CurrentEmulatorRunId = null;

        CryptoDatabase database = new();
        database.Open();
        symbol = CreateTestSymbol(database);
        DeleteAllPositionRelatedStuff(database);
        symbol.LastPrice = 60000m;

        CryptoAsset quote = PaperAssets.FindOrCreateAsset(GlobalData.ActiveExchange!, symbol.Quote);
        quote.Total = StartCapital;
        quote.Free = StartCapital;
        quote.Locked = 0;
        return database;
    }


    /// <summary>The balance of one coin, or zero when it was dropped for being empty.</summary>
    private static decimal Balance(string name) =>
        GlobalData.ActiveExchange!.Data.AssetList.TryGetValue(name, out CryptoAsset? asset) ? asset.Total : 0m;


    /// <summary>
    /// A position that is NOT in the position list, which is the state a delete leaves behind:
    /// RemovePosition runs first, so the reservation of its open orders is already released.
    /// </summary>
    private static CryptoPosition CreatePosition(CryptoSymbol symbol, CryptoTradeSide side) =>
        PositionTools.CreatePosition(symbol, "stobb", side, "Test", symbol.Data.SymbolIntervalList[0], Moment);


    /// <summary>One fill, booked the way the trader books it.</summary>
    private static void Fill(CryptoPosition position, CryptoOrderSide side, decimal quantity, decimal price)
    {
        PaperAssets.Change(GlobalData.ActiveExchange!, position.Symbol, position.Side, side,
            CryptoOrderStatus.Filled, quantity, quantity * price, $"test-{side}");
    }


    private static void Fee(CryptoPosition position, decimal commissionBase, decimal commissionQuote)
    {
        PaperAssets.BookCommission(GlobalData.ActiveExchange!, position.Symbol, commissionBase, commissionQuote, "test-fee");
    }


    private static List<CryptoAssetAdjustment> ReadLedger(CryptoDatabase database) =>
        [.. database.Connection.Query<CryptoAssetAdjustment>(
            "select * from AssetAdjustment where Reason = @reason order by Id",
            new { reason = (int)CryptoAssetAdjustmentReason.PositionDeleted })];


    [TestMethod]
    public void An_open_short_gives_its_proceeds_back()
    {
        using CryptoDatabase database = Arrange(out CryptoSymbol symbol);
        CryptoPosition position = CreatePosition(symbol, CryptoTradeSide.Short);

        // Sell 0,1 at 60.000: 6.000 proceeds in, 6 commission out
        Fill(position, CryptoOrderSide.Sell, 0.1m, 60000m);
        Fee(position, 0m, 6m);
        Assert.AreEqual(15994m, Balance(symbol.Quote), "the proceeds are on the balance while the short runs");

        position.Quantity = 0.1m;
        position.Invested = 6000m;
        position.Returned = 0m;
        position.Commission = 6m;
        position.CommissionQuote = 6m;

        PaperAssets.ReversePosition(GlobalData.ActiveExchange!, position);

        Assert.AreEqual(StartCapital, Balance(symbol.Quote), "and are gone again with the position");
        Assert.AreEqual(0m, Balance(symbol.Base), "a short never held any coins");
    }


    [TestMethod]
    public void An_open_long_gives_its_coins_back()
    {
        using CryptoDatabase database = Arrange(out CryptoSymbol symbol);
        CryptoPosition position = CreatePosition(symbol, CryptoTradeSide.Long);

        // Buy 0,05 at 60.000: 3.000 out plus 3 commission, 0,05 coins in
        Fill(position, CryptoOrderSide.Buy, 0.05m, 60000m);
        Fee(position, 0m, 3m);
        Assert.AreEqual(6997m, Balance(symbol.Quote));
        Assert.AreEqual(0.05m, Balance(symbol.Base));

        position.Quantity = 0.05m;
        position.Invested = 3000m;
        position.Returned = 0m;
        position.Commission = 3m;
        position.CommissionQuote = 3m;

        PaperAssets.ReversePosition(GlobalData.ActiveExchange!, position);

        Assert.AreEqual(StartCapital, Balance(symbol.Quote));
        Assert.AreEqual(0m, Balance(symbol.Base), "the coins no position owns any more are gone too");
    }


    [TestMethod]
    public void A_closed_position_gives_its_profit_back()
    {
        using CryptoDatabase database = Arrange(out CryptoSymbol symbol);
        CryptoPosition position = CreatePosition(symbol, CryptoTradeSide.Long);

        // In at 60.000, out at 62.000: 93,90 earned after two commissions
        Fill(position, CryptoOrderSide.Buy, 0.05m, 60000m);
        Fee(position, 0m, 3m);
        Fill(position, CryptoOrderSide.Sell, 0.05m, 62000m);
        Fee(position, 0m, 3.1m);
        Assert.AreEqual(StartCapital + 93.9m, Balance(symbol.Quote));

        position.Quantity = 0m;
        position.Invested = 3000m;
        position.Returned = 3100m;
        position.Commission = 6.1m;
        position.CommissionQuote = 6.1m;

        PaperAssets.ReversePosition(GlobalData.ActiveExchange!, position);

        Assert.AreEqual(StartCapital, Balance(symbol.Quote),
            "a deleted position may not leave its result behind in the balance");
    }


    [TestMethod]
    public void A_commission_charged_in_the_coin_itself_is_handled()
    {
        using CryptoDatabase database = Arrange(out CryptoSymbol symbol);
        CryptoPosition position = CreatePosition(symbol, CryptoTradeSide.Long);

        // Spot charges the fee of a long entry in the coin you receive (see PaperTrading), so the
        // amounts on the position are not the amounts that moved: Quantity and Invested are already
        // net of it, and Commission carries it converted to quote.
        Fill(position, CryptoOrderSide.Buy, 0.05m, 60000m);
        Fee(position, 0.00005m, 0m);
        Assert.AreEqual(7000m, Balance(symbol.Quote));
        Assert.AreEqual(0.04995m, Balance(symbol.Base));

        position.Quantity = 0.04995m;
        position.Invested = 2997m;      // 60.000 x 0,04995
        position.Returned = 0m;
        position.Commission = 3m;       // 0,00005 x 60.000, the fee expressed in quote
        position.CommissionBase = 0.00005m;

        PaperAssets.ReversePosition(GlobalData.ActiveExchange!, position);

        Assert.AreEqual(StartCapital, Balance(symbol.Quote));
        Assert.AreEqual(0m, Balance(symbol.Base));
    }


    [TestMethod]
    public void A_position_that_never_filled_changes_nothing()
    {
        using CryptoDatabase database = Arrange(out CryptoSymbol symbol);
        CryptoPosition position = CreatePosition(symbol, CryptoTradeSide.Long);

        PaperAssets.ReversePosition(GlobalData.ActiveExchange!, position);

        Assert.AreEqual(StartCapital, Balance(symbol.Quote));
        Assert.AreEqual(0, ReadLedger(database).Count, "nothing moved, so nothing to write down");
    }


    [TestMethod]
    public void The_reversal_is_booked_as_a_correction_and_not_as_a_result()
    {
        using CryptoDatabase database = Arrange(out CryptoSymbol symbol);
        database.Connection.Execute("delete from AssetAdjustment");
        CryptoPosition position = CreatePosition(symbol, CryptoTradeSide.Short);

        Fill(position, CryptoOrderSide.Sell, 0.1m, 60000m);
        Fee(position, 0m, 6m);
        position.Quantity = 0.1m;
        position.Invested = 6000m;
        position.Commission = 6m;
        position.CommissionQuote = 6m;

        PaperAssets.ReversePosition(GlobalData.ActiveExchange!, position);

        // The capital line reads the ledger to keep this out of the traded result - without it the
        // 5.994 leaving the account would show up as a loss on the day of the delete.
        CryptoAssetAdjustment entry = ReadLedger(database).Single(e => e.Name == symbol.Quote);
        Assert.AreEqual(-5994m, entry.Quantity);
        Assert.AreEqual(-5994m, entry.Value);
    }
}
