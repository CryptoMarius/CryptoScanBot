using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// A cached kline ticker invents a flat candle for every minute it received nothing, and until
/// 26-09-2026 the candle administration could not tell those apart from candles the exchange had
/// confirmed. The 1m list stayed contiguous, LastCandleSynchronized walked up to "now" and the REST
/// catch-up found nothing left to fetch, so the invented minutes stayed in the database forever and
/// ResetDerivedStateAfterGapAsync never fired. Measured on HyperLiquid Perpetual, 19-09-2026
/// 08:34-08:36 UTC: 95, 93 and 95 coins flat at the same time, BTC, ETH, SOL and HYPE among them.
/// <para>
/// CryptoSymbolInterval.SynthesizedFrom is where the pointer now waits, and SynthesizedReplaced
/// counts the invented candles the exchange overwrote.
/// </para>
/// </summary>
[TestClass]
public class SynthesizedCandleTests
{
    [TestInitialize]
    public void Init() => TestBase.InitTestSession();


    private static CryptoSymbol CreateSymbol(string baseAsset)
    {
        var quoteData = GlobalData.AddQuoteData("USDT");
        return new CryptoSymbol
        {
            Status = 1,
            Exchange = GlobalData.ActiveExchange!,
            Base = baseAsset,
            Quote = "USDT",
            Name = baseAsset + "USDT",
            ExchangeName = baseAsset + "USDT",
            QuoteData = quoteData,
            PriceTickSize = 0.01m,
        };
    }

    private static DateTime Minute(int hour, int minute)
        => new(2026, 9, 19, hour, minute, 0, DateTimeKind.Utc);

    private static CandleTime Time(int hour, int minute)
        => CandleTime.AlignFromDateTime(Minute(hour, minute), 1);


    [TestMethod]
    public async Task InventedMinutesHoldThePointerUntilTheExchangeConfirmsThem()
    {
        CryptoSymbol symbol = CreateSymbol("SYNA");
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
        symbolInterval.LastCandleSynchronized = Time(9, 57);

        // Three minutes the exchange really delivered
        for (int i = 0; i < 3; i++)
            await CandleTools.Process1mCandleAsync(symbol, Minute(9, 57 + i), 10m, 11m, 9m, 10m, 100);

        Assert.AreEqual(Time(10, 00), symbolInterval.LastCandleSynchronized!.Value, "the pointer follows the real candles");
        Assert.IsNull(symbolInterval.SynthesizedFrom, "nothing was invented yet");

        // The connection drops over three minute boundaries and the flush invents a flat candle for
        // each of them, at the last known price
        for (int i = 0; i < 3; i++)
            await CandleTools.Process1mCandleAsync(symbol, Minute(10, i), 10m, 10m, 10m, 10m, 0, isFilled: true);

        Assert.AreEqual(Time(10, 00), symbolInterval.SynthesizedFrom!.Value, "the first invented minute");
        Assert.AreEqual(Time(10, 00), symbolInterval.LastCandleSynchronized!.Value,
            "the pointer must not walk over invented minutes, or the catch-up has nothing left to ask for");

        // Every interval that is built on those minutes is just as invented
        CryptoSymbolInterval symbolInterval5m = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval5m);
        Assert.AreEqual(Time(10, 00), symbolInterval5m.SynthesizedFrom!.Value, "the 5m candle over 10:00 is invented too");

        // The catch-up asks the exchange for that stretch and gets what really traded
        for (int i = 0; i < 3; i++)
            CandleTools.CreateCandle(symbol, GlobalData.IntervalList[0], Minute(10, i), 10m, 12m, 9m, 11m, 500);

        Assert.AreEqual(3, symbolInterval.SynthesizedReplaced, "three invented candles were replaced by real ones");

        // What CandleBase.GetCandlesForIntervalAsync does once the exchange has been asked
        symbolInterval.SynthesizedFrom = null;
        CandleTools.UpdateCandleFetched(symbol, GlobalData.IntervalList[0]);

        Assert.AreEqual(Time(10, 03), symbolInterval.LastCandleSynchronized!.Value,
            "with the stretch confirmed the pointer walks on");
    }


    [TestMethod]
    public async Task RealCandlesLeaveTheCounterAndThePointerAlone()
    {
        CryptoSymbol symbol = CreateSymbol("SYNB");
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
        symbolInterval.LastCandleSynchronized = Time(9, 57);

        for (int i = 0; i < 3; i++)
            await CandleTools.Process1mCandleAsync(symbol, Minute(9, 57 + i), 10m, 11m, 9m, 10m, 100);

        // The same minute delivered a second time (a repeated fetch) is not a repair
        CandleTools.CreateCandle(symbol, GlobalData.IntervalList[0], Minute(9, 58), 10m, 11m, 9m, 10m, 120);

        Assert.AreEqual(0, symbolInterval.SynthesizedReplaced);
        Assert.IsNull(symbolInterval.SynthesizedFrom);
        Assert.AreEqual(Time(10, 00), symbolInterval.LastCandleSynchronized!.Value,
            "a market that keeps delivering must not be slowed down by any of this");
    }
}
