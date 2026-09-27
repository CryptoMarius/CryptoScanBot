using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// The Keltner/PSAR profit lock (open point 48): the stop follows the lower of the lower Keltner band
/// and the SAR, jumps to the upper band when the whole candle is above it, never goes below the fixed
/// lock level and only ever moves towards the take profit. Short is the mirror image.
/// </summary>
[TestClass]
public class KeltnerPsarStopTests
{
    private const decimal Tick = 0.01m;

    [TestMethod]
    public void LongTakesTheLowerOfBandAndSar()
    {
        // lower band 98, SAR 97 -> 97 - tick
        decimal stop = ProfitLockCalculator.KeltnerPsarStop(CryptoTradeSide.Long,
            candleLow: 100m, candleHigh: 103m, keltnerLower: 98m, keltnerUpper: 104m, psar: 97m,
            Tick, floor: 90m, currentTrailingStop: 0m);
        Assert.AreEqual(96.99m, stop);
    }

    [TestMethod]
    public void LongJumpsToTheUpperBandWhenTheCandleIsAboveIt()
    {
        // low 105 above the upper band 104 -> 104 - tick
        decimal stop = ProfitLockCalculator.KeltnerPsarStop(CryptoTradeSide.Long,
            candleLow: 105m, candleHigh: 107m, keltnerLower: 98m, keltnerUpper: 104m, psar: 97m,
            Tick, floor: 90m, currentTrailingStop: 0m);
        Assert.AreEqual(103.99m, stop);
    }

    [TestMethod]
    public void LongNeverBelowTheFloor()
    {
        decimal stop = ProfitLockCalculator.KeltnerPsarStop(CryptoTradeSide.Long,
            candleLow: 100m, candleHigh: 103m, keltnerLower: 98m, keltnerUpper: 104m, psar: 97m,
            Tick, floor: 99m, currentTrailingStop: 0m);
        Assert.AreEqual(99m, stop);
    }

    [TestMethod]
    public void LongNeverMovesBack()
    {
        decimal stop = ProfitLockCalculator.KeltnerPsarStop(CryptoTradeSide.Long,
            candleLow: 100m, candleHigh: 103m, keltnerLower: 98m, keltnerUpper: 104m, psar: 97m,
            Tick, floor: 90m, currentTrailingStop: 101m);
        Assert.AreEqual(101m, stop);
    }

    [TestMethod]
    public void ShortIsTheMirror()
    {
        // upper band 102, SAR 103 -> 103 + tick
        decimal stop = ProfitLockCalculator.KeltnerPsarStop(CryptoTradeSide.Short,
            candleLow: 97m, candleHigh: 100m, keltnerLower: 96m, keltnerUpper: 102m, psar: 103m,
            Tick, floor: 110m, currentTrailingStop: 0m);
        Assert.AreEqual(103.01m, stop);

        // the whole candle below the lower band -> lower band + tick
        stop = ProfitLockCalculator.KeltnerPsarStop(CryptoTradeSide.Short,
            candleLow: 93m, candleHigh: 95m, keltnerLower: 96m, keltnerUpper: 102m, psar: 103m,
            Tick, floor: 110m, currentTrailingStop: 0m);
        Assert.AreEqual(96.01m, stop);

        // floor and ratchet
        stop = ProfitLockCalculator.KeltnerPsarStop(CryptoTradeSide.Short,
            candleLow: 97m, candleHigh: 100m, keltnerLower: 96m, keltnerUpper: 102m, psar: 103m,
            Tick, floor: 101m, currentTrailingStop: 0m);
        Assert.AreEqual(101m, stop);
        stop = ProfitLockCalculator.KeltnerPsarStop(CryptoTradeSide.Short,
            candleLow: 97m, candleHigh: 100m, keltnerLower: 96m, keltnerUpper: 102m, psar: 103m,
            Tick, floor: 110m, currentTrailingStop: 99m);
        Assert.AreEqual(99m, stop);
    }
}
