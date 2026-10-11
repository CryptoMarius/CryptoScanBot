using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Which open positions DelistedPositions closes itself, at which price, and on what candle
/// (open point 120: STORJUSDT on Binance Perpetual, settled 26-08-2026 09:00 UTC).
/// </summary>
[TestClass]
public class DelistedPositionsTests
{
    private CryptoTradeVia _savedTradeVia;
    private bool _savedEmulatorMode;

    [TestInitialize]
    public void SaveGlobals()
    {
        // The interval list, so a symbol has its 1m candle list
        TestBase.InitTestSession();
        _savedTradeVia = GlobalData.Settings.Trading.TradeVia;
        _savedEmulatorMode = GlobalData.IsEmulatorMode;
        GlobalData.IsEmulatorMode = false;
    }

    [TestCleanup]
    public void RestoreGlobals()
    {
        GlobalData.Settings.Trading.TradeVia = _savedTradeVia;
        GlobalData.IsEmulatorMode = _savedEmulatorMode;
    }

    private static CryptoPosition BuildPosition(int symbolStatus, DateTime? closeTime = null, decimal? lastPrice = 0.03923m)
    {
        var exchange = new Exchange { Id = 1, Name = "Binance Perpetual" };
        var symbol = new CryptoSymbol
        {
            Id = 1,
            Base = "STORJ",
            Quote = "USDT",
            Name = "STORJUSDT.PERP",
            Exchange = exchange,
            ExchangeName = "STORJUSDT",
            QuoteData = new CryptoQuoteData { Name = "USDT" },
            Status = symbolStatus,
            LastPrice = lastPrice,
            PriceDecimals = 5,
        };
        return new CryptoPosition
        {
            Id = 11,
            Symbol = symbol,
            Exchange = exchange,
            Side = CryptoTradeSide.Long,
            Interval = new CryptoInterval { Id = 1, Name = "5m", IntervalPeriod = CryptoIntervalPeriod.interval5m, Duration = 5 },
            CloseTime = closeTime,
        };
    }


    [TestMethod]
    public void OpenPaperPositionOnADelistedSymbolIsClosed()
    {
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;
        Assert.IsTrue(DelistedPositions.ShouldClose(BuildPosition(symbolStatus: 0)));

        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTradingAndAltrady;
        Assert.IsTrue(DelistedPositions.ShouldClose(BuildPosition(symbolStatus: 0)),
            "a delegated paper position is closed in our own administration too");
    }


    [TestMethod]
    public void PositionOnATradedSymbolIsLeftAlone()
    {
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;
        Assert.IsFalse(DelistedPositions.ShouldClose(BuildPosition(symbolStatus: 1)));
    }


    [TestMethod]
    public void ClosedPositionIsLeftAlone()
    {
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;
        Assert.IsFalse(DelistedPositions.ShouldClose(BuildPosition(symbolStatus: 0, closeTime: DateTime.UtcNow)));
    }


    [TestMethod]
    public void RealTradingIsNeverClosedByTheScanner()
    {
        // An order on a settled market would only be refused; the user gets a message instead
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.RealTrading;
        Assert.IsFalse(DelistedPositions.ShouldClose(BuildPosition(symbolStatus: 0)));
    }


    [TestMethod]
    public void EmulatorNeverClosesADelistedPosition()
    {
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;
        GlobalData.IsEmulatorMode = true;
        Assert.IsFalse(DelistedPositions.ShouldClose(BuildPosition(symbolStatus: 0)));
    }


    [TestMethod]
    public void ClosingPriceIsTheLastPrice()
    {
        Assert.AreEqual(0.03923m, DelistedPositions.ClosingPrice(BuildPosition(symbolStatus: 0).Symbol));
    }


    [TestMethod]
    public void WithoutLastPriceTheNewestCandleCloseIsUsed()
    {
        CryptoSymbol symbol = BuildPosition(symbolStatus: 0, lastPrice: null).Symbol;
        Assert.IsNull(DelistedPositions.ClosingPrice(symbol), "no last price and no candle: nothing to close at");

        CandleTime open = new(1000);
        symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m).CandleList.TryAdd(open,
            new CryptoCandle { TickDecimals = 5, OpenTime = open, Open = 0.04m, High = 0.04m, Low = 0.039m, Close = 0.0395m });
        Assert.AreEqual(0.0395m, DelistedPositions.ClosingPrice(symbol));
    }


    [TestMethod]
    public void FlatCandleSitsOnThePrice()
    {
        CryptoSymbol symbol = BuildPosition(symbolStatus: 0).Symbol;
        CryptoCandle candle = DelistedPositions.FlatCandle(symbol, new CandleTime(2000), 0.03923m);

        Assert.AreEqual(new CandleTime(2000), candle.OpenTime);
        Assert.AreEqual(0.03923m, candle.Open);
        Assert.AreEqual(0.03923m, candle.High);
        Assert.AreEqual(0.03923m, candle.Low);
        Assert.AreEqual(0.03923m, candle.Close);
        Assert.AreEqual(0m, candle.Volume);
    }


    [TestMethod]
    public void FillCandleReachesOneTickPastThePrice()
    {
        // A paper limit order only fills when the price goes through it, and a target exactly on the
        // last price stays there (HandlePosition tolerates one tick), so the candle needs the tick
        CryptoSymbol symbol = BuildPosition(symbolStatus: 0).Symbol;
        symbol.PriceTickSize = 0.00001m;
        CryptoCandle candle = DelistedPositions.FlatCandle(symbol, new CandleTime(2000), 0.03923m);

        Assert.AreEqual(0.03923m, candle.Close);
        Assert.AreEqual(0.03924m, candle.High);
        Assert.AreEqual(0.03922m, candle.Low);
    }
}
