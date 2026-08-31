using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.CoreTests;

namespace CryptoScanner.Core.Trader.Tests;

/// <summary>
/// TradeTools.CheckOrderSetAgainstSymbolLimits: whether every order a new position is going to
/// produce - the entry, the DCA levels behind it and the exit orders - fits inside the symbol's own
/// quantity and value limits. One that does not means no position at all.
/// <para>
/// The case this was written for is ZECUSDC.PERP on HyperLiquid, 31-08-2026. An entry amount of 15
/// USDC at a price of 816.19 works out to 0.018 ZEC, the size grid rounds that down to 0.01, and
/// 0.01 x 816.19 = 8.16 against a minimum order value of 10. Nothing refused it, because the check
/// that stood there weighed the amount that was MEANT to be staked rather than the order that was
/// really placed. Hours later the closing check in CalculatePositionResultsViaOrders read the same
/// 8.16 as an unsellable remainder and wrote the position off as a total loss - while all 0.01 ZEC
/// was still held and the exit order was still on the book. That reading is gone from the closing
/// check now; this is where the question belongs.
/// </para>
/// </summary>
[TestClass]
public class EntryOrderSetLimitTests : TestBase
{
    private CryptoTradeVia _savedTradeVia;
    private List<CryptoDcaEntry> _savedDcaList = [];
    private List<CryptoTpEntry> _savedTpList = [];
    private decimal _savedStopLossPercentage;
    private decimal _savedStopLossLimitPercentage;

    [TestInitialize]
    public void SaveSettings()
    {
        InitTestSession();

        _savedTradeVia = GlobalData.Settings.Trading.TradeVia;
        _savedDcaList = GlobalData.Settings.Trading.DcaList;
        _savedTpList = GlobalData.Settings.Trading.TpList;
        _savedStopLossPercentage = GlobalData.Settings.Trading.StopLossPercentage;
        _savedStopLossLimitPercentage = GlobalData.Settings.Trading.StopLossLimitPercentage;

        // The HyperLiquid Perpetual settings of 31-08-2026, which is what the ZEC case ran on
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;
        GlobalData.Settings.Trading.DcaList =
        [
            new CryptoDcaEntry { Percentage = 2m, Factor = 200m },
            new CryptoDcaEntry { Percentage = 4m, Factor = 400m },
        ];
        GlobalData.Settings.Trading.TpList = [new CryptoTpEntry { Percentage = 7.5m, Factor = 100m }];
        GlobalData.Settings.Trading.StopLossPercentage = 4m;
        GlobalData.Settings.Trading.StopLossLimitPercentage = 5m;
    }

    [TestCleanup]
    public void RestoreSettings()
    {
        GlobalData.Settings.Trading.TradeVia = _savedTradeVia;
        GlobalData.Settings.Trading.DcaList = _savedDcaList;
        GlobalData.Settings.Trading.TpList = _savedTpList;
        GlobalData.Settings.Trading.StopLossPercentage = _savedStopLossPercentage;
        GlobalData.Settings.Trading.StopLossLimitPercentage = _savedStopLossLimitPercentage;
    }


    /// <summary>
    /// ZECUSDC.PERP as it stood on 31-08-2026. No database and no exchange involved: the method
    /// reads nothing but the symbol's limits and the trading settings.
    /// </summary>
    private static CryptoSymbol ZecSymbol()
    {
        // Exchange/ExchangeName/QuoteData are required members of CryptoSymbol; nothing in this
        // method reads them, they just have to be there.
        var exchange = GlobalData.ExchangeListName[GlobalData.Settings.General.ExchangeName];
        return new()
        {
            Name = "ZECUSDC.PERP",
            Base = "ZEC",
            Quote = "USDC",
            Exchange = exchange,
            ExchangeId = exchange.Id,
            ExchangeName = "ZEC",
            QuoteData = GlobalData.AddQuoteData("USDC"),

            QuantityMinimum = 0.01m,
            QuantityMaximum = 0m, // HyperLiquid publishes none
            QuantityTickSize = 0.01m,

            PriceMinimum = 0m,
            PriceMaximum = 0m,
            PriceTickSize = 0.01m,

            QuoteValueMinimum = 10m,
            QuoteValueMaximum = 20_000_000m,
        };
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The entry
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The position that started all of this. One size tick of ZEC is worth 8.16 and the exchange
    /// asks for ten, so no position is opened - the quantity itself is exactly ON the minimum, which
    /// is why a check against QuantityMinimum let it through.
    /// </summary>
    [TestMethod]
    public void TheZecEntryOfOneTickIsRefusedOnItsValue()
    {
        bool fits = TradeTools.CheckOrderSetAgainstSymbolLimits(ZecSymbol(), CryptoTradeSide.Long,
            entryPrice: 816.19m, entryQuantity: 0.01m, signalSlPercentage: null, out string reason);

        Assert.IsFalse(fits, "0.01 ZEC at 816.19 is 8.16, under the minimum order value of 10");
        StringAssert.Contains(reason, "entry value");
        StringAssert.Contains(reason, "8.1619");
    }


    /// <summary>
    /// Two ticks is 16.32 and clears everything behind it as well: both DCA levels are larger still
    /// (200% and 400% of the entry), the take profit sits above the entry for a long, and the stop
    /// limit at 14.89 stays over the minimum.
    /// </summary>
    [TestMethod]
    public void TwoTicksOfZecFitsWithEverythingBehindIt()
    {
        bool fits = TradeTools.CheckOrderSetAgainstSymbolLimits(ZecSymbol(), CryptoTradeSide.Long,
            entryPrice: 816.19m, entryQuantity: 0.02m, signalSlPercentage: null, out string reason);

        Assert.IsTrue(fits, reason);
        Assert.AreEqual("", reason);
    }


    /// <summary>A quantity under the size grid's first step is refused on the quantity itself.</summary>
    [TestMethod]
    public void AQuantityUnderTheMinimumIsRefused()
    {
        bool fits = TradeTools.CheckOrderSetAgainstSymbolLimits(ZecSymbol(), CryptoTradeSide.Long,
            entryPrice: 816.19m, entryQuantity: 0.005m, signalSlPercentage: null, out string reason);

        Assert.IsFalse(fits);
        StringAssert.Contains(reason, "entry quantity");
    }


    /// <summary>And one over the maximum ORDER value the exchange accepts.</summary>
    [TestMethod]
    public void AnEntryOverTheMaximumValueIsRefused()
    {
        var symbol = ZecSymbol();
        symbol.QuoteValueMaximum = 1000m;

        bool fits = TradeTools.CheckOrderSetAgainstSymbolLimits(symbol, CryptoTradeSide.Long,
            entryPrice: 816.19m, entryQuantity: 2m, signalSlPercentage: null, out string reason);

        Assert.IsFalse(fits, "2 ZEC is 1632, over the 1000 maximum");
        StringAssert.Contains(reason, "entry value");
        StringAssert.Contains(reason, "maximum");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The DCA levels behind it
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A DCA level is a percentage OF the entry value, so a factor under 100 makes it smaller than
    /// the entry - and that is how a level lands under the minimum order value while the entry
    /// itself is fine. The entry of 16.32 passes, its 50% level of 8.16 does not, so the position is
    /// not opened: entering means committing to a DCA that cannot be placed.
    /// </summary>
    [TestMethod]
    public void ADcaLevelUnderTheMinimumValueRefusesTheWholeEntry()
    {
        GlobalData.Settings.Trading.DcaList = [new CryptoDcaEntry { Percentage = 2m, Factor = 50m }];

        bool fits = TradeTools.CheckOrderSetAgainstSymbolLimits(ZecSymbol(), CryptoTradeSide.Long,
            entryPrice: 816.19m, entryQuantity: 0.02m, signalSlPercentage: null, out string reason);

        Assert.IsFalse(fits);
        StringAssert.Contains(reason, "dca 1");
    }


    /// <summary>
    /// A level at or beyond the signal SL never fills, so it is not weighed either - the same rule
    /// PositionMonitor.GetMissingFixedPercentageDcaPrices applies when it places them. With an SL at
    /// 1% both configured levels (2% and 4%) drop out, and the 50% level that fails the test above
    /// no longer has any say.
    /// </summary>
    [TestMethod]
    public void ADcaLevelBeyondTheSignalStopLossIsNotWeighed()
    {
        GlobalData.Settings.Trading.DcaList = [new CryptoDcaEntry { Percentage = 2m, Factor = 50m }];

        bool fits = TradeTools.CheckOrderSetAgainstSymbolLimits(ZecSymbol(), CryptoTradeSide.Long,
            entryPrice: 816.19m, entryQuantity: 0.02m, signalSlPercentage: 1m, out string reason);

        Assert.IsTrue(fits, reason);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The exit
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The way out has to fit too. For a long the stop limit sits below everything else - anchored
    /// beyond the last DCA level and another limit percentage under that - so an entry can clear the
    /// minimum order value while the order that has to close it does not.
    /// <para>
    /// At a price of 100 the entry is exactly 10, dead on the minimum. The stop is anchored on the
    /// 4% DCA level at 96 and the limit sits 5% under that anchor at 91.20, which makes the exit
    /// order worth 9.12. Enter that and there is a position that cannot be closed at its stop.
    /// </para>
    /// </summary>
    [TestMethod]
    public void AnExitThatFallsUnderTheMinimumRefusesTheEntry()
    {
        var symbol = ZecSymbol();

        bool fits = TradeTools.CheckOrderSetAgainstSymbolLimits(symbol, CryptoTradeSide.Long,
            entryPrice: 100m, entryQuantity: 0.10m, signalSlPercentage: null, out string reason);

        Assert.IsFalse(fits, "the entry is 10.00 but the stop limit order is only worth 9.12");
        StringAssert.Contains(reason, "stop loss");
    }


    /// <summary>
    /// Without a stop loss there is no stop order to weigh, and the same entry passes. This is also
    /// what keeps the check honest for real trading, where no SL order is placed at all.
    /// </summary>
    [TestMethod]
    public void WithoutAStopLossTheSameEntryPasses()
    {
        GlobalData.Settings.Trading.StopLossPercentage = 0m;
        GlobalData.Settings.Trading.StopLossLimitPercentage = 0m;

        bool fits = TradeTools.CheckOrderSetAgainstSymbolLimits(ZecSymbol(), CryptoTradeSide.Long,
            entryPrice: 100m, entryQuantity: 0.10m, signalSlPercentage: null, out string reason);

        Assert.IsTrue(fits, reason);
    }


    /// <summary>
    /// Real trading places no stop loss order (that would need OCO), so the stop prices are not
    /// weighed there either - the same condition PositionMonitor.CalculateSlPrices applies. The
    /// entry that is refused for paper trading above goes through here.
    /// </summary>
    [TestMethod]
    public void RealTradingDoesNotWeighAStopLossItNeverPlaces()
    {
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.RealTrading;

        bool fits = TradeTools.CheckOrderSetAgainstSymbolLimits(ZecSymbol(), CryptoTradeSide.Long,
            entryPrice: 100m, entryQuantity: 0.10m, signalSlPercentage: null, out string reason);

        Assert.IsTrue(fits, reason);
    }


    /// <summary>
    /// For a short the profit target sits BELOW the entry, so it is the take profit rather than the
    /// stop that can fall under the minimum. Same entry of exactly 10 at a price of 100, a profit
    /// distance of 7.5%, and the exit order comes out at 9.25.
    /// </summary>
    [TestMethod]
    public void AShortIsWeighedOnItsProfitTargetInstead()
    {
        GlobalData.Settings.Trading.StopLossPercentage = 0m;
        GlobalData.Settings.Trading.StopLossLimitPercentage = 0m;

        bool fits = TradeTools.CheckOrderSetAgainstSymbolLimits(ZecSymbol(), CryptoTradeSide.Short,
            entryPrice: 100m, entryQuantity: 0.10m, signalSlPercentage: null, out string reason);

        Assert.IsFalse(fits, "the short's take profit at 92.50 makes the exit order worth 9.25");
        StringAssert.Contains(reason, "take profit");
    }


    /// <summary>
    /// Splitting the exit over several take profit levels makes each order smaller than the position,
    /// and a level's share can land under the minimum while the position as a whole is comfortably
    /// over it. Three equal levels on an entry of 16.32 leaves about 5.44 per order.
    /// </summary>
    [TestMethod]
    public void ATakeProfitLevelsShareCanFallUnderTheMinimum()
    {
        GlobalData.Settings.Trading.TpList =
        [
            new CryptoTpEntry { Percentage = 1.5m, Factor = 33m },
            new CryptoTpEntry { Percentage = 3.0m, Factor = 33m },
            new CryptoTpEntry { Percentage = 7.5m, Factor = 34m },
        ];

        bool fits = TradeTools.CheckOrderSetAgainstSymbolLimits(ZecSymbol(), CryptoTradeSide.Long,
            entryPrice: 816.19m, entryQuantity: 0.02m, signalSlPercentage: null, out string reason);

        Assert.IsFalse(fits, "0.02 ZEC split three ways is under one size tick per level");
        StringAssert.Contains(reason, "take profit");
    }
}
