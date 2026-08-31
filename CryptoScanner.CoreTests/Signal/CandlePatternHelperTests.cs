using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// The candlestick reversal shapes, on candles drawn by hand so the expected answer is obvious from
/// reading the numbers.
/// <para>
/// The test that carries the most weight is <see cref="TheSameShape_MatchesAtAnyPriceScale"/>. This
/// helper exists because the OHLC_Candlestick_Patterns package measured shapes with ABSOLUTE price
/// thresholds: on BTCUSDT at 65 000 an engulfing never fired once, while on a coin at 0.01 half of
/// all candles came back as a "long black candlestick". Anything that creeps back into these
/// definitions in absolute terms will fail that one test and nothing else.
/// </para>
/// </summary>
[TestClass]
public class CandlePatternHelperTests
{
    private static readonly CandlePatternSettings Settings = new();

    /// <summary>
    /// CryptoCandle stores its prices as an INT of price x 10^TickDecimals, which cuts both ways:
    /// too few decimals and a candle quietly becomes a different shape than the one written down
    /// here, too many and the int overflows (8 decimals on a price of 100 does not fit). Four covers
    /// everything up to five figures; the sub-cent candles in the scale test ask for eight.
    /// </summary>
    private static CryptoCandle Candle(decimal open, decimal high, decimal low, decimal close, byte decimals = 4)
        => new() { TickDecimals = decimals, Open = open, High = high, Low = low, Close = close };


    // ═══════════════════════════════════════════════════════════════════════
    //  The measurements themselves
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void TheGeometry_IsMeasuredAgainstTheCandlesOwnRange()
    {
        // range 100..110, body 102..104, so 20% body, 60% upper wick, 20% lower wick
        CryptoCandle candle = Candle(open: 102m, high: 110m, low: 100m, close: 104m);

        Assert.AreEqual(20m, CandlePatternHelper.BodyPercentage(candle));
        Assert.AreEqual(60m, CandlePatternHelper.UpperWickPercentage(candle));
        Assert.AreEqual(20m, CandlePatternHelper.LowerWickPercentage(candle));
        Assert.IsTrue(CandlePatternHelper.IsBullish(candle));
    }


    /// <summary>A candle with no range at all must not divide by zero, and is no pattern.</summary>
    [TestMethod]
    public void AFlatCandle_IsNotAPattern()
    {
        CryptoCandle flat = Candle(50m, 50m, 50m, 50m);

        Assert.AreEqual(0m, CandlePatternHelper.BodyPercentage(flat));
        Assert.AreEqual(0m, CandlePatternHelper.UpperWickPercentage(flat));
        Assert.IsFalse(CandlePatternHelper.Matches(CryptoCandlePattern.Hammer, CryptoTradeSide.Long,
            flat, null, null, Settings));
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Single-candle shapes
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void AHammer_IsASmallBodyOnTopOfALongLowerWick()
    {
        // range 100..110, body 108..110 (20%), lower wick 80%, no upper wick
        CryptoCandle hammer = Candle(open: 108m, high: 110m, low: 100m, close: 110m);

        Assert.IsTrue(CandlePatternHelper.Matches(CryptoCandlePattern.Hammer, CryptoTradeSide.Long,
            hammer, null, null, Settings));
        Assert.IsFalse(CandlePatternHelper.Matches(CryptoCandlePattern.InvertedHammer, CryptoTradeSide.Long,
            hammer, null, null, Settings), "de lange lont zit onder, niet boven");
    }


    [TestMethod]
    public void ALongUpperWick_DisqualifiesAHammer()
    {
        // the same small body, but now with wicks on both ends: 40% under, 40% over
        CryptoCandle both = Candle(open: 104m, high: 110m, low: 100m, close: 106m);

        Assert.IsFalse(CandlePatternHelper.Matches(CryptoCandlePattern.Hammer, CryptoTradeSide.Long,
            both, null, null, Settings));
    }


    [TestMethod]
    public void AnInvertedHammer_IsTheSameShapeUpsideDown()
    {
        // range 100..110, body 100..102 (20%), upper wick 80%
        CryptoCandle inverted = Candle(open: 100m, high: 110m, low: 100m, close: 102m);

        Assert.IsTrue(CandlePatternHelper.Matches(CryptoCandlePattern.InvertedHammer, CryptoTradeSide.Long,
            inverted, null, null, Settings));
        // A shooting star is that same candle read the other way, so the side must not change it.
        Assert.IsTrue(CandlePatternHelper.Matches(CryptoCandlePattern.InvertedHammer, CryptoTradeSide.Short,
            inverted, null, null, Settings));
    }


    /// <summary>
    /// The reason this helper was written instead of taking a package: a shape has to be recognised
    /// on a coin at 65 000 and on one at 0.01, because the shape is the same. The package that
    /// prompted this failed exactly here.
    /// </summary>
    [TestMethod]
    public void TheSameShape_MatchesAtAnyPriceScale()
    {
        // The same hammer proportions, three price scales apart.
        CryptoCandle expensive = Candle(open: 64_800m, high: 65_000m, low: 64_000m, close: 65_000m);
        CryptoCandle ordinary = Candle(open: 0.54m, high: 0.55m, low: 0.50m, close: 0.55m);
        CryptoCandle cheap = Candle(open: 0.0000108m, high: 0.000011m, low: 0.00001m, close: 0.000011m, decimals: 8);

        foreach (CryptoCandle candle in new[] { expensive, ordinary, cheap })
        {
            Assert.IsTrue(CandlePatternHelper.Matches(CryptoCandlePattern.Hammer, CryptoTradeSide.Long,
                candle, null, null, Settings), $"dezelfde vorm hoort te passen, ook rond {candle.Close}");
        }
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Two- and three-candle shapes
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void AnEngulfing_CoversThePreviousBodyCompletely()
    {
        CryptoCandle previous = Candle(open: 108m, high: 109m, low: 103m, close: 104m);  // red, body 104..108
        CryptoCandle last = Candle(open: 103m, high: 110m, low: 102m, close: 109m);      // green, body 103..109

        Assert.IsTrue(CandlePatternHelper.Matches(CryptoCandlePattern.Engulfing, CryptoTradeSide.Long,
            last, previous, null, Settings));
        Assert.IsFalse(CandlePatternHelper.Matches(CryptoCandlePattern.Engulfing, CryptoTradeSide.Short,
            last, previous, null, Settings), "dit is de bullish lezing, niet de bearish");
    }


    [TestMethod]
    public void ABodyThatFallsShort_IsNotAnEngulfing()
    {
        CryptoCandle previous = Candle(open: 108m, high: 109m, low: 103m, close: 104m);
        CryptoCandle last = Candle(open: 104.5m, high: 110m, low: 104m, close: 107m);   // blijft binnen

        Assert.IsFalse(CandlePatternHelper.Matches(CryptoCandlePattern.Engulfing, CryptoTradeSide.Long,
            last, previous, null, Settings));
    }


    /// <summary>
    /// Two nearly flat candles technically cover each other in both directions. That is noise, and
    /// without the minimum body it would be the most common "pattern" on the chart.
    /// </summary>
    [TestMethod]
    public void TwoNearlyFlatCandles_AreNotAnEngulfing()
    {
        CryptoCandle previous = Candle(open: 100.1m, high: 105m, low: 95m, close: 100m);
        CryptoCandle last = Candle(open: 100m, high: 106m, low: 94m, close: 100.2m);

        Assert.IsFalse(CandlePatternHelper.Matches(CryptoCandlePattern.Engulfing, CryptoTradeSide.Long,
            last, previous, null, Settings));
    }


    [TestMethod]
    public void AHarami_IsAnEngulfingTheOtherWayRound()
    {
        CryptoCandle previous = Candle(open: 110m, high: 111m, low: 99m, close: 100m);   // red, big body
        CryptoCandle last = Candle(open: 103m, high: 108m, low: 102m, close: 106m);      // green, inside it

        Assert.IsTrue(CandlePatternHelper.Matches(CryptoCandlePattern.Harami, CryptoTradeSide.Long,
            last, previous, null, Settings));
        Assert.IsFalse(CandlePatternHelper.Matches(CryptoCandlePattern.Engulfing, CryptoTradeSide.Long,
            last, previous, null, Settings), "harami en engulfing sluiten elkaar uit");
    }


    /// <summary>
    /// A piercing line stops halfway; one that closes past the previous open is an engulfing, and
    /// counting it as both would double every signal.
    /// </summary>
    [TestMethod]
    public void APiercingLine_StopsInsideThePreviousBody()
    {
        CryptoCandle previous = Candle(open: 110m, high: 111m, low: 99m, close: 100m);   // red 100..110
        CryptoCandle piercing = Candle(open: 99m, high: 107m, low: 98m, close: 106m);    // sluit boven het midden
        CryptoCandle tooFar = Candle(open: 99m, high: 112m, low: 98m, close: 111m);      // sluit boven de opening

        Assert.IsTrue(CandlePatternHelper.Matches(CryptoCandlePattern.PiercingLine, CryptoTradeSide.Long,
            piercing, previous, null, Settings));
        Assert.IsFalse(CandlePatternHelper.Matches(CryptoCandlePattern.PiercingLine, CryptoTradeSide.Long,
            tooFar, previous, null, Settings), "die is doorgeschoten, dat is een engulfing");
    }


    [TestMethod]
    public void AMorningStar_NeedsAHesitantCandleInTheMiddle()
    {
        CryptoCandle first = Candle(open: 110m, high: 111m, low: 99m, close: 100m);      // lange rode
        CryptoCandle small = Candle(open: 99m, high: 100m, low: 97m, close: 98.5m);      // klein lichaam
        CryptoCandle last = Candle(open: 99m, high: 107m, low: 98m, close: 106m);        // lange groene

        Assert.IsTrue(CandlePatternHelper.Matches(CryptoCandlePattern.MorningStar, CryptoTradeSide.Long,
            last, small, first, Settings));

        CryptoCandle notSmall = Candle(open: 99m, high: 100m, low: 97m, close: 97.2m);
        Assert.IsFalse(CandlePatternHelper.Matches(CryptoCandlePattern.MorningStar, CryptoTradeSide.Long,
            last, notSmall, first, Settings), "de middelste candle is de aarzeling, die hoort klein te zijn");
    }


    [TestMethod]
    public void ATweezer_AllowsTheTwoLowsToDifferALittle()
    {
        CryptoCandle previous = Candle(open: 108m, high: 109m, low: 100m, close: 102m);  // red
        CryptoCandle sameLow = Candle(open: 102m, high: 107m, low: 100.2m, close: 106m); // green, low bijna gelijk
        CryptoCandle otherLow = Candle(open: 102m, high: 107m, low: 96m, close: 106m);   // green, veel lager

        Assert.IsTrue(CandlePatternHelper.Matches(CryptoCandlePattern.Tweezer, CryptoTradeSide.Long,
            sameLow, previous, null, Settings));
        Assert.IsFalse(CandlePatternHelper.Matches(CryptoCandlePattern.Tweezer, CryptoTradeSide.Long,
            otherLow, previous, null, Settings));
    }


    /// <summary>
    /// A pattern asked for before enough candles exist must say no, not throw - the first candles of
    /// every run are exactly that case.
    /// </summary>
    [TestMethod]
    public void WithoutEnoughCandles_APatternSimplyDoesNotMatch()
    {
        CryptoCandle last = Candle(open: 103m, high: 110m, low: 102m, close: 109m);

        Assert.IsFalse(CandlePatternHelper.Matches(CryptoCandlePattern.Engulfing, CryptoTradeSide.Long,
            last, null, null, Settings));
        Assert.IsFalse(CandlePatternHelper.Matches(CryptoCandlePattern.MorningStar, CryptoTradeSide.Long,
            last, last, null, Settings));
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Symmetry
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Mirrors a candle around a price: what was a low becomes a high and the body flips.</summary>
    private static CryptoCandle Mirror(in CryptoCandle candle, decimal around = 200m)
        => new()
        {
            TickDecimals = 4,
            Open = around - candle.Open,
            High = around - candle.Low,
            Low = around - candle.High,
            Close = around - candle.Close,
        };


    /// <summary>
    /// Hammer and inverted hammer are each other's reflection, and so are long and short. So mirror
    /// the candles, mirror the pattern, mirror the side, and the answer has to be identical - for
    /// EVERY pattern and every candle. Anything that treats one direction differently from the other
    /// breaks this and nothing else would show it.
    /// <para>
    /// This exists because the measured results of the mirror pairs were nowhere near each other
    /// (Hammer +396.55 against InvertedHammer -174.55 on run 525/526, and Harami +384.83 against
    /// Engulfing -149.21). That difference has to come from the market or from the setup, and this
    /// test is what rules out the third possibility: the detection itself.
    /// </para>
    /// </summary>
    [TestMethod]
    public void EveryPattern_IsExactlyItsOwnMirrorImage()
    {
        // Pattern under reflection: only the two hammers swap, the rest map onto themselves.
        Dictionary<CryptoCandlePattern, CryptoCandlePattern> mirrored = new()
        {
            [CryptoCandlePattern.Hammer] = CryptoCandlePattern.InvertedHammer,
            [CryptoCandlePattern.InvertedHammer] = CryptoCandlePattern.Hammer,
            [CryptoCandlePattern.Engulfing] = CryptoCandlePattern.Engulfing,
            [CryptoCandlePattern.Harami] = CryptoCandlePattern.Harami,
            [CryptoCandlePattern.PiercingLine] = CryptoCandlePattern.PiercingLine,
            [CryptoCandlePattern.MorningStar] = CryptoCandlePattern.MorningStar,
            [CryptoCandlePattern.Tweezer] = CryptoCandlePattern.Tweezer,
        };

        // A spread of shapes: hammers, engulfings, haramis, stars, tweezers and plain candles, so
        // every branch is actually reached in both directions.
        CryptoCandle[] shapes =
        [
            Candle(108m, 110m, 100m, 110m),     // hammer
            Candle(100m, 110m, 100m, 102m),     // inverted hammer
            Candle(108m, 109m, 103m, 104m),     // red body
            Candle(103m, 110m, 102m, 109m),     // green body covering it
            Candle(110m, 111m, 99m, 100m),      // long red
            Candle(103m, 108m, 102m, 106m),     // small green inside
            Candle(99m, 100m, 97m, 98.5m),      // small body
            Candle(99m, 107m, 98m, 106m),       // long green
            Candle(102m, 107m, 100.2m, 106m),   // equal-ish low
            Candle(104m, 110m, 100m, 106m),     // wicks both ends
        ];

        int checks = 0;
        foreach (CryptoCandlePattern pattern in Enum.GetValues<CryptoCandlePattern>())
        {
            foreach (CryptoTradeSide side in new[] { CryptoTradeSide.Long, CryptoTradeSide.Short })
            {
                for (int i = 2; i < shapes.Length; i++)
                {
                    bool straight = CandlePatternHelper.Matches(pattern, side,
                        shapes[i], shapes[i - 1], shapes[i - 2], Settings);

                    CryptoTradeSide otherSide = side == CryptoTradeSide.Long
                        ? CryptoTradeSide.Short : CryptoTradeSide.Long;
                    bool reflected = CandlePatternHelper.Matches(mirrored[pattern], otherSide,
                        Mirror(shapes[i]), Mirror(shapes[i - 1]), Mirror(shapes[i - 2]), Settings);

                    Assert.AreEqual(straight, reflected,
                        $"{pattern}/{side} op candle {i} wijkt af van {mirrored[pattern]}/{otherSide} gespiegeld");
                    checks++;
                }
            }
        }

        Assert.AreEqual(112, checks, "alle zeven patronen maal twee zijden maal acht candles");
    }


    /// <summary>
    /// And the geometry underneath it: mirroring a candle swaps its two wicks and leaves the body
    /// alone. If that does not hold, the test above is comparing two different shapes.
    /// </summary>
    [TestMethod]
    public void MirroringACandle_SwapsItsWicks()
    {
        CryptoCandle candle = Candle(open: 102m, high: 110m, low: 100m, close: 104m);
        CryptoCandle flipped = Mirror(candle);

        Assert.AreEqual(CandlePatternHelper.BodyPercentage(candle), CandlePatternHelper.BodyPercentage(flipped));
        Assert.AreEqual(CandlePatternHelper.UpperWickPercentage(candle), CandlePatternHelper.LowerWickPercentage(flipped));
        Assert.AreEqual(CandlePatternHelper.LowerWickPercentage(candle), CandlePatternHelper.UpperWickPercentage(flipped));
        Assert.AreEqual(CandlePatternHelper.IsBullish(candle), CandlePatternHelper.IsBearish(flipped));
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Several shapes at once
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A candle that is both an engulfing and a tweezer, which is what makes the order in the list
    /// visible: the first shape that fits is the one reported, and that name ends up in the signal.
    /// </summary>
    [TestMethod]
    public void MatchesAny_ReportsTheFirstShapeInTheListThatFits()
    {
        CryptoCandle previous = Candle(open: 105m, high: 106m, low: 100m, close: 102m);   // red, body 50%
        CryptoCandle last = Candle(open: 101m, high: 108m, low: 100.1m, close: 107m);     // green, covers it, same low

        Assert.IsTrue(CandlePatternHelper.MatchesAny(["Engulfing", "Tweezer"], CryptoTradeSide.Long,
            last, previous, null, Settings, "Patterns", out CryptoCandlePattern first));
        Assert.AreEqual(CryptoCandlePattern.Engulfing, first);

        Assert.IsTrue(CandlePatternHelper.MatchesAny(["Tweezer", "Engulfing"], CryptoTradeSide.Long,
            last, previous, null, Settings, "Patterns", out CryptoCandlePattern second));
        Assert.AreEqual(CryptoCandlePattern.Tweezer, second);
    }


    /// <summary>
    /// The point of the list: a shape that does not fit does not stop the ones behind it. Without
    /// this the strategy would be no better off than with the single choice it replaces.
    /// </summary>
    [TestMethod]
    public void MatchesAny_KeepsLookingAfterAShapeThatDoesNotFit()
    {
        CryptoCandle previous = Candle(open: 110m, high: 111m, low: 99m, close: 100m);    // red, big body
        CryptoCandle last = Candle(open: 103m, high: 108m, low: 102m, close: 106m);       // green, inside it

        // Harami and engulfing exclude each other, so the first name in this list cannot match.
        Assert.IsTrue(CandlePatternHelper.MatchesAny(["Engulfing", "Harami"], CryptoTradeSide.Long,
            last, previous, null, Settings, "Patterns", out CryptoCandlePattern matched));
        Assert.AreEqual(CryptoCandlePattern.Harami, matched);
    }


    /// <summary>
    /// A three-candle shape in the list while only two candles exist must not take the other shapes
    /// down with it - that is every run's opening candles.
    /// </summary>
    [TestMethod]
    public void MatchesAny_WithAThreeCandleShapeAndNoThirdCandle_StillMatchesTheOthers()
    {
        CryptoCandle previous = Candle(open: 110m, high: 111m, low: 99m, close: 100m);
        CryptoCandle last = Candle(open: 103m, high: 108m, low: 102m, close: 106m);

        Assert.IsTrue(CandlePatternHelper.MatchesAny(["MorningStar", "Harami"], CryptoTradeSide.Long,
            last, previous, null, Settings, "Patterns", out CryptoCandlePattern matched));
        Assert.AreEqual(CryptoCandlePattern.Harami, matched);

        Assert.IsFalse(CandlePatternHelper.MatchesAny(["MorningStar"], CryptoTradeSide.Long,
            last, previous, null, Settings, "Patterns", out _));
    }


    [TestMethod]
    public void MatchesAny_WithNothingSelected_DoesNotMatch()
    {
        CryptoCandle previous = Candle(open: 110m, high: 111m, low: 99m, close: 100m);
        CryptoCandle last = Candle(open: 103m, high: 108m, low: 102m, close: 106m);

        Assert.IsFalse(CandlePatternHelper.MatchesAny([], CryptoTradeSide.Long,
            last, previous, null, Settings, "Patterns", out _));
    }


    /// <summary>
    /// A typo has to be loud. Silently skipping the name would reject every signal and read exactly
    /// like "the strategy produced nothing", and the message has to name the setting to correct.
    /// </summary>
    [TestMethod]
    public void MatchesAny_OnAnUnknownName_Throws()
    {
        CryptoCandle previous = Candle(open: 110m, high: 111m, low: 99m, close: 100m);
        CryptoCandle last = Candle(open: 103m, high: 108m, low: 102m, close: 106m);

        var thrown = Assert.ThrowsExactly<InvalidOperationException>(() =>
            CandlePatternHelper.MatchesAny(["Hamer", "Harami"], CryptoTradeSide.Long,
                last, previous, null, Settings, "Patterns", out _));

        StringAssert.Contains(thrown.Message, "Hamer");
        StringAssert.Contains(thrown.Message, "Patterns");
    }
}
