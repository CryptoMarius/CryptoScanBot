using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange.Altrady;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// The close signal the scanner sends when it is finished with a delegated position. The scanner
/// owns the exit and Altrady only executes, so every position that was really delegated has to be
/// closed on their side as well - their webhook has no action that moves a running stop.
///
/// Field reference: help.altrady.com, "Webhook Signal Reference: Open, Increase, Reduce, Close, and
/// Reverse". A close needs the credentials, the action, the market and an order type; a market order
/// type must NOT be combined with a price.
/// </summary>
[TestClass]
public class AltradyCloseSignalTests
{
    private static CryptoPosition BuildPosition(CryptoTradeSide side, string? altradyId)
    {
        var exchange = new Exchange { Id = 1, Name = "HyperLiquid Perpetual" };
        var quoteData = new CryptoQuoteData { Name = "USDC" };
        var symbol = new CryptoSymbol
        {
            Id = 1,
            Status = 1,
            Base = "INJ",
            Quote = "USDC",
            Name = "INJUSDC.PERP",
            Exchange = exchange,
            ExchangeName = "INJ",
            QuoteData = quoteData,
            PriceTickSize = 0.001m,
        };
        return new CryptoPosition
        {
            Symbol = symbol,
            Exchange = exchange,
            Side = side,
            Interval = new CryptoInterval { Id = 1, Name = "5m", IntervalPeriod = CryptoIntervalPeriod.interval5m, Duration = 5 },
            AltradyPositionId = altradyId,
        };
    }


    [TestMethod]
    public void CloseSignalCarriesEverythingAltradyRequires()
    {
        var position = BuildPosition(CryptoTradeSide.Long, "g-e7b31217-b693-4ac1-94de-74360bad6ae8");

        var payload = AltradyWebhook.BuildClosePayload(position, "HYPE", "key", "secret");

        Assert.AreEqual("close", (string?)payload["action"]);
        Assert.AreEqual("key", (string?)payload["api_key"]);
        Assert.AreEqual("secret", (string?)payload["api_secret"]);
        Assert.AreEqual("HYPE", (string?)payload["exchange"]);
        Assert.AreEqual("HYPE_USDC_INJ", (string?)payload["symbol"], "symbol is exchange_quote_base");
        Assert.AreEqual("long", (string?)payload["side"]);
        Assert.AreEqual("market", (string?)payload["order_type"], "order type is required on a close");
        Assert.IsNull(payload["price"], "a market close must not carry a price");
    }


    [TestMethod]
    public void CloseSignalAddressesOnePositionByItsSignalId()
    {
        var position = BuildPosition(CryptoTradeSide.Short, "g-35e03e64-37cf-4ca7-8b71-abeb1055cb6e");

        var payload = AltradyWebhook.BuildClosePayload(position, "BIFU", "key", "secret");

        Assert.AreEqual("short", (string?)payload["side"]);
        Assert.AreEqual("g-35e03e64-37cf-4ca7-8b71-abeb1055cb6e", (string?)payload["signal_id"]);
    }


    [TestMethod]
    public void WithoutAnIdTheSignalCarriesNoSignalId()
    {
        // Altrady then closes every position of this market and side that its bot opened, which is
        // what we want when the id was lost; the caller only sends a close when an id is present.
        var position = BuildPosition(CryptoTradeSide.Long, null);

        var payload = AltradyWebhook.BuildClosePayload(position, "BIFU", "key", "secret");

        Assert.IsNull(payload["signal_id"]);
    }

    [TestMethod]
    public void AnEmptySuccessAnswerIsAnAcceptedClose()
    {
        // Every close of 27-09-2026 came back with an empty body; that is a receipt, not a refusal.
        Assert.IsTrue(AltradyWebhook.IsCloseAccepted(200, ""));
        Assert.IsTrue(AltradyWebhook.IsCloseAccepted(204, ""));
    }


    [TestMethod]
    public void AnErrorMemberOrAnErrorStatusIsARefusedClose()
    {
        Assert.IsFalse(AltradyWebhook.IsCloseAccepted(200, "{\"error\":\"Too many positions opened\"}"));
        Assert.IsFalse(AltradyWebhook.IsCloseAccepted(400, ""));
        Assert.IsFalse(AltradyWebhook.IsCloseAccepted(500, "Internal Server Error"));
    }


    [TestMethod]
    public void StopLossBlockHandsTheProfitLockToAltrady()
    {
        // The live settings of 26-09-2026: trailing method, trigger at 6,5% and 1,5% behind price.
        var block = AltradyWebhook.BuildStopLossBlock(8m, true, CryptoProfitLockMethod.TrailingPercentage, 6.5m, 1.5m);

        Assert.AreEqual(8m, (decimal?)block["stop_percentage"], "the initial stop stays what it was");
        Assert.AreEqual("PRICE", (string?)block["protection_type"], "Follow Price is their trailing stop");
        Assert.AreEqual(6.5m, (decimal?)block["trailing_percentage"], "trailing starts at our trigger");
        Assert.AreEqual(1.5m, (decimal?)block["trailing_distance"], "and follows at our distance");
    }


    [TestMethod]
    public void TheFixedProfitLockIsNotHandedOver()
    {
        // Break even plus a percentage has no counterpart at Altrady: their BREAK_EVEN mode moves
        // the stop when a take profit fills and needs two or more targets.
        var block = AltradyWebhook.BuildStopLossBlock(8m, true, CryptoProfitLockMethod.Fixed, 6.5m, 1.5m);

        Assert.AreEqual(8m, (decimal?)block["stop_percentage"]);
        Assert.IsNull(block["protection_type"]);
    }


    [TestMethod]
    public void WithoutAProfitLockTheStopStaysWhereItIs()
    {
        var block = AltradyWebhook.BuildStopLossBlock(8m, false, CryptoProfitLockMethod.TrailingPercentage, 6.5m, 1.5m);

        Assert.AreEqual(8m, (decimal?)block["stop_percentage"]);
        Assert.IsNull(block["protection_type"]);
        Assert.IsNull(block["trailing_distance"]);
    }


    [TestMethod]
    public void FollowTakeProfitIsHandedToAltrady()
    {
        var block = AltradyWebhook.BuildStopLossBlock(5m, true, CryptoProfitLockMethod.FollowTakeProfit, 6.5m, 1.5m, 3);

        Assert.AreEqual(5m, (decimal?)block["stop_percentage"]);
        Assert.AreEqual("FOLLOW_TAKE_PROFIT", (string?)block["protection_type"]);
        Assert.IsNull(block["trailing_percentage"], "their take profit protection has no trigger of its own");
        Assert.IsNull(block["trailing_distance"]);
    }


    [TestMethod]
    public void BreakEvenAfterTheFirstTakeProfitIsHandedToAltrady()
    {
        var block = AltradyWebhook.BuildStopLossBlock(5m, true, CryptoProfitLockMethod.BreakEvenAfterFirstTakeProfit, 6.5m, 1.5m, 2);

        Assert.AreEqual("BREAK_EVEN", (string?)block["protection_type"]);
        Assert.IsNull(block["trailing_percentage"]);
    }


    [TestMethod]
    public void TheTakeProfitProtectionsNeedTwoTargets()
    {
        // Altrady: with one take profit the stop never moves, so nothing is sent
        var block = AltradyWebhook.BuildStopLossBlock(5m, true, CryptoProfitLockMethod.FollowTakeProfit, 6.5m, 1.5m, 1);

        Assert.AreEqual(5m, (decimal?)block["stop_percentage"]);
        Assert.IsNull(block["protection_type"]);
    }


    [TestMethod]
    public void TheLastTakeProfitTrailsAtAltrady()
    {
        var levels = new List<CryptoTpEntry>
        {
            new() { Percentage = 5m, Factor = 25m },
            new() { Percentage = 10m, Factor = 25m },
            new() { Percentage = 24m, Factor = 50m },
        };
        var block = AltradyWebhook.BuildTakeProfitBlock(levels, 3m);

        Assert.AreEqual(3, block.Count);
        Assert.IsNull(block[0]["trailing_distance"], "only the last take profit may trail");
        Assert.IsNull(block[1]["trailing_distance"]);
        Assert.AreEqual(3m, (decimal?)block[2]["trailing_distance"]);
        Assert.AreEqual(24m, (decimal?)block[2]["price_percentage"], "trailing starts at the last target");
        Assert.AreEqual(50m, (decimal?)block[2]["position_percentage"]);
    }


    [TestMethod]
    public void WithoutTrailingTheTakeProfitsAreSentAsTheyAre()
    {
        var levels = new List<CryptoTpEntry> { new() { Percentage = 24m, Factor = 100m } };
        var block = AltradyWebhook.BuildTakeProfitBlock(levels, 0m);

        Assert.AreEqual(1, block.Count);
        Assert.IsNull(block[0]["trailing_distance"]);
        Assert.AreEqual(24m, (decimal?)block[0]["price_percentage"]);
    }


    [TestMethod]
    public void AltradyGetsItsOwnEntryAmountWhenItIsSet()
    {
        Assert.AreEqual(100m, AltradyWebhook.AltradyAmount(15m, 100m));
        Assert.AreEqual(15m, AltradyWebhook.AltradyAmount(15m, 0m), "0 = the scanner's own entry amount");
    }


    [TestMethod]
    public void TakeProfitsBelowTheExchangeMinimumAreReported()
    {
        var levels = new List<CryptoTpEntry>
        {
            new() { Percentage = 5m, Factor = 25m }, new() { Percentage = 10m, Factor = 25m },
            new() { Percentage = 20m, Factor = 25m }, new() { Percentage = 40m, Factor = 25m },
        };
        Assert.AreEqual(4, AltradyWebhook.TakeProfitsBelowMinimum(levels, 15m, 5m, "USDT").Count, "15 / 4 = 3.75 < 5");
        Assert.AreEqual(0, AltradyWebhook.TakeProfitsBelowMinimum(levels, 100m, 5m, "USDT").Count, "100 / 4 = 25 >= 5");
        Assert.AreEqual(0, AltradyWebhook.TakeProfitsBelowMinimum(levels, 15m, 0m, "USDT").Count, "no minimum known");
        // Only a minimum quantity known: 15 / 4 = 3.75 USDT at price 1 is 3.75 coins, below 10
        Assert.AreEqual(4, AltradyWebhook.TakeProfitsBelowMinimum(levels, 15m, 0m, "USDT", 1m, 10m, "ABC").Count);
        Assert.AreEqual(0, AltradyWebhook.TakeProfitsBelowMinimum(levels, 100m, 0m, "USDT", 1m, 10m, "ABC").Count);
    }
}
