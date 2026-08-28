using CryptoScanner.Core.Model;

// CryptoExchange is a package namespace as well, so the model type gets an alias here
using TestExchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// The index on the instrument name is the only one the exchange itself can address: a price ticker
/// arrives with the name the exchange gave the instrument, never with the scanner name. These tests
/// guard that the index keeps up when that name changes, which is what makes it usable as a lookup.
/// </summary>
[TestClass]
public class SymbolExchangeNameTests
{
    private static TestExchange CreateExchange() => new()
    {
        Id = 1,
        Name = "Test Perpetual",
    };

    private static CryptoSymbol AddSymbol(TestExchange exchange, int id, string name, string exchangeName)
    {
        CryptoSymbol symbol = new()
        {
            Id = id,
            Exchange = exchange,
            ExchangeId = exchange.Id,
            Name = name,
            Base = name.Replace("USDT", "").Replace("USDC", ""),
            Quote = name.EndsWith("USDC") ? "USDC" : "USDT",
            ExchangeName = exchangeName,
            QuoteData = new CryptoQuoteData { Name = name.EndsWith("USDC") ? "USDC" : "USDT" },
            Status = 1,
        };
        exchange.SymbolListId.Add(symbol.Id, symbol);
        exchange.SymbolListName.Add(symbol.Name, symbol);
        exchange.SymbolListExchangeName.Add(symbol.ExchangeName, symbol);
        return symbol;
    }


    /// <summary>
    /// A renamed instrument moves to its new key, and the old key is gone. Without this the symbol
    /// stays reachable only under a name the exchange no longer sends, so its price stops updating
    /// while nothing reports an error.
    /// </summary>
    [TestMethod]
    public void RenamedInstrumentMovesToTheNewKey()
    {
        TestExchange exchange = CreateExchange();
        CryptoSymbol symbol = AddSymbol(exchange, 1, "BTCUSDT", "BTC-USDT-SWAP");

        exchange.SetSymbolExchangeName(symbol, "BTC-USDT-SWAP-V2");

        Assert.AreEqual("BTC-USDT-SWAP-V2", symbol.ExchangeName);
        Assert.IsTrue(exchange.SymbolListExchangeName.TryGetValue("BTC-USDT-SWAP-V2", out CryptoSymbol? found));
        Assert.AreSame(symbol, found);
        Assert.IsFalse(exchange.SymbolListExchangeName.ContainsKey("BTC-USDT-SWAP"));
    }


    /// <summary>
    /// The same name twice is not a rename and must leave the index alone.
    /// </summary>
    [TestMethod]
    public void SameNameChangesNothing()
    {
        TestExchange exchange = CreateExchange();
        CryptoSymbol symbol = AddSymbol(exchange, 1, "BTCUSDT", "BTC-USDT-SWAP");

        exchange.SetSymbolExchangeName(symbol, "BTC-USDT-SWAP");

        Assert.AreEqual(1, exchange.SymbolListExchangeName.Count);
        Assert.AreSame(symbol, exchange.SymbolListExchangeName["BTC-USDT-SWAP"]);
    }


    /// <summary>
    /// A symbol that does not hold the old key must not take it away from the one that does. Two
    /// instruments can carry the same name for a moment while a market is being refreshed, and the
    /// one that is actually indexed there has to survive it.
    /// </summary>
    [TestMethod]
    public void RenameDoesNotStealAnotherSymbolsKey()
    {
        TestExchange exchange = CreateExchange();
        CryptoSymbol first = AddSymbol(exchange, 1, "BTCUSDT", "BTC-USDT-SWAP");

        // Not in any index yet, the way a symbol looks before GlobalData.AddSymbol has run
        CryptoSymbol second = new()
        {
            Id = 0,
            Exchange = exchange,
            ExchangeId = exchange.Id,
            Name = "BTCUSDC",
            Base = "BTC",
            Quote = "USDC",
            ExchangeName = "BTC-USDT-SWAP",
            QuoteData = new CryptoQuoteData { Name = "USDC" },
            Status = 1,
        };

        exchange.SetSymbolExchangeName(second, "BTC-USD_UM_XPERP-310404");

        Assert.AreEqual("BTC-USD_UM_XPERP-310404", second.ExchangeName);
        Assert.IsTrue(exchange.SymbolListExchangeName.ContainsKey("BTC-USDT-SWAP"));
        Assert.AreSame(first, exchange.SymbolListExchangeName["BTC-USDT-SWAP"]);
    }


    /// <summary>
    /// A symbol without an id is not in the indexes yet, so it must not be put in one here either.
    /// GlobalData.AddSymbol does that once the database has handed out the id, and adding it twice
    /// would throw on the duplicate key.
    /// </summary>
    [TestMethod]
    public void SymbolWithoutIdIsNotIndexed()
    {
        TestExchange exchange = CreateExchange();
        CryptoSymbol symbol = new()
        {
            Id = 0,
            Exchange = exchange,
            ExchangeId = exchange.Id,
            Name = "ETHUSDC",
            Base = "ETH",
            Quote = "USDC",
            ExchangeName = "",
            QuoteData = new CryptoQuoteData { Name = "USDC" },
            Status = 1,
        };

        exchange.SetSymbolExchangeName(symbol, "ETH-USD_UM_XPERP-310404");

        Assert.AreEqual("ETH-USD_UM_XPERP-310404", symbol.ExchangeName);
        Assert.AreEqual(0, exchange.SymbolListExchangeName.Count);
    }
}
