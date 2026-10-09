using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Which positions the "Position close" menu item may act on, and which route the close takes:
/// an open position leaves through the trader's own exit door, a position that is already closed
/// here can only be closed at Altrady, and only when it really got there (it has an Altrady id)
/// in a mode that delegates to Altrady.
/// </summary>
[TestClass]
public class ManualExitTests
{
    private CryptoTradeVia _savedTradeVia;
    private bool _savedEmulatorMode;

    [TestInitialize]
    public void SaveGlobals()
    {
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

    private static CryptoPosition BuildPosition(DateTime? closeTime, string? altradyId)
    {
        var exchange = new Exchange { Id = 1, Name = "HyperLiquid Perpetual" };
        var symbol = new CryptoSymbol
        {
            Id = 1,
            Base = "INJ",
            Quote = "USDC",
            Name = "INJUSDC.PERP",
            Exchange = exchange,
            ExchangeName = "INJ",
            QuoteData = new CryptoQuoteData { Name = "USDC" },
        };
        return new CryptoPosition
        {
            Id = 7,
            Symbol = symbol,
            Exchange = exchange,
            Side = CryptoTradeSide.Long,
            Interval = new CryptoInterval { Id = 1, Name = "5m", IntervalPeriod = CryptoIntervalPeriod.interval5m, Duration = 5 },
            CloseTime = closeTime,
            AltradyPositionId = altradyId,
        };
    }


    [TestMethod]
    public void OpenPositionCanAlwaysBeClosed()
    {
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;
        var position = BuildPosition(closeTime: null, altradyId: null);

        Assert.IsTrue(ManualExit.CanExit(position));
        Assert.IsFalse(ManualExit.CanCloseAtAltrady(position), "no Altrady id, so nothing to close there");
        StringAssert.Contains(ManualExit.ConfirmationText(position), "current price");
    }


    [TestMethod]
    public void ClosedPositionWithoutAltradyIdHasNothingLeftToClose()
    {
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTradingAndAltrady;
        var position = BuildPosition(closeTime: DateTime.UtcNow, altradyId: null);

        Assert.IsFalse(ManualExit.CanExit(position));
    }


    [TestMethod]
    public void ClosedPositionWithAltradyIdCanBeClosedAtAltrady()
    {
        var position = BuildPosition(closeTime: DateTime.UtcNow, altradyId: "g-e7b31217-b693-4ac1-94de-74360bad6ae8");

        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.Altrady;
        Assert.IsTrue(ManualExit.CanExit(position));
        Assert.IsTrue(ManualExit.CanCloseAtAltrady(position));
        StringAssert.Contains(ManualExit.ConfirmationText(position), "Altrady");

        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTradingAndAltrady;
        Assert.IsTrue(ManualExit.CanCloseAtAltrady(position));
    }


    [TestMethod]
    public void AltradyCloseIsNotOfferedOutsideAnAltradyMode()
    {
        var position = BuildPosition(closeTime: DateTime.UtcNow, altradyId: "g-e7b31217-b693-4ac1-94de-74360bad6ae8");

        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;
        Assert.IsFalse(ManualExit.CanCloseAtAltrady(position));

        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.RealTrading;
        Assert.IsFalse(ManualExit.CanCloseAtAltrady(position));
    }


    [TestMethod]
    public void EmulatorNeverClosesAtAltrady()
    {
        var position = BuildPosition(closeTime: DateTime.UtcNow, altradyId: "g-e7b31217-b693-4ac1-94de-74360bad6ae8");
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.Altrady;

        GlobalData.IsEmulatorMode = true;
        Assert.IsFalse(ManualExit.CanCloseAtAltrady(position));
        Assert.IsFalse(ManualExit.CanExit(position));
    }
}
