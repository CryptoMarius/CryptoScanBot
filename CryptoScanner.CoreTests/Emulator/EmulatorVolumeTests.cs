using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Emulator.Engine;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The emulator reads the symbol's 24 hour volume from the daily candle it is replaying instead of
/// from the last "fetch symbols".
/// <para>
/// What these tests really guard is the consequence, not the assignment: CheckValidMinimalVolume in
/// Core rejects a symbol for the whole tick when its volume is under the minimum, so this one value
/// decides which symbols take part at all. Measured on 29-08-2026, 17 of a run's 50 symbols sat
/// under the threshold and not one of them opened a position. Feeding it from the replayed day is
/// what lets a symbol drop in and out over the run the way it did at the time.
/// </para>
/// </summary>
// AddQuoteData hands out the ONE shared USDT entry, so the minimum set here is visible to every
// other test that reads it. Hence the restore in cleanup and no parallel run alongside others.
[DoNotParallelize]
[TestClass]
public class EmulatorVolumeTests : TestBase
{
    private const double Threshold = 15_000_000;

    private double savedMinimalVolume;
    private bool savedFetchCandles;

    [TestInitialize]
    public void Setup()
    {
        // GetSymbolInterval indexes a list built from GlobalData.IntervalList, so the intervals have
        // to exist before a symbol can be asked for one.
        InitTestSession();
        var quote = GlobalData.AddQuoteData("USDT");
        savedMinimalVolume = quote.MinimalVolume;
        savedFetchCandles = quote.FetchCandles;
    }

    [TestCleanup]
    public void Restore()
    {
        var quote = GlobalData.AddQuoteData("USDT");
        quote.MinimalVolume = savedMinimalVolume;
        quote.FetchCandles = savedFetchCandles;
        EmulatorVolume.ResetDiagnostics();
    }

    private static CryptoSymbol MakeSymbol()
    {
        var exchange = new Exchange { Id = 1, Name = "TestExchange", FeeRate = 0.1m };
        var quote = GlobalData.AddQuoteData("USDT");
        quote.MinimalVolume = Threshold;
        quote.FetchCandles = true;
        return new CryptoSymbol
        {
            Id = 1,
            Name = "TESTUSDT",
            Base = "TEST",
            Quote = "USDT",
            Exchange = exchange,
            ExchangeId = exchange.Id,
            ExchangeName = exchange.Name,
            QuoteData = quote,
            PriceTickSize = 0.01m,
            // What the last "fetch symbols" left behind - comfortably over the threshold, so a test
            // that still passes on this value has not switched anything over.
            Volume = 900_000_000,
        };
    }

    /// <summary>A daily candle at the given day offset, carrying the given quote volume.</summary>
    private static CryptoCandle DailyCandle(int dayOffset, decimal volume) => new()
    {
        OpenTime = new CandleTime((uint)(dayOffset * 1440)),
        Open = 100m,
        High = 100m,
        Low = 100m,
        Close = 100m,
        Volume = volume,
    };

    private static void AddDaily(CryptoSymbol symbol, CryptoCandle candle)
        => symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1d).CandleList.TryAdd(candle.OpenTime, candle);


    [TestMethod]
    public void ADailyCandle_BecomesTheSymbolsVolume()
    {
        CryptoSymbol symbol = MakeSymbol();

        EmulatorVolume.ApplyDailyVolume(symbol, DailyCandle(10, 42_000_000m));

        Assert.AreEqual(42_000_000d, symbol.Volume, "het volume komt uit de dagcandle");
    }


    /// <summary>
    /// The seed takes the NEWEST day in the warmup, not the first one it happens to walk past. The
    /// warmup loads about 270 daily candles, so picking the wrong end would judge the run on
    /// liquidity from nine months before it starts.
    /// </summary>
    [TestMethod]
    public void TheSeed_TakesTheNewestDayOfTheWarmup()
    {
        CryptoSymbol symbol = MakeSymbol();
        AddDaily(symbol, DailyCandle(1, 5_000_000m));
        AddDaily(symbol, DailyCandle(3, 30_000_000m));
        AddDaily(symbol, DailyCandle(2, 90_000_000m));

        EmulatorVolume.SeedFromWarmup(symbol);

        Assert.AreEqual(30_000_000d, symbol.Volume, "dag 3 is de laatste, ook al is dag 2 drukker");
    }


    /// <summary>
    /// Without a daily candle the honest answer is zero, not the value from the last fetch. Keeping
    /// the fetched value would put exactly the look-ahead back that this exists to remove.
    /// </summary>
    [TestMethod]
    public void WithoutADailyCandle_TheVolumeIsZeroAndCounted()
    {
        CryptoSymbol symbol = MakeSymbol();
        EmulatorVolume.ResetDiagnostics();

        EmulatorVolume.SeedFromWarmup(symbol);

        Assert.AreEqual(0d, symbol.Volume, "onbekend is nul, niet de waarde van de laatste fetch");
        Assert.AreEqual(1, EmulatorVolume.SymbolsWithoutDailyVolume, "en het wordt geteld voor het log");
    }


    /// <summary>
    /// The point of the whole change: over the run a symbol crosses the minimum in both directions,
    /// so it takes part on the days it was liquid and sits out on the days it was not. Asserted
    /// through the Core check that actually gates the pipeline, not through the field.
    /// </summary>
    [TestMethod]
    public void OverTheRun_ASymbolDropsInAndOutOfService()
    {
        CryptoSymbol symbol = MakeSymbol();
        CandleTime anyMoment = new(10 * 1440);

        EmulatorVolume.ApplyDailyVolume(symbol, DailyCandle(1, 3_000_000m));
        Assert.IsFalse(symbol.CheckValidMinimalVolume(anyMoment, 1440, out string _),
            "op een rustige dag doet de munt niet mee");

        EmulatorVolume.ApplyDailyVolume(symbol, DailyCandle(2, 40_000_000m));
        Assert.IsTrue(symbol.CheckValidMinimalVolume(anyMoment, 1440, out string _),
            "en op een drukke dag weer wel");

        EmulatorVolume.ApplyDailyVolume(symbol, DailyCandle(3, 1_000_000m));
        Assert.IsFalse(symbol.CheckValidMinimalVolume(anyMoment, 1440, out string _),
            "en er daarna weer uit");
    }
}
