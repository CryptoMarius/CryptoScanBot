using CryptoScanner.Core.Exchange.Altrady;

using System.Net;
using System.Net.Sockets;

namespace CryptoScanner.CoreTests.Exchanges;

/// <summary>
/// The translation of our Altrady addresses into the route the local WebSocket of the desktop
/// application wants, and the reading of its two answers. The socket itself is only tested for
/// the case that matters most in practice: nobody listening, which must answer false quickly
/// instead of throwing, because that is what hands the click to the hidden browser.
/// </summary>
[TestClass]
public class AltradyDeepLinkTests
{
    [TestMethod]
    public void RouteOf_ShortWebLinkWithMinutes_BecomesTradeRouteWithResolution()
    {
        Assert.AreEqual("#/trade/BINA_USDT_BTC?resolution=60", AltradyDeepLink.RouteOf("https://app.altrady.com/d/BINA_USDT_BTC:60"));
    }

    [TestMethod]
    public void RouteOf_ShortWebLinkWithProductAndExpiry_KeepsTheWholeSymbol()
    {
        Assert.AreEqual("#/trade/OKEXF_USDC_PLTR_UM-XPERP-310801?resolution=15",
            AltradyDeepLink.RouteOf("https://app.altrady.com/d/OKEXF_USDC_PLTR_UM-XPERP-310801:15"));
        Assert.AreEqual("#/trade/HYPERLIQUIDF_USDC_TSLA_XYZ?resolution=30",
            AltradyDeepLink.RouteOf("https://app.altrady.com/d/HYPERLIQUIDF_USDC_TSLA_XYZ:30"));
    }

    [TestMethod]
    public void RouteOf_DayAndWeek_UseTheLetterCodes()
    {
        Assert.AreEqual("#/trade/BINA_USDT_BTC?resolution=D", AltradyDeepLink.RouteOf("https://app.altrady.com/d/BINA_USDT_BTC:1440"));
        Assert.AreEqual("#/trade/BINA_USDT_BTC?resolution=W", AltradyDeepLink.RouteOf("https://app.altrady.com/d/BINA_USDT_BTC:10080"));
        Assert.AreEqual("#/trade/BINA_USDT_BTC?resolution=240", AltradyDeepLink.RouteOf("https://app.altrady.com/d/BINA_USDT_BTC:240"));
    }

    [TestMethod]
    public void RouteOf_ShortWebLinkWithoutInterval_HasNoResolution()
    {
        Assert.AreEqual("#/trade/BINA_USDT_BTC", AltradyDeepLink.RouteOf("https://app.altrady.com/d/BINA_USDT_BTC"));
    }

    [TestMethod]
    public void RouteOf_DashboardFragmentForms_AreTakenLiterally()
    {
        Assert.AreEqual("#/trade/OKEXF_USDT_SHIB_SWAP?resolution=5",
            AltradyDeepLink.RouteOf("https://app.altrady.com/dashboard#/d/OKEXF_USDT_SHIB_SWAP?resolution=5"));
        Assert.AreEqual("#/trade/BINA_USDT_BTC?resolution=D",
            AltradyDeepLink.RouteOf("https://app.altrady.com/dashboard#/o/trade/BINA_USDT_BTC?resolution=D"));
        Assert.AreEqual("#/bots", AltradyDeepLink.RouteOf("https://app.altrady.com/dashboard#/o/bots"));
        Assert.AreEqual("#/chat/1234?message=5678", AltradyDeepLink.RouteOf("https://app.altrady.com/dashboard#/o/chat/1234?message=5678"));
    }

    [TestMethod]
    public void RouteOf_ProtocolLinks_BecomeRoutes()
    {
        Assert.AreEqual("#/trade/BINA_USDT_BTC?resolution=240", AltradyDeepLink.RouteOf("altrady://trade/BINA_USDT_BTC?resolution=240"));
        Assert.AreEqual("#/trade/BINA_USDT_BTC", AltradyDeepLink.RouteOf("altrady://open/#/trade/BINA_USDT_BTC"));
        Assert.AreEqual("#/settings/profile", AltradyDeepLink.RouteOf("altrady://settings/profile"));
    }

    [TestMethod]
    public void RouteOf_SomethingElse_IsNull()
    {
        Assert.IsNull(AltradyDeepLink.RouteOf(null));
        Assert.IsNull(AltradyDeepLink.RouteOf(""));
        Assert.IsNull(AltradyDeepLink.RouteOf("https://www.tradingview.com/chart/?symbol=BINANCE:BTCUSDT&interval=60"));
        Assert.IsNull(AltradyDeepLink.RouteOf("https://www.binance.com/en/trade/BTC_USDT"));
        // The Altrady website itself is not a deep link
        Assert.IsNull(AltradyDeepLink.RouteOf("https://app.altrady.com/"));
        Assert.IsNull(AltradyDeepLink.RouteOf("https://app.altrady.com/dashboard"));
        Assert.IsNull(AltradyDeepLink.RouteOf("not a url at all"));
    }

    [TestMethod]
    public void IsPong_OnlyForAltradysOwnAnswer()
    {
        Assert.IsTrue(AltradyDeepLink.IsPong("{\"type\":\"pong\",\"application\":\"altrady\",\"app\":\"5.4.0\"}"));
        Assert.IsTrue(AltradyDeepLink.IsPong("{\"application\":\"Altrady\",\"type\":\"PONG\"}"));
        // Something else on the port
        Assert.IsFalse(AltradyDeepLink.IsPong("{\"type\":\"pong\"}"));
        Assert.IsFalse(AltradyDeepLink.IsPong("{\"type\":\"pong\",\"application\":\"other\"}"));
        Assert.IsFalse(AltradyDeepLink.IsPong("pong"));
        Assert.IsFalse(AltradyDeepLink.IsPong(""));
        Assert.IsFalse(AltradyDeepLink.IsPong("[1,2]"));
    }

    [TestMethod]
    public void IsDeepLinkAccepted_OnlyForOk()
    {
        Assert.IsTrue(AltradyDeepLink.IsDeepLinkAccepted("{\"type\":\"deepLink\",\"result\":\"ok\"}"));
        Assert.IsFalse(AltradyDeepLink.IsDeepLinkAccepted("{\"type\":\"deepLink\",\"result\":\"error\"}"));
        Assert.IsFalse(AltradyDeepLink.IsDeepLinkAccepted("{\"type\":\"pong\",\"result\":\"ok\"}"));
        Assert.IsFalse(AltradyDeepLink.IsDeepLinkAccepted("{broken"));
    }

    [TestMethod]
    public async Task TrySendAsync_NobodyListening_AnswersFalseWithoutThrowing()
    {
        // A port that was free a moment ago; nothing listens on it when the client connects
        int port = FreePort();

        DateTime start = DateTime.UtcNow;
        bool accepted = await AltradyDeepLink.TrySendAsync("#/trade/BINA_USDT_BTC?resolution=60", port);
        TimeSpan elapsed = DateTime.UtcNow - start;

        Assert.IsFalse(accepted);
        Assert.IsTrue(elapsed < AltradyDeepLink.Timeout + TimeSpan.FromSeconds(2), $"took {elapsed.TotalSeconds:0.0} seconds");
    }

    [TestMethod]
    public void Open_NotAnAltradyLink_GoesStraightToTheFallback()
    {
        string? handed = null;
        AltradyDeepLink.Open("https://www.tradingview.com/chart/?symbol=BINANCE:BTCUSDT", url => handed = url);
        Assert.AreEqual("https://www.tradingview.com/chart/?symbol=BINANCE:BTCUSDT", handed);
    }

    private static int FreePort()
    {
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
