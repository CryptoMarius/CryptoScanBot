using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;
using CryptoScanner.Emulator.Engine;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// When the replay measures the market trend of a quote coin.
/// <para>
/// From 19-09-2026 it was measured every replayed minute, for every coin and for both zigzag
/// settings, and when the barometer series of an earlier run was read back the stored market trend
/// then overwrote it. 30,8 million trend calculations per run for nothing: 2.023s of the 2.838s run
/// 1870 took, and a run of 20 minutes became one of 53. Since 03-10-2026 it is only measured when the
/// stored series did not hand it back, or when the trader reads the trend that measuring leaves on
/// every coin.
/// </para>
/// </summary>
[TestClass]
public class BarometerMarketTrendSkipTests
{
    private readonly Dictionary<CryptoTradeSide, SettingsCompiled?> saved = [];


    [TestInitialize]
    public void Setup()
    {
        foreach (CryptoTradeSide side in new[] { CryptoTradeSide.Long, CryptoTradeSide.Short })
        {
            saved[side] = TradingConfig.Trading.TryGetValue(side, out var existing) ? existing : null;
            TradingConfig.Trading[side] = new SettingsCompiled();
        }
    }


    [TestCleanup]
    public void Cleanup()
    {
        foreach ((CryptoTradeSide side, SettingsCompiled? previous) in saved)
        {
            if (previous == null)
                TradingConfig.Trading.Remove(side);
            else
                TradingConfig.Trading[side] = previous;
        }
    }


    /// <summary>Every interval came back from the stored series with its market trend: nothing to measure.</summary>
    [TestMethod]
    public void NothingIsMeasuredWhenTheSeriesHandedEverythingBack()
    {
        Assert.IsFalse(BarometerReplay.MustMeasureMarketTrend(0, traderReadsTrend: false));
    }


    /// <summary>
    /// One interval without a stored market trend - a series written before the market trend existed,
    /// or a minute the earlier run never reached - is measured, so it does not stay empty.
    /// </summary>
    [TestMethod]
    public void AnIntervalWithoutAStoredMarketTrendIsMeasured()
    {
        Assert.IsTrue(BarometerReplay.MustMeasureMarketTrend(1, traderReadsTrend: false));
    }


    /// <summary>
    /// The trader checks its trend conditions on the trend stored on the coin. Measuring the market
    /// trend is what keeps that current every minute, so with such a condition set it still runs.
    /// </summary>
    [TestMethod]
    public void ATraderTrendConditionKeepsTheMeasurementGoing()
    {
        Assert.IsTrue(BarometerReplay.MustMeasureMarketTrend(0, traderReadsTrend: true));
    }


    /// <summary>
    /// A $BMX candle stored before the market trend existed has zero in both fields. That is a
    /// minute nobody measured, not a flat market, so it has to be measured instead of read.
    /// </summary>
    [TestMethod]
    public void TwoZeroesMeanTheMarketTrendWasNeverStored()
    {
        Assert.IsFalse(BarometerReplay.MarketTrendWasStored(0m, 0m));
        Assert.IsFalse(BarometerReplay.MarketTrendWasStored(null, null));
        Assert.IsTrue(BarometerReplay.MarketTrendWasStored(12.5m, 0m));
        Assert.IsTrue(BarometerReplay.MarketTrendWasStored(0m, -3.25m));
    }


    [TestMethod]
    public void WithoutTraderTrendConditionsTheCachedTrendIsNotRead()
    {
        Assert.IsFalse(BarometerReplay.TraderReadsCachedTrend());
    }


    [TestMethod]
    public void AnIntervalTrendConditionReadsTheCachedTrend()
    {
        TradingConfig.Trading[CryptoTradeSide.Long].Trend[CryptoIntervalPeriod.interval1h] = CryptoTrendIndicator.Bullish;

        Assert.IsTrue(BarometerReplay.TraderReadsCachedTrend());
    }


    [TestMethod]
    public void ASymbolTrendRangeReadsTheCachedTrend()
    {
        TradingConfig.Trading[CryptoTradeSide.Short].SymbolTrend.Add((-100m, 0m));

        Assert.IsTrue(BarometerReplay.TraderReadsCachedTrend());
    }


    [TestMethod]
    public void ASecondarySymbolTrendRangeReadsTheCachedTrend()
    {
        TradingConfig.Trading[CryptoTradeSide.Long].SymbolTrendSecondary.Add((0m, 100m));

        Assert.IsTrue(BarometerReplay.TraderReadsCachedTrend());
    }
}
