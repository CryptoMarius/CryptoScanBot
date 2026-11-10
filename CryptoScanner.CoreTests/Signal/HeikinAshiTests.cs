using CryptoScanner.Analyzers.HeikinAshi;
using CryptoScanner.Analyzers.HeikinAshi.Signal;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// The Heikin Ashi trend: in on the colour change of the Heikin Ashi candle with the higher interval
/// on the same colour, out on the first candle of the other colour or with a wick on the other side.
/// <para>
/// The daily series falls one point a day for twenty days (red Heikin Ashi candles) and then turns
/// with one strong candle the other way (the first candle of the other colour). The weekly series
/// rises or falls steadily.
/// </para>
/// </summary>
[DoNotParallelize]
[TestClass]
public class HeikinAshiTests : TestBase
{
    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        ApplyDefaults(HeikinAshiPlugin.Settings);
    }

    [TestCleanup]
    public void Restore() => ApplyDefaults(HeikinAshiPlugin.Settings);

    private static void ApplyDefaults(HeikinAshiSettings settings)
    {
        HeikinAshiSettings fresh = new();
        settings.HigherInterval = fresh.HigherInterval;
        settings.ExitOnSignal = fresh.ExitOnSignal;
        settings.ExitOnOppositeWick = fresh.ExitOnOppositeWick;
        settings.WickMinimumPercentage = fresh.WickMinimumPercentage;
        settings.StopBeyondSignalCandle = fresh.StopBeyondSignalCandle;
    }


    private static CryptoSymbol MakeSymbol()
    {
        Exchange exchange = new() { Id = 1, Name = "TestExchange", TakerFeeRate = 0.1m, MakerFeeRate = 0.1m };
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

    private static CryptoCandle Candle(uint openTime, decimal open, decimal high, decimal low, decimal close) => new()
    {
        TickDecimals = 2,
        OpenTime = new CandleTime(openTime),
        Open = open,
        High = high,
        Low = low,
        Close = close,
        Volume = 10m,
    };

    private const uint Day = 1440;
    private const uint Week = 7 * 1440;
    private const uint FirstDay = 200 * Week;
    private const int TrendDays = 20;


    /// <summary>
    /// Twenty days moving one point a day against <paramref name="side"/>, then the turn: one strong
    /// candle with the trade (shaped by <paramref name="turn"/> when given). Plus twenty weekly candles
    /// that close before the week of the last daily candle, rising when <paramref name="weekUp"/>.
    /// </summary>
    private static HeikinAshiBase MakeSeries(CryptoTradeSide side, bool weekUp, Func<uint, CryptoCandle>? turn = null,
        List<CryptoCandle>? extra = null)
    {
        CryptoSymbol symbol = MakeSymbol();
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1d];
        CryptoSymbolInterval daily = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1d);
        CryptoSymbolInterval weekly = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1w);

        int direction = side == CryptoTradeSide.Long ? 1 : -1;
        List<CryptoCandle> days = [];
        decimal price = 100m;
        for (int i = 0; i < TrendDays; i++)
        {
            decimal next = price - direction;
            days.Add(Candle(FirstDay + (uint)i * Day, price, Math.Max(price, next), Math.Min(price, next), next));
            price = next;
        }
        uint turnTime = FirstDay + TrendDays * Day;
        days.Add(turn != null
            ? turn(turnTime)
            : Candle(turnTime, price, Math.Max(price, price + 10 * direction), Math.Min(price, price + 10 * direction), price + 10 * direction));
        if (extra != null)
            days.AddRange(extra);
        foreach (CryptoCandle candle in days)
        {
            daily.CandleList.TryAdd(candle.OpenTime, candle);
            daily.Data[candle.OpenTime] = new CryptoData();
        }

        // The weekly candles up to the last week that had closed when the last daily candle closed.
        CandleTime moment = days[^1].OpenTime + Day;
        CandleTime lastWeek = (moment - 1u).AlignToIntervalMinutes(Week) - Week;
        decimal weekPrice = 100m;
        for (int i = 19; i >= 0; i--)
        {
            decimal next = weekUp ? weekPrice + 2 : weekPrice - 2;
            CryptoCandle candle = Candle(lastWeek.Minutes - (uint)i * Week, weekPrice, Math.Max(weekPrice, next), Math.Min(weekPrice, next), next);
            weekly.CandleList.TryAdd(candle.OpenTime, candle);
            weekPrice = next;
        }

        return new HeikinAshiBase
        {
            Symbol = symbol,
            Interval = interval,
            SymbolInterval = daily,
            SignalSide = side,
            SignalStrategy = "heikinashi",
            CandleLast = new MyData { Candle = days[^1], CandleData = new CryptoData() },
        };
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The calculation
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Calculator_FollowsTheFormulas()
    {
        List<CryptoCandle> candles =
        [
            Candle(0, 10m, 14m, 8m, 12m),
            Candle(Day, 12m, 16m, 11m, 15m),
        ];
        List<HeikinAshiCandle> ha = HeikinAshiCalculator.Calculate(candles);

        Assert.AreEqual(11m, ha[0].Open);    // (10 + 12) / 2, the seed
        Assert.AreEqual(11m, ha[0].Close);   // (10 + 14 + 8 + 12) / 4
        Assert.AreEqual(11m, ha[1].Open);    // (11 + 11) / 2
        Assert.AreEqual(13.5m, ha[1].Close); // (12 + 16 + 11 + 15) / 4
        Assert.AreEqual(16m, ha[1].High);
        Assert.AreEqual(11m, ha[1].Low);
        Assert.AreEqual(1, ha[1].Colour);
        Assert.IsFalse(ha[1].HasLowerWick(5m));
        Assert.IsTrue(ha[1].HasUpperWick(5m));
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The entry
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void TurningGreen_WithTheWeekGreen_IsALong()
    {
        var algorithm = MakeSeries(CryptoTradeSide.Long, weekUp: true);

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "turned green");
    }

    [TestMethod]
    public void TurningRed_WithTheWeekRed_IsAShort()
    {
        var algorithm = MakeSeries(CryptoTradeSide.Short, weekUp: false);

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "turned red");
    }

    [TestMethod]
    public void TurningGreen_WithTheWeekRed_IsNoLong()
    {
        var algorithm = MakeSeries(CryptoTradeSide.Long, weekUp: false);

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "1w candle not green");
    }

    [TestMethod]
    public void TurningGreen_WithoutHigherInterval_IsALongWhateverTheWeek()
    {
        HeikinAshiPlugin.Settings.HigherInterval = "";
        var algorithm = MakeSeries(CryptoTradeSide.Long, weekUp: false);

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
    }

    [TestMethod]
    public void ASecondGreenCandle_IsNoColourChange()
    {
        // One more strong green day after the turn: green again, but no change any more.
        var algorithm = MakeSeries(CryptoTradeSide.Long, weekUp: true,
            extra: [Candle(FirstDay + (TrendDays + 1) * Day, 90m, 100m, 90m, 100m)]);

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "no colour change");
    }

    [TestMethod]
    public void TheStop_IsTheDistanceToTheLowOfTheSignalCandle()
    {
        // The turn closes at 100 with its low at 95: a stop 5% under the signal price.
        var algorithm = MakeSeries(CryptoTradeSide.Long, weekUp: true,
            turn: time => Candle(time, 96m, 100m, 95m, 100m));

        Assert.AreEqual(5m, algorithm.OverrideSlPercentage);

        HeikinAshiPlugin.Settings.StopBeyondSignalCandle = false;
        Assert.IsNull(algorithm.OverrideSlPercentage);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The exit
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void AStrongGreenCandle_KeepsTheLong()
    {
        // After the turn another green day without a wick under the body.
        var algorithm = MakeSeries(CryptoTradeSide.Long, weekUp: true,
            extra: [Candle(FirstDay + (TrendDays + 1) * Day, 90m, 100m, 90m, 100m)]);

        Assert.IsFalse(algorithm.IsExitSignal(), algorithm.ExtraText);
    }

    [TestMethod]
    public void AGreenCandleWithAWickUnderTheBody_ClosesTheLong()
    {
        // Green, but the real low dips far under the Heikin Ashi body.
        var algorithm = MakeSeries(CryptoTradeSide.Long, weekUp: true,
            extra: [Candle(FirstDay + (TrendDays + 1) * Day, 90m, 100m, 70m, 100m)]);

        Assert.IsTrue(algorithm.IsExitSignal());
        StringAssert.Contains(algorithm.ExtraText, "wick under the body");

        HeikinAshiPlugin.Settings.ExitOnOppositeWick = false;
        Assert.IsFalse(algorithm.IsExitSignal());
    }

    [TestMethod]
    public void ARedCandle_ClosesTheLong()
    {
        var algorithm = MakeSeries(CryptoTradeSide.Long, weekUp: true,
            extra: [Candle(FirstDay + (TrendDays + 1) * Day, 90m, 90m, 70m, 70m)]);

        Assert.IsTrue(algorithm.IsExitSignal());
        StringAssert.Contains(algorithm.ExtraText, "turned red");

        HeikinAshiPlugin.Settings.ExitOnSignal = false;
        Assert.IsFalse(algorithm.IsExitSignal());
    }
}
