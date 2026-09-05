using CryptoScanner.Analyzers.MacdCrossBand;
using CryptoScanner.Analyzers.MacdCrossBand.Signal;
using CryptoScanner.Analyzers.Vbs;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// The MACD crossover with a band break behind it. The cross itself is the same rule the
/// MacdCrossTests already cover, so the tests here are about the lookback only: every series has a
/// clean cross on the newest candle and differs in where - and whether - the band was broken.
/// <para>
/// Only the VBS lookback is exercised: its band values sit on the candle, so a test can put them
/// there by hand. The AtrRb and Dbr lookbacks compute their own bands from a few hundred candles of
/// real price history, which is a different kind of test than this one.
/// </para>
/// </summary>
[DoNotParallelize]
[TestClass]
public class MacdCrossBandTests : TestBase
{
    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        // Settings has an internal setter, so the shared instance is adjusted in place rather than
        // replaced - and put back in cleanup, because every test in the process reads that one object.
        ApplyDefaults(MacdCrossBandPlugin.Settings);
    }

    [TestCleanup]
    public void Restore() => ApplyDefaults(MacdCrossBandPlugin.Settings);

    private static void ApplyDefaults(MacdCrossBandSettings settings)
    {
        MacdCrossBandSettings fresh = new();
        settings.ConfirmationCandles = fresh.ConfirmationCandles;
        settings.MinimumDistancePercentage = fresh.MinimumDistancePercentage;
        settings.RequireCrossBeyondZeroLine = fresh.RequireCrossBeyondZeroLine;
        settings.AdxMinimum = fresh.AdxMinimum;
        settings.AdxRecentlyBelow = fresh.AdxRecentlyBelow;
        settings.AdxRecentlyWithinCandles = fresh.AdxRecentlyWithinCandles;
        settings.RelativeVolumeMinimum = fresh.RelativeVolumeMinimum;
        settings.RelativeVolumeCandles = fresh.RelativeVolumeCandles;
        settings.RelativeVolumeAverageCandles = fresh.RelativeVolumeAverageCandles;
        settings.ExitOnCrossBack = fresh.ExitOnCrossBack;
        settings.ExitConfirmationCandles = fresh.ExitConfirmationCandles;
        settings.LookbackWithinCandles = fresh.LookbackWithinCandles;
        settings.LookbackVbs = fresh.LookbackVbs;
        settings.LookbackAtrRb = fresh.LookbackAtrRb;
        settings.LookbackDbr = fresh.LookbackDbr;
        settings.AcceptEitherBand = fresh.AcceptEitherBand;
        settings.VbsRequireCloseBeyondBand = fresh.VbsRequireCloseBeyondBand;
    }


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

    private static CryptoInterval MakeInterval()
        => GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m];


    /// <summary>
    /// Builds <paramref name="count"/> candles ending at index 0 (the newest), every one trading
    /// between 99 and 101 and closing at 100, with VBS bands at 95 and 105 - so no candle touches a
    /// band until the test moves one. The MACD line is on the trade's side on candle 0 only, which
    /// is exactly one clean cross.
    /// </summary>
    private static (MacdCrossBandBase Algorithm, CryptoCandle[] Candles, CryptoData[] Data) MakeSeries(
        CryptoTradeSide side, int count, Action<CryptoCandle[], CryptoData[]> shape)
    {
        CryptoSymbol symbol = MakeSymbol();
        CryptoInterval interval = MakeInterval();
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

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
            data[i] = new CryptoData
            {
                // Against the trade everywhere; candle 0 is put on our side below.
                MacdValue = side == CryptoTradeSide.Long ? -1.0 : 1.0,
                MacdSignal = 0.0,
            };
            data[i].SetPluginData(new VbsCandleData { Basis = 100, Lower = 95, Upper = 105, Acs = 1 });
        }
        data[0].MacdValue = side == CryptoTradeSide.Long ? 1.0 : -1.0;

        shape(candles, data);

        for (int i = 0; i < count; i++)
        {
            symbolInterval.CandleList.TryAdd(candles[i].OpenTime, candles[i]);
            symbolInterval.Data[candles[i].OpenTime] = data[i];
        }

        return (new MacdCrossBandBase
        {
            Symbol = symbol,
            Interval = interval,
            SymbolInterval = symbolInterval,
            SignalSide = side,
            SignalStrategy = "macdcrossband",
            CandleLast = new MyData { Candle = candles[0], CandleData = data[0] },
        }, candles, data);
    }

    /// <summary>Pushes the Low of candle <paramref name="index"/> through the lower band (95).</summary>
    private static void BreakLowerBand(CryptoCandle[] candles, int index)
        => candles[index].Low = 94m;

    /// <summary>Pushes the High of candle <paramref name="index"/> through the upper band (105).</summary>
    private static void BreakUpperBand(CryptoCandle[] candles, int index)
        => candles[index].High = 106m;

    private const int Enough = 30;


    // ═══════════════════════════════════════════════════════════════════════
    //  The lookback itself
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The cross alone is not enough: without a band break in the window the signal is refused,
    /// which is the whole difference with the plain macdcross.
    /// </summary>
    [TestMethod]
    public void ACrossWithoutABandBreak_IsNoSignal()
    {
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough, (_, _) => { });

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "no band break");
    }


    [TestMethod]
    public void ACrossAfterALowerBandBreak_IsALong()
    {
        MacdCrossBandPlugin.Settings.LookbackWithinCandles = 5;

        var (algorithm, candles, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            (c, _) => BreakLowerBand(c, 3));

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "crossed above");
        StringAssert.Contains(algorithm.ExtraText, "vbs lower band 3 candle(s) ago");
        Assert.AreEqual(94m, candles[3].Low);
    }


    [TestMethod]
    public void ACrossAfterAnUpperBandBreak_IsAShort()
    {
        MacdCrossBandPlugin.Settings.LookbackWithinCandles = 5;

        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Short, Enough,
            (c, _) => BreakUpperBand(c, 2));

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "crossed under");
        StringAssert.Contains(algorithm.ExtraText, "vbs upper band 2 candle(s) ago");
    }


    /// <summary>A break on the signal candle itself is worded differently, and still counts.</summary>
    [TestMethod]
    public void ABreakOnTheSignalCandleItself_Counts()
    {
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            (c, _) => BreakLowerBand(c, 0));

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "vbs lower band on this candle");
    }


    /// <summary>
    /// The window ends at the signal candle: a break older than it is out of reach, which is what
    /// keeps the strategy pointing at charts where the two things happened close together.
    /// </summary>
    [TestMethod]
    public void ABreakOlderThanTheWindow_DoesNotCount()
    {
        MacdCrossBandPlugin.Settings.LookbackWithinCandles = 5;

        var (justInside, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            (c, _) => BreakLowerBand(c, 4));
        Assert.IsTrue(justInside.IsSignal(), justInside.ExtraText);

        var (justOutside, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            (c, _) => BreakLowerBand(c, 5));
        Assert.IsFalse(justOutside.IsSignal());
        StringAssert.Contains(justOutside.ExtraText, "no band break in the last 5 candle(s)");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Which band
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// By default a long wants the LOWER band: price stretched away and momentum turning back. An
    /// upper-band break before a long cross is the other situation entirely and does not count.
    /// </summary>
    [TestMethod]
    public void ByDefault_ALongDoesNotAcceptAnUpperBandBreak()
    {
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            (c, _) => BreakUpperBand(c, 3));

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "no band break");
    }


    [TestMethod]
    public void WithAcceptEitherBand_ALongAlsoTakesAnUpperBandBreak()
    {
        MacdCrossBandPlugin.Settings.AcceptEitherBand = true;

        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            (c, _) => BreakUpperBand(c, 3));

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "vbs upper band 3 candle(s) ago");
    }


    /// <summary>
    /// The wick counts by default, the same way the VBS strategy reads its own band break. With the
    /// stricter setting the close has to be beyond the band, so a wick through it is not enough.
    /// </summary>
    [TestMethod]
    public void CloseBeyondTheBandOnly_IgnoresAWickThroughIt()
    {
        MacdCrossBandPlugin.Settings.VbsRequireCloseBeyondBand = true;

        var (wickOnly, _, _) = MakeSeries(CryptoTradeSide.Long, Enough,
            (c, _) => BreakLowerBand(c, 3));
        Assert.IsFalse(wickOnly.IsSignal());

        var (closedThrough, _, _) = MakeSeries(CryptoTradeSide.Long, Enough, (c, _) =>
        {
            BreakLowerBand(c, 3);
            c[3].Close = 94m;
        });
        Assert.IsTrue(closedThrough.IsSignal(), closedThrough.ExtraText);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The edges
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Bands that are not there yet (the VBS indicator is still warming up, or is not running) is a
    /// different answer than "the price stayed inside", and the text has to say so - otherwise a
    /// silent no reads as a verdict about a price nothing measured.
    /// </summary>
    [TestMethod]
    public void WithoutBandValues_ItSaysSoInsteadOfClaimingNoBreak()
    {
        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough, (_, data) =>
        {
            for (int i = 0; i < data.Length; i++)
                data[i].SetPluginData<VbsCandleData>(null!);
        });

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "vbs bands not available yet");
    }


    /// <summary>
    /// With nothing ticked nothing is looked up and the strategy is the plain crossover again -
    /// the baseline a run compares the lookback against.
    /// </summary>
    [TestMethod]
    public void WithNoLookbackEnabled_ItIsThePlainCrossover()
    {
        MacdCrossBandPlugin.Settings.LookbackVbs = false;

        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, Enough, (_, _) => { });

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "crossed above");
    }


    /// <summary>
    /// A window longer than the history available is a quiet no about the history, not an
    /// exception and not a claim about the bands.
    /// </summary>
    [TestMethod]
    public void AWindowLongerThanTheHistory_SaysNoInsteadOfThrowing()
    {
        MacdCrossBandPlugin.Settings.LookbackWithinCandles = 50;

        var (algorithm, _, _) = MakeSeries(CryptoTradeSide.Long, 10, (c, _) => BreakLowerBand(c, 3));

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "not enough candles");
    }
}
