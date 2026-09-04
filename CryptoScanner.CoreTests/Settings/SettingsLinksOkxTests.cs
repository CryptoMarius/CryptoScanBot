using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;
using OkxPerpetual = CryptoScanner.Core.Exchange.Okx.Perpetual.Api;

namespace CryptoScanner.CoreTests.Settings;

/// <summary>
/// The links of Okx Perpetual, whose instruments come in two contract forms that the outside world
/// names differently: the swap (BTC-USDT-SWAP, TradingView OKX:BTCUSDT.P) and the X-Perp
/// (ACT-USD_UM_XPERP-310704, TradingView OKX:ACTUSD.UMN2031). Two things went wrong until
/// 04-09-2026: the X-Perp link named the family (ACTUSD.UM), which opens an empty chart, and a
/// TradFi X-Perp was routed by its product to the swap templates.
/// </summary>
[TestClass]
public class SettingsLinksOkxTests
{
    private const string ExchangeName = "Okx Perpetual";

    private static (CryptoExternalUrlList Links, Exchange Exchange) Setup()
    {
        CryptoExternalUrlList links = [];
        links[ExchangeName] = OkxPerpetual.GetExchangeLinks();
        return (links, new Exchange { Id = 1, Name = ExchangeName });
    }

    private static CryptoSymbol CreateSymbol(Exchange exchange, string baseName, string quote, string product, string exchangeName)
    {
        return new CryptoSymbol
        {
            Id = 1,
            Status = 1,
            Base = baseName,
            Quote = quote,
            Name = $"{baseName}{quote}.{product}",
            Product = product,
            Exchange = exchange,
            ExchangeName = exchangeName,
            QuoteData = new CryptoQuoteData { Name = quote },
            PriceTickSize = 0.01m,
            QuantityTickSize = 0.01m,
        };
    }

    private static readonly CryptoInterval Interval15m = new() { Name = "15m", Duration = 15 };


    [TestMethod]
    [DataRow("310704", "N2031")]
    [DataRow("310801", "Q2031")]
    [DataRow("310410", "J2031")]
    [DataRow("261231", "Z2026")]
    [DataRow("", "")]
    [DataRow("SWAP", "")]
    [DataRow("311304", "")]
    public void ExpiryCodeOf_SpellsTheContractTheWayAFuturesChartDoes(string expiry, string expected)
    {
        Assert.AreEqual(expected, CryptoExternalUrlList.ExpiryCodeOf(expiry));
    }


    [TestMethod]
    public void AnXPerp_OpensTheContractAtTradingView_NotTheFamily()
    {
        var (links, exchange) = Setup();
        CryptoSymbol act = CreateSymbol(exchange, "ACT", "USDC", CryptoProduct.XPerp, "ACT-USD_UM_XPERP-310704");

        var (url, _) = links.GetExternalRef(exchange, CryptoTradingApp.TradingView, false, act, Interval15m);

        Assert.AreEqual("https://www.tradingview.com/chart/?symbol=OKX:ACTUSD.UMN2031&interval=15", url);
    }


    /// <summary>
    /// The product says share, the instrument name says X-Perp; the links follow the instrument.
    /// </summary>
    [TestMethod]
    public void ATradFiXPerp_TakesTheXPerpAddresses()
    {
        var (links, exchange) = Setup();
        CryptoSymbol pltr = CreateSymbol(exchange, "PLTR", "USDC", CryptoProduct.TradFi, "PLTR-USD_UM_XPERP-310801");

        var (tradingView, _) = links.GetExternalRef(exchange, CryptoTradingApp.TradingView, false, pltr, Interval15m);
        var (altrady, _) = links.GetExternalRef(exchange, CryptoTradingApp.Altrady, false, pltr, Interval15m);
        var (okx, _) = links.GetExternalRef(exchange, CryptoTradingApp.ExchangeUrl, false, pltr, Interval15m);

        Assert.AreEqual("https://www.tradingview.com/chart/?symbol=OKX:PLTRUSD.UMQ2031&interval=15", tradingView);
        Assert.AreEqual("https://app.altrady.com/d/OKEXF_USDC_PLTR_UM-XPERP-310801:15", altrady);
        Assert.AreEqual("https://www.okx.com/trade-futures/pltr-usd_um_xperp-310801", okx);
    }


    [TestMethod]
    public void ATradFiSwap_KeepsTheSwapAddresses()
    {
        var (links, exchange) = Setup();
        CryptoSymbol asts = CreateSymbol(exchange, "ASTS", "USDT", CryptoProduct.TradFi, "ASTS-USDT-SWAP");

        var (tradingView, _) = links.GetExternalRef(exchange, CryptoTradingApp.TradingView, false, asts, Interval15m);
        var (altrady, _) = links.GetExternalRef(exchange, CryptoTradingApp.Altrady, false, asts, Interval15m);

        Assert.AreEqual("https://www.tradingview.com/chart/?symbol=OKX:ASTSUSDT.P&interval=15", tradingView);
        Assert.AreEqual("https://app.altrady.com/d/OKEXF_USDT_ASTS_SWAP:15", altrady);
    }


    [TestMethod]
    public void ASwap_IsUnchanged()
    {
        var (links, exchange) = Setup();
        CryptoSymbol btc = CreateSymbol(exchange, "BTC", "USDT", CryptoProduct.Perpetual, "BTC-USDT-SWAP");

        var (url, _) = links.GetExternalRef(exchange, CryptoTradingApp.TradingView, false, btc, Interval15m);

        Assert.AreEqual("https://www.tradingview.com/chart/?symbol=OKX:BTCUSDT.P&interval=15", url);
    }
}
