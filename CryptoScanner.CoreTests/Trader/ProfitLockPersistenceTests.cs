using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Does the trailing profit lock survive a restart?
/// <para>
/// This is the failure mode that no arithmetic test can catch and that nobody would notice from the
/// outside: if <see cref="CryptoPosition.TrailingStopPrice"/> is not actually written to and read
/// back from the database, a restart resets it to zero, the next candle recomputes the trail from
/// the CURRENT price, and the stop silently drops back to just under wherever the market happens to
/// be. The position keeps trading and the log says nothing - it just hands back every bit of ground
/// it had gained. So the round trip gets its own test.
/// </para>
/// </summary>
[TestClass]
public class ProfitLockPersistenceTests : TestBase
{
    private static (CryptoDatabase database, CryptoSymbol symbol) Arrange()
    {
        InitTestSession();
        CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        DeleteAllPositionRelatedStuff(database);
        return (database, symbol);
    }

    private static CryptoPosition MakePosition(CryptoSymbol symbol) => new()
    {
        CreateTime = DateTime.UtcNow,
        UpdateTime = DateTime.UtcNow,
        Exchange = GlobalData.ActiveExchange!,
        ExchangeId = GlobalData.ActiveExchange!.Id,
        Symbol = symbol,
        SymbolId = symbol.Id,
        Interval = GlobalData.IntervalList[0],
        IntervalId = GlobalData.IntervalList[0].Id,
        Side = CryptoTradeSide.Long,
        Status = CryptoPositionStatus.Trading,
        Strategy = "dbr",
    };

    [TestMethod]
    public void TrailingStopPrice_SurvivesTheRoundTripToTheDatabase()
    {
        var (database, symbol) = Arrange();
        using (database)
        {
            CryptoPosition position = MakePosition(symbol);
            position.SlMovedToBreakEven = true;
            position.TrailingStopPrice = 108.35m;
            database.Connection.Insert(position);

            CryptoPosition? reloaded = database.Connection.Get<CryptoPosition>(position.Id);

            Assert.IsNotNull(reloaded);
            Assert.IsTrue(reloaded.SlMovedToBreakEven, "de vlag hoort ook terug te komen");
            Assert.AreEqual(108.35m, reloaded.TrailingStopPrice);
        }
    }

    [TestMethod]
    public void TrailingStopPrice_UpdateIsStoredAsWell()
    {
        var (database, symbol) = Arrange();
        using (database)
        {
            CryptoPosition position = MakePosition(symbol);
            position.SlMovedToBreakEven = true;
            position.TrailingStopPrice = 101.455m;
            database.Connection.Insert(position);

            // The trail follows a new high, exactly as CalculateSlPrices does it
            position.TrailingStopPrice = ProfitLockCalculator.TrailingStop(
                CryptoTradeSide.Long, 112m, 1.5m, position.TrailingStopPrice);
            database.Connection.Update(position);

            CryptoPosition? reloaded = database.Connection.Get<CryptoPosition>(position.Id);
            Assert.AreEqual(110.32m, reloaded!.TrailingStopPrice);
        }
    }

    [TestMethod]
    public void WithoutAProfitLock_TrailingStopPriceStaysZero()
    {
        var (database, symbol) = Arrange();
        using (database)
        {
            CryptoPosition position = MakePosition(symbol);
            database.Connection.Insert(position);

            CryptoPosition? reloaded = database.Connection.Get<CryptoPosition>(position.Id);
            Assert.AreEqual(0m, reloaded!.TrailingStopPrice);
            Assert.IsFalse(reloaded.SlMovedToBreakEven);
        }
    }

    [TestMethod]
    public void AfterAReload_TheTrailContinuesFromTheStoredLevelInsteadOfTheCurrentPrice()
    {
        var (database, symbol) = Arrange();
        using (database)
        {
            CryptoPosition position = MakePosition(symbol);
            position.SlMovedToBreakEven = true;
            position.TrailingStopPrice = ProfitLockCalculator.TrailingStop(CryptoTradeSide.Long, 112m, 1.5m, 0m);
            database.Connection.Insert(position);

            // Restart: the position comes back from the database and the next candle is a pullback
            CryptoPosition reloaded = database.Connection.Get<CryptoPosition>(position.Id)!;
            decimal afterRestart = ProfitLockCalculator.TrailingStop(
                CryptoTradeSide.Long, 104m, 1.5m, reloaded.TrailingStopPrice);

            Assert.AreEqual(110.32m, afterRestart, "de stop hoort te blijven staan, niet terug te vallen");

            // And this is what it would have become if the level had been lost in the round trip
            decimal ifItHadBeenLost = ProfitLockCalculator.TrailingStop(CryptoTradeSide.Long, 104m, 1.5m, 0m);
            Assert.AreEqual(102.44m, ifItHadBeenLost);
            Assert.AreNotEqual(ifItHadBeenLost, afterRestart);
        }
    }
}
