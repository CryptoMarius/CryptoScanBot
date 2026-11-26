using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.CoreTests.Zones;

/// <summary>
/// The impulse volume of a gap: volume of the middle candle against the median volume of the 20
/// candles before it (ZonesFvg.MinimumImpulseVolumeRatio, open point I25).
/// </summary>
[TestClass]
public class ZoneFvgImpulseVolumeTests
{
    private static readonly CryptoInterval Hour = CryptoInterval.CreateStandardIntervalList()
        .First(interval => interval.IntervalPeriod == CryptoIntervalPeriod.interval1h);

    private static readonly CandleTime Start = CandleTime.AlignFromDateTime(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), 60);

    private static CryptoCandle Candle(int hour, decimal volume)
        => new()
        {
            OpenTime = Start + (uint)(hour * 60),
            Open = 100m,
            High = 101m,
            Low = 99m,
            Close = 100m,
            Volume = volume,
        };

    /// <summary>Twenty candles before the impulse with the given volumes, oldest first, then the impulse at hour 20.</summary>
    private static (CryptoCandleList list, CryptoCandle impulse) Series(Func<int, decimal> volumeAt, decimal impulseVolume, params int[] missingHours)
    {
        CryptoCandleList list = [];
        for (int hour = 0; hour < 20; hour++)
        {
            if (!missingHours.Contains(hour))
                list.Add(Candle(hour, volumeAt(hour)).OpenTime, Candle(hour, volumeAt(hour)));
        }
        CryptoCandle impulse = Candle(20, impulseVolume);
        list.Add(impulse.OpenTime, impulse);
        return (list, impulse);
    }


    [TestMethod]
    public void Ratio_IsImpulseVolumeOverMedianOfTheTwentyBefore()
    {
        // Volumes 1..20 before the impulse: median of an even count = (10 + 11) / 2 = 10.5.
        var (list, impulse) = Series(hour => hour + 1, 21m);

        double? ratio = ZoneFvg.ImpulseVolumeRatio(list, Hour, impulse);

        Assert.IsNotNull(ratio);
        Assert.AreEqual(21.0 / 10.5, ratio.Value, 1e-9);
    }


    [TestMethod]
    public void Ratio_OneSpikeBeforeDoesNotMoveTheMedian()
    {
        // A median, not an average: one huge candle in the lookback leaves the baseline at 100.
        var (list, impulse) = Series(hour => hour == 5 ? 1_000_000m : 100m, 150m);

        double? ratio = ZoneFvg.ImpulseVolumeRatio(list, Hour, impulse);

        Assert.AreEqual(1.5, ratio!.Value, 1e-9);
    }


    [TestMethod]
    public void Ratio_HolesInTheSeriesAreSkipped()
    {
        var (list, impulse) = Series(hour => 100m, 300m, 0, 1, 2, 3, 4);

        double? ratio = ZoneFvg.ImpulseVolumeRatio(list, Hour, impulse);

        Assert.AreEqual(3.0, ratio!.Value, 1e-9);
    }


    [TestMethod]
    public void Ratio_TooFewCandlesBeforeIsUnknown()
    {
        // Only 9 of the 20 candles are there: not enough to say what normal volume is.
        var (list, impulse) = Series(hour => 100m, 300m, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

        Assert.IsNull(ZoneFvg.ImpulseVolumeRatio(list, Hour, impulse));
    }


    [TestMethod]
    public void Ratio_ZeroMedianIsUnknown()
    {
        var (list, impulse) = Series(hour => 0m, 300m);

        Assert.IsNull(ZoneFvg.ImpulseVolumeRatio(list, Hour, impulse));
    }
}
