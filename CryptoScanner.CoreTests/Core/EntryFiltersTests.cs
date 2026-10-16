using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;
using CryptoScanner.Emulator.Engine;

using ExchangeModel = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// The entry filters of the market-regime study: BTC move, weekdays, the coin on a higher interval,
/// entries per hour, and their queue keys. Each one off by default, each one per side.
/// </summary>
[DoNotParallelize]
[TestClass]
public class EntryFiltersTests : TestBase
{
    private const string QuoteName = "EFQ";
    // A Wednesday
    private static readonly CandleTime Moment =
        CandleTime.AlignFromDateTime(new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc), 1);

    private CryptoQuoteData quote = null!;
    private readonly List<CryptoSymbol> added = [];

    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        quote = GlobalData.AddQuoteData(QuoteName);
        quote.MinimalVolume = 0;
        quote.FetchCandles = true;
        EntryFilters.Reset();
    }

    [TestCleanup]
    public void Cleanup()
    {
        foreach (CryptoSymbol symbol in added)
            GlobalData.ActiveExchange!.SymbolListName.Remove(symbol.Name);
        added.Clear();
        quote.SymbolList = [];
        EntryFilters.Reset();
    }

    private CryptoSymbol NewSymbol(string coin, int id)
    {
        ExchangeModel exchange = new() { Id = 94, Name = "EntryFiltersExchange" };
        return new CryptoSymbol
        {
            Id = id,
            Name = coin + QuoteName,
            Base = coin,
            Quote = QuoteName,
            Exchange = exchange,
            ExchangeId = exchange.Id,
            ExchangeName = coin + QuoteName,
            QuoteData = quote,
            PriceTickSize = 0.0001m,
            Volume = 1_000_000_000,
        };
    }

    /// <summary>A coin whose candles on <paramref name="interval"/> move by <paramref name="step"/> per candle.</summary>
    private CryptoSymbol Coin(string coin, int id, CryptoIntervalPeriod interval, decimal step, int count = 300)
    {
        CryptoSymbol symbol = NewSymbol(coin, id);
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval);
        uint duration = symbolInterval.Interval.Duration;
        CandleTime last = (Moment - 1u).AlignToIntervalMinutes(duration) - duration;
        for (int k = 0; k < count; k++)
        {
            CandleTime open = last - (uint)(duration * k);
            symbolInterval.CandleList.TryAdd(open, new CryptoCandle { OpenTime = open, TickDecimals = 4, Close = 1000m + (count - k) * step });
        }
        GlobalData.ActiveExchange!.SymbolListName[symbol.Name] = symbol;
        added.Add(symbol);
        return symbol;
    }


    [TestMethod]
    public void Rsi_RisingIsHundredAlternatingIsFifty()
    {
        Assert.AreEqual(100.0, EntryFilters.Rsi([.. Enumerable.Range(0, 30).Select(i => (double)i)], 14)!.Value, 1e-9);
        Assert.AreEqual(50.0, EntryFilters.Rsi([.. Enumerable.Range(0, 31).Select(i => i % 2 == 0 ? 10.0 : 11.0)], 14)!.Value, 5.0);
        Assert.IsNull(EntryFilters.Rsi([1, 2, 3], 14));
    }

    [TestMethod]
    public void Weekday_IsTheUtcDayOfThePreviousMinute()
    {
        Assert.AreEqual((int)DayOfWeek.Wednesday, EntryFilters.Weekday(Moment));
        // Midnight belongs to the day that just ended
        CandleTime midnight = CandleTime.AlignFromDateTime(new DateTime(2026, 4, 9, 0, 0, 0, DateTimeKind.Utc), 1);
        Assert.AreEqual((int)DayOfWeek.Wednesday, EntryFilters.Weekday(midnight));
    }

    [TestMethod]
    public void BtcMove_MeasuresHoursBack()
    {
        // BTC 1h closes rise by 10 per hour from about 1000: over 24 hours that is about 240/3990 = 6%
        Coin("BTC", 970, CryptoIntervalPeriod.interval1h, 10m);

        double? move = EntryFilters.BtcMove(quote, Moment, 24);

        Assert.IsNotNull(move);
        Assert.IsTrue(move.Value > 5 && move.Value < 7, $"move {move}");
        Assert.IsNull(EntryFilters.BtcMove(quote, Moment, 0));
    }

    [TestMethod]
    public void HigherInterval_RsiAndEma200()
    {
        CryptoSymbol rising = Coin("UP", 971, CryptoIntervalPeriod.interval1h, 1m);

        (double? rsi, bool? above) = EntryFilters.HigherInterval(rising, CryptoIntervalPeriod.interval1h, Moment);

        Assert.AreEqual(100.0, rsi!.Value, 1e-9);
        Assert.IsTrue(above);
    }

    [TestMethod]
    public void Valid_SkipsWhatTheSettingsDoNotAllow()
    {
        CryptoSymbol rising = Coin("UP", 972, CryptoIntervalPeriod.interval1h, 1m);

        // Weekend only: a Wednesday is skipped
        SettingsCompiled weekend = new() { WeekdaysActive = true, WeekdaysAllowed = [0, 6] };
        Assert.IsFalse(EntryFilters.Valid(rising, CryptoTradeSide.Long, Moment, weekend, out string reaction));
        StringAssert.Contains(reaction, "Wednesday");

        // A short only on a coin below its 1h EMA200: this coin is above it
        SettingsCompiled weakCoin = new() { HigherIntervalActive = true, HigherIntervalEma200 = -1 };
        Assert.IsFalse(EntryFilters.Valid(rising, CryptoTradeSide.Short, Moment, weakCoin, out reaction));
        StringAssert.Contains(reaction, "EMA200");

        // A short only with the 1h RSI at most 50: this coin is at 100
        SettingsCompiled lowRsi = new() { HigherIntervalActive = true, HigherIntervalRsiMaximum = 50m };
        Assert.IsFalse(EntryFilters.Valid(rising, CryptoTradeSide.Short, Moment, lowRsi, out _));

        // Everything off lets everything through
        Assert.IsTrue(EntryFilters.Valid(rising, CryptoTradeSide.Long, Moment, new SettingsCompiled(), out _));
    }

    [TestMethod]
    public void EntriesInLastHour_CountsPerSideWithinTheHour()
    {
        DateTime now = new(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc);
        EntryFilters.RecordEntry(CryptoTradeSide.Long, now.AddMinutes(-70));
        EntryFilters.RecordEntry(CryptoTradeSide.Long, now.AddMinutes(-30));
        EntryFilters.RecordEntry(CryptoTradeSide.Long, now.AddMinutes(-5));
        EntryFilters.RecordEntry(CryptoTradeSide.Short, now.AddMinutes(-5));

        Assert.AreEqual(2, EntryFilters.EntriesInLastHour(CryptoTradeSide.Long, now));
        Assert.AreEqual(1, EntryFilters.EntriesInLastHour(CryptoTradeSide.Short, now));
    }

    [TestMethod]
    public void Queue_AppliesTheNewKeys()
    {
        SettingsTextual signal = new(), trading = new();
        EmulatorSideConfig.Apply(new EmulatorSideConfig
        {
            BtcMove = [24m, -3m, 100m],
            Weekdays = [0, 6],
            MarketRsiCross = [35m, 12m],
            HigherInterval = "4h",
            HigherIntervalRsi = [0m, 50m],
            HigherIntervalEma200 = "below",
            EntryLimitPerHour = 5,
            MarketExit = 5m,
            MarketExitBtc = true,
        }, signal, trading);

        Assert.IsTrue(signal.BtcMove.Active);
        Assert.AreEqual(24, signal.BtcMove.Hours);
        Assert.AreEqual(-3m, signal.BtcMove.Minimum);
        CollectionAssert.AreEqual(new[] { 0, 6 }, signal.Weekdays.Days);
        Assert.IsTrue(signal.MarketRsiCross.Active);
        Assert.AreEqual(12, signal.MarketRsiCross.Hours);
        Assert.IsTrue(signal.HigherInterval.Active);
        Assert.AreEqual("4h", signal.HigherInterval.Interval);
        Assert.AreEqual(50m, signal.HigherInterval.RsiMaximum);
        Assert.AreEqual(-1, signal.HigherInterval.Ema200);
        Assert.IsTrue(trading.EntryLimit.Active);
        Assert.AreEqual(5, trading.EntryLimit.MaxPerHour);
        Assert.IsTrue(trading.MarketExit.Active);
        Assert.IsTrue(trading.MarketExit.UseBtc);
        Assert.IsFalse(signal.EntryLimit.Active, "the entry limit belongs to the trader");

        // Empty lists switch them off again
        EmulatorSideConfig.Apply(new EmulatorSideConfig { BtcMove = [], Weekdays = [], MarketRsiCross = [], EntryLimitPerHour = 0 }, signal, trading);
        Assert.IsFalse(signal.BtcMove.Active);
        Assert.IsFalse(signal.Weekdays.Active);
        Assert.IsFalse(signal.MarketRsiCross.Active);
        Assert.IsFalse(trading.EntryLimit.Active);

        Assert.ThrowsExactly<NotSupportedException>(() =>
            EmulatorSideConfig.Apply(new EmulatorSideConfig { Weekdays = [7] }, signal, trading));
        Assert.ThrowsExactly<NotSupportedException>(() =>
            EmulatorSideConfig.Apply(new EmulatorSideConfig { HigherIntervalEma200 = "up" }, signal, trading));
    }
}
