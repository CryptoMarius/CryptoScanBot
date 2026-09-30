using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// SignalStatistics: the price extremes since the signal and the virtual outcome (run, sl,
/// tp1..tp5) of a position at the signal price. The status only moves forward, the stop only
/// counts before the first take-profit level, and a short mirrors a long in log space.
/// </summary>
[TestClass]
public class SignalStatisticsTests : TestBase
{
    private static readonly DateTime SignalOpen = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private List<CryptoTpEntry> _savedTpList = [];
    private decimal _savedStopLoss;

    [TestInitialize]
    public void SaveSettings()
    {
        InitTestSession();
        _savedTpList = GlobalData.Settings.Trading.TpList;
        _savedStopLoss = GlobalData.Settings.Trading.StopLossPercentage;
        // Three levels: 1%, 2% and 3%; the stop at 2%
        GlobalData.Settings.Trading.TpList =
        [
            new CryptoTpEntry { Percentage = 1m, Factor = 33m },
            new CryptoTpEntry { Percentage = 2m, Factor = 33m },
            new CryptoTpEntry { Percentage = 3m, Factor = 100m },
        ];
        GlobalData.Settings.Trading.StopLossPercentage = 2m;
    }

    [TestCleanup]
    public void RestoreSettings()
    {
        GlobalData.Settings.Trading.TpList = _savedTpList;
        GlobalData.Settings.Trading.StopLossPercentage = _savedStopLoss;
    }

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

    private static CryptoInterval MakeInterval()
        => GlobalData.IntervalListPeriod.Count > 0
            ? GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m]
            : new CryptoInterval { Id = 4, Name = "5m", Duration = 5 };

    /// <summary>A signal at price 100 on a 5m candle, statistics initialised the way SignalCreate does.</summary>
    private static CryptoSignal MakeSignal(CryptoTradeSide side)
    {
        var symbol = MakeSymbol();
        var interval = MakeInterval();
        return new CryptoSignal
        {
            Exchange = symbol.Exchange,
            ExchangeId = symbol.ExchangeId,
            Symbol = symbol,
            SymbolId = symbol.Id,
            Interval = interval,
            IntervalId = interval.Id,
            Candle = null,
            Side = side,
            Strategy = "stobb",
            OpenDate = SignalOpen,
            CloseDate = SignalOpen.AddMinutes(interval.Duration),
            SignalPrice = 100m,
            PriceMin = 100m,
            PriceMax = 100m,
            SignalStatus = CryptoSignalStatus.Run,
        };
    }

    /// <summary>Replace the last 1m candle of the symbol, <paramref name="minutesAfterClose"/> after the signal candle closed.</summary>
    private static void SetLastCandle(CryptoSignal signal, decimal low, decimal high, int minutesAfterClose = 1)
    {
        var candles = signal.Symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m).CandleList;
        var candle = new CryptoCandle
        {
            TickDecimals = 2,
            OpenTime = CandleTime.FromDateTime(signal.CloseDate.AddMinutes(minutesAfterClose)),
            Open = 100m,
            High = high,
            Low = low,
            Close = 100m,
            Volume = 1m,
        };
        candles.Add(candle.OpenTime, candle);
    }

    [TestMethod]
    public void ExtremesFollowTheCandles_AsPercentageOfTheSignalPrice()
    {
        var signal = MakeSignal(CryptoTradeSide.Long);

        SetLastCandle(signal, low: 99.5m, high: 100.5m, minutesAfterClose: 1);
        Assert.IsTrue(SignalStatistics.Update(signal));
        Assert.AreEqual(99.5m, signal.PriceMin);
        Assert.AreEqual(100.5m, signal.PriceMax);
        Assert.AreEqual(-0.5f, signal.PriceMinPerc, 0.001f);
        Assert.AreEqual(0.5f, signal.PriceMaxPerc, 0.001f);
        Assert.AreEqual(CryptoSignalStatus.Run, signal.SignalStatus);

        // A narrower candle changes nothing
        SetLastCandle(signal, low: 99.8m, high: 100.2m, minutesAfterClose: 2);
        Assert.IsFalse(SignalStatistics.Update(signal));
        Assert.AreEqual(99.5m, signal.PriceMin);
        Assert.AreEqual(100.5m, signal.PriceMax);
    }

    [TestMethod]
    public void CandleFromBeforeTheSignal_IsIgnored_LastPriceStandsIn()
    {
        var signal = MakeSignal(CryptoTradeSide.Long);
        signal.Symbol.LastPrice = 101.5m;

        // The only 1m candle opened before the signal candle closed
        SetLastCandle(signal, low: 90m, high: 110m, minutesAfterClose: -3);
        Assert.IsTrue(SignalStatistics.Update(signal));
        Assert.AreEqual(100m, signal.PriceMin);
        Assert.AreEqual(101.5m, signal.PriceMax);
        Assert.AreEqual(CryptoSignalStatus.Tp1, signal.SignalStatus);
    }

    [TestMethod]
    public void Long_ClimbsThroughTheLevels_AndNeverDropsBack()
    {
        var signal = MakeSignal(CryptoTradeSide.Long);

        SetLastCandle(signal, low: 99.9m, high: 101m, minutesAfterClose: 1);
        SignalStatistics.Update(signal);
        Assert.AreEqual(CryptoSignalStatus.Tp1, signal.SignalStatus);

        SetLastCandle(signal, low: 100.5m, high: 102.5m, minutesAfterClose: 2);
        SignalStatistics.Update(signal);
        Assert.AreEqual(CryptoSignalStatus.Tp2, signal.SignalStatus);

        // Through the stop afterwards: the profit was already taken, the status stays
        SetLastCandle(signal, low: 97m, high: 100m, minutesAfterClose: 3);
        SignalStatistics.Update(signal);
        Assert.AreEqual(CryptoSignalStatus.Tp2, signal.SignalStatus);
        Assert.AreEqual(97m, signal.PriceMin);

        SetLastCandle(signal, low: 100m, high: 103.2m, minutesAfterClose: 4);
        SignalStatistics.Update(signal);
        Assert.AreEqual(CryptoSignalStatus.Tp3, signal.SignalStatus);
    }

    [TestMethod]
    public void Long_StopBeforeAnyLevel_IsFinal()
    {
        var signal = MakeSignal(CryptoTradeSide.Long);

        SetLastCandle(signal, low: 98m, high: 100.5m, minutesAfterClose: 1);
        SignalStatistics.Update(signal);
        Assert.AreEqual(CryptoSignalStatus.Lost, signal.SignalStatus);

        // A rally afterwards does not turn a stopped signal into a win
        SetLastCandle(signal, low: 100m, high: 105m, minutesAfterClose: 2);
        SignalStatistics.Update(signal);
        Assert.AreEqual(CryptoSignalStatus.Lost, signal.SignalStatus);
        Assert.AreEqual(105m, signal.PriceMax);
    }

    [TestMethod]
    public void WhipsawInOneMinute_TheStopWins()
    {
        var signal = MakeSignal(CryptoTradeSide.Long);

        SetLastCandle(signal, low: 97m, high: 103.5m, minutesAfterClose: 1);
        SignalStatistics.Update(signal);
        Assert.AreEqual(CryptoSignalStatus.Lost, signal.SignalStatus);
    }

    [TestMethod]
    public void Short_IsTheMirrorImage_InLogSpace()
    {
        var signal = MakeSignal(CryptoTradeSide.Short);

        // Log mirror: 1% profit on a short lies at 100 / 1.01 = 99.0099, so 99.02 is not there yet
        SetLastCandle(signal, low: 99.02m, high: 100.5m, minutesAfterClose: 1);
        SignalStatistics.Update(signal);
        Assert.AreEqual(CryptoSignalStatus.Run, signal.SignalStatus);

        SetLastCandle(signal, low: 99.00m, high: 100.5m, minutesAfterClose: 2);
        SignalStatistics.Update(signal);
        Assert.AreEqual(CryptoSignalStatus.Tp1, signal.SignalStatus);

        // The stop of a short at 2% lies at 100 / 0.98 = 102.04; reached, but tp1 was first
        SetLastCandle(signal, low: 100m, high: 102.1m, minutesAfterClose: 3);
        SignalStatistics.Update(signal);
        Assert.AreEqual(CryptoSignalStatus.Tp1, signal.SignalStatus);
    }

    [TestMethod]
    public void Short_StopBeforeAnyLevel()
    {
        var signal = MakeSignal(CryptoTradeSide.Short);

        SetLastCandle(signal, low: 99.5m, high: 102.1m, minutesAfterClose: 1);
        SignalStatistics.Update(signal);
        Assert.AreEqual(CryptoSignalStatus.Lost, signal.SignalStatus);
    }

    [TestMethod]
    public void OwnSlAndTpOfTheSignal_ReplaceTheTraderSettings()
    {
        var signal = MakeSignal(CryptoTradeSide.Long);
        signal.SlPercentage = 0.5m;
        signal.TpPercentage = 4m;

        // Below the signal's own stop, above tp1..tp3 of the trader: the signal's own numbers count
        SetLastCandle(signal, low: 99.4m, high: 103.5m, minutesAfterClose: 1);
        SignalStatistics.Update(signal);
        Assert.AreEqual(CryptoSignalStatus.Lost, signal.SignalStatus);

        signal = MakeSignal(CryptoTradeSide.Long);
        signal.SlPercentage = 0.5m;
        signal.TpPercentage = 4m;
        SetLastCandle(signal, low: 99.6m, high: 104m, minutesAfterClose: 1);
        SignalStatistics.Update(signal);
        // One level only, whatever the trader's grid says
        Assert.AreEqual(CryptoSignalStatus.Tp1, signal.SignalStatus);
    }

    [TestMethod]
    public void NoStopConfigured_NeverLost()
    {
        GlobalData.Settings.Trading.StopLossPercentage = 0m;
        var signal = MakeSignal(CryptoTradeSide.Long);

        SetLastCandle(signal, low: 80m, high: 100.2m, minutesAfterClose: 1);
        SignalStatistics.Update(signal);
        Assert.AreEqual(CryptoSignalStatus.Run, signal.SignalStatus);
        Assert.AreEqual(-20f, signal.PriceMinPerc, 0.001f);
    }

    [TestMethod]
    public void Position_KeepsTheExtremes_FromTheCandleTheTraderHandsIt()
    {
        var symbol = MakeSymbol();
        var position = new CryptoPosition
        {
            Exchange = symbol.Exchange,
            ExchangeId = symbol.ExchangeId,
            Symbol = symbol,
            SymbolId = symbol.Id,
            Interval = MakeInterval(),
            Side = CryptoTradeSide.Long,
            SignalPrice = 100m,
            PriceMin = 100m,
            PriceMax = 100m,
        };

        Assert.IsTrue(SignalStatistics.UpdatePosition(position, low: 99m, high: 101m));
        Assert.AreEqual(99m, position.PriceMin);
        Assert.AreEqual(101m, position.PriceMax);
        Assert.AreEqual(-1f, position.PriceMinPerc, 0.001f);
        Assert.AreEqual(1f, position.PriceMaxPerc, 0.001f);

        // Inside the range so far: nothing changes
        Assert.IsFalse(SignalStatistics.UpdatePosition(position, low: 99.5m, high: 100.5m));

        // An empty candle (no trades) is ignored
        Assert.IsFalse(SignalStatistics.UpdatePosition(position, low: 0m, high: 0m));
        Assert.AreEqual(99m, position.PriceMin);
    }

    [TestMethod]
    public void StatusText_AndColourClass()
    {
        Assert.AreEqual("run", SignalStatistics.GetStatusText(CryptoSignalStatus.Run));
        Assert.AreEqual("sl", SignalStatistics.GetStatusText(CryptoSignalStatus.Lost));
        Assert.AreEqual("tp1", SignalStatistics.GetStatusText(CryptoSignalStatus.Tp1));
        Assert.AreEqual("tp5", SignalStatistics.GetStatusText(CryptoSignalStatus.Tp5));
        Assert.IsFalse(SignalStatistics.IsWin(CryptoSignalStatus.Run));
        Assert.IsFalse(SignalStatistics.IsWin(CryptoSignalStatus.Lost));
        Assert.IsTrue(SignalStatistics.IsWin(CryptoSignalStatus.Tp1));
        Assert.IsTrue(SignalStatistics.IsWin(CryptoSignalStatus.Tp5));
    }
}
