using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Model;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// The barometer line over the price chart takes, per candle, the last stored value from before the
/// close of that candle. It must never show a value that was only known later, and a hole in the
/// stored history must stay a hole instead of becoming a flat line.
/// </summary>
[TestClass]
public class BarometerChartOverlayTests
{
    private static CandleTime T(uint minutes) => new(minutes);


    [TestMethod]
    public void TakesTheLastValueBeforeTheClose()
    {
        // arrange - values per minute, 1h candles
        List<(CandleTime, decimal)> stored = [(T(600), 10m), (T(630), 20m), (T(659), 30m), (T(660), 40m)];
        List<CandleTime> opens = [T(600), T(660)];

        // act
        var points = BarometerChartOverlay.Sample(stored, opens, 60, BarometerChartOverlay.MaxHoldMinutes);

        // assert - the candle 600..660 ends before the value stamped 660
        Assert.AreEqual(2, points.Count);
        Assert.AreEqual(30d, points[0].Value);
        Assert.AreEqual(40d, points[1].Value);
    }


    [TestMethod]
    public void NoValueBeforeTheFirstStoredOne()
    {
        // arrange
        List<(CandleTime, decimal)> stored = [(T(700), 55m)];
        List<CandleTime> opens = [T(600), T(660), T(720)];

        // act
        var points = BarometerChartOverlay.Sample(stored, opens, 60, BarometerChartOverlay.MaxHoldMinutes);

        // assert - the candle 600..660 closes before minute 700 and gets nothing
        Assert.AreEqual(2, points.Count);
        Assert.AreEqual(T(660), points[0].Time);
        Assert.AreEqual(T(720), points[1].Time);
    }


    [TestMethod]
    public void HourlyHistoryBridgesTheHourOnMinuteCandles()
    {
        // arrange - the thinned part of the history: one value per whole hour, 5m candles
        List<(CandleTime, decimal)> stored = [(T(600), 60m), (T(660), 70m)];
        List<CandleTime> opens = [T(600), T(630), T(655), T(660)];

        // act
        var points = BarometerChartOverlay.Sample(stored, opens, 5, BarometerChartOverlay.MaxHoldMinutes);

        // assert
        Assert.AreEqual(4, points.Count);
        Assert.AreEqual(60d, points[0].Value);
        Assert.AreEqual(60d, points[1].Value);
        Assert.AreEqual(60d, points[2].Value);
        Assert.AreEqual(70d, points[3].Value);
    }


    [TestMethod]
    public void ALongHoleStaysAHole()
    {
        // arrange - nothing stored for three hours (the scanner was not running)
        List<(CandleTime, decimal)> stored = [(T(600), 60m), (T(840), 80m)];
        List<CandleTime> opens = [T(600), T(660), T(720), T(780), T(840)];

        // act
        var points = BarometerChartOverlay.Sample(stored, opens, 60, BarometerChartOverlay.MaxHoldMinutes);

        // assert - 600 and 660 still lean on minute 600, 720 and 780 are too far from it
        Assert.AreEqual(3, points.Count);
        Assert.AreEqual(T(600), points[0].Time);
        Assert.AreEqual(T(660), points[1].Time);
        Assert.AreEqual(T(840), points[2].Time);
    }
}
