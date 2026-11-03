using CryptoScanner.Analyzers.PsarCross;
using CryptoScanner.Analyzers.PsarCross.Signal;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// The parabolic SAR cross: in when the SAR flips to the other side of the price, out when it flips
/// back.
/// <para>
/// Every candle closes at 100 and the SAR is written by hand per candle. The series is "SAR against
/// us" everywhere (above the price for a long, under it for a short), and the test moves the SAR to
/// the other side on the candles it wants flipped.
/// </para>
/// </summary>
[DoNotParallelize]
[TestClass]
public class PsarCrossTests : TestBase
{
    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        ApplyDefaults(PsarCrossPlugin.Settings);
    }

    [TestCleanup]
    public void Restore() => ApplyDefaults(PsarCrossPlugin.Settings);

    private static void ApplyDefaults(PsarCrossSettings settings)
    {
        PsarCrossSettings fresh = new();
        settings.ConfirmationCandles = fresh.ConfirmationCandles;
        settings.MinimumDistancePercentage = fresh.MinimumDistancePercentage;
        settings.RelativeVolumeMinimum = fresh.RelativeVolumeMinimum;
        settings.RelativeVolumeCandles = fresh.RelativeVolumeCandles;
        settings.RelativeVolumeAverageCandles = fresh.RelativeVolumeAverageCandles;
        settings.ExitOnFlipBack = fresh.ExitOnFlipBack;
        settings.ExitConfirmationCandles = fresh.ExitConfirmationCandles;
    }


    private static CryptoSymbol MakeSymbol()
    {
        Exchange exchange = new() { Id = 1, Name = "TestExchange", TakerFeeRate = 0.1m, MakerFeeRate = 0.1m };
        return new CryptoSymbol
        {
            Id = 1,
            Name = "TESTUSDT",
            Base = "TEST",
            Quote = "USDT",
            Exchange = exchange,
            ExchangeId = exchange.Id,
            ExchangeName = exchange.Name,
            QuoteData = GlobalData.AddQuoteData("USDT"),
            PriceTickSize = 0.01m,
        };
    }

    private static CryptoInterval MakeInterval()
        => GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1d];


    /// <summary>
    /// Builds <paramref name="count"/> candles ending at index 0 (the newest), every one closing at
    /// 100 with the SAR AGAINST the given side: at 102 for a long, at 98 for a short.
    /// </summary>
    private static PsarCrossBase MakeSeries(CryptoTradeSide side, int count, Action<CryptoData[]> shape,
        Action<CryptoCandle[]>? shapeCandles = null)
    {
        CryptoSymbol symbol = MakeSymbol();
        CryptoInterval interval = MakeInterval();
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        CryptoCandle[] candles = new CryptoCandle[count];
        CryptoData[] data = new CryptoData[count];
        for (int i = 0; i < count; i++)
        {
            candles[i] = new CryptoCandle
            {
                TickDecimals = 2,
                OpenTime = new CandleTime((uint)((count - i) * interval.Duration)),
                Open = 100m,
                High = 101m,
                Low = 99m,
                Close = 100m,
                Volume = 10m,
            };
            data[i] = new CryptoData
            {
                PSar = side == CryptoTradeSide.Long ? 102.0 : 98.0,
            };
        }
        shape(data);
        shapeCandles?.Invoke(candles);

        for (int i = 0; i < count; i++)
        {
            symbolInterval.CandleList.TryAdd(candles[i].OpenTime, candles[i]);
            symbolInterval.Data[candles[i].OpenTime] = data[i];
        }

        return new PsarCrossBase
        {
            Symbol = symbol,
            Interval = interval,
            SymbolInterval = symbolInterval,
            SignalSide = side,
            SignalStrategy = "psarcross",
            CandleLast = new MyData { Candle = candles[0], CandleData = data[0] },
        };
    }

    /// <summary>Puts the SAR on the trade's side of the close (100) on candle <paramref name="index"/>.</summary>
    private static void OnOurSide(CryptoData[] data, CryptoTradeSide side, int index, double distance = 2.0)
        => data[index].PSar = side == CryptoTradeSide.Long ? 100.0 - distance : 100.0 + distance;

    private const int Enough = 10;


    // ═══════════════════════════════════════════════════════════════════════
    //  The flip
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void TheSarFlippingUnderThePrice_IsALong()
    {
        var algorithm = MakeSeries(CryptoTradeSide.Long, Enough, data =>
            OnOurSide(data, CryptoTradeSide.Long, 0));

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "flipped under");
    }


    [TestMethod]
    public void TheSarFlippingAboveThePrice_IsAShort()
    {
        var algorithm = MakeSeries(CryptoTradeSide.Short, Enough, data =>
            OnOurSide(data, CryptoTradeSide.Short, 0));

        Assert.IsTrue(algorithm.IsSignal(), algorithm.ExtraText);
        StringAssert.Contains(algorithm.ExtraText, "flipped above");
    }


    /// <summary>A SAR that has been under the price all along has not flipped.</summary>
    [TestMethod]
    public void ASarThatWasAlreadyOnThisSide_IsNoSignal()
    {
        var algorithm = MakeSeries(CryptoTradeSide.Long, Enough, data =>
        {
            for (int i = 0; i < data.Length; i++)
                OnOurSide(data, CryptoTradeSide.Long, i);
        });

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "no flip");
    }


    [TestMethod]
    public void ASarAgainstTheTrade_IsNoSignal()
    {
        var algorithm = MakeSeries(CryptoTradeSide.Long, Enough, _ => { });

        Assert.IsFalse(algorithm.IsSignal());
        StringAssert.Contains(algorithm.ExtraText, "not under the price");
    }


    /// <summary>A hub that is still warming up has no SAR yet. That has to be a quiet no.</summary>
    [TestMethod]
    public void WithoutSarValues_ItSaysNoInsteadOfThrowing()
    {
        var noSar = MakeSeries(CryptoTradeSide.Long, Enough, data => data[0].PSar = null);
        Assert.IsFalse(noSar.IndicatorsOkay(noSar.CandleLast));

        var noPrevious = MakeSeries(CryptoTradeSide.Long, Enough, data =>
        {
            OnOurSide(data, CryptoTradeSide.Long, 0);
            data[1].PSar = null;
        });
        Assert.IsFalse(noPrevious.IsSignal());
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Confirmation and distance
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void WithConfirmation_TheFlipHasToBeThatManyCandlesOld()
    {
        PsarCrossPlugin.Settings.ConfirmationCandles = 2;

        var held = MakeSeries(CryptoTradeSide.Long, Enough, data =>
        {
            for (int i = 0; i <= 2; i++)
                OnOurSide(data, CryptoTradeSide.Long, i);
        });
        Assert.IsTrue(held.IsSignal(), held.ExtraText);
        StringAssert.Contains(held.ExtraText, "2 candle(s) ago");

        var fresh = MakeSeries(CryptoTradeSide.Long, Enough, data =>
            OnOurSide(data, CryptoTradeSide.Long, 0));
        Assert.IsFalse(fresh.IsSignal());
        StringAssert.Contains(fresh.ExtraText, "not held");
    }


    /// <summary>The SAR 2 under a close of 100 is 2% away.</summary>
    [TestMethod]
    public void MinimumDistance_IsAPercentageOfThePrice()
    {
        PsarCrossPlugin.Settings.MinimumDistancePercentage = 3m;
        var narrow = MakeSeries(CryptoTradeSide.Long, Enough, data =>
            OnOurSide(data, CryptoTradeSide.Long, 0, distance: 2.0));
        Assert.IsFalse(narrow.IsSignal());
        StringAssert.Contains(narrow.ExtraText, "2.000% from the price");

        PsarCrossPlugin.Settings.MinimumDistancePercentage = 1m;
        var wide = MakeSeries(CryptoTradeSide.Short, Enough, data =>
            OnOurSide(data, CryptoTradeSide.Short, 0, distance: 2.0));
        Assert.IsTrue(wide.IsSignal(), wide.ExtraText);
    }


    [TestMethod]
    public void RelativeVolume_WantsMoreThanTheUsualPace()
    {
        PsarCrossPlugin.Settings.RelativeVolumeMinimum = 2m;
        PsarCrossPlugin.Settings.RelativeVolumeCandles = 1;
        PsarCrossPlugin.Settings.RelativeVolumeAverageCandles = 5;

        var quiet = MakeSeries(CryptoTradeSide.Long, Enough, data =>
            OnOurSide(data, CryptoTradeSide.Long, 0));
        Assert.IsFalse(quiet.IsSignal());
        StringAssert.Contains(quiet.ExtraText, "volume 1.00x");

        var busy = MakeSeries(CryptoTradeSide.Long, Enough,
            data => OnOurSide(data, CryptoTradeSide.Long, 0),
            candles => candles[0].Volume = 30m);
        Assert.IsTrue(busy.IsSignal(), busy.ExtraText);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The exit
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ALongLeaves_WhenTheSarIsBackAboveThePrice()
    {
        var against = MakeSeries(CryptoTradeSide.Long, Enough, _ => { });
        Assert.IsTrue(against.HasExitSignal);
        Assert.IsTrue(against.IsExitSignal(), against.ExtraText);
        StringAssert.Contains(against.ExtraText, "flipped back above");

        var still = MakeSeries(CryptoTradeSide.Long, Enough, data =>
            OnOurSide(data, CryptoTradeSide.Long, 0));
        Assert.IsFalse(still.IsExitSignal());
    }


    [TestMethod]
    public void AShortLeaves_WhenTheSarIsBackUnderThePrice()
    {
        var against = MakeSeries(CryptoTradeSide.Short, Enough, _ => { });
        Assert.IsTrue(against.IsExitSignal(), against.ExtraText);
        StringAssert.Contains(against.ExtraText, "flipped back under");
    }


    [TestMethod]
    public void ExitConfirmation_WaitsForTheFlipBackToHold()
    {
        PsarCrossPlugin.Settings.ExitConfirmationCandles = 1;

        // Flipped back on this candle only: candle 1 still on our side.
        var fresh = MakeSeries(CryptoTradeSide.Long, Enough, data =>
            OnOurSide(data, CryptoTradeSide.Long, 1));
        Assert.IsFalse(fresh.IsExitSignal());
        StringAssert.Contains(fresh.ExtraText, "not held");

        var held = MakeSeries(CryptoTradeSide.Long, Enough, _ => { });
        Assert.IsTrue(held.IsExitSignal(), held.ExtraText);
    }


    [TestMethod]
    public void WithoutExitOnFlipBack_ThereIsNoExit()
    {
        PsarCrossPlugin.Settings.ExitOnFlipBack = false;
        var algorithm = MakeSeries(CryptoTradeSide.Long, Enough, _ => { });

        Assert.IsFalse(algorithm.HasExitSignal);
        Assert.IsFalse(algorithm.IsExitSignal());
    }
}
