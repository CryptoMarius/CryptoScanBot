using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// Tests for CandleHelpers.Clamp and CandleHelpers.ClampPrice.
/// <para>
/// The two exist side by side on purpose. A QUANTITY always rounds down: up could cost more than the
/// balance holds, and the exchange rejects anything that is not a multiple of the step. A PRICE
/// follows Settings.Trading.PriceRounding, and three of its four settings need to know which side
/// the trade is - "up" is what a long does not want and what a short does.
/// </para>
/// </summary>
[TestClass]
public class ClampTests
{
    private const decimal NoMinimum = 0m;
    private const decimal NoMaximum = 0m;
    private const decimal Tick = 0.1m;

    // Pin the setting, so a test never depends on what a previous test left behind.
    [TestInitialize]
    public void SetTheRoundingRule()
    {
        GlobalData.Settings.Trading.PriceRounding = CryptoPriceRounding.AgainstPosition;
    }

    private static decimal Price(decimal value, CryptoTradeSide side, CryptoPriceRounding rounding)
    {
        GlobalData.Settings.Trading.PriceRounding = rounding;
        return value.ClampPrice(side, NoMinimum, NoMaximum, Tick);
    }

    [TestMethod]
    public void AQuantityAlwaysRoundsDownWhateverTheSettingSays()
    {
        foreach (CryptoPriceRounding rounding in Enum.GetValues<CryptoPriceRounding>())
        {
            GlobalData.Settings.Trading.PriceRounding = rounding;
            Assert.AreEqual(1.2m, 1.26m.Clamp(NoMinimum, NoMaximum, Tick), $"quantity under {rounding}");
            Assert.AreEqual(1.2m, 1.25m.Clamp(NoMinimum, NoMaximum, Tick), $"quantity halfway under {rounding}");
        }
    }

    /// <summary>
    /// The setting Marius asked for: long up, short down. Both sides get the same treatment, and it
    /// is the unfavourable one - the entry is bought dearer or sold cheaper, the target moves further
    /// away and the stop moves closer to the entry.
    /// </summary>
    [TestMethod]
    public void AgainstPositionRoundsALongUpAndAShortDown()
    {
        Assert.AreEqual(1.3m, Price(1.24m, CryptoTradeSide.Long, CryptoPriceRounding.AgainstPosition), "long rounds up");
        Assert.AreEqual(1.2m, Price(1.26m, CryptoTradeSide.Short, CryptoPriceRounding.AgainstPosition), "short rounds down");
    }

    [TestMethod]
    public void FavourPositionIsTheMirrorImage()
    {
        Assert.AreEqual(1.2m, Price(1.26m, CryptoTradeSide.Long, CryptoPriceRounding.FavourPosition), "long rounds down");
        Assert.AreEqual(1.3m, Price(1.24m, CryptoTradeSide.Short, CryptoPriceRounding.FavourPosition), "short rounds up");
    }

    /// <summary>The one switch that puts everything back exactly as it was before 22-08-2026.</summary>
    [TestMethod]
    public void DownPutsBothSidesBackOnTheOriginalRule()
    {
        Assert.AreEqual(1.2m, Price(1.26m, CryptoTradeSide.Long, CryptoPriceRounding.Down));
        Assert.AreEqual(1.2m, Price(1.26m, CryptoTradeSide.Short, CryptoPriceRounding.Down));
        Assert.AreEqual(1.2m, Price(1.24m, CryptoTradeSide.Long, CryptoPriceRounding.Down));
        Assert.AreEqual(1.2m, Price(1.24m, CryptoTradeSide.Short, CryptoPriceRounding.Down));
    }

    [TestMethod]
    public void NearestIgnoresTheSideAndPicksTheCloserTick()
    {
        foreach (CryptoTradeSide side in new[] { CryptoTradeSide.Long, CryptoTradeSide.Short })
        {
            Assert.AreEqual(1.2m, Price(1.24m, side, CryptoPriceRounding.Nearest), $"{side} nearer the tick below");
            Assert.AreEqual(1.3m, Price(1.26m, side, CryptoPriceRounding.Nearest), $"{side} nearer the tick above");
            Assert.AreEqual(1.3m, Price(1.25m, side, CryptoPriceRounding.Nearest), $"{side} exactly halfway rounds up");
        }
    }

    /// <summary>
    /// A value that is already on a tick may never move, whichever setting is chosen. Rounding it
    /// "up" would add a whole tick out of nowhere, which is a far bigger error than the one all of
    /// this is about.
    /// </summary>
    [TestMethod]
    public void AValueAlreadyOnATickNeverMoves()
    {
        foreach (CryptoPriceRounding rounding in Enum.GetValues<CryptoPriceRounding>())
        {
            foreach (CryptoTradeSide side in new[] { CryptoTradeSide.Long, CryptoTradeSide.Short })
                Assert.AreEqual(1.2m, Price(1.2m, side, rounding), $"{rounding} {side}");
        }
    }

    /// <summary>
    /// What the whole discussion was about: under the original rule a long and a short are pulled
    /// apart, under the other three they are not. Both prices sit the same distance either side of
    /// an anchor, so an even-handed rule has to keep those two distances equal.
    /// </summary>
    [TestMethod]
    public void OnlyTheOriginalRuleTreatsTheTwoSidesDifferently()
    {
        const decimal anchor = 100m;
        const decimal offset = 2.034m;

        foreach (CryptoPriceRounding rounding in Enum.GetValues<CryptoPriceRounding>())
        {
            decimal longTarget = Price(anchor + offset, CryptoTradeSide.Long, rounding);
            decimal shortTarget = Price(anchor - offset, CryptoTradeSide.Short, rounding);
            decimal longDistance = longTarget - anchor;
            decimal shortDistance = anchor - shortTarget;

            if (rounding == CryptoPriceRounding.Down)
                Assert.AreNotEqual(longDistance, shortDistance, "the original rule pulls the two sides apart");
            else
                Assert.AreEqual(longDistance, shortDistance, $"{rounding} has to treat both sides alike");
        }
    }

    /// <summary>
    /// The size of it, over a spread of values inside one tick. Down and the two direction-aware
    /// settings all shift by half a tick on average - the point of those two is not that they are
    /// smaller but that both sides get the same shift. Nearest is the only one without a lean.
    /// <para>
    /// Half of the 0.1 tick would be 0.05; the numbers below are 0.0495 because a value that is
    /// already on a tick does not move, and one in every hundred sampled values is. Nearest keeps
    /// +0.0005, and that residue is the sampling and not the rule: the value exactly halfway rounds
    /// up, so the grid itself leans up by half a sample step.
    /// </para>
    /// </summary>
    [TestMethod]
    public void EveryRuleButNearestShiftsByHalfATickOnAverage()
    {
        foreach (var (rounding, side, expected) in new[]
        {
            (CryptoPriceRounding.Down, CryptoTradeSide.Long, -0.0495m),
            (CryptoPriceRounding.Down, CryptoTradeSide.Short, -0.0495m),
            (CryptoPriceRounding.AgainstPosition, CryptoTradeSide.Long, +0.0495m),
            (CryptoPriceRounding.AgainstPosition, CryptoTradeSide.Short, -0.0495m),
            (CryptoPriceRounding.FavourPosition, CryptoTradeSide.Long, -0.0495m),
            (CryptoPriceRounding.FavourPosition, CryptoTradeSide.Short, +0.0495m),
            (CryptoPriceRounding.Nearest, CryptoTradeSide.Long, +0.0005m),
        })
        {
            decimal total = 0m;
            int count = 0;
            // A hundred values a tenth of a tick apart, so every position within a tick is visited.
            for (decimal step = 0m; step < 1m; step += 0.001m)
            {
                decimal value = 100m + step;
                total += Price(value, side, rounding) - value;
                count++;
            }
            Assert.AreEqual(expected, total / count, $"{rounding} {side}");
        }
    }

    [TestMethod]
    public void TheMinimumAndMaximumStillApplyAfterRounding()
    {
        GlobalData.Settings.Trading.PriceRounding = CryptoPriceRounding.AgainstPosition;
        Assert.AreEqual(5m, 1.26m.ClampPrice(CryptoTradeSide.Long, 5m, 10m, Tick), "below the minimum");
        Assert.AreEqual(10m, 99.9m.ClampPrice(CryptoTradeSide.Long, 5m, 10m, Tick), "above the maximum");
        // A maximum of zero means "no maximum", the same as it does for Clamp.
        Assert.AreEqual(100m, 99.94m.ClampPrice(CryptoTradeSide.Long, 0m, 0m, Tick), "no maximum");
    }

    [TestMethod]
    public void WithoutATickSizeNothingIsRounded()
    {
        Assert.AreEqual(1.23456m, 1.23456m.ClampPrice(CryptoTradeSide.Long, NoMinimum, NoMaximum, null));
        Assert.AreEqual(1.23456m, 1.23456m.Clamp(NoMinimum, NoMaximum, null));
    }
}
