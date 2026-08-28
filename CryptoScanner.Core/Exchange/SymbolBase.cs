using CryptoExchange.Net.Interfaces.Clients;
using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Json;
using CryptoScanner.Core.Model;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace CryptoScanner.Core.Exchange;

public class SymbolBase()
{
    internal class SymbolInfo
    {
        // Exchange name (can be sometimes different than base+quote)
        public string ExchangeName { get; set; } = string.Empty;

        public string Base { get; set; } = string.Empty;
        public string Quote { get; set; } = string.Empty;

        // Which instrument this is: a code from CryptoProduct, or the market an outside party deployed
        public string Product { get; set; } = string.Empty;

        // Base, quote and the product: BTCUSDT.PERP
        public string ScannerName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Where the exchange dumps (symbols.json, tickers.json) go: straight into the data folder.
    /// They used to sit in a subfolder named after the market, which said nothing extra - a data
    /// folder serves one market. Subfolders left by an older version can be deleted by hand.
    /// </summary>
    private static string ExchangeInfoFileName(string name)
    {
        return Path.Combine(GlobalData.AppDataFolder, name);
    }

    internal static void SaveExchangeInfo(object exchangeInfo, string name = "symbols.json")
    {
        // Save for debug
        try
        {
            string text = JsonSerializer.Serialize(exchangeInfo, JsonTools.JsonSerializerIndented);
            File.WriteAllText(ExchangeInfoFileName(name), text);
        }
        catch
        {
            // ignore
        }

    }

    internal static void SaveExchangeInfo(string? text, string name = "symbols.json")
    {
        if (text == null)
            return;

        // Reformat (all on 1 line)
        text = JsonTools.FormatJson(text);

        // Save for debug
        try
        {
            File.WriteAllText(ExchangeInfoFileName(name), text);
        }
        catch
        {
            // ignore
        }

    }

    /// <summary>
    /// Turns one instrument of an exchange into the four things the scanner keeps about it.
    /// <para>
    /// The scanner name carries the product behind a dot: BTCUSDT.PERP, BTCUSDT.SPOT, BTCUSDC.HYNA.
    /// That is what makes it unique. An exchange can offer the same pair as several instruments -
    /// BTC-USDT next to BTC-USDT-SWAP - and both parse to the pair BTCUSDT; without the product the
    /// second one silently disappears in GlobalData.AddSymbol and the market ends up holding candles
    /// that belong to the other instrument.
    /// </para>
    /// <para>
    /// It also means the product is readable wherever the name travels and the badge does not come
    /// along: a log line, the clipboard, the black and white list, an Altrady link.
    /// </para>
    /// </summary>
    /// <param name="exchangeName">The name the exchange gave the instrument, unique at that exchange.</param>
    /// <param name="product">One of <see cref="CryptoProduct"/>, or the name of the market an outside party deployed.</param>
    static internal SymbolInfo ParseSymbol(string exchangeName, string baseAsset, string quoteAsset, string product)
    {
        string pair = baseAsset.ToUpper() + quoteAsset.ToUpper();
        var info = new SymbolInfo
        {
            Base = baseAsset,
            Quote = quoteAsset.ToUpper(),
            ExchangeName = exchangeName,
            Product = product.ToUpper(),
            ScannerName = product.Length > 0 ? pair + CryptoProduct.Separator + product.ToUpper() : pair,
        };
        return info;
    }


    /// <summary>
    /// The product code that belongs to the market this instance serves, for the markets that offer
    /// one product and say so through their trading type. A market that carries several products -
    /// Okx Perpetual holds the swaps and the X-Perps - names the product per instrument instead.
    /// </summary>
    static internal string ProductOfExchange(Model.CryptoExchange exchange)
    {
        // One mapping, kept on the exchange so the pair lookup uses the exact same one
        return exchange.DefaultProduct;
    }

    /// <summary>
    /// Records which scanner names cover more than one instrument on this exchange, by intersecting
    /// the names of the instruments that were REJECTED with the names that were accepted. A dated
    /// delivery contract carries the same base and quote as its perpetual, so both produce the same
    /// scanner name and candles stored under that name cannot be attributed to either one.
    /// Call once per <c>GetSymbolsAsync</c>, after its loop.
    /// That is also why the callers assign into their activeSymbols list instead of adding to it: two
    /// instruments sharing one scanner name would otherwise throw, and roll back the whole update.
    /// </summary>
    static internal void RegisterAmbiguousSymbolNames(Model.CryptoExchange exchange,
        IEnumerable<string> rejectedScannerNames, IEnumerable<string> acceptedScannerNames)
    {
        // Compared in PAIR space, without the product suffix. The version-2 candle files this set
        // protects were written before the product moved into the name, so their rows carry the
        // bare pair - and the callers hand in a mix as well: rejected names are usually built as
        // base + quote while accepted names carry the suffix. Comparing the raw strings would
        // therefore never intersect and silently record nothing.
        static string PairOf(string name)
        {
            int dot = name.IndexOf(CryptoProduct.Separator);
            return dot > 0 ? name[..dot] : name;
        }

        HashSet<string> accepted = new(StringComparer.OrdinalIgnoreCase);
        foreach (string name in acceptedScannerNames)
            accepted.Add(PairOf(name));

        exchange.Data.AmbiguousSymbolNames.Clear();
        foreach (string name in rejectedScannerNames)
        {
            string pair = PairOf(name);
            if (accepted.Contains(pair))
                exchange.Data.AmbiguousSymbolNames.Add(pair);
        }

        if (exchange.Data.AmbiguousSymbolNames.Count != 0)
        {
            string names = string.Join(',', exchange.Data.AmbiguousSymbolNames.OrderBy(x => x));
            ScannerLog.Logger.Trace($"{exchange.Name} symbols covering more than one instrument: {names}");
        }
    }


    /// <summary>
    /// Turns a number of decimals into a tick size (3 -> 0.001). Multiplying stays exact in decimal,
    /// where the Math.Pow detour would go through a double first. Negative decimals are valid and
    /// mean the opposite (Kraken Perpetual states -2 for a contract that trades in steps of 100).
    /// <para>
    /// Exchanges state their precision either way: as a tick size (Binance stepSize, Okx lotSize) or
    /// as a number of decimals (Kraken lot_decimals, HyperLiquid szDecimals). Only the first kind can
    /// be assigned straight to PriceTickSize/QuantityTickSize - a number of decimals has to come
    /// through here first. Assigning it directly is silently wrong instead of loudly wrong, because
    /// GlobalData derives PriceDecimals from the digits AFTER the decimal point of the tick size, so
    /// a "8" arrives as zero decimals and every price ends up rounded to a whole number.
    /// </para>
    /// <para>
    /// Capped at 15 decimals because CryptoCandle keeps its tick decimals in a nibble, so a finer
    /// tick size than that would wrap around there instead of being stored.
    /// </para>
    /// </summary>
    static internal decimal TickSizeFromDecimals(int decimals)
    {
        if (decimals > 15)
            decimals = 15;

        decimal tickSize = 1m;
        for (int i = 0; i < decimals; i++)
            tickSize *= 0.1m;
        for (int i = 0; i > decimals; i--)
            tickSize *= 10m;
        return tickSize;
    }

    /// <summary>
    /// Caps a number of price decimals so a price of this size still fits in the int that
    /// CryptoCandle keeps its prices in (see CryptoCandle._openTicks: the value stored is
    /// price / tickSize). Beyond that the cast wraps around without an exception and the candle
    /// silently holds a nonsense price, which is worse than losing a digit of precision.
    /// A reference price of zero leaves the decimals alone - there is nothing to measure against.
    /// </summary>
    static internal int LimitDecimalsToCandleRange(int decimals, decimal referencePrice)
    {
        if (referencePrice <= 0)
            return decimals;

        while (decimals > 0 && referencePrice / TickSizeFromDecimals(decimals) > int.MaxValue)
            decimals--;
        return decimals;
    }

    static internal bool IsSymbolAccepted(Model.CryptoExchange exchange, SymbolInfo info, IRestApiClient api, TradingMode mode, [NotNullWhen(true)] out CryptoSymbol? symbol)
    {
        // Some exchanges publish instruments without a base and/or quote asset (Okx does this
        // for instruments in the "preopen" state). Those would all end up with the same empty
        // ScannerName, which makes the caller crash on a duplicate key in its symbol list.
        if (string.IsNullOrWhiteSpace(info.Base) || string.IsNullOrWhiteSpace(info.Quote))
        {
            symbol = null;
            return false;
        }

        // Recognised by the name the EXCHANGE gave the instrument, not by the scanner name. Those two
        // are not the same question: an exchange can offer one pair as several instruments, and every
        // one of them parses to the same pair. Looking up by the scanner name handed back the symbol
        // of the OTHER instrument and then overwrote its instrument name, so the market kept fetching
        // candles for something else while the database looked perfectly normal. The instrument name
        // is unique at every exchange the scanner talks to - checked on 28-08-2026 over the complete
        // answer of all of them, including the products we filter out.
        if (!exchange.SymbolListExchangeName.TryGetValue(info.ExchangeName, out symbol))
        {
            var quoteData = GlobalData.AddQuoteData(info.Quote);
            symbol = new()
            {
                Exchange = exchange,
                ExchangeId = exchange.Id,
                Name = info.ScannerName,
                Base = info.Base,
                Quote = info.Quote,
                Product = info.Product,
                QuoteData = quoteData,
                ExchangeName = info.ExchangeName,
                Status = 1,
            };
        }

        // An instrument that changed pair or product keeps its identity and gets the new name. Rare,
        // but a name that no longer matches what the exchange says is worse than a renamed symbol.
        // Through the exchange so the scanner-name index moves along with the rename.
        symbol.Base = info.Base;
        symbol.Quote = info.Quote;
        symbol.Product = info.Product;
        exchange.SetSymbolName(symbol, info.ScannerName);

        // Fill the new storage ExchangeName field, through the exchange so the index on that name
        // moves along. A price ticker is looked up by the name the exchange uses, so an index left
        // on the old key means the symbol stops being found.
        exchange.SetSymbolExchangeName(symbol, info.ExchangeName);
        return true;
    }
}
