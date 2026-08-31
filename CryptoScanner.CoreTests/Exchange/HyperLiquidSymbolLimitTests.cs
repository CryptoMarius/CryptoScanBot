using CryptoScanner.Core.Core;
using CryptoScanner.Core.Exchange.HyperLiquid;
using CryptoScanner.Core.Model;

using System.Text.Json;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

// The namespace is deliberately NOT CryptoScanner.CoreTests.Exchange: that would shadow the
// "using Exchange = CryptoScanner.Core.Model.CryptoExchange" alias that a dozen sibling test
// files rely on, because a namespace member of the enclosing namespace wins from a using alias.
namespace CryptoScanner.CoreTests.Exchanges;

/// <summary>
/// Builds a symbol out of the meta HyperLiquid really answers with, and checks the limits that come
/// out of it. The two json files beside this one are trimmed copies of the answers the scanner
/// stored on 31-08-2026 - the perpetual one keeps eight instruments covering all four leverage
/// brackets and the whole szDecimals range, the spot one keeps six pairs including the three whose
/// size tick outweighs the minimum order value.
/// <para>
/// What this does NOT cover: that Spot/Symbol.cs and Perpetual/Symbol.cs actually call ApplyLimits,
/// and that they hand it the right fields. Both calls live inside GetSymbolsAsync, which needs the
/// exchange and the database, so only the rules and the shape of the data they are fed can be
/// pinned down here.
/// </para>
/// </summary>
[TestClass]
public class HyperLiquidSymbolLimitTests : TestBase
{
    [TestInitialize]
    public void Init() => InitTestSession();


    private sealed record Instrument(string Name, int SizeDecimals, int? MaxLeverage, decimal Price);


    private static string FixturePath(string file)
    {
        string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
            ?? throw new Exception("Error assembly");
        return Path.Combine(path, "Exchange\\HyperLiquid\\" + file);
    }


    /// <summary>The perpetual instruments, read the way the meta writes them: szDecimals and maxLeverage.</summary>
    private static List<Instrument> ReadPerpetual()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(FixturePath("perpetual-meta.json")));
        List<Instrument> result = [];
        foreach (var market in doc.RootElement.GetProperty("universe").EnumerateArray())
        {
            result.Add(new Instrument(
                market.GetProperty("name").GetString()!,
                market.GetProperty("szDecimals").GetInt32(),
                market.GetProperty("maxLeverage").GetInt32(),
                market.GetProperty("markPx").GetDecimal()));
        }
        return result;
    }


    /// <summary>
    /// The spot pairs. szDecimals belongs to the BASE token there and not to the pair, so the pair's
    /// first token index has to be looked up in the token list - the same step the package takes
    /// before it hands the reader a BaseAsset.
    /// </summary>
    private static List<Instrument> ReadSpot()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(FixturePath("spot-meta.json")));
        Dictionary<int, int> sizeDecimals = [];
        foreach (var token in doc.RootElement.GetProperty("tokens").EnumerateArray())
            sizeDecimals[token.GetProperty("index").GetInt32()] = token.GetProperty("szDecimals").GetInt32();

        List<Instrument> result = [];
        foreach (var pair in doc.RootElement.GetProperty("universe").EnumerateArray())
        {
            int baseToken = pair.GetProperty("tokens")[0].GetInt32();
            result.Add(new Instrument(
                pair.GetProperty("name").GetString()!,
                sizeDecimals[baseToken],
                null,                                   // spot has no leverage
                pair.GetProperty("midPx").GetDecimal()));
        }
        return result;
    }


    private static CryptoSymbol Build(Instrument instrument)
    {
        var exchange = new Exchange { Id = 1, Name = "HyperLiquid", FeeRate = 0.045m };
        CryptoSymbol symbol = new()
        {
            Id = 1,
            Name = instrument.Name.Replace("/", ""),
            Base = instrument.Name.Split('/')[0],
            Quote = "USDC",
            Exchange = exchange,
            ExchangeId = exchange.Id,
            ExchangeName = exchange.Name,
            QuoteData = GlobalData.AddQuoteData("USDC"),
            LastPrice = instrument.Price,
            // A leftover from an earlier refresh, to show ApplyLimits overwrites it rather than
            // leaving whatever the symbol row happened to carry.
            QuantityMaximum = 12345m,
        };
        HyperLiquidOrderLimits.ApplyLimits(symbol, instrument.SizeDecimals, instrument.MaxLeverage);
        return symbol;
    }


    private static CryptoSymbol Find(List<Instrument> instruments, string name)
        => Build(instruments.Single(i => i.Name == name));


    /// <summary>The size tick is ten to the power minus szDecimals, on both markets.</summary>
    [TestMethod]
    public void TheSizeTickComesFromSizeDecimals()
    {
        var perpetual = ReadPerpetual();
        Assert.AreEqual(0.00001m, Find(perpetual, "BTC").QuantityTickSize, "BTC, szDecimals 5");
        Assert.AreEqual(0.01m, Find(perpetual, "SOL").QuantityTickSize, "SOL, szDecimals 2");
        Assert.AreEqual(0.1m, Find(perpetual, "DOT").QuantityTickSize, "DOT, szDecimals 1");
        Assert.AreEqual(1m, Find(perpetual, "XRP").QuantityTickSize, "XRP, szDecimals 0 - whole coins");

        var spot = ReadSpot();
        Assert.AreEqual(0.00001m, Find(spot, "UBTC/USDC").QuantityTickSize, "UBTC, szDecimals 5");
        Assert.AreEqual(1m, Find(spot, "PURR/USDC").QuantityTickSize, "PURR, szDecimals 0");
    }


    /// <summary>The minimum quantity is one tick, and there is never a maximum quantity.</summary>
    [TestMethod]
    public void TheMinimumQuantityIsOneTickAndThereIsNoMaximum()
    {
        foreach (var instrument in ReadPerpetual().Concat(ReadSpot()))
        {
            CryptoSymbol symbol = Build(instrument);
            Assert.AreEqual(symbol.QuantityTickSize, symbol.QuantityMinimum, $"{instrument.Name} minimum quantity");
            Assert.AreEqual(0m, symbol.QuantityMaximum, $"{instrument.Name} maximum quantity");
        }
    }


    /// <summary>Ten in the quote currency, on every instrument of both markets.</summary>
    [TestMethod]
    public void EveryInstrumentCarriesTheMinimumOrderValue()
    {
        foreach (var instrument in ReadPerpetual().Concat(ReadSpot()))
            Assert.AreEqual(10m, Build(instrument).QuoteValueMinimum, instrument.Name);
    }


    /// <summary>
    /// A perpetual instrument gets a maximum order value out of its own maxLeverage; a spot pair
    /// gets none, because HyperLiquid states none for spot.
    /// </summary>
    [TestMethod]
    public void OnlyAPerpetualInstrumentGetsAMaximumOrderValue()
    {
        var perpetual = ReadPerpetual();
        Assert.AreEqual(300_000_000m, Find(perpetual, "BTC").QuoteValueMaximum, "BTC, leverage 40");
        Assert.AreEqual(300_000_000m, Find(perpetual, "ETH").QuoteValueMaximum, "ETH, leverage 25");
        Assert.AreEqual(50_000_000m, Find(perpetual, "SOL").QuoteValueMaximum, "SOL, leverage 20");
        Assert.AreEqual(20_000_000m, Find(perpetual, "HYPE").QuoteValueMaximum, "HYPE, leverage 10");
        Assert.AreEqual(5_000_000m, Find(perpetual, "ATOM").QuoteValueMaximum, "ATOM, leverage 5");

        foreach (var instrument in ReadSpot())
            Assert.AreEqual(0m, Build(instrument).QuoteValueMaximum, $"{instrument.Name} has no maximum");
    }


    /// <summary>
    /// The point of storing both: the minimum entry is the larger of one tick and the minimum order
    /// value. On every perpetual instrument the ten wins; on spot XAUT0 and TSLA are the exceptions,
    /// and there one tick alone is worth more than the minimum order value.
    /// </summary>
    [TestMethod]
    public void TheMinimumEntryIsTheLargerOfTheTwo()
    {
        foreach (var instrument in ReadPerpetual())
        {
            CryptoSymbol symbol = Build(instrument);
            Assert.IsTrue(symbol.QuantityTickSize * instrument.Price < 10m,
                $"{instrument.Name}: one tick is worth {symbol.QuantityTickSize * instrument.Price}, so the ten should decide");
            Assert.AreEqual(10m, MinEntry(symbol), instrument.Name);
        }

        var spot = ReadSpot();
        Assert.AreEqual(44.6215m, MinEntry(Find(spot, "XAUT0/USDC")), "XAUT0/USDC, a tick of 0.01 at 4462.15");
        Assert.AreEqual(15.80m, MinEntry(Find(spot, "TSLA/USDC")), "TSLA/USDC, a tick of 0.01 at 1580");
        Assert.AreEqual(10m, MinEntry(Find(spot, "HYPE/USDC")), "HYPE/USDC, where the ten decides");
    }


    /// <summary>The minimum entry as the grids show it, straight off a signal on this symbol.</summary>
    private static decimal MinEntry(CryptoSymbol symbol)
    {
        CryptoSignal signal = new()
        {
            Exchange = symbol.Exchange,
            ExchangeId = symbol.ExchangeId,
            Symbol = symbol,
            SymbolId = symbol.Id,
            Interval = GlobalData.IntervalListId.Count > 0
                ? GlobalData.IntervalListId.Values[0]
                : new CryptoInterval { Id = 1, Name = "1m", Duration = 1 },
            IntervalId = 1,
            Candle = null,
        };
        return signal.MinEntry;
    }
}
