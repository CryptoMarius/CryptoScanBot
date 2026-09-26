using CryptoScanner.Analyzers.Jump;
using CryptoScanner.Analyzers.Jump.Signal;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Analyzer.Jump;

/// <summary>
/// Pins down the percentage the jump strategy measures over its lookback window, on both sides.
/// <para>
/// The long side looks for a rise: the min came first, so the rise from that starting point is
/// max / min - 1. The short side looks for a drop: there the max came first, so the drop from that
/// starting point is 1 - min / max, which is the smaller number. Both sides used max / min - 1, so a
/// short fired on a drop smaller than the configured percentage (from 100 to 96.15 is 3.85%, but the
/// old expression made 4.00% of it) and the percentage it reported in ExtraText was too high for
/// every short.
/// </para>
/// <para>
/// The strategy needs nothing but candles: it reads the low and the high of the last N candles
/// through GetPrevCandle. That walks CryptoSymbolInterval.TryGetCandle, which only hands out a MyData
/// when the time is in CandleList AND in Data, and SignalCandleJump leaves IndicatorsOkay alone, so
/// the base version demands CandleData != null. Hence an empty CryptoData per candle. A hand-built
/// symbol and symbol interval are enough, in the same way AtrRbBandsTests and StobbSoftSbmTests build
/// them - no indicator hub and no database.
/// </para>
/// </summary>
[TestClass]
public class JumpPercentageTests
{
    private const byte TickDec = 4;

    // The 15m candles start well past the epoch: the base IndicatorsOkay refuses OpenTime 0.
    private const uint FirstCandleMinutes = 15 * 100;
    private const uint IntervalMinutes = 15;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        GlobalData.Settings ??= new SettingsBasic();
        SetupIntervalList();
    }

    private static void SetupIntervalList()
    {
        if (GlobalData.IntervalList.Count > 0)
            return;

        int id = 0;
        foreach (CryptoInterval interval in CryptoInterval.CreateStandardIntervalList())
        {
            interval.Id = id++;
            GlobalData.IntervalList.Add(interval);
            GlobalData.IntervalListId.Add(interval.Id, interval);
            GlobalData.IntervalListPeriodName.Add(interval.Name, interval);
            GlobalData.IntervalListPeriod.Add(interval.IntervalPeriod, interval);
        }
    }

    private static CryptoSymbol CreateSymbol()
    {
        var exchange = new Exchange { Id = 1, Name = "TestExchange" };
        var quoteData = new CryptoQuoteData { Name = "USDT" };
        return new CryptoSymbol
        {
            Id = 1,
            Status = 1,
            Base = "TEST",
            Quote = "USDT",
            Name = "TESTUSDT",
            Exchange = exchange,
            ExchangeName = exchange.Name,
            QuoteData = quoteData,
            PriceDecimals = TickDec,
            PriceTickSize = 0.0001m,
            PriceMinimum = 0m,
            PriceMaximum = 0m,
            QuantityTickSize = 0.01m,
            QuantityMinimum = 0.01m,
            QuantityMaximum = 100000m,
            QuoteValueMinimum = 1m,
            QuoteValueMaximum = 200000m,
        };
    }

    /// <summary>
    /// Puts one flat candle per price in the symbol interval, oldest price first, and returns the last
    /// one as the candle the check runs on. Open equals Close, so with UseLowHighCalculation off both
    /// GetLowValue and GetHighValue are exactly that price and the window has no other extremes in it.
    /// </summary>
    private static MyData LoadCandles(CryptoSymbol symbol, params decimal[] prices)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval15m);
        symbolInterval.CandleList.Clear();
        symbolInterval.Data.Clear();

        MyData? last = null;
        for (int i = 0; i < prices.Length; i++)
        {
            decimal price = prices[i];
            CandleTime openTime = new(FirstCandleMinutes + (uint)i * IntervalMinutes);
            CryptoCandle candle = new()
            {
                TickDecimals = TickDec,
                OpenTime = openTime,
                Open = price,
                High = price + 1m,
                Low = price - 1m,
                Close = price,
                Volume = 1000m,
            };
            CryptoData candleData = new();

            symbolInterval.CandleList.TryAdd(openTime, candle);
            symbolInterval.Data.Add(openTime, candleData);

            last = new MyData { Candle = candle, CandleData = candleData };
        }

        return last!;
    }

    private static SignalCandleJumpLong CreateLong(CryptoSymbol symbol, MyData candleLast)
    {
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m];
        return new SignalCandleJumpLong
        {
            Symbol = symbol,
            Interval = interval,
            SymbolInterval = symbol.GetSymbolInterval(interval),
            SignalSide = CryptoTradeSide.Long,
            SignalStrategy = "jump",
            CandleLast = candleLast,
        };
    }

    private static SignalCandleJumpShort CreateShort(CryptoSymbol symbol, MyData candleLast)
    {
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m];
        return new SignalCandleJumpShort
        {
            Symbol = symbol,
            Interval = interval,
            SymbolInterval = symbol.GetSymbolInterval(interval),
            SignalSide = CryptoTradeSide.Short,
            SignalStrategy = "jump",
            CandleLast = candleLast,
        };
    }

    /// <summary>
    /// Runs IsSignal with a 4% threshold over 5 candles on Open/Close, and puts the settings back
    /// afterwards - JumpPlugin.Settings is process-static, so a changed value would travel to the
    /// next test.
    /// </summary>
    private static bool RunIsSignal(SignalCreateBase signal, out string extraText)
    {
        JumpSettings settings = JumpPlugin.Settings;
        decimal candlePercentage = settings.CandlePercentage;
        int lookback = settings.CandlesLookbackCount;
        bool useLowHigh = settings.UseLowHighCalculation;
        try
        {
            settings.CandlePercentage = 4m;
            settings.CandlesLookbackCount = 5;
            settings.UseLowHighCalculation = false;

            bool result = signal.IsSignal();
            extraText = signal.ExtraText;
            return result;
        }
        finally
        {
            settings.CandlePercentage = candlePercentage;
            settings.CandlesLookbackCount = lookback;
            settings.UseLowHighCalculation = useLowHigh;
        }
    }

    /// <summary>
    /// The text the strategy builds for a percentage. IsSignal formats with "N2", which follows the
    /// culture of the machine running the test, so the expected text is built the same way: what these
    /// tests pin down is the number, not the decimal separator.
    /// </summary>
    private static string Expected(string sign, decimal perc) => sign + perc.ToString("N2") + "%";

    // The loop reads 5 candles and then asks for one more before it stops, so the window of 5 needs a
    // sixth candle in front of it. The two oldest prices below sit inside the range of the window and
    // are never read as an extreme.

    [TestMethod]
    public void JumpShort_DropOfFourPercent_FiresAndReportsFourPercent()
    {
        CryptoSymbol symbol = CreateSymbol();
        // 100 -> 96 is a drop of exactly 4.00%; the old expression made 4.17% of it.
        MyData candleLast = LoadCandles(symbol, 99m, 99m, 100m, 99m, 98m, 97m, 96m);

        bool result = RunIsSignal(CreateShort(symbol, candleLast), out string extraText);

        Assert.IsTrue(result, $"A drop of 4.00% must fire on a 4% threshold, but it was refused with '{extraText}'");
        Assert.AreEqual(Expected("-", 4.00m), extraText);
    }

    [TestMethod]
    public void JumpShort_DropBelowFourPercent_DoesNotFire()
    {
        CryptoSymbol symbol = CreateSymbol();
        // 100 -> 96.15 is a drop of 3.85%; the old expression made 4.00% of it and fired.
        MyData candleLast = LoadCandles(symbol, 99m, 99m, 100m, 99m, 98m, 97m, 96.15m);

        bool result = RunIsSignal(CreateShort(symbol, candleLast), out string extraText);

        Assert.IsFalse(result, $"A drop of 3.85% is under the 4% threshold and must not fire, but it reported '{extraText}'");
    }

    [TestMethod]
    public void JumpLong_RiseOfFourPercent_FiresAndReportsTheRise()
    {
        CryptoSymbol symbol = CreateSymbol();
        // 96 -> 100 is a rise of 4.17% measured from the min, which is what the long side means by it.
        MyData candleLast = LoadCandles(symbol, 97m, 97m, 96m, 97m, 98m, 99m, 100m);

        bool result = RunIsSignal(CreateLong(symbol, candleLast), out string extraText);

        Assert.IsTrue(result, $"A rise of 4.17% must fire on a 4% threshold, but it was refused with '{extraText}'");
        Assert.AreEqual(Expected("+", 4.17m), extraText);
    }
}
