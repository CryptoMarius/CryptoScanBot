using CryptoScanner.Core.Context;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// The profit of a position that closed with only dust left. Rebuilt from ETHEUR.SPOT on Bitvavo,
/// position 2 (27-10-2026): entry and one DCA, a take profit that was replaced 31 times and then filled.
/// The log said "position closed in loss=-15.72 EUR (-24.08%)" while the trade made +0.49 EUR (+1.00%):
/// the dust of every replaced take profit order was counted again, and that dust was valued at a
/// break-even price divided by the tiny quantity left (-60,690,178 EUR).
/// </summary>
[TestClass]
public class RemainingDustProfitTests : TestBase
{
    private const decimal TakeProfitDust = 0.0000000081m;

    private static CryptoPosition CreatePosition()
    {
        InitTestSession();
        var database = new CryptoDatabase();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
        CryptoPosition position = PositionTools.CreatePosition(symbol, "fvgladder",
            CryptoTradeSide.Long, "Test", symbolInterval, DateTime.UtcNow);
        position.Exchange.FeeRate = 0.25m;
        // Still Trading: the profit is calculated before the position is marked Ready
        position.Status = CryptoPositionStatus.Trading;
        return position;
    }

    private static CryptoPositionPart AddPart(CryptoPosition position, CryptoPartPurpose purpose, int partNumber)
    {
        var part = new CryptoPositionPart
        {
            Position = position,
            Symbol = position.Symbol,
            Exchange = position.Exchange,
            Purpose = purpose,
            Interval = position.Interval,
        };
        position.PartList.Add(partNumber, part);
        return part;
    }

    private static void AddFilledBuy(CryptoPositionPart part, decimal price, decimal quantity, decimal commissionBase, decimal commission)
    {
        part.StepList.Add(part.StepList.Count + 1, new CryptoPositionStep
        {
            Side = CryptoOrderSide.Buy,
            Status = CryptoOrderStatus.Filled,
            AveragePrice = price,
            Price = price,
            Quantity = quantity,
            QuantityFilled = quantity,
            QuoteQuantityFilled = price * quantity,
            CommissionBase = commissionBase,
            Commission = commission,
        });
    }

    private static CryptoPosition CreateEthEurPosition()
    {
        CryptoPosition position = CreatePosition();
        AddFilledBuy(AddPart(position, CryptoPartPurpose.Entry, 1), 2101.92m, 0.0078024m, 0.000019506m, 0.04100005152m);
        AddFilledBuy(AddPart(position, CryptoPartPurpose.Dca, 2), 2070.4m, 0.01584236m, 0.0000396059m, 0.08200005536m);

        CryptoPositionPart takeProfit = AddPart(position, CryptoPartPurpose.TakeProfit, 3);
        // 31 replaced take profit orders, each carrying the same expected dust
        for (int i = 0; i < 31; i++)
        {
            takeProfit.StepList.Add(takeProfit.StepList.Count + 1, new CryptoPositionStep
            {
                Side = CryptoOrderSide.Sell,
                Status = CryptoOrderStatus.ChangedBreakEven,
                Price = 2112.14m,
                Quantity = 0.02358564m,
                RemainingDust = TakeProfitDust,
            });
        }
        // The one that filled
        takeProfit.StepList.Add(takeProfit.StepList.Count + 1, new CryptoPositionStep
        {
            Side = CryptoOrderSide.Sell,
            Status = CryptoOrderStatus.PartiallyAndClosed,
            AveragePrice = 2112.14m,
            Price = 2112.14m,
            Quantity = 0.02358564m,
            QuantityFilled = 0.02358564m,
            QuoteQuantityFilled = 49.8161736696m,
            Commission = 0.124540434174m,
            CommissionQuote = 0.124540434174m,
            RemainingDust = TakeProfitDust,
        });
        return position;
    }

    [TestMethod]
    public void DustOfReplacedTakeProfitOrders_IsNotCountedAgain()
    {
        CryptoPosition position = CreateEthEurPosition();

        TradeTools.CalculateProfitAndBreakEvenPrice(position);

        Assert.AreEqual(TakeProfitDust, position.RemainingDust, "only the dust of the filled take profit order counts");
    }

    [TestMethod]
    public void PositionClosedWithDust_ReportsItsRealProfit()
    {
        CryptoPosition position = CreateEthEurPosition();

        TradeTools.CalculateProfitAndBreakEvenPrice(position);

        // Returned 49.8162 - invested 49.0770 - commission 0.2475 = +0.4916, the dust (8.1e-9 ETH,
        // about 0.00002 EUR) hardly matters
        Assert.AreEqual(0.49m, Math.Round(position.Profit, 2), $"profit={position.Profit}");
        Assert.AreEqual(1.00m, Math.Round(position.Percentage - 100m, 2), $"percentage={position.Percentage}");
    }
}
