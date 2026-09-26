using CryptoScanner.Analyzers.Stobb;
using CryptoScanner.Analyzers.Stobb.Signal;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Analyzer.Stobb;

/// <summary>
/// Pins down which MA stacking the "With SBM conditions MA-lines" option (StobbSettings.IncludeSoftSbm)
/// demands on each side of the stobb strategy.
/// <para>
/// A long wants the bearish stacking sma200 &gt; sma50 &gt; sma20 (SmaHelper.IsSbmConditionsOversold) -
/// the same stacking SignalStobbMultiLong and SignalSbm1Long ask for - and a short wants the mirror
/// image sma200 &lt; sma50 &lt; sma20 (IsSbmConditionsOverbought). SignalStobbLong asked for the
/// overbought stacking, so with the option switched on it only fired on setups that belong to the
/// short side; these tests fail on that code and pass on the corrected version.
/// </para>
/// <para>
/// Only the MA-lines option is switched on. The other three options of the strategy
/// (IncludeSbmPercAndCrossing, IncludeRsi, OnlyIfPreviousStobb) stay off, so AdditionalChecks runs
/// nothing but the stacking check and the test needs no candle history, no indicator hub and no
/// database. That is also why a hand-built symbol and symbol interval are enough here, in the same
/// way AtrRbBandsTests builds them.
/// </para>
/// </summary>
[TestClass]
public class StobbSoftSbmTests
{
    private const byte TickDec = 4;

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
    /// Builds a candle with the three SMA lines stacked as asked. The other indicator values are
    /// left alone: the MA-lines check reads nothing else.
    /// </summary>
    private static MyData MakeData(double sma200, double sma50, double sma20)
    {
        return new MyData
        {
            Candle = new CryptoCandle
            {
                TickDecimals = TickDec,
                OpenTime = new CandleTime(3600),
                Open = 100m,
                High = 101m,
                Low = 99m,
                Close = 100m,
                Volume = 1000m,
            },
            CandleData = new CryptoData
            {
                Sma200 = sma200,
                Sma50 = sma50,
                Sma20 = sma20,
            },
        };
    }

    /// <summary>
    /// Fills the properties every SignalCreateBase requires. Not a generic factory with a new()
    /// constraint: SignalCreateBase has required members, and a type with those cannot satisfy new().
    /// </summary>
    private static SignalStobbLong CreateLong(MyData candleLast)
    {
        CryptoSymbol symbol = CreateSymbol();
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m];
        return new SignalStobbLong
        {
            Symbol = symbol,
            Interval = interval,
            SymbolInterval = symbol.GetSymbolInterval(interval),
            SignalSide = CryptoTradeSide.Long,
            SignalStrategy = "stobb",
            CandleLast = candleLast,
        };
    }

    private static SignalStobbShort CreateShort(MyData candleLast)
    {
        CryptoSymbol symbol = CreateSymbol();
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m];
        return new SignalStobbShort
        {
            Symbol = symbol,
            Interval = interval,
            SymbolInterval = symbol.GetSymbolInterval(interval),
            SignalSide = CryptoTradeSide.Short,
            SignalStrategy = "stobb",
            CandleLast = candleLast,
        };
    }

    /// <summary>
    /// Switches the MA-lines option on and the other three off, and puts them back afterwards -
    /// StobbPlugin.Settings is process-static, so a changed value would travel to the next test.
    /// </summary>
    private static bool RunAdditionalChecks(SignalCreateBase signal, out string response)
    {
        var settings = StobbPlugin.Settings;
        bool softSbm = settings.IncludeSoftSbm;
        bool percAndCrossing = settings.IncludeSbmPercAndCrossing;
        bool rsi = settings.IncludeRsi;
        bool previousStobb = settings.OnlyIfPreviousStobb;
        try
        {
            settings.IncludeSoftSbm = true;
            settings.IncludeSbmPercAndCrossing = false;
            settings.IncludeRsi = false;
            settings.OnlyIfPreviousStobb = false;

            return signal.AdditionalChecks(signal.CandleLast, out response);
        }
        finally
        {
            settings.IncludeSoftSbm = softSbm;
            settings.IncludeSbmPercAndCrossing = percAndCrossing;
            settings.IncludeRsi = rsi;
            settings.OnlyIfPreviousStobb = previousStobb;
        }
    }

    // ── Long ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void StobbLong_Sma200AboveSma50AboveSma20_Accepted()
    {
        MyData data = MakeData(sma200: 120, sma50: 110, sma20: 100);

        bool result = RunAdditionalChecks(CreateLong(data), out string response);

        Assert.IsTrue(result, $"A long needs sma200 > sma50 > sma20, but it was refused with '{response}'");
    }

    [TestMethod]
    public void StobbLong_Sma200BelowSma50BelowSma20_Rejected()
    {
        MyData data = MakeData(sma200: 100, sma50: 110, sma20: 120);

        bool result = RunAdditionalChecks(CreateLong(data), out string response);

        Assert.IsFalse(result, "sma200 < sma50 < sma20 is the short stacking and must not pass on a long");
        Assert.AreEqual("no sbm conditions", response);
    }

    // ── Short ────────────────────────────────────────────────────────────

    [TestMethod]
    public void StobbShort_Sma200BelowSma50BelowSma20_Accepted()
    {
        MyData data = MakeData(sma200: 100, sma50: 110, sma20: 120);

        bool result = RunAdditionalChecks(CreateShort(data), out string response);

        Assert.IsTrue(result, $"A short needs sma200 < sma50 < sma20, but it was refused with '{response}'");
    }

    [TestMethod]
    public void StobbShort_Sma200AboveSma50AboveSma20_Rejected()
    {
        MyData data = MakeData(sma200: 120, sma50: 110, sma20: 100);

        bool result = RunAdditionalChecks(CreateShort(data), out string response);

        Assert.IsFalse(result, "sma200 > sma50 > sma20 is the long stacking and must not pass on a short");
        Assert.AreEqual("no sbm conditions", response);
    }
}
