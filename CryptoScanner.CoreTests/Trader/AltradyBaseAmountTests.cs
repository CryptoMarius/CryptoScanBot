using CryptoScanner.Core.Exchange.Altrady;
using CryptoScanner.Core.Model;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// The conversion of the quote entry amount into the base_amount that the Altrady webhook sends
/// for a market entry. Background: on HyperLiquid Perpetual (05-09-2026) every long market entry
/// sent with quote_amount came back with "Market order doesn't support quote currency".
/// </summary>
[TestClass]
public class AltradyBaseAmountTests
{
    private static CryptoSymbol CreateSymbol(decimal tick, decimal minimum, decimal maximum)
    {
        var exchange = new Exchange { Id = 1, Name = "TestExchange" };
        var quoteData = new CryptoQuoteData { Name = "USDC" };
        return new CryptoSymbol
        {
            Id = 1,
            Status = 1,
            Base = "TEST",
            Quote = "USDC",
            Name = "TESTUSDC.PERP",
            Exchange = exchange,
            ExchangeName = exchange.Name,
            QuoteData = quoteData,
            PriceTickSize = 0.000001m,
            PriceMinimum = 0m,
            PriceMaximum = 0m,
            QuantityTickSize = tick,
            QuantityMinimum = minimum,
            QuantityMaximum = maximum,
        };
    }


    [TestMethod]
    public void OnGridValueIsNotRaisedByATick()
    {
        var symbol = CreateSymbol(tick: 1m, minimum: 1m, maximum: 0m);

        decimal? result = AltradyWebhook.CalculateBaseAmount(symbol, quoteAmount: 15m, price: 0.01m);

        Assert.AreEqual(1500m, result);
    }


    [TestMethod]
    public void OffGridValueRoundsUpSoTheQuoteAmountIsCovered()
    {
        // 15 / 3.3 = 4.5454..; a plain Clamp gives 4.54 (14.982 USDC), one tick up gives 4.55
        var symbol = CreateSymbol(tick: 0.01m, minimum: 0.01m, maximum: 0m);

        decimal? result = AltradyWebhook.CalculateBaseAmount(symbol, quoteAmount: 15m, price: 3.3m);

        Assert.AreEqual(4.55m, result);
        Assert.IsTrue(result!.Value * 3.3m >= 15m, "the base amount must buy at least the quote amount");
    }


    [TestMethod]
    public void KBonkExampleFromTheLog()
    {
        // The refused kBONK long of 05-09-2026: 15.000027 USDC, the expiry price 0.003529225 is
        // the entry price plus 7.5%, so the entry was 0.003283. Whole-token tick as on HyperLiquid.
        // 4569 * 0.003283 = 15.000027 exactly: the quote amount in the log was already the paper
        // trade's quantity times its price, so the conversion lands on the grid and gives that
        // quantity back without an extra tick.
        var symbol = CreateSymbol(tick: 1m, minimum: 1m, maximum: 0m);

        decimal? result = AltradyWebhook.CalculateBaseAmount(symbol, quoteAmount: 15.000027m, price: 0.003283m);

        Assert.AreEqual(4569m, result);
    }


    [TestMethod]
    public void MaximumStillWinsOverTheExtraTick()
    {
        var symbol = CreateSymbol(tick: 0.01m, minimum: 0.01m, maximum: 10m);

        decimal? result = AltradyWebhook.CalculateBaseAmount(symbol, quoteAmount: 100m, price: 1m);

        Assert.AreEqual(10m, result);
    }


    [TestMethod]
    public void InvalidInputsGiveNullSoTheCallerFallsBackToQuoteAmount()
    {
        var symbol = CreateSymbol(tick: 0.01m, minimum: 0.01m, maximum: 0m);

        Assert.IsNull(AltradyWebhook.CalculateBaseAmount(symbol, quoteAmount: 0m, price: 1m));
        Assert.IsNull(AltradyWebhook.CalculateBaseAmount(symbol, quoteAmount: 15m, price: 0m));
    }
}
