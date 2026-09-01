using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using System.Globalization;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// The result appended to the "filled takeprofit" message that goes to the log and to Telegram
/// (TradeTools.FormatRealizedResult). The message used to stop at price/quantity/value, which says
/// what happened but not what it earned - the number the user actually wants to read.
/// <para>
/// The reference is the break-even price of the position, so the figure is net: the entry cost and
/// the commissions are already inside that price. A short earns the distance below break-even,
/// which is the one place where the sign is easy to get backwards.
/// </para>
/// </summary>
[TestClass]
public class FilledOrderMessageTests
{
    private CultureInfo _savedCulture = CultureInfo.InvariantCulture;

    /// <summary>
    /// The formatting follows the current culture (same as the rest of the message). Pin it, or the
    /// expected strings below only hold on a machine that happens to use a decimal point.
    /// </summary>
    [TestInitialize]
    public void PinCulture()
    {
        _savedCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    [TestCleanup]
    public void RestoreCulture() => CultureInfo.CurrentCulture = _savedCulture;


    private static CryptoPosition MakePosition(CryptoTradeSide side, string quote = "USDC")
    {
        var exchange = new Exchange { Id = 1, Name = "TestExchange", FeeRate = 0.1m };
        var symbol = new CryptoSymbol
        {
            Id = 1,
            Name = "TEST" + quote,
            Base = "TEST",
            Quote = quote,
            Exchange = exchange,
            ExchangeId = exchange.Id,
            ExchangeName = exchange.Name,
            QuoteData = GlobalData.AddQuoteData(quote),
            PriceTickSize = 0.00001m,
        };
        return new CryptoPosition
        {
            Id = 1,
            CreateTime = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc),
            Exchange = exchange,
            ExchangeId = exchange.Id,
            Symbol = symbol,
            SymbolId = 1,
            Interval = new CryptoInterval { Id = 6, Name = "15m", Duration = 900 },
            IntervalId = 6,
            Side = side,
            Status = CryptoPositionStatus.Trading,
            HasOrdersAndTradesLoaded = true,
        };
    }

    private static CryptoPositionStep MakeStep(CryptoOrderSide side, decimal averagePrice, decimal quantityFilled)
    {
        return new CryptoPositionStep
        {
            Id = 1,
            Side = side,
            AveragePrice = averagePrice,
            QuantityFilled = quantityFilled,
            Status = CryptoOrderStatus.Filled,
        };
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Long
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Long_TakeProfitAboveBreakEven_ReportsProfit()
    {
        // 34 sold at 0.43233 against a break-even of 0.42, so 0.01233 x 34 = 0.41922 -> 0.42 USDC,
        // and 0.01233 / 0.42 = 2.94%
        CryptoPosition position = MakePosition(CryptoTradeSide.Long);
        CryptoPositionStep step = MakeStep(CryptoOrderSide.Sell, 0.43233m, 34m);

        Assert.AreEqual(" profit=+0.42 USDC (+2.94%)", TradeTools.FormatRealizedResult(position, step, 0.42m));
    }

    [TestMethod]
    public void Long_TakeProfitBelowBreakEven_ReportsLoss()
    {
        // The take profit side is also the side a stop-loss leaves on, so this line has to be able
        // to report a loss - it carries its own minus sign, without a plus in front of it
        CryptoPosition position = MakePosition(CryptoTradeSide.Long);
        CryptoPositionStep step = MakeStep(CryptoOrderSide.Sell, 0.40m, 34m);

        Assert.AreEqual(" profit=-0.68 USDC (-4.76%)", TradeTools.FormatRealizedResult(position, step, 0.42m));
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Short
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Short_BuyBackBelowBreakEven_ReportsProfit()
    {
        // A short buys back to close, and it earns the distance below break-even
        CryptoPosition position = MakePosition(CryptoTradeSide.Short);
        CryptoPositionStep step = MakeStep(CryptoOrderSide.Buy, 0.40m, 34m);

        Assert.AreEqual(" profit=+0.68 USDC (+4.76%)", TradeTools.FormatRealizedResult(position, step, 0.42m));
    }

    [TestMethod]
    public void Short_BuyBackAboveBreakEven_ReportsLoss()
    {
        CryptoPosition position = MakePosition(CryptoTradeSide.Short);
        CryptoPositionStep step = MakeStep(CryptoOrderSide.Buy, 0.43233m, 34m);

        Assert.AreEqual(" profit=-0.42 USDC (-2.94%)", TradeTools.FormatRealizedResult(position, step, 0.42m));
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Nothing to say
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void EntryFill_SaysNothing()
    {
        // An entry has not earned anything yet; the message stays as it was
        CryptoPosition position = MakePosition(CryptoTradeSide.Long);
        CryptoPositionStep step = MakeStep(CryptoOrderSide.Buy, 0.42m, 34m);

        Assert.AreEqual("", TradeTools.FormatRealizedResult(position, step, 0.42m));
    }

    [TestMethod]
    public void WithoutBreakEvenPrice_SaysNothing()
    {
        // Rather no figure at all than a percentage measured against zero
        CryptoPosition position = MakePosition(CryptoTradeSide.Long);
        CryptoPositionStep step = MakeStep(CryptoOrderSide.Sell, 0.43233m, 34m);

        Assert.AreEqual("", TradeTools.FormatRealizedResult(position, step, 0m));
    }

    [TestMethod]
    public void WithoutFilledQuantity_SaysNothing()
    {
        CryptoPosition position = MakePosition(CryptoTradeSide.Long);
        CryptoPositionStep step = MakeStep(CryptoOrderSide.Sell, 0.43233m, 0m);

        Assert.AreEqual("", TradeTools.FormatRealizedResult(position, step, 0.42m));
    }

    // ════════════════════════════════════════════════════════════════════════
    //  The fill that closes the position: straight from the administration
    // ════════════════════════════════════════════════════════════════════════

    private static CryptoPosition MakeClosedPosition(decimal profit, decimal percentage, decimal invested = 14.28m)
    {
        CryptoPosition position = MakePosition(CryptoTradeSide.Short);
        position.Status = CryptoPositionStatus.Ready;
        position.Profit = profit;
        position.Percentage = percentage;
        position.Invested = invested;
        return position;
    }

    [TestMethod]
    public void ClosedInProfit_ReportsTheStoredFigures()
    {
        // The stored percentage is 100 based, so 102.94 is the +2.94% the message has to show
        Assert.AreEqual(" position closed in profit=+0.43 USDC (+2.94%)",
            TradeTools.FormatClosedPosition(MakeClosedPosition(0.43m, 102.94m)));
    }

    [TestMethod]
    public void ClosedInLoss_SaysLossInsteadOfProfit()
    {
        Assert.AreEqual(" position closed in loss=-0.68 USDC (-4.76%)",
            TradeTools.FormatClosedPosition(MakeClosedPosition(-0.68m, 95.24m)));
    }

    [TestMethod]
    public void ClosedWithoutInvestment_DoesNotReportMinusHundred()
    {
        // A position that never invested anything has a stored percentage of 0, and reading that as
        // a 100 based figure would turn an empty position into a total loss
        Assert.AreEqual(" position closed in profit=+0 USDC (+0.00%)",
            TradeTools.FormatClosedPosition(MakeClosedPosition(0m, 0m, invested: 0m)));
    }

    [TestMethod]
    public void ClosedPosition_ReportsTheWholePositionNotTheLastFill()
    {
        // The point of the whole exercise: three take profit levels of 5 USDC each earn 1.29 in
        // total, while the level that happens to close the position booked only its own 0.43
        CryptoPosition position = MakeClosedPosition(1.29m, 109.03m);
        CryptoPositionStep step = MakeStep(CryptoOrderSide.Buy, 0.40m, 34m);

        Assert.AreEqual(" position closed in profit=+1.29 USDC (+9.03%)", TradeTools.FormatClosedPosition(position));
        Assert.AreEqual(" profit=+0.68 USDC (+4.76%)", TradeTools.FormatRealizedResult(position, step, 0.42m));
    }


    [TestMethod]
    public void AtBreakEven_ReportsZeroWithAPlus()
    {
        // Exactly break-even is not a loss, so it reads as +0
        CryptoPosition position = MakePosition(CryptoTradeSide.Long);
        CryptoPositionStep step = MakeStep(CryptoOrderSide.Sell, 0.42m, 34m);

        Assert.AreEqual(" profit=+0 USDC (+0.00%)", TradeTools.FormatRealizedResult(position, step, 0.42m));
    }
}
