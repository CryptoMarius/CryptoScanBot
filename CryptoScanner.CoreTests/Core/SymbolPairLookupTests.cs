using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

// CryptoExchange is a package namespace as well, so the model type gets an alias here
using TestExchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// A symbol name carries its product behind a dot (BTCUSDT.PERP), but a whole row of callers only
/// ever has a base and a quote: the barometer, the pause symbol, a Telegram command, a stored chart
/// session, the dashboard, the emulator run configuration and the hand-edited queue. Those cannot
/// build the name themselves - they do not know which of the instruments on the pair they mean - so
/// <see cref="TestExchange.TryGetSymbolByPair"/> answers that question for them.
/// <para>
/// It is the one lookup that replaced every direct SymbolListName.TryGetValue, which is why its
/// three steps are pinned down here: the name as given, the pair plus the product of this market,
/// and the pair against the products this market turned out to carry - that last one only when
/// exactly one of them matches.
/// </para>
/// </summary>
[TestClass]
public class SymbolPairLookupTests
{
    private static TestExchange CreateExchange(CryptoTradingType tradingType) => new()
    {
        Id = 1,
        Name = "Test " + tradingType,
        TradingType = tradingType,
    };


    /// <summary>
    /// Registers a symbol the way GlobalData.AddSymbol does: in all three indexes, and its product
    /// in the set the market keeps. Everything the lookup reads comes from there.
    /// </summary>
    private static CryptoSymbol AddSymbol(TestExchange exchange, int id, string baseAsset, string quote, string product)
    {
        string pair = baseAsset + quote;
        CryptoSymbol symbol = new()
        {
            Id = id,
            Exchange = exchange,
            ExchangeId = exchange.Id,
            Name = product.Length > 0 ? pair + CryptoProduct.Separator + product : pair,
            Base = baseAsset,
            Quote = quote,
            Product = product,
            ExchangeName = "instrument-" + id,
            QuoteData = new CryptoQuoteData { Name = quote },
            Status = 1,
        };
        exchange.SymbolListId.Add(symbol.Id, symbol);
        exchange.SymbolListName.Add(symbol.Name, symbol);
        exchange.SymbolListExchangeName.Add(symbol.ExchangeName, symbol);

        // Barometer symbols carry no product, and counting them would make a market of one product
        // look like a market of two
        if (symbol.Product.Length > 0)
            exchange.Products.Add(symbol.Product);
        return symbol;
    }


    /// <summary>
    /// A caller that does have the full name gets that symbol back, so the lookup can be used
    /// everywhere without knowing which of the two spellings it is holding.
    /// </summary>
    [TestMethod]
    public void FullNameIsFoundAsGiven()
    {
        TestExchange exchange = CreateExchange(CryptoTradingType.Perpetual);
        CryptoSymbol perp = AddSymbol(exchange, 1, "BTC", "USDT", CryptoProduct.Perpetual);

        Assert.IsTrue(exchange.TryGetSymbolByPair("BTCUSDT.PERP", out CryptoSymbol? found));
        Assert.AreSame(perp, found);
    }


    /// <summary>
    /// The normal case: a bare pair lands on the product this market is about. This is what keeps
    /// every configuration file, queue entry and Telegram command written before the dot existed
    /// working exactly as it did.
    /// </summary>
    [TestMethod]
    public void BarePairFindsThisMarketsOwnProduct()
    {
        TestExchange exchange = CreateExchange(CryptoTradingType.Perpetual);
        CryptoSymbol perp = AddSymbol(exchange, 1, "BTC", "USDT", CryptoProduct.Perpetual);

        Assert.IsTrue(exchange.TryGetSymbolByPair("BTCUSDT", out CryptoSymbol? found));
        Assert.AreSame(perp, found);
    }


    /// <summary>
    /// The same pair on a spot market answers with the spot instrument. The product comes from the
    /// market and not from the caller, which is the whole reason this lookup sits on the exchange.
    /// </summary>
    [TestMethod]
    public void OnASpotMarketTheSamePairFindsTheSpotInstrument()
    {
        TestExchange exchange = CreateExchange(CryptoTradingType.Spot);
        CryptoSymbol spot = AddSymbol(exchange, 1, "BTC", "USDT", CryptoProduct.Spot);

        Assert.IsTrue(exchange.TryGetSymbolByPair("BTCUSDT", out CryptoSymbol? found));
        Assert.AreSame(spot, found);
        Assert.AreEqual("BTCUSDT.SPOT", found.Name);
    }


    /// <summary>
    /// A barometer symbol is ours rather than an instrument of the exchange, so it has no product
    /// and its name IS the pair. The first step of the lookup is what keeps it reachable.
    /// </summary>
    [TestMethod]
    public void BarometerSymbolWithoutAProductIsFoundByItsPlainName()
    {
        TestExchange exchange = CreateExchange(CryptoTradingType.Perpetual);
        AddSymbol(exchange, 1, "BTC", "USDT", CryptoProduct.Perpetual);
        CryptoSymbol barometer = AddSymbol(exchange, 2, "$BMP", "USDT", "");

        Assert.IsTrue(exchange.TryGetSymbolByPair("$BMPUSDT", out CryptoSymbol? found));
        Assert.AreSame(barometer, found);
    }


    /// <summary>
    /// A pair that only exists on a market an outside party deployed is still found: HyperLiquid
    /// carries GOLD on the XYZ order book and nowhere else, and the dashboard asking for GOLDUSDC
    /// has no way of knowing that.
    /// </summary>
    [TestMethod]
    public void PairOnlyOnADeployedMarketIsFound()
    {
        TestExchange exchange = CreateExchange(CryptoTradingType.Perpetual);
        AddSymbol(exchange, 1, "BTC", "USDC", CryptoProduct.Perpetual);
        CryptoSymbol gold = AddSymbol(exchange, 2, "GOLD", "USDC", "XYZ");

        Assert.IsTrue(exchange.TryGetSymbolByPair("GOLDUSDC", out CryptoSymbol? found));
        Assert.AreSame(gold, found);
    }


    /// <summary>
    /// With the pair on both, the product of the market itself wins. HyENA runs a BTC of its own,
    /// and a caller asking for BTCUSDC means the BTC of the exchange - that is the market it is
    /// looking at.
    /// </summary>
    [TestMethod]
    public void TheMarketsOwnProductWinsFromADeployedOne()
    {
        TestExchange exchange = CreateExchange(CryptoTradingType.Perpetual);
        CryptoSymbol perp = AddSymbol(exchange, 1, "BTC", "USDC", CryptoProduct.Perpetual);
        AddSymbol(exchange, 2, "BTC", "USDC", "HYNA");

        Assert.IsTrue(exchange.TryGetSymbolByPair("BTCUSDC", out CryptoSymbol? found));
        Assert.AreSame(perp, found);
    }


    /// <summary>
    /// Two deployed markets on the same pair and no instrument of the exchange itself: the pair
    /// does not point at one symbol, so the lookup says so instead of handing back whichever came
    /// first. Answering at random would put the candles, the signal or the order on the wrong
    /// order book without a word.
    /// </summary>
    [TestMethod]
    public void APairCarriedByTwoDeployedMarketsHasNoAnswer()
    {
        TestExchange exchange = CreateExchange(CryptoTradingType.Perpetual);
        AddSymbol(exchange, 1, "ETH", "USDC", CryptoProduct.Perpetual);
        AddSymbol(exchange, 2, "BTC", "USDC", "HYNA");
        AddSymbol(exchange, 3, "BTC", "USDC", "XYZ");

        Assert.IsFalse(exchange.TryGetSymbolByPair("BTCUSDC", out CryptoSymbol? found));
        Assert.IsNull(found);
    }


    /// <summary>
    /// A pair this market does not list answers false with nothing attached, which every caller
    /// leans on: the dashboard falls back to the USDT pair, the emulator refuses the run, and the
    /// white and black list warns that the rule matches nothing.
    /// </summary>
    [TestMethod]
    public void AnUnknownPairIsNotFound()
    {
        TestExchange exchange = CreateExchange(CryptoTradingType.Perpetual);
        AddSymbol(exchange, 1, "BTC", "USDT", CryptoProduct.Perpetual);

        Assert.IsFalse(exchange.TryGetSymbolByPair("ETHUSDT", out CryptoSymbol? found));
        Assert.IsNull(found);
    }


    /// <summary>
    /// The badge beside a symbol only says something on a market that holds more than one product.
    /// On a market where every line is a perpetual it would be the same word on every row, which
    /// marks nothing and only takes space.
    /// </summary>
    [TestMethod]
    public void MarketLabelOnlyShowsOnAMarketWithSeveralProducts()
    {
        TestExchange single = CreateExchange(CryptoTradingType.Perpetual);
        CryptoSymbol lonely = AddSymbol(single, 1, "BTC", "USDT", CryptoProduct.Perpetual);

        Assert.IsFalse(single.HasSeveralProducts);
        Assert.AreEqual("", lonely.MarketLabel);

        TestExchange several = CreateExchange(CryptoTradingType.Perpetual);
        CryptoSymbol swap = AddSymbol(several, 1, "BTC", "USDT", CryptoProduct.Perpetual);
        CryptoSymbol xperp = AddSymbol(several, 2, "BTC", "USDC", CryptoProduct.XPerp);

        Assert.IsTrue(several.HasSeveralProducts);
        Assert.AreEqual("PERP", swap.MarketLabel);
        Assert.AreEqual("XPERP", xperp.MarketLabel);
    }


    /// <summary>
    /// Switching exchange has to forget the products as well. A leftover product would keep the
    /// badge on every row of a market that carries one, and send the third step of the lookup after
    /// names that are no longer there.
    /// </summary>
    [TestMethod]
    public void ClearForgetsTheProducts()
    {
        TestExchange exchange = CreateExchange(CryptoTradingType.Perpetual);
        AddSymbol(exchange, 1, "BTC", "USDT", CryptoProduct.Perpetual);
        AddSymbol(exchange, 2, "BTC", "USDC", "HYNA");
        Assert.IsTrue(exchange.HasSeveralProducts);

        exchange.Clear();

        Assert.AreEqual(0, exchange.Products.Count);
        Assert.IsFalse(exchange.HasSeveralProducts);
        Assert.IsFalse(exchange.TryGetSymbolByPair("BTCUSDT", out _));
    }
}
