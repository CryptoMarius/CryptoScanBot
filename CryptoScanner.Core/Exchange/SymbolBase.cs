using CryptoExchange.Net.Interfaces.Clients;
using CryptoExchange.Net.SharedApis;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Json;
using CryptoScanner.Core.Model;

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

        // The combination of base and quote
        public string ScannerName { get; set; } = string.Empty;
    }

    internal static void SaveExchangeInfo(object exchangeInfo, string name = "symbols.json")
    {
        // Save for debug
        try
        {
            string folderName = Path.Combine(GlobalData.AppDataFolder, ExchangeBase.ExchangeOptions.ExchangeName);
            Directory.CreateDirectory(folderName);
            string filename = Path.Combine(folderName, name);

            string text = JsonSerializer.Serialize(exchangeInfo, JsonTools.JsonSerializerIndented);
            File.WriteAllText(filename, text);
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
            string folderName = Path.Combine(GlobalData.AppDataFolder, ExchangeBase.ExchangeOptions.ExchangeName);
            Directory.CreateDirectory(folderName);
            string filename = Path.Combine(folderName, name);

            File.WriteAllText(filename, text);
        }
        catch
        {
            // ignore
        }

    }

    static internal SymbolInfo ParseSymbol(string exchangeName, string baseAsset, string quoteAsset)
    {
        var info = new SymbolInfo
        {
            Base = baseAsset,
            Quote = quoteAsset.ToUpper(),
            ExchangeName = exchangeName,
            ScannerName = baseAsset.ToUpper() + quoteAsset.ToUpper(),
        };
        return info;
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
        HashSet<string> accepted = new(acceptedScannerNames, StringComparer.OrdinalIgnoreCase);

        exchange.Data.AmbiguousSymbolNames.Clear();
        foreach (string name in rejectedScannerNames)
        {
            if (accepted.Contains(name))
                exchange.Data.AmbiguousSymbolNames.Add(name);
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
    /// mean the opposite (Kraken Futures states -2 for a contract that trades in steps of 100).
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

    static internal bool IsSymbolAccepted(Model.CryptoExchange exchange, SymbolInfo info, IRestApiClient api, TradingMode mode, out CryptoSymbol? symbol)
    {
        // Some exchanges publish instruments without a base and/or quote asset (Okx does this
        // for instruments in the "preopen" state). Those would all end up with the same empty
        // ScannerName, which makes the caller crash on a duplicate key in its symbol list.
        if (string.IsNullOrWhiteSpace(info.Base) || string.IsNullOrWhiteSpace(info.Quote))
        {
            symbol = null;
            return false;
        }

        if (!exchange.SymbolListName.TryGetValue(info.ScannerName, out symbol))
        {
            var quoteData = GlobalData.AddQuoteData(info.Quote);
            symbol = new()
            {
                Exchange = exchange,
                ExchangeId = exchange.Id,
                Name = info.ScannerName,
                Base = info.Base,
                Quote = info.Quote,
                QuoteData = quoteData,
                ExchangeName = info.ExchangeName,
                Status = 1,
            };
        }

        // Fill the new storage ExchangeName field
        symbol.ExchangeName = info.ExchangeName;
        return true;
    }
}
