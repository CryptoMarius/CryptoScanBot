using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;
using ExchangeModel = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Tests for ThreadCheckFinishedPosition.NextDueKey: the queue takes the first position that is due,
/// so a position waiting for its DelayUntil (10s after a fill) does not hold up the entries behind it.
/// </summary>
[TestClass]
public class ThreadCheckFinishedPositionQueueTests
{
    private static readonly DateTime Now = new(2026, 11, 5, 16, 0, 6, DateTimeKind.Utc);

    private static (CryptoPosition position, string? orderId, CryptoOrderStatus? status) Item(string name, DateTime? delayUntil)
    {
        ExchangeModel exchange = new() { Id = 1, Name = "TestExchange", TakerFeeRate = 0.1m, MakerFeeRate = 0.1m };
        CryptoSymbol symbol = new()
        {
            Id = 1,
            Name = name,
            Base = name.Replace("USDC", ""),
            Quote = "USDC",
            Exchange = exchange,
            ExchangeId = exchange.Id,
            ExchangeName = exchange.Name,
            QuoteData = GlobalData.AddQuoteData("USDC"),
        };
        CryptoPosition position = new()
        {
            Exchange = exchange,
            Symbol = symbol,
            Interval = new CryptoInterval { Id = 3, Name = "5m", Duration = 300 },
            Side = CryptoTradeSide.Long,
            DelayUntil = delayUntil,
        };
        return (position, null, null);
    }

    [TestMethod]
    public void DelayedPositionInFront_DoesNotBlockTheNewEntryBehindIt()
    {
        // JUP was just filled and waits 10s; BTC is a new entry and must go now
        Dictionary<string, (CryptoPosition position, string? orderId, CryptoOrderStatus? status)> queue = new()
        {
            ["JUPUSDC#1"] = Item("JUPUSDC", Now.AddSeconds(10)),
            ["BTCUSDC#2"] = Item("BTCUSDC", null),
        };

        Assert.AreEqual("BTCUSDC#2", ThreadCheckFinishedPosition.NextDueKey(queue, Now));
    }

    [TestMethod]
    public void AllDelayed_NothingIsDue()
    {
        Dictionary<string, (CryptoPosition position, string? orderId, CryptoOrderStatus? status)> queue = new()
        {
            ["JUPUSDC#1"] = Item("JUPUSDC", Now.AddSeconds(10)),
            ["BTCUSDC#2"] = Item("BTCUSDC", Now),
        };

        Assert.IsNull(ThreadCheckFinishedPosition.NextDueKey(queue, Now), "a DelayUntil equal to now still waits, like ProcessPosition");
    }

    [TestMethod]
    public void PassedDelay_IsDueInQueueOrder()
    {
        Dictionary<string, (CryptoPosition position, string? orderId, CryptoOrderStatus? status)> queue = new()
        {
            ["JUPUSDC#1"] = Item("JUPUSDC", Now.AddSeconds(-1)),
            ["BTCUSDC#2"] = Item("BTCUSDC", null),
        };

        Assert.AreEqual("JUPUSDC#1", ThreadCheckFinishedPosition.NextDueKey(queue, Now));
    }

    [TestMethod]
    public void EmptyQueue_NothingIsDue()
    {
        Assert.IsNull(ThreadCheckFinishedPosition.NextDueKey([], Now));
    }
}
