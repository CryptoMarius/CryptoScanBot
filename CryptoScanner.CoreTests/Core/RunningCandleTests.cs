using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// The still-running higher timeframe candle, rebuilt from the 1m candles.
/// <para>
/// Taken from a real case: VVVUSDC.PERP on 30-08-2026. A short opened at 13:30 with its first dca as
/// a sell limit at 17.07, and at 18:31 the price ran through it - the fill was reported and the
/// position grid showed two parts, while the 30m chart still stopped at the 18:00-18:30 candle whose
/// high was 16.947. The spike sat in the 30m candle that had not closed yet, and a candle in a higher
/// timeframe is only written once that timeframe is complete, so there was nothing to draw it on.
/// </para>
/// </summary>
[TestClass]
public class RunningCandleTests : TestBase
{
    // The 1m candles of 18:30-18:36 local (16:30-16:36 UTC), straight from the exchange data
    private static readonly (int Minute, decimal Open, decimal High, decimal Low, decimal Close)[] Spike =
    [
        (30, 16.917m, 17.040m, 16.912m, 16.973m),
        (31, 16.973m, 17.097m, 16.973m, 17.009m),
        (32, 17.010m, 17.108m, 17.005m, 17.095m),
        (33, 17.102m, 17.184m, 17.093m, 17.129m),
        (34, 17.133m, 17.153m, 17.054m, 17.119m),
        (35, 17.114m, 17.116m, 17.088m, 17.099m),
        (36, 17.141m, 17.239m, 17.141m, 17.163m),
    ];

    private const decimal DcaPrice = 17.07m;
    private const decimal MinuteVolume = 2m;
    private static readonly DateTime HourStart = new(2026, 08, 30, 16, 00, 00, DateTimeKind.Utc);

    /// <summary>
    /// Feed 16:00-16:29 flat so the 30m candle at 16:00 closes, then the seven minutes of the spike.
    /// Returns the symbol with one complete 30m candle and an interval that is only 7/30 in.
    /// </summary>
    private static async Task<CryptoSymbol> ArrangeSymbolAsync(CryptoDatabase database)
    {
        CryptoSymbol symbol = CreateTestSymbol(database);
        // TESTUSDT defaults to 2 decimals, which would round 16.917 to 16.92 and lose the whole point
        symbol.PriceDecimals = 3;

        for (int minute = 0; minute < 30; minute++)
            await CandleTools.Process1mCandleAsync(symbol, HourStart.AddMinutes(minute), 16.9m, 16.9m, 16.9m, 16.9m, 1);

        foreach (var (minute, open, high, low, close) in Spike)
            await CandleTools.Process1mCandleAsync(symbol, HourStart.AddMinutes(minute), open, high, low, close, MinuteVolume);

        return symbol;
    }

    /// <summary>
    /// BuildRunningCandles is display-only and refuses to run in emulator mode, where the replay
    /// decides what now is. Flip the flag off around the call and put it back.
    /// </summary>
    private static List<CryptoCandle> BuildOutsideEmulator(CryptoSymbol symbol, CryptoInterval interval,
        CandleTime lastClosed, CandleTime minDate, CandleTime maxDate)
    {
        bool wasEmulator = GlobalData.IsEmulatorMode;
        GlobalData.IsEmulatorMode = false;
        try
        {
            return CandleTools.BuildRunningCandles(symbol, interval, lastClosed, minDate, maxDate);
        }
        finally
        {
            GlobalData.IsEmulatorMode = wasEmulator;
        }
    }

    [TestMethod]
    public async Task StoredCandlesStopAtTheLastClosedIntervalAsync()
    {
        InitTestSession();
        using CryptoDatabase database = new();
        database.Open();

        CryptoSymbol symbol = await ArrangeSymbolAsync(database);
        CryptoInterval interval30m = GlobalData.IntervalListPeriodName["30m"];
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval30m.IntervalPeriod);

        // This is the situation the chart was drawing: one closed 30m candle, nothing for the
        // interval that is running, and a high that stays well under the dca level.
        Assert.AreEqual(1, symbolInterval.CandleList.Count, "30m candles stored");
        CryptoCandle closed = symbolInterval.CandleList.Values.First();
        Assert.AreEqual(CandleTime.AlignFromDateTime(HourStart, 1), closed.OpenTime, "open time of the closed candle");
        Assert.IsTrue(closed.High < DcaPrice, $"high {closed.High} of the closed candle stays under the dca at {DcaPrice}");
    }

    [TestMethod]
    public async Task RunningCandleCarriesTheSpikeThroughTheDcaAsync()
    {
        InitTestSession();
        using CryptoDatabase database = new();
        database.Open();

        CryptoSymbol symbol = await ArrangeSymbolAsync(database);
        CryptoInterval interval30m = GlobalData.IntervalListPeriodName["30m"];
        CandleTime lastClosed = CandleTime.AlignFromDateTime(HourStart, 1);

        List<CryptoCandle> running = BuildOutsideEmulator(symbol, interval30m, lastClosed,
            CandleTime.MinValue, CandleTime.MaxValue);

        Assert.AreEqual(1, running.Count, "one candle, the interval that is still running");
        CryptoCandle candle = running[0];

        Assert.AreEqual(lastClosed + interval30m.Duration, candle.OpenTime, "open time");
        Assert.AreEqual(Spike[0].Open, candle.Open, "open comes from the first minute");
        Assert.AreEqual(Spike.Max(c => c.High), candle.High, "high is the highest minute so far");
        Assert.AreEqual(Spike.Min(c => c.Low), candle.Low, "low is the lowest minute so far");
        Assert.AreEqual(Spike[^1].Close, candle.Close, "close follows the last minute received");
        Assert.AreEqual(MinuteVolume * Spike.Length, candle.Volume, "volume is the sum of the minutes");

        // The whole point: the dca fill now has a candle to sit on.
        Assert.IsTrue(candle.High > DcaPrice, $"high {candle.High} reaches through the dca at {DcaPrice}");
    }

    [TestMethod]
    public async Task RunningCandleIsSkippedInEmulatorModeAsync()
    {
        InitTestSession();
        using CryptoDatabase database = new();
        database.Open();

        CryptoSymbol symbol = await ArrangeSymbolAsync(database);
        CryptoInterval interval30m = GlobalData.IntervalListPeriodName["30m"];
        CandleTime lastClosed = CandleTime.AlignFromDateTime(HourStart, 1);

        bool wasEmulator = GlobalData.IsEmulatorMode;
        GlobalData.IsEmulatorMode = true;
        try
        {
            List<CryptoCandle> running = CandleTools.BuildRunningCandles(symbol, interval30m, lastClosed,
                CandleTime.MinValue, CandleTime.MaxValue);
            Assert.AreEqual(0, running.Count, "the replay decides what now is, so nothing is synthesized");
        }
        finally
        {
            GlobalData.IsEmulatorMode = wasEmulator;
        }
    }

    [TestMethod]
    public async Task RunningCandleOutsideTheDrawnRangeIsSkippedAsync()
    {
        InitTestSession();
        using CryptoDatabase database = new();
        database.Open();

        CryptoSymbol symbol = await ArrangeSymbolAsync(database);
        CryptoInterval interval30m = GlobalData.IntervalListPeriodName["30m"];
        CandleTime lastClosed = CandleTime.AlignFromDateTime(HourStart, 1);

        // A position window that ends before the running candle starts - it must not stick out past it
        List<CryptoCandle> running = BuildOutsideEmulator(symbol, interval30m, lastClosed,
            CandleTime.MinValue, lastClosed);

        Assert.AreEqual(0, running.Count, "candle opens past the right edge of the drawn range");
    }

    [TestMethod]
    public async Task NothingToBuildOnTheOneMinuteIntervalAsync()
    {
        InitTestSession();
        using CryptoDatabase database = new();
        database.Open();

        CryptoSymbol symbol = await ArrangeSymbolAsync(database);
        CryptoInterval interval1m = GlobalData.IntervalList[0];
        CandleTime lastClosed = CandleTime.AlignFromDateTime(HourStart.AddMinutes(35), 1);

        List<CryptoCandle> running = BuildOutsideEmulator(symbol, interval1m, lastClosed,
            CandleTime.MinValue, CandleTime.MaxValue);

        Assert.AreEqual(0, running.Count, "1m has no lower timeframe to synthesize from");
    }
}
