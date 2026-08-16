using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal.Indicators;

namespace CryptoScanner.CoreTests.Analyzer.Indicators;

/// <summary>
/// Validates <see cref="BandRangeTracker"/>: the band-width median, the favourable/adverse
/// excursion ratio, and the rule that no number is shown while there is too little to say.
/// </summary>
[TestClass]
public class BandRangeTrackerTests
{
    private static CryptoCandle MakeCandle(int index, decimal low, decimal high, decimal close)
    {
        return new CryptoCandle
        {
            TickDecimals = 4,
            OpenTime = new CandleTime((uint)(index * 60)),
            Open = close,
            High = high,
            Low = low,
            Close = close,
            Volume = 1000m,
        };
    }

    /// <summary>Bands that sit a fixed percentage around a fixed middle line, so the expected
    /// width is known exactly.</summary>
    private static void FeedFlatBands(BandRangeTracker tracker, int count,
        double middle = 100.0, double halfWidth = 2.0, decimal close = 100m)
    {
        for (int i = 0; i < count; i++)
        {
            var candle = MakeCandle(i, close, close, close);
            tracker.Add(candle, middle, middle + halfWidth, middle - halfWidth);
        }
    }


    [TestMethod]
    public void MedianWidth_Is_Null_Until_Enough_Candles()
    {
        BandRangeTracker tracker = new();
        FeedFlatBands(tracker, 50);
        Assert.IsNull(tracker.MedianWidth, "Fewer than 100 candles is not a representative median");

        FeedFlatBands(tracker, 60);
        Assert.IsNotNull(tracker.MedianWidth, "Past 100 candles the median must appear");
    }


    [TestMethod]
    public void MedianWidth_Matches_The_Stored_BollingerBandsPercentage()
    {
        BandRangeTracker tracker = new();

        // Bands at 98 / 102 → 100 * (102 / 98 - 1) = 4.0816%, the same definition the scanner
        // already stores in CryptoData.BollingerBandsPercentage.
        FeedFlatBands(tracker, 150, middle: 100.0, halfWidth: 2.0);

        Assert.AreEqual(100.0 * (102.0 / 98.0 - 1.0), tracker.MedianWidth!.Value, 0.0001,
            "Width must use the same formula as BollingerBandsPercentage");
    }


    [TestMethod]
    public void Index_Stays_Null_Below_The_Minimum_Number_Of_Measurements()
    {
        BandRangeTracker tracker = new();
        FeedFlatBands(tracker, 200);

        Assert.IsNotNull(tracker.MedianWidth, "The width itself is available");
        Assert.AreEqual(0, tracker.MeasurementCount, "Price never touched a band");
        Assert.IsNull(tracker.Ratio, "No excursions means no ratio");
        Assert.IsNull(tracker.Index, "And therefore no index — not a misleading zero");
    }


    [TestMethod]
    public void A_Touch_Followed_By_A_Return_To_The_Middle_Closes_One_Measurement()
    {
        BandRangeTracker tracker = new();
        const double middle = 100.0;
        const double upper = 102.0;
        const double lower = 98.0;

        // Candle 0 closes on the lower band → starts a long measurement at 98.
        tracker.Add(MakeCandle(0, 98m, 98m, 98m), middle, upper, lower);
        Assert.AreEqual(0, tracker.MeasurementCount, "The measurement is running, not finished");

        // Candle 1 dips to 97 (adverse) and closes back at the middle line → closes it.
        tracker.Add(MakeCandle(1, 97m, 100m, 100m), middle, upper, lower);
        Assert.AreEqual(1, tracker.MeasurementCount, "Reaching the middle line closes the measurement");
    }


    [TestMethod]
    public void Ratio_Divides_Average_Favourable_By_Average_Adverse()
    {
        BandRangeTracker tracker = new();
        const double middle = 100.0;
        const double upper = 102.0;
        const double lower = 98.0;

        // Ten identical long measurements: entry 100 (on the band), then a candle running from
        // 99 to 102 that closes at the middle line. Favourable = +2%, adverse = -1%, so 2.00.
        for (int i = 0; i < 20; i += 2)
        {
            tracker.Add(MakeCandle(i, 100m, 100m, 100m), middle, upper, lower + 2.0);
            tracker.Add(MakeCandle(i + 1, 99m, 102m, 100m), middle, upper, lower);
        }

        Assert.AreEqual(10, tracker.MeasurementCount, "Every pair of candles is one measurement");
        Assert.AreEqual(2.0, tracker.Ratio!.Value, 0.0001,
            "2% favourable against 1% adverse is a ratio of 2");
    }


    [TestMethod]
    public void Measurement_Closes_When_The_Hold_Expires_Without_Reaching_The_Middle()
    {
        BandRangeTracker tracker = new();
        const double middle = 100.0;
        const double upper = 102.0;
        const double lower = 98.0;

        tracker.Add(MakeCandle(0, 98m, 98m, 98m), middle, upper, lower);

        // Never returns to the middle line; MaximumHold candles later it is closed anyway.
        for (int i = 1; i <= BandRangeTracker.MaximumHold; i++)
            tracker.Add(MakeCandle(i, 97m, 97.5m, 97m), middle, upper, lower);

        Assert.AreEqual(1, tracker.MeasurementCount,
            "An excursion that never comes back is closed at whatever it reached");
    }


    [TestMethod]
    public void Measurements_On_The_Same_Side_Never_Overlap()
    {
        BandRangeTracker tracker = new();
        const double middle = 100.0;
        const double upper = 102.0;
        const double lower = 98.0;

        // Five candles in a row all close on the lower band. That is one running measurement,
        // not five — the offline calculation resumes only after the previous one closed.
        for (int i = 0; i < 5; i++)
            tracker.Add(MakeCandle(i, 98m, 98m, 98m), middle, upper, lower);

        // Back to the middle line closes exactly one.
        tracker.Add(MakeCandle(5, 98m, 100m, 100m), middle, upper, lower);

        Assert.AreEqual(1, tracker.MeasurementCount, "Overlapping touches must not each count");
    }


    [TestMethod]
    public void Index_Is_Width_Times_Ratio()
    {
        GlobalData.Settings = new SettingsBasic();
        BandRangeTracker tracker = new();
        const double middle = 100.0;
        const double upper = 102.0;
        const double lower = 98.0;

        // Enough candles for the width median, and enough round trips for the ratio.
        for (int i = 0; i < 200; i += 2)
        {
            tracker.Add(MakeCandle(i, 98m, 98m, 98m), middle, upper, lower);
            tracker.Add(MakeCandle(i + 1, 97m, 100m, 100m), middle, upper, lower);
        }

        Assert.IsNotNull(tracker.Index);
        Assert.AreEqual(tracker.MedianWidth!.Value * tracker.Ratio!.Value, tracker.Index!.Value, 0.0001,
            "The index is the product of the two, nothing else");
    }
}
