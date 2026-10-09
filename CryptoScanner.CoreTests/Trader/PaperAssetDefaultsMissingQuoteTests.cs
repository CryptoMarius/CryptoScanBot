using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// The warning when the default paper assets leave out the coin the exchange trades in: on
/// 09-10-2026 a reset on HyperLiquid handed out USDT while every symbol there trades in USDC, and
/// every signal was refused.
/// </summary>
[TestClass]
public class PaperAssetDefaultsMissingQuoteTests
{
    private static List<CryptoQuoteData> Quotes(params (string name, bool fetch)[] q)
        => q.Select(x => new CryptoQuoteData { Name = x.name, FetchCandles = x.fetch }).ToList();

    [TestMethod]
    public void ATradedQuoteMissingFromTheDefaults_IsReported()
    {
        var missing = PaperAssets.TradedQuotesMissing(Quotes(("USDC", true)),
            [new CryptoPaperAssetDefault { Name = "USDT", Total = 10000m }]);
        CollectionAssert.AreEqual(new[] { "USDC" }, missing);
    }

    [TestMethod]
    public void ATradedQuoteInTheDefaults_IsFine()
    {
        var missing = PaperAssets.TradedQuotesMissing(Quotes(("USDC", true), ("USDT", false)),
            [new CryptoPaperAssetDefault { Name = "usdc", Total = 10000m }]);
        Assert.AreEqual(0, missing.Count);
    }

    [TestMethod]
    public void ADefaultWithoutAnAmount_DoesNotCount()
    {
        var missing = PaperAssets.TradedQuotesMissing(Quotes(("USDC", true)),
            [new CryptoPaperAssetDefault { Name = "USDC", Total = 0m }]);
        CollectionAssert.AreEqual(new[] { "USDC" }, missing);
    }
}
