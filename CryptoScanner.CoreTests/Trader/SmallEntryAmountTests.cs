using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.CoreTests;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Trader.Tests;

/// <summary>
/// The unhappy flow of a trade plan that is too large for the money put into it: an entry amount
/// that clears the exchange minimum by itself, while one of the orders the plan splits it into does
/// not. Such a position is never opened - not in the scanner, not in the emulator and not in Altrady.
/// <para>
/// The case that prompted this, 11-10-2026: the live mac setup on Binance Perpetual traded 15 USDT
/// with four take profits of a quarter each. Every take profit order was worth about 3.75-3.94 USDT
/// against the 5 USDT minimum of Binance Futures, and every signal was refused with "take profit 1
/// (5%) value 3.9 USDT &lt; minimum 5". On HyperLiquid Perpetual the same plan meets a minimum of
/// 10 USDC, and there the paper balance held no USDC at all.
/// </para>
/// <para>
/// The minimum is weighed on the VALUE of each order (quantity x its own price), so the price
/// distance of a take profit matters: for a long it sits above the entry and helps, for a short it
/// sits below the entry and hurts. That is why the same entry amount can pass long and fail short.
/// </para>
/// </summary>
[TestClass]
public class SmallEntryAmountTests : TestBase
{
    private CryptoTradeVia _savedTradeVia;
    private List<CryptoDcaEntry> _savedDcaList = [];
    private List<CryptoTpEntry> _savedTpList = [];
    private decimal _savedStopLossPercentage;
    private decimal _savedStopLossLimitPercentage;
    private bool _savedUseAssetManagement;

    [TestInitialize]
    public void SaveSettings()
    {
        InitTestSession();

        _savedTradeVia = GlobalData.Settings.Trading.TradeVia;
        _savedDcaList = GlobalData.Settings.Trading.DcaList;
        _savedTpList = GlobalData.Settings.Trading.TpList;
        _savedStopLossPercentage = GlobalData.Settings.Trading.StopLossPercentage;
        _savedStopLossLimitPercentage = GlobalData.Settings.Trading.StopLossLimitPercentage;
        _savedUseAssetManagement = GlobalData.Settings.Trading.UseAssetManagement;

        // The live mac plan of 11-10-2026: no DCA, four take profits of a quarter each. The stop is
        // left out here (an ATR stop in the live setup), so only the take profits are weighed.
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;
        GlobalData.Settings.Trading.DcaList = [];
        GlobalData.Settings.Trading.TpList =
        [
            new CryptoTpEntry { Percentage = 5m, Factor = 25m },
            new CryptoTpEntry { Percentage = 10m, Factor = 25m },
            new CryptoTpEntry { Percentage = 20m, Factor = 25m },
            new CryptoTpEntry { Percentage = 40m, Factor = 25m },
        ];
        GlobalData.Settings.Trading.StopLossPercentage = 0m;
        GlobalData.Settings.Trading.StopLossLimitPercentage = 0m;
        GlobalData.Settings.Trading.UseAssetManagement = true;
    }

    [TestCleanup]
    public void RestoreSettings()
    {
        GlobalData.Settings.Trading.TradeVia = _savedTradeVia;
        GlobalData.Settings.Trading.DcaList = _savedDcaList;
        GlobalData.Settings.Trading.TpList = _savedTpList;
        GlobalData.Settings.Trading.StopLossPercentage = _savedStopLossPercentage;
        GlobalData.Settings.Trading.StopLossLimitPercentage = _savedStopLossLimitPercentage;
        GlobalData.Settings.Trading.UseAssetManagement = _savedUseAssetManagement;
    }


    /// <summary>
    /// A symbol with a fine size grid, so the only limit that can bite is the minimum order value.
    /// Price 1 keeps the arithmetic readable: the quantity IS the value of the entry.
    /// </summary>
    private static CryptoSymbol Symbol(string quote, decimal quoteValueMinimum)
    {
        var exchange = GlobalData.ExchangeListName[GlobalData.Settings.General.ExchangeName];
        return new()
        {
            Name = "TESTCOIN" + quote + ".PERP",
            Base = "TESTCOIN",
            Quote = quote,
            Exchange = exchange,
            ExchangeId = exchange.Id,
            ExchangeName = "TESTCOIN",
            QuoteData = GlobalData.AddQuoteData(quote),

            QuantityMinimum = 0.001m,
            QuantityMaximum = 0m,
            QuantityTickSize = 0.001m,

            PriceMinimum = 0m,
            PriceMaximum = 0m,
            PriceTickSize = 0.0001m,

            QuoteValueMinimum = quoteValueMinimum,
            QuoteValueMaximum = 0m,
        };
    }


    private static bool Fits(CryptoSymbol symbol, CryptoTradeSide side, decimal entryAmount, out string reason)
        => TradeTools.CheckOrderSetAgainstSymbolLimits(symbol, side, entryPrice: 1m, entryQuantity: entryAmount,
            signalSlPercentage: null, out reason);


    // ═══════════════════════════════════════════════════════════════════════
    //  The take profit split (Binance Perpetual, minimum 5 USDT)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The live case. 15 USDT clears the minimum of 5 by itself, but a quarter of it is 3.75 and the
    /// first take profit at +5% is worth 3.94: the position is not opened.
    /// </summary>
    [TestMethod]
    public void FifteenUsdtSplitIntoFourTakeProfitsIsRefusedOnBinance()
    {
        bool fits = Fits(Symbol("USDT", 5m), CryptoTradeSide.Long, 15m, out string reason);

        Assert.IsFalse(fits, "a quarter of 15 is 3.75, at +5% that is 3.94 against a minimum of 5");
        StringAssert.Contains(reason, "take profit 1");
    }

    /// <summary>
    /// A long is weighed on its take profit price, which sits ABOVE the entry: 20 USDT gives
    /// quarters of 5.00 that are worth 5.25 at +5%, and the plan fits.
    /// </summary>
    [TestMethod]
    public void TwentyUsdtFitsForALong()
    {
        bool fits = Fits(Symbol("USDT", 5m), CryptoTradeSide.Long, 20m, out string reason);

        Assert.IsTrue(fits, reason);
    }

    /// <summary>
    /// The same 20 USDT as a short: its first take profit sits 5% BELOW the entry, so the quarter of
    /// 5.00 is worth only 4.75 there. A short needs a little more money than a long for the same plan.
    /// </summary>
    [TestMethod]
    public void TwentyUsdtIsRefusedForAShort()
    {
        bool fits = Fits(Symbol("USDT", 5m), CryptoTradeSide.Short, 20m, out string reason);

        Assert.IsFalse(fits, "a quarter of 20 at -5% is 4.75 against a minimum of 5");
        StringAssert.Contains(reason, "take profit 1");
    }

    /// <summary>
    /// The deepest take profit of a short is the one that limits it: with 30 USDT the first three
    /// quarters of 7.50 still clear the minimum, but the fourth at -40% is worth only 4.50. With four
    /// levels down to -40% a short needs a quarter of at least 5 / 0.60 = 8.34, an entry of about
    /// 33.34 USDT.
    /// </summary>
    [TestMethod]
    public void AShortIsLimitedByItsDeepestTakeProfit()
    {
        //bool fits30 = Fits(Symbol("USDT", 5m), CryptoTradeSide.Short, 30m, out string reason30);
        //bool fits34 = Fits(Symbol("USDT", 5m), CryptoTradeSide.Short, 34m, out string reason34);
        //
        //Assert.IsFalse(fits30, "a quarter of 30 at -40% is 4.50");
        //StringAssert.Contains(reason30, "take profit 4");
        //Assert.IsTrue(fits34, reason34);

        // Since 24-10-2026 a short's take profit is divided the way Altrady places it: the fourth level
        // at 40% lies at 1 / 1.40 of the entry, so a quarter needs 5 * 1.40 = 7.00, an entry of 28 USDT
        bool fits27 = Fits(Symbol("USDT", 5m), CryptoTradeSide.Short, 27m, out string reason27);
        bool fits29 = Fits(Symbol("USDT", 5m), CryptoTradeSide.Short, 29m, out string reason29);

        Assert.IsFalse(fits27, "a quarter of 27 at 1 / 1.40 is 4.82");
        StringAssert.Contains(reason27, "take profit 4");
        Assert.IsTrue(fits29, reason29);
    }

    /// <summary>
    /// One take profit for the whole position is the other way out: 15 USDT in a single exit order
    /// clears the minimum of 5 for a long and a short alike.
    /// </summary>
    [TestMethod]
    public void OneTakeProfitForTheWholePositionFits()
    {
        GlobalData.Settings.Trading.TpList = [new CryptoTpEntry { Percentage = 5m, Factor = 100m }];

        Assert.IsTrue(Fits(Symbol("USDT", 5m), CryptoTradeSide.Long, 15m, out string reasonLong), reasonLong);
        Assert.IsTrue(Fits(Symbol("USDT", 5m), CryptoTradeSide.Short, 15m, out string reasonShort), reasonShort);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The same plan on HyperLiquid Perpetual (minimum 10 USDC)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// HyperLiquid asks twice as much per order. The amount that just fits a long on Binance (20) is
    /// refused here; 40 USDC gives quarters of 10 that are worth 10.50 at +5%.
    /// </summary>
    [TestMethod]
    public void HyperLiquidNeedsTwiceTheMoneyForTheSamePlan()
    {
        bool fits20 = Fits(Symbol("USDC", 10m), CryptoTradeSide.Long, 20m, out _);
        bool fits40 = Fits(Symbol("USDC", 10m), CryptoTradeSide.Long, 40m, out string reason40);

        Assert.IsFalse(fits20, "a quarter of 20 at +5% is 5.25 against a minimum of 10");
        Assert.IsTrue(fits40, reason40);
    }

    /// <summary>
    /// The planned emulator entry of 100 clears every order of the four-way split on both exchanges,
    /// long and short - including a short's deepest take profit at -40% (a quarter of 100 worth 15).
    /// </summary>
    [TestMethod]
    public void AHundredFitsTheFourWaySplitEverywhere()
    {
        foreach (var (quote, minimum) in new[] { ("USDT", 5m), ("USDC", 10m) })
            foreach (var side in new[] { CryptoTradeSide.Long, CryptoTradeSide.Short })
                Assert.IsTrue(Fits(Symbol(quote, minimum), side, 100m, out string reason), $"{quote} {side}: {reason}");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  DCA's on a small entry
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A DCA is a percentage of the entry, so a factor under 100 makes it SMALLER than the entry. An
    /// entry of 8 with a DCA of 50% places an order of 4 against a minimum of 5: refused as a whole,
    /// because entering means committing to a DCA that cannot be placed.
    /// </summary>
    [TestMethod]
    public void ADcaSmallerThanTheEntryCanFallUnderTheMinimum()
    {
        GlobalData.Settings.Trading.TpList = [new CryptoTpEntry { Percentage = 5m, Factor = 100m }];
        GlobalData.Settings.Trading.DcaList = [new CryptoDcaEntry { Percentage = 2m, Factor = 50m }];

        bool fits = Fits(Symbol("USDT", 5m), CryptoTradeSide.Long, 8m, out string reason);

        Assert.IsFalse(fits, "a DCA of 50% of 8 is an order of 4");
        StringAssert.Contains(reason, "dca 1");
    }

    /// <summary>
    /// The take profits are weighed on the entry alone: a DCA that fills only ever makes the exit
    /// bigger, so a DCA does not rescue a take profit split that is too small at the entry.
    /// </summary>
    [TestMethod]
    public void ADcaDoesNotRescueATakeProfitSplitThatIsTooSmall()
    {
        GlobalData.Settings.Trading.DcaList =
        [
            new CryptoDcaEntry { Percentage = 2m, Factor = 200m },
            new CryptoDcaEntry { Percentage = 4m, Factor = 400m },
        ];

        bool fits = Fits(Symbol("USDT", 5m), CryptoTradeSide.Long, 15m, out string reason);

        Assert.IsFalse(fits, reason);
        StringAssert.Contains(reason, "take profit 1");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Not enough money in the paper balance
    // ═══════════════════════════════════════════════════════════════════════

    private static CryptoSymbol ArrangeBalance(string quote, decimal total)
    {
        CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        DeleteAllPositionRelatedStuff(database);

        GlobalData.ActiveExchange!.Data.AssetList.TryRemove(quote, out _);
        if (total > 0)
        {
            CryptoAsset asset = new() { Name = quote, Total = total, Free = total, Locked = 0 };
            GlobalData.ActiveExchange!.Data.AssetList.TryAdd(asset.Name, asset);
            database.Connection.Insert(asset);
        }
        return symbol;
    }

    /// <summary>
    /// The HyperLiquid case: a paper balance without the coin the exchange trades in refuses every
    /// signal, and the reaction says where to book it instead of only "No assets available".
    /// </summary>
    [TestMethod]
    public void APaperBalanceWithoutTheQuoteCoinSaysWhereToBookIt()
    {
        CryptoSymbol symbol = ArrangeBalance("USDT", 0m);
        symbol.QuoteData!.EntryAmount = 100m;
        symbol.QuoteData.EntryPercentage = 0;

        var result = AssetTools.CheckAvailableAssets(GlobalData.ActiveExchange!, symbol, reserveForDca: true);

        Assert.IsFalse(result.success);
        StringAssert.Contains(result.reaction, "No assets available for USDT");
        StringAssert.Contains(result.reaction, "Paper assets");
    }

    /// <summary>
    /// A balance that holds the entry but not the DCA's behind it: 100 plus DCA's of 200% and 400%
    /// needs 700, and 500 is not enough. The position is not opened, because the DCA orders that
    /// should defend it would be refused for lack of money.
    /// </summary>
    [TestMethod]
    public void ABalanceThatHoldsTheEntryButNotItsDcasIsRefused()
    {
        GlobalData.Settings.Trading.DcaList =
        [
            new CryptoDcaEntry { Percentage = 2m, Factor = 200m },
            new CryptoDcaEntry { Percentage = 4m, Factor = 400m },
        ];
        CryptoSymbol symbol = ArrangeBalance("USDT", 500m);
        symbol.QuoteData!.EntryAmount = 100m;
        symbol.QuoteData.EntryPercentage = 0;

        var result = AssetTools.CheckAvailableAssets(GlobalData.ActiveExchange!, symbol, reserveForDca: true);

        Assert.IsFalse(result.success, "100 + 200 + 400 = 700 does not fit in 500");
        StringAssert.Contains(result.reaction, "dca's");
    }

    /// <summary>And 700 is exactly enough for the entry and both DCA's.</summary>
    [TestMethod]
    public void ABalanceOfEntryPlusDcasExactlyFits()
    {
        GlobalData.Settings.Trading.DcaList =
        [
            new CryptoDcaEntry { Percentage = 2m, Factor = 200m },
            new CryptoDcaEntry { Percentage = 4m, Factor = 400m },
        ];
        CryptoSymbol symbol = ArrangeBalance("USDT", 700m);
        symbol.QuoteData!.EntryAmount = 100m;
        symbol.QuoteData.EntryPercentage = 0;

        var result = AssetTools.CheckAvailableAssets(GlobalData.ActiveExchange!, symbol, reserveForDca: true);

        Assert.IsTrue(result.success, result.reaction);
        Assert.AreEqual(100m, result.entryQuoteAsset);
    }
}
