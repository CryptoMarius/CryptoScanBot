using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;
using HyperLiquidPerpetual = CryptoScanner.Core.Exchange.HyperLiquid.Perpetual.Api;

namespace CryptoScanner.CoreTests.Settings;

/// <summary>
/// The links to Altrady and TradingView for a market an outside party deployed on HyperLiquid.
/// Testers reported on 04-09-2026 that TSLAUSDC.XYZ opened nothing in either app: both file such a
/// market under the deployer, Altrady as HYPERLIQUIDF_USDC_TSLA_XYZ and TradingView as
/// HIP3XYZ:TSLAUSDC.P, where the scanner sent the address of the exchange's own perpetuals.
/// </summary>
[TestClass]
public class SettingsLinksDeployedTests
{
    private const string ExchangeName = "HyperLiquid Perpetual";

    private static (CryptoExternalUrlList Links, Exchange Exchange) Setup()
    {
        CryptoExternalUrlList links = [];
        links[ExchangeName] = HyperLiquidPerpetual.GetExchangeLinks();
        return (links, new Exchange { Id = 1, Name = ExchangeName });
    }

    private static CryptoSymbol CreateSymbol(Exchange exchange, string baseName, string product, string exchangeName)
    {
        return new CryptoSymbol
        {
            Id = 1,
            Status = 1,
            Base = baseName,
            Quote = "USDC",
            Name = product == "" ? $"{baseName}USDC" : $"{baseName}USDC.{product}",
            Product = product,
            Exchange = exchange,
            ExchangeName = exchangeName,
            QuoteData = new CryptoQuoteData { Name = "USDC" },
            PriceTickSize = 0.01m,
            QuantityTickSize = 0.01m,
        };
    }

    private static readonly CryptoInterval Interval30m = new() { Name = "30m", Duration = 30 };


    [TestMethod]
    public void ADeployedMarket_OpensUnderItsDeployerAtAltrady()
    {
        var (links, exchange) = Setup();
        CryptoSymbol tesla = CreateSymbol(exchange, "TSLA", "XYZ", "xyz:TSLA");

        var (url, _) = links.GetExternalRef(exchange, CryptoTradingApp.Altrady, false, tesla, Interval30m);

        Assert.AreEqual("https://app.altrady.com/d/HYPERLIQUIDF_USDC_TSLA_XYZ:30", url);
    }


    [TestMethod]
    public void ADeployedMarket_OpensUnderItsHip3SourceAtTradingView()
    {
        var (links, exchange) = Setup();
        CryptoSymbol avgo = CreateSymbol(exchange, "AVGO", "PARA", "para:AVGO");

        var (url, _) = links.GetExternalRef(exchange, CryptoTradingApp.TradingView, false, avgo, Interval30m);

        Assert.AreEqual("https://www.tradingview.com/chart/?symbol=HIP3PARA:AVGOUSDC.P&interval=30", url);
    }


    /// <summary>
    /// The exchange's own perpetual is a product of ours and keeps the address of the market itself.
    /// </summary>
    [TestMethod]
    public void TheExchangesOwnPerpetual_KeepsTheAddressOfTheMarket()
    {
        var (links, exchange) = Setup();
        CryptoSymbol ada = CreateSymbol(exchange, "ADA", CryptoProduct.Perpetual, "ADA");

        var (altrady, _) = links.GetExternalRef(exchange, CryptoTradingApp.Altrady, false, ada, Interval30m);
        var (tradingView, _) = links.GetExternalRef(exchange, CryptoTradingApp.TradingView, false, ada, Interval30m);

        Assert.AreEqual("https://app.altrady.com/d/HYPERLIQUIDF_USDC_ADA:30", altrady);
        Assert.AreEqual("https://www.tradingview.com/chart/?symbol=HYPERLIQUID:ADAUSDC.P&interval=30", tradingView);
    }


    /// <summary>
    /// An app the deployed template does not mention falls back to the address of the market, the
    /// same way a PerProduct entry does: the exchange's trade page is addressed by ExchangeName.
    /// </summary>
    [TestMethod]
    public void AnAppTheDeployedTemplateLeavesOut_FallsBackToTheMarket()
    {
        var (links, exchange) = Setup();
        CryptoSymbol tesla = CreateSymbol(exchange, "TSLA", "XYZ", "xyz:TSLA");

        var (url, _) = links.GetExternalRef(exchange, CryptoTradingApp.ExchangeUrl, false, tesla, Interval30m);

        Assert.AreEqual("https://app.hyperliquid.xyz/trade/xyz:tsla", url);
    }
}
