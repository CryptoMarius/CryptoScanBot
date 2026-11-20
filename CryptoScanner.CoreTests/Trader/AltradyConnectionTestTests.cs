using CryptoScanner.Core.Exchange.Altrady;

using Newtonsoft.Json.Linq;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// The connection test behind the Test button of the Altrady API key: an open signal for bitcoin with
/// Altrady's own test flag, and the answer turned into one line for the screen.
///
/// Reference: help.altrady.com, "Webhook Signals: Testing and Errors". With `test` on the signal is
/// checked and processed without placing an order; the error texts below are the ones listed there.
/// </summary>
[TestClass]
public class AltradyConnectionTestTests
{
    [TestMethod]
    public void Payload_IsAnOpenSignalWithTheTestFlag()
    {
        JObject payload = AltradyWebhook.BuildTestPayload("HYPE_USDC_BTC", "HYPE", "key", "secret");

        // Without the flag a valid open with valid keys places a real order
        Assert.AreEqual(true, payload.Value<bool>("test"));
        Assert.AreEqual("open", payload.Value<string>("action"));
        Assert.AreEqual("long", payload.Value<string>("side"));
        Assert.AreEqual("market", payload.Value<string>("order_type"));
        Assert.AreEqual("key", payload.Value<string>("api_key"));
        Assert.AreEqual("secret", payload.Value<string>("api_secret"));
        Assert.AreEqual("HYPE", payload.Value<string>("exchange"));
        Assert.AreEqual("HYPE_USDC_BTC", payload.Value<string>("symbol"));
    }

    [TestMethod]
    public void Payload_CarriesNoAmountPriceOrExits()
    {
        JObject payload = AltradyWebhook.BuildTestPayload("HYPE_USDC_BTC", "HYPE", "key", "secret");

        foreach (string field in new[] { "quote_amount", "base_amount", "signal_price", "take_profit", "stop_loss", "dca_orders" })
            Assert.IsNull(payload[field], field);
    }

    [TestMethod]
    public void Answer_AcceptedPositionIsAPass()
    {
        string body = "{\"signalBotPositions\":{\"id\":1,\"coinraySymbol\":\"HYPE_USDC_BTC\",\"status\":\"new\",\"message\":null," +
            "\"signalData\":{\"markAsTest\":true,\"signalId\":\"g-1\"}}}";
        (bool ok, string message) = AltradyWebhook.InterpretTestAnswer(200, body);

        Assert.IsTrue(ok);
        Assert.AreEqual("OK, marked as test (status 200)", message);
    }

    [TestMethod]
    public void Answer_EmptyTwoHundredIsAPass()
    {
        (bool ok, string message) = AltradyWebhook.InterpretTestAnswer(204, "");

        Assert.IsTrue(ok);
        Assert.AreEqual("OK (status 204)", message);
    }

    [TestMethod]
    public void Answer_ErrorMemberIsAFailureWithItsText()
    {
        (bool ok, string message) = AltradyWebhook.InterpretTestAnswer(200, "{\"error\":\"Invalid secret\"}");
        Assert.IsFalse(ok);
        Assert.AreEqual("Invalid secret (status 200)", message);

        (ok, message) = AltradyWebhook.InterpretTestAnswer(404, "{\"error\":\"Signal bot not found\"}");
        Assert.IsFalse(ok);
        Assert.AreEqual("Signal bot not found (status 404)", message);
    }

    [TestMethod]
    public void Answer_HttpErrorWithoutJsonShowsTheRawText()
    {
        (bool ok, string message) = AltradyWebhook.InterpretTestAnswer(500, "Internal Server Error");

        Assert.IsFalse(ok);
        Assert.AreEqual("Internal Server Error (status 500)", message);
    }

    // Altrady creates a position for the test signal despite the test flag (13-11-2026), so the test
    // closes it again, addressed by the signal id of the answer.

    [TestMethod]
    public void SignalId_IsReadFromTheAnswer()
    {
        string body = "{\"signalBotPositions\":{\"id\":61248898,\"signalData\":{\"markAsTest\":true,\"signalId\":\"g-ec7c4df9\"}}}";

        Assert.AreEqual("g-ec7c4df9", AltradyWebhook.TestSignalIdOf(body));
    }

    [TestMethod]
    public void SignalId_IsEmptyWithoutOne()
    {
        Assert.AreEqual("", AltradyWebhook.TestSignalIdOf(""));
        Assert.AreEqual("", AltradyWebhook.TestSignalIdOf("{\"error\":\"Invalid secret\"}"));
        Assert.AreEqual("", AltradyWebhook.TestSignalIdOf("Internal Server Error"));
        Assert.AreEqual("", AltradyWebhook.TestSignalIdOf("{broken"));
    }

    [TestMethod]
    public void ClosePayload_AddressesOnlyTheTestPosition()
    {
        JObject payload = AltradyWebhook.BuildTestClosePayload("HYPE_USDC_BTC", "HYPE", "key", "secret", "g-1");

        Assert.AreEqual("close", (string?)payload["action"]);
        Assert.AreEqual("g-1", (string?)payload["signal_id"]);
        Assert.AreEqual("HYPE_USDC_BTC", (string?)payload["symbol"]);
        Assert.AreEqual("HYPE", (string?)payload["exchange"]);
        Assert.AreEqual("long", (string?)payload["side"]);
        Assert.AreEqual("market", (string?)payload["order_type"]);
        Assert.IsNull(payload["price"], "a market close with a price is refused by Altrady");
    }
}
