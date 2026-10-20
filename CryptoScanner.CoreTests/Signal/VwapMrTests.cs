using CryptoScanner.Analyzers.VwapMr;
using CryptoScanner.Analyzers.VwapMr.Signal;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// VWAP mean reversion: the day VWAP with stdev bands, in on a band touch with a reversal candle,
/// out when a candle closes back at the VWAP. The calculator is tested on numbers worked out by
/// hand; the signal on band values written per candle, so what it looks at is readable here.
/// </summary>
[DoNotParallelize]
[TestClass]
public class VwapMrTests : TestBase
{
    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        ApplyDefaults(VwapMrPlugin.Settings);
    }

    [TestCleanup]
    public void Restore() => ApplyDefaults(VwapMrPlugin.Settings);

    private static void ApplyDefaults(VwapMrSettings settings)
    {
        VwapMrSettings fresh = new();
        settings.StdevLength = fresh.StdevLength;
        settings.Mult = fresh.Mult;
        settings.RequireReversalCandle = fresh.RequireReversalCandle;
        settings.StopLossPercentage = fresh.StopLossPercentage;
        settings.ExitAtVwap = fresh.ExitAtVwap;
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The calculator
    // ═══════════════════════════════════════════════════════════════════════

    private static readonly DateTime Midnight = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Two candles from midnight, stdev over 2 closes, multiplier 1.
    /// hlc3: (12+8+10)/3 = 10 with volume 1, (24+18+21)/3 = 21 with volume 3.
    /// vwap = (10*1 + 21*3) / (1+3) = 73/4 = 18.25.
    /// closes 10 and 21: mean 15.5, population stdev = 5.5. Bands 18.25 +/- 5.5.
    /// </summary>
    [TestMethod]
    public void TheVwapIsVolumeWeightedAndTheStdevIsThePopulationStdevOfTheClose()
    {
        var calculator = new VwapMrBandCalculator(stdevLength: 2, mult: 1.0);
        var first = calculator.Add(Midnight, 12m, 8m, 10m, 1m);
        Assert.IsFalse(first.HasValue, "one close is not enough for a stdev over two");

        var second = calculator.Add(Midnight.AddHours(1), 24m, 18m, 21m, 3m);
        Assert.IsTrue(second.HasValue);
        Assert.AreEqual(18.25, second.Vwap, 1e-9);
        Assert.AreEqual(18.25 + 5.5, second.Upper, 1e-9);
        Assert.AreEqual(18.25 - 5.5, second.Lower, 1e-9);
    }


    [TestMethod]
    public void TheVwapStartsAgainAtMidnight_TheStdevDoesNot()
    {
        var calculator = new VwapMrBandCalculator(stdevLength: 2, mult: 1.0);
        calculator.Add(Midnight.AddHours(-1), 101m, 99m, 100m, 5m);   // previous day, ignored as partial
        calculator.Add(Midnight, 51m, 49m, 50m, 2m);

        var band = calculator.Add(Midnight.AddHours(1), 53m, 51m, 52m, 2m);
        Assert.IsTrue(band.HasValue);
        // Only the two candles of the new day: (50*2 + 52*2) / 4 = 51.
        Assert.AreEqual(51.0, band.Vwap, 1e-9);
        // The stdev over closes 50 and 52 = 1.
        Assert.AreEqual(52.0, band.Upper, 1e-9);
    }


    /// <summary>
    /// A warm-up that starts in the middle of a day has not seen that day start: no value until the
    /// next day, instead of a VWAP over part of the day.
    /// </summary>
    [TestMethod]
    public void ADayThatWasAlreadyRunning_HasNoValueUntilTheNextDay()
    {
        var calculator = new VwapMrBandCalculator(stdevLength: 1, mult: 2.0);
        var partial1 = calculator.Add(Midnight.AddHours(13), 11m, 9m, 10m, 1m);
        var partial2 = calculator.Add(Midnight.AddHours(14), 11m, 9m, 10m, 1m);
        Assert.IsFalse(partial1.HasValue);
        Assert.IsFalse(partial2.HasValue);

        var nextDay = calculator.Add(Midnight.AddDays(1), 11m, 9m, 10m, 1m);
        Assert.IsTrue(nextDay.HasValue);
    }


    [TestMethod]
    public void ADayWithoutVolume_HasNoVwap()
    {
        var calculator = new VwapMrBandCalculator(stdevLength: 1, mult: 2.0);
        Assert.IsFalse(calculator.Add(Midnight, 11m, 9m, 10m, 0m).HasValue);
        Assert.IsTrue(calculator.Add(Midnight.AddMinutes(1), 11m, 9m, 10m, 1m).HasValue);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The signal
    // ═══════════════════════════════════════════════════════════════════════

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

    /// <summary>
    /// Two candles, index 0 the newest, both with a band of 95 / 100 / 105 and a neutral candle
    /// (open 100, high 101, low 99, close 100) that touches nothing. The caller shapes them.
    /// </summary>
    private static VwapMrBase MakeAlgorithm(CryptoTradeSide side, Action<CryptoCandle[]> shape,
        bool withBand = true)
    {
        CryptoSymbol symbol = MakeSymbol();
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m];
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        const int count = 2;
        CryptoCandle[] candles = new CryptoCandle[count];
        CryptoData[] data = new CryptoData[count];
        for (int i = 0; i < count; i++)
        {
            candles[i] = new CryptoCandle
            {
                TickDecimals = 2,
                OpenTime = new CandleTime((uint)((count - i) * interval.Duration)),
                Open = 100m,
                High = 101m,
                Low = 99m,
                Close = 100m,
            };
            data[i] = new CryptoData();
            if (withBand)
                data[i].SetPluginData(new VwapMrCandleData { Vwap = 100, Upper = 105, Lower = 95 });
        }
        shape(candles);

        for (int i = 0; i < count; i++)
        {
            symbolInterval.CandleList.TryAdd(candles[i].OpenTime, candles[i]);
            symbolInterval.Data[candles[i].OpenTime] = data[i];
        }

        return side == CryptoTradeSide.Long
            ? new VwapMrLong
            {
                Symbol = symbol,
                Interval = interval,
                SymbolInterval = symbolInterval,
                SignalSide = side,
                SignalStrategy = "vwapmr",
                CandleLast = new MyData { Candle = candles[0], CandleData = data[0] },
            }
            : new VwapMrShort
            {
                Symbol = symbol,
                Interval = interval,
                SymbolInterval = symbolInterval,
                SignalSide = side,
                SignalStrategy = "vwapmr",
                CandleLast = new MyData { Candle = candles[0], CandleData = data[0] },
            };
    }

    private static void GreenTouchingLower(ref CryptoCandle c)
    {
        c.Open = 95.5m;
        c.Low = 94m;
        c.Close = 97m;
    }

    private static void RedTouchingUpper(ref CryptoCandle c)
    {
        c.Open = 104.5m;
        c.High = 106m;
        c.Close = 103m;
    }


    [TestMethod]
    public void AGreenCandleTouchingTheLowerBand_IsALong_WithTheStopLoss()
    {
        var algorithm = MakeAlgorithm(CryptoTradeSide.Long, c => GreenTouchingLower(ref c[0]));

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        Assert.AreEqual(1.5m, algorithm.OverrideSlPercentage);
        StringAssert.Contains(algorithm.ExtraText, "lower band");
    }


    [TestMethod]
    public void ARedCandleTouchingTheUpperBand_IsAShort()
    {
        var algorithm = MakeAlgorithm(CryptoTradeSide.Short, c => RedTouchingUpper(ref c[0]));

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "upper band");
    }


    [TestMethod]
    public void ATouchWithoutTheReversalCandle_IsNoSignal_UnlessSwitchedOff()
    {
        var algorithm = MakeAlgorithm(CryptoTradeSide.Long, c =>
        {
            c[0].Open = 98m;
            c[0].Low = 94m;
            c[0].Close = 96m;   // red
        });
        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "no green candle");

        VwapMrPlugin.Settings.RequireReversalCandle = false;
        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
    }


    /// <summary>The previous candle touched, this one is the green candle that turns.</summary>
    [TestMethod]
    public void ATouchOnThePreviousCandle_WithAGreenCandleNow_IsALong()
    {
        var algorithm = MakeAlgorithm(CryptoTradeSide.Long, c =>
        {
            c[1].Low = 94m;
            c[0].Open = 99m;
            c[0].Close = 100.5m;
        });

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "previous candle");
    }


    [TestMethod]
    public void NoTouch_IsNoSignal()
    {
        var algorithm = MakeAlgorithm(CryptoTradeSide.Long, c => c[0].Close = 100.5m);

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "no touch");
    }


    [TestMethod]
    public void WithoutACompleteDayVwap_ThereIsNoSignalAndNoExit()
    {
        var algorithm = MakeAlgorithm(CryptoTradeSide.Long, c => GreenTouchingLower(ref c[0]), withBand: false);

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "day vwap");
        Assert.IsFalse(algorithm.IsExitSignal());
    }


    [TestMethod]
    public void AStopLossOfZero_LeavesTheStopToTheTradingSettings()
    {
        VwapMrPlugin.Settings.StopLossPercentage = 0m;
        var algorithm = MakeAlgorithm(CryptoTradeSide.Long, c => GreenTouchingLower(ref c[0]));

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        Assert.IsNull(algorithm.OverrideSlPercentage);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The exit
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ACloseAtOrBeyondTheVwap_AsksForTheExit()
    {
        var longAt = MakeAlgorithm(CryptoTradeSide.Long, c => c[0].Close = 100m);
        Assert.IsTrue(longAt.IsExitSignal(), longAt.ExtraText);

        var longBelow = MakeAlgorithm(CryptoTradeSide.Long, c => c[0].Close = 99.5m);
        Assert.IsFalse(longBelow.IsExitSignal());

        var shortBeyond = MakeAlgorithm(CryptoTradeSide.Short, c => c[0].Close = 99.5m);
        Assert.IsTrue(shortBeyond.IsExitSignal(), shortBeyond.ExtraText);

        var shortAbove = MakeAlgorithm(CryptoTradeSide.Short, c => c[0].Close = 100.5m);
        Assert.IsFalse(shortAbove.IsExitSignal());
    }


    [TestMethod]
    public void TheVwapExitSwitchedOff_NeverAsks()
    {
        VwapMrPlugin.Settings.ExitAtVwap = false;
        var algorithm = MakeAlgorithm(CryptoTradeSide.Long, c => c[0].Close = 101m);

        Assert.IsFalse(algorithm.HasExitSignal);
        Assert.IsFalse(algorithm.IsExitSignal());
    }
}
