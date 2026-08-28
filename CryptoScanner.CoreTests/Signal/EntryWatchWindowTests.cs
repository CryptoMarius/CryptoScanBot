using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// EntryConditions.EntryWaitMinutes / EntryMaxAdversePercentage: watch a signal for a while before
/// acting on it, and drop it when price ran too far the wrong way in the meantime.
/// <para>
/// The two halves live in different methods on purpose, and that split is what these tests guard.
/// AllowStepIn returning false means "not yet, keep waiting" - the signal stays in SignalList and
/// every symbol's list is walked on every candle, so a REJECTED signal must not linger there. The
/// rejection therefore sits in GiveUp, which removes it.
/// </para>
/// </summary>
[TestClass]
public class EntryWatchWindowTests : TestBase
{
    private static readonly DateTime SignalOpen = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private SettingsEntryConditions _saved = new();

    [TestInitialize]
    public void SaveSettings()
    {
        InitTestSession();
        _saved = GlobalData.Settings.Trading.EntryConditions;
        GlobalData.Settings.Trading.EntryConditions = new SettingsEntryConditions();
    }

    [TestCleanup]
    public void RestoreSettings() => GlobalData.Settings.Trading.EntryConditions = _saved;

    private static CryptoSymbol MakeSymbol()
    {
        var exchange = new Exchange { Id = 1, Name = "TestExchange", FeeRate = 0.1m };
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

    private static CryptoInterval MakeInterval() => GlobalData.IntervalListPeriod.Count > 0
        ? GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m]
        : new CryptoInterval { Id = 4, Name = "5m", Duration = 300 };

    /// <summary>A signal at price 100, on the side under test.</summary>
    private static CryptoSignal MakeSignal(CryptoTradeSide side)
    {
        var symbol = MakeSymbol();
        return new CryptoSignal
        {
            Exchange = symbol.Exchange,
            ExchangeId = symbol.ExchangeId,
            Symbol = symbol,
            SymbolId = symbol.Id,
            Interval = MakeInterval(),
            IntervalId = 4,
            Candle = null,
            Side = side,
            Strategy = "stobb",
            OpenDate = SignalOpen,
            SignalPrice = 100m,
        };
    }

    /// <summary>The algorithm under test, positioned on a candle with the given range and time.</summary>
    private static SignalCreateBase MakeAlgorithm(CryptoSignal signal, DateTime candleTime,
        decimal low, decimal high)
    {
        // SignalCreateBase itself is concrete and carries everything the rule uses; no strategy of
        // its own is needed to test a check that lives on the base class.
        return new SignalCreateBase
        {
            Symbol = signal.Symbol,
            Interval = signal.Interval,
            SymbolInterval = signal.Symbol.GetSymbolInterval(signal.Interval.IntervalPeriod),
            SignalSide = signal.Side,
            SignalStrategy = "stobb",
            CandleLast = new MyData
            {
                Candle = new CryptoCandle
                {
                    // Without decimals the candle stores prices as whole ticks and 98.5 is
                    // rounded to 99 - the rule would then measure a different move than the
                    // test intends.
                    TickDecimals = 2,
                    OpenTime = CandleTime.FromDateTime(candleTime),
                    Open = 100m,
                    High = high,
                    Low = low,
                    Close = 100m,
                },
                // The rule only looks at the candle itself, not at indicators.
                CandleData = new CryptoData(),
            },
        };
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Waiting
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void WithTheRuleOff_TheSignalIsActedOnStraightAway()
    {
        GlobalData.Settings.Trading.EntryConditions.EntryWaitMinutes = 0;
        var signal = MakeSignal(CryptoTradeSide.Long);
        var algorithm = MakeAlgorithm(signal, SignalOpen, 100m, 100m);

        Assert.IsTrue(algorithm.AllowStepIn(signal), "without a wait there is no delay");
    }


    [TestMethod]
    [DataRow(CryptoTradeSide.Long, DisplayName = "long")]
    [DataRow(CryptoTradeSide.Short, DisplayName = "short")]
    public void InsideTheWatchWindow_TheSignalWaits(CryptoTradeSide side)
    {
        GlobalData.Settings.Trading.EntryConditions.EntryWaitMinutes = 15;
        var signal = MakeSignal(side);

        var tooEarly = MakeAlgorithm(signal, SignalOpen.AddMinutes(14), 100m, 100m);
        Assert.IsFalse(tooEarly.AllowStepIn(signal), "no entry while inside the window");

        var onTime = MakeAlgorithm(signal, SignalOpen.AddMinutes(15), 100m, 100m);
        Assert.IsTrue(onTime.AllowStepIn(signal), "at the end of the window entry is allowed");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Rejecting
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A long whose price dipped 3% during the watch: over a 2% limit, so the signal is abandoned.
    /// The dip is seen on a candle INSIDE the window and has to still count once it closes.
    /// </summary>
    [TestMethod]
    public void Long_ThatRanTooFarDown_IsGivenUpAfterTheWindow()
    {
        GlobalData.Settings.Trading.EntryConditions.EntryWaitMinutes = 15;
        GlobalData.Settings.Trading.EntryConditions.EntryMaxAdversePercentage = 2m;
        var signal = MakeSignal(CryptoTradeSide.Long);

        var during = MakeAlgorithm(signal, SignalOpen.AddMinutes(5), low: 97m, high: 100m);
        Assert.IsFalse(during.GiveUp(signal), "no giving up while inside the window");
        Assert.AreEqual(3m, signal.WorstAdversePercentage, "the move is remembered");

        var after = MakeAlgorithm(signal, SignalOpen.AddMinutes(15), low: 100m, high: 100m);
        Assert.IsTrue(after.GiveUp(signal), "3% is more than the 2% allowed");
    }


    [TestMethod]
    public void Short_ThatRanTooFarUp_IsGivenUpAfterTheWindow()
    {
        GlobalData.Settings.Trading.EntryConditions.EntryWaitMinutes = 15;
        GlobalData.Settings.Trading.EntryConditions.EntryMaxAdversePercentage = 2m;
        var signal = MakeSignal(CryptoTradeSide.Short);

        var during = MakeAlgorithm(signal, SignalOpen.AddMinutes(5), low: 100m, high: 103m);
        Assert.IsFalse(during.GiveUp(signal), "no giving up while inside the window");
        Assert.AreEqual(3m, signal.WorstAdversePercentage, "for a short the upward move counts");

        var after = MakeAlgorithm(signal, SignalOpen.AddMinutes(15), low: 100m, high: 100m);
        Assert.IsTrue(after.GiveUp(signal));
    }


    /// <summary>
    /// The dip stayed within the limit, so the signal survives the window and is taken. This is the
    /// case the whole rule exists to keep: on run 401 the entries that dipped and recovered are the
    /// ones that won every time.
    /// </summary>
    [TestMethod]
    public void ADipWithinTheLimit_SurvivesAndIsTaken()
    {
        GlobalData.Settings.Trading.EntryConditions.EntryWaitMinutes = 15;
        GlobalData.Settings.Trading.EntryConditions.EntryMaxAdversePercentage = 2m;
        var signal = MakeSignal(CryptoTradeSide.Long);

        var during = MakeAlgorithm(signal, SignalOpen.AddMinutes(5), low: 98.5m, high: 100m);
        Assert.IsFalse(during.GiveUp(signal));
        Assert.AreEqual(1.5m, signal.WorstAdversePercentage);

        var after = MakeAlgorithm(signal, SignalOpen.AddMinutes(15), low: 100m, high: 100m);
        Assert.IsFalse(after.GiveUp(signal), "1.5% stays within the 2%");
        Assert.IsTrue(after.AllowStepIn(signal), "and so the signal may be taken");
    }


    /// <summary>
    /// Only the worst excursion counts, not the last one: a recovery must not erase the dip that
    /// came before it.
    /// </summary>
    [TestMethod]
    public void ARecoveryDoesNotEraseTheWorstMove()
    {
        GlobalData.Settings.Trading.EntryConditions.EntryWaitMinutes = 15;
        GlobalData.Settings.Trading.EntryConditions.EntryMaxAdversePercentage = 2m;
        var signal = MakeSignal(CryptoTradeSide.Long);

        MakeAlgorithm(signal, SignalOpen.AddMinutes(5), low: 97m, high: 100m).GiveUp(signal);
        MakeAlgorithm(signal, SignalOpen.AddMinutes(10), low: 101m, high: 102m).GiveUp(signal);

        Assert.AreEqual(3m, signal.WorstAdversePercentage, "the deepest move stays recorded");
        var after = MakeAlgorithm(signal, SignalOpen.AddMinutes(15), low: 102m, high: 103m);
        Assert.IsTrue(after.GiveUp(signal), "the recovery does not undo the earlier dip");
    }


    /// <summary>
    /// A wait without a limit only delays the entry - it may never skip one. That is the
    /// combination that isolates "does a better entry price help" from "does skipping help".
    /// </summary>
    [TestMethod]
    public void AWaitWithoutALimit_DelaysButNeverSkips()
    {
        GlobalData.Settings.Trading.EntryConditions.EntryWaitMinutes = 15;
        GlobalData.Settings.Trading.EntryConditions.EntryMaxAdversePercentage = 0m;
        var signal = MakeSignal(CryptoTradeSide.Long);

        var during = MakeAlgorithm(signal, SignalOpen.AddMinutes(5), low: 80m, high: 100m);
        Assert.IsFalse(during.GiveUp(signal));
        var after = MakeAlgorithm(signal, SignalOpen.AddMinutes(15), low: 80m, high: 100m);
        Assert.IsFalse(after.GiveUp(signal), "without a limit nothing is ever rejected");
        Assert.IsTrue(after.AllowStepIn(signal));
    }


    /// <summary>
    /// The first evaluation happens on the tick that closed the signal candle itself, and
    /// SignalPrice is that candle's close. Its wick describes what happened BEFORE the signal
    /// fired, so it must not count as adverse movement - a signal candle that dipped 3% and
    /// closed strong would otherwise be rejected even when price only rose afterwards.
    /// </summary>
    [TestMethod]
    public void TheSignalCandlesOwnWick_DoesNotCountAsAdverseMovement()
    {
        GlobalData.Settings.Trading.EntryConditions.EntryWaitMinutes = 15;
        GlobalData.Settings.Trading.EntryConditions.EntryMaxAdversePercentage = 2m;
        var signal = MakeSignal(CryptoTradeSide.Long);

        // The signal candle itself: open time equals the signal's open, low 3% under the close
        var signalCandle = MakeAlgorithm(signal, SignalOpen, low: 97m, high: 100m);
        Assert.IsFalse(signalCandle.GiveUp(signal));
        Assert.AreEqual(0m, signal.WorstAdversePercentage, "the pre-signal wick is not adverse movement");

        var after = MakeAlgorithm(signal, SignalOpen.AddMinutes(15), low: 100m, high: 100m);
        Assert.IsFalse(after.GiveUp(signal), "nothing moved against the signal after it fired");
        Assert.IsTrue(after.AllowStepIn(signal));
    }
}
