using CryptoScanner.Analyzers.Mac;
using CryptoScanner.Analyzers.Mac.Signal;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal;
using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// The exit the reference draws as Close Long and Close Short: the close crossing back through the
/// SECOND line against the position.
/// <para>
/// Measured against twelve catalogued markers on four coins, this rule hits every one of them -
/// eight of eight on the short side and four of four on the long side. The fast line hits four of
/// twelve and an RSI crossing five, so the second line is not one candidate among several.
/// </para>
/// </summary>
[TestClass]
public class MacSecondLineExitTests : TestBase
{
    private const int Enough = 6;

    private MacSettings _before = new();

    [ClassInitialize]
    public static void Register(TestContext _) => TestBase.RegisterPlugin(new MacPlugin());

    [TestInitialize]
    public void Remember()
    {
        InitTestSession();
        _before = MacPlugin.Settings;
    }

    [TestCleanup]
    public void Restore() => new MacPlugin().SettingsBase = _before;


    /// <summary>
    /// Two candles with the second line where the test wants it: the newest one is the candle in
    /// hand, the one before it the candle the crossing is measured against.
    /// </summary>
    private static MacBase Series(CryptoTradeSide side,
                                  decimal closePrev, double secondPrev,
                                  decimal close, double second)
    {
        CryptoSymbol symbol = MakeSymbol();
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m];
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        for (int i = 0; i < Enough; i++)
        {
            CryptoCandle candle = new()
            {
                TickDecimals = 2,
                OpenTime = new CandleTime((uint)((Enough - i) * interval.Duration)),
                Open = i == 0 ? close : closePrev,
                High = (i == 0 ? close : closePrev) + 1m,
                Low = (i == 0 ? close : closePrev) - 1m,
                Close = i == 0 ? close : closePrev,
            };
            CryptoData data = new();
            // The cloud points the way of the position and the slow line is still behind it, so
            // every test starts from a candle that only lacks the crossing itself. A test that is
            // about one of those two conditions moves it afterwards.
            double line = i == 0 ? second : secondPrev;
            data.SetPluginData(new MacCandleData
            {
                EmaFast = side == CryptoTradeSide.Long ? line + 1 : line - 1,
                EmaSecond = line,
                // The third line sits under the close for a long, so the candle before the
                // crossing is outside the cloud - which the exit asks for.
                SmaMedium = side == CryptoTradeSide.Long ? (double)candle.Close - 2
                                                         : (double)candle.Close + 2,
                SmaSlow = side == CryptoTradeSide.Long ? (double)candle.Close - 10
                                                       : (double)candle.Close + 10,
            });
            symbolInterval.CandleList.TryAdd(candle.OpenTime, candle);
            symbolInterval.Data[candle.OpenTime] = data;
            if (i == 0)
            {
                MyData newest = new() { Candle = candle, CandleData = data };
                _pending = side == CryptoTradeSide.Long
                    ? new MacLong
                    {
                        Symbol = symbol,
                        Interval = interval,
                        SymbolInterval = symbolInterval,
                        SignalSide = side,
                        SignalStrategy = MacPlugin.StrategyInternal.ToLower(),
                        CandleLast = newest,
                    }
                    : new MacShort
                    {
                        Symbol = symbol,
                        Interval = interval,
                        SymbolInterval = symbolInterval,
                        SignalSide = side,
                        SignalStrategy = MacPlugin.StrategyInternal.ToLower(),
                        CandleLast = newest,
                    };
            }
        }
        return _pending!;
    }

    private static MacBase? _pending;


    private static CryptoSymbol MakeSymbol()
    {
        Exchange exchange = new() { Id = 1, Name = "TestExchange", FeeRate = 0.1m };
        return new CryptoSymbol
        {
            Id = 1,
            Name = "TESTUSDT",
            Base = "TEST",
            Quote = "USDT",
            Exchange = exchange,
            ExchangeId = exchange.Id,
            ExchangeName = exchange.Name,
            QuoteData = GlobalData.AddQuoteData("USDT"),
            PriceTickSize = 0.01m,
        };
    }


    [TestMethod]
    public void Off_NeverFires()
    {
        new MacPlugin().SettingsBase = new MacSettings
        {
            ExitOnSecondLineCross = false,
            ExitOnCloudFlip = false,
        };
        // A close that crosses the line downwards, which the setting is supposed to act on.
        MacBase strategy = Series(CryptoTradeSide.Long, 105m, 100.0, 95m, 100.0);
        Assert.IsFalse(strategy.IsExitSignal(), "with both exits off nothing may fire");
    }


    [TestMethod]
    public void ALongLeavesWhenTheCloseCrossesUnderTheSecondLine()
    {
        new MacPlugin().SettingsBase = new MacSettings
        {
            ExitOnSecondLineCross = true,
            ExitOnCloudFlip = false,
        };
        MacBase strategy = Series(CryptoTradeSide.Long, 105m, 100.0, 95m, 100.0);
        Assert.IsTrue(strategy.IsExitSignal(), strategy.ExtraText);
        StringAssert.Contains(strategy.ExtraText, "under the second line");
    }


    [TestMethod]
    public void ALongStaysWhileTheCloseIsStillAboveTheSecondLine()
    {
        new MacPlugin().SettingsBase = new MacSettings
        {
            ExitOnSecondLineCross = true,
            ExitOnCloudFlip = false,
        };
        MacBase strategy = Series(CryptoTradeSide.Long, 105m, 100.0, 103m, 100.0);
        Assert.IsFalse(strategy.IsExitSignal(), "the close never reached the line");
    }


    [TestMethod]
    public void ALongStaysWhenItWasAlreadyUnderTheLine()
    {
        // The crossing itself is the signal, not the state that follows it: a position that is
        // already under the line must not be asked to leave again on every candle.
        new MacPlugin().SettingsBase = new MacSettings
        {
            ExitOnSecondLineCross = true,
            ExitOnCloudFlip = false,
        };
        MacBase strategy = Series(CryptoTradeSide.Long, 95m, 100.0, 93m, 100.0);
        Assert.IsFalse(strategy.IsExitSignal(), "this is the state after the crossing, not the crossing");
    }


    [TestMethod]
    public void AShortLeavesWhenTheCloseCrossesOverTheSecondLine()
    {
        new MacPlugin().SettingsBase = new MacSettings
        {
            ExitOnSecondLineCross = true,
            ExitOnCloudFlip = false,
        };
        MacBase strategy = Series(CryptoTradeSide.Short, 95m, 100.0, 105m, 100.0);
        Assert.IsTrue(strategy.IsExitSignal(), strategy.ExtraText);
        StringAssert.Contains(strategy.ExtraText, "over the second line");
    }


    /// <summary>
    /// The crossing only counts while the cloud still points the way of the position. Without that
    /// the very same candle is an exit for a long AND for a short, which is how this fired 314
    /// times against the 134 markers the reference draws.
    /// </summary>
    [TestMethod]
    public void ACrossingWithTheCloudAgainstUs_IsNoExit()
    {
        new MacPlugin().SettingsBase = new MacSettings
        {
            ExitOnSecondLineCross = true,
            ExitOnCloudFlip = false,
        };
        MacBase strategy = Series(CryptoTradeSide.Long, 105m, 100.0, 95m, 100.0);
        // The cloud of a short: the fast line under the second one.
        strategy.CandleLast.CandleData!.GetPluginData<MacCandleData>()!.EmaFast = 99.0;
        Assert.IsFalse(strategy.IsExitSignal(), "the cloud points the other way");
    }


    /// <summary>
    /// The price itself may be past the slow line. What stood here until 25 September 2026 said it
    /// may not, and that cost 51 of the reference's 1083 close markers - every one of them a
    /// crossing where the stack was still whole while the close had slipped past the slow line.
    /// </summary>
    [TestMethod]
    public void ACrossingWithThePricePastTheSlowLine_IsStillAnExit()
    {
        new MacPlugin().SettingsBase = new MacSettings
        {
            ExitOnSecondLineCross = true,
            ExitOnCloudFlip = false,
        };
        MacBase strategy = Series(CryptoTradeSide.Long, 105m, 100.0, 95m, 100.0);
        // The close of 95 sits under the slow line, but the lines themselves are still stacked the
        // way a long wants them: the fast at 101 over the second at 100, over the third at 98, over
        // the slow one at 96 - and only then the price at 95.
        MacCandleData mac = strategy.CandleLast.CandleData!.GetPluginData<MacCandleData>()!;
        mac.SmaMedium = 98.0;
        mac.SmaSlow = 96.0;
        Assert.IsTrue(strategy.IsExitSignal(), strategy.ExtraText);
    }


    /// <summary>
    /// The cloud has to be stacked the way of the position ALL THE WAY: the second line on our side
    /// of the third and the third on our side of the slow one.
    /// <para>
    /// This is the exit rule and there is nothing else to it. Over 4282 crossings of the second
    /// line on eleven coins and five timeframes the reference draws 1083 close markers, and the
    /// full stack picks out 1083 of those 1083 - no miss on any set, on either side - while firing
    /// three times more than it should.
    /// </para>
    /// </summary>
    [TestMethod]
    public void ACrossingWithTheCloudUnstacked_IsNoExit()
    {
        new MacPlugin().SettingsBase = new MacSettings
        {
            ExitOnSecondLineCross = true,
            ExitOnCloudFlip = false,
        };
        MacBase strategy = Series(CryptoTradeSide.Long, 105m, 100.0, 95m, 100.0);
        // The third line over the second one: the cloud is not stacked for a long.
        strategy.CandleLast.CandleData!.GetPluginData<MacCandleData>()!.SmaMedium = 101.0;
        Assert.IsFalse(strategy.IsExitSignal(), "the second line is under the third");
    }


    /// <summary>The other half of the stack: the third line has to be on our side of the slow one.</summary>
    [TestMethod]
    public void ACrossingWithTheThirdLinePastTheSlowOne_IsNoExit()
    {
        new MacPlugin().SettingsBase = new MacSettings
        {
            ExitOnSecondLineCross = true,
            ExitOnCloudFlip = false,
        };
        MacBase strategy = Series(CryptoTradeSide.Long, 105m, 100.0, 95m, 100.0);
        // The slow line over the third: the stack is broken at its far end, whatever the price does.
        strategy.CandleLast.CandleData!.GetPluginData<MacCandleData>()!.SmaSlow = 94.0;
        Assert.IsFalse(strategy.IsExitSignal(), "the third line sits under the slow one");
    }


    /// <summary>
    /// Every way out has to be announced by HasExitSignal, or the position monitor never asks.
    /// <para>
    /// This is not a detail: with the second line crossing switched on and this saying no, the
    /// emulator produced numbers identical to a run without any exit at all, and the setting read
    /// as measured and worthless while it had never been asked a single time.
    /// </para>
    /// </summary>
    [TestMethod]
    public void EveryExitIsAnnounced()
    {
        new MacPlugin().SettingsBase = new MacSettings
        {
            ExitOnSecondLineCross = true,
            ExitOnCloudFlip = false,
        };
        Assert.IsTrue(Series(CryptoTradeSide.Long, 100m, 100.0, 100m, 100.0).HasExitSignal,
            "with only the second line crossing on, the monitor still has to ask");

        new MacPlugin().SettingsBase = new MacSettings
        {
            ExitOnSecondLineCross = false,
            ExitOnCloudFlip = true,
        };
        Assert.IsTrue(Series(CryptoTradeSide.Long, 100m, 100.0, 100m, 100.0).HasExitSignal, "the cloud flip has to be asked as well");

        new MacPlugin().SettingsBase = new MacSettings
        {
            ExitOnSecondLineCross = false,
            ExitOnCloudFlip = false,
        };
        Assert.IsFalse(Series(CryptoTradeSide.Long, 100m, 100.0, 100m, 100.0).HasExitSignal,
            "with every way out off there is nothing to ask about");
    }
}
