using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;
using ExchangeModel = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Tests for PositionTools.IsParkedTakeProfit and PendingTakeProfitTrail, the display-only
/// helpers the charts use for a trailing last take profit: its order is parked at ten times the
/// target until the target is reached, so the charts leave that price out and draw the target as
/// "tp trigger" instead.
/// </summary>
[TestClass]
public class PendingTakeProfitTrailTests
{
    private static readonly DateTime Placed = new(2026, 11, 5, 13, 35, 0, DateTimeKind.Utc);

    private static CryptoPosition Position(CryptoTradeSide side, decimal entry, decimal target, decimal takeProfitOrderPrice)
    {
        ExchangeModel exchange = new() { Id = 1, Name = "TestExchange", TakerFeeRate = 0.1m, MakerFeeRate = 0.1m };
        CryptoSymbol symbol = new()
        {
            Id = 1,
            Name = "GRAMUSDC",
            Base = "GRAM",
            Quote = "USDC",
            Exchange = exchange,
            ExchangeId = exchange.Id,
            ExchangeName = exchange.Name,
            QuoteData = GlobalData.AddQuoteData("USDC"),
            PriceTickSize = 0.000001m,
            PriceMinimum = 0.000001m,
            PriceMaximum = 1_000_000m,
        };
        CryptoPosition position = new()
        {
            Exchange = exchange,
            Symbol = symbol,
            Interval = new CryptoInterval { Id = 3, Name = "5m", Duration = 300 },
            Side = side,
            EntryPrice = entry,
            ProfitPrice = target,
        };
        CryptoPositionPart part = new()
        {
            Position = position,
            Exchange = exchange,
            Symbol = symbol,
            Purpose = CryptoPartPurpose.TakeProfit,
            PartNumber = 1,
        };
        part.StepList.Add(1, new CryptoPositionStep
        {
            Side = side == CryptoTradeSide.Long ? CryptoOrderSide.Sell : CryptoOrderSide.Buy,
            Price = takeProfitOrderPrice,
            CreateTime = Placed,
        });
        position.PartList.Add(1, part);
        return position;
    }

    [TestMethod]
    public void Long_ParkedOrder_ShowsTheTargetAsTrigger()
    {
        // GRAM 05-11: entry 1.1407, target 1.150717, the order parked at ten times the target
        CryptoPosition position = Position(CryptoTradeSide.Long, 1.1407m, 1.150717m, 11.50717m);

        Assert.IsTrue(PositionTools.IsParkedTakeProfit(position, position.PartList[1].StepList[1]));
        var pending = PositionTools.PendingTakeProfitTrail(position);
        Assert.IsNotNull(pending);
        Assert.AreEqual(1.150717m, pending.Value.TargetPrice);
        Assert.AreEqual(Placed, pending.Value.Since);
    }

    [TestMethod]
    public void Short_ParkedOrder_ShowsTheTargetAsTrigger()
    {
        CryptoPosition position = Position(CryptoTradeSide.Short, 1.1407m, 1.129m, 0.1129m);

        Assert.IsTrue(PositionTools.IsParkedTakeProfit(position, position.PartList[1].StepList[1]));
        Assert.AreEqual(1.129m, PositionTools.PendingTakeProfitTrail(position)!.Value.TargetPrice);
    }

    [TestMethod]
    public void NormalTakeProfit_IsNotParkedAndHasNoTrigger()
    {
        CryptoPosition position = Position(CryptoTradeSide.Long, 1.1407m, 1.150717m, 1.150717m);

        Assert.IsFalse(PositionTools.IsParkedTakeProfit(position, position.PartList[1].StepList[1]));
        Assert.IsNull(PositionTools.PendingTakeProfitTrail(position));
    }

    [TestMethod]
    public void ArmedTrail_HasNoTrigger()
    {
        CryptoPosition position = Position(CryptoTradeSide.Long, 1.1407m, 1.150717m, 11.50717m);
        position.TakeProfitTrailStopPrice = 1.1450m;

        Assert.IsNull(PositionTools.PendingTakeProfitTrail(position), "once reached the trail is drawn as the stop price");
        Assert.IsTrue(PositionTools.IsParkedTakeProfit(position, position.PartList[1].StepList[1]), "the parked price stays out of the chart");
    }

    [TestMethod]
    public void ClosedPosition_HasNoTrigger()
    {
        CryptoPosition position = Position(CryptoTradeSide.Long, 1.1407m, 1.150717m, 11.50717m);
        position.CloseTime = Placed.AddHours(1);

        Assert.IsNull(PositionTools.PendingTakeProfitTrail(position));
    }
}
