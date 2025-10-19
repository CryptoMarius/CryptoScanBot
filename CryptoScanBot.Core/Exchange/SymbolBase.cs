using CryptoExchange.Net.Interfaces;
using CryptoExchange.Net.SharedApis;

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Json;
using CryptoScanBot.Core.Model;

using System.Text.Json;

namespace CryptoScanBot.Core.Exchange;

public class SymbolBase()
{
    internal class SymbolInfo
    {
        // Exchange name (can be sometimes different than base+quote)
        public string ExchangeSymbol { get; set; } = string.Empty;

        public string Base { get; set; } = string.Empty;
        public string Quote { get; set; } = string.Empty;

        // The combination of base and quote
        public string ScannerSymbol { get; set; } = string.Empty;
    }

    internal static void SaveExchangeInfo(object exchangeInfo, string name = "symbols.json")
    {
        // Save for debug
        try
        {
            string filename = GlobalData.GetBaseDir();
            filename += $@"\{ExchangeBase.ExchangeOptions.ExchangeName}\";
            Directory.CreateDirectory(filename);
            filename += name;

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
            string filename = GlobalData.GetBaseDir();
            filename += $@"\{ExchangeBase.ExchangeOptions.ExchangeName}\";
            Directory.CreateDirectory(filename);
            filename += name;

            File.WriteAllText(filename, text);
        }
        catch
        {
            // ignore
        }

    }

    static internal SymbolInfo ParseSymbol(string exchangeSymbol, string baseAsset, string quoteAsset)
    {
        var info = new SymbolInfo
        {
            Base = baseAsset,
            Quote = quoteAsset,
            ExchangeSymbol = exchangeSymbol,
            ScannerSymbol = baseAsset + quoteAsset,
        };
        return info;
    }


    static internal bool IsSymbolAccepted(Model.CryptoExchange exchange, SymbolInfo info, IRestApiClient api, TradingMode mode, out CryptoSymbol? symbol)
    {
        if (!exchange.SymbolListName.TryGetValue(info.ScannerSymbol, out symbol))
        {
            var quoteData = GlobalData.AddQuoteData(info.Quote);
            symbol = new()
            {
                Exchange = exchange,
                ExchangeId = exchange.Id,
                Name = info.ScannerSymbol,
                Base = info.Base,
                Quote = info.Quote,
                QuoteData = quoteData,
                ExchangeName = info.ExchangeSymbol,
                Status = 1,
            };
        }
        // Fix because we introduced a new storage Symbol.ExchangeName field
        symbol.ExchangeName = info.ExchangeSymbol;

        // Is it a weird symbol name? Delivery dates or other name mangling
        // **This does not work for Kraken Spot. Lets accept all symbols for now.**

        string formattedName = api.FormatSymbol(info.Base, info.Quote, mode);
        if (formattedName != info.ExchangeSymbol)
        {
#if DEBUG
            GlobalData.AddTextToLogTab($"Ignoring symbol {formattedName} {info.Base} {info.Quote} weird symbol name? {info.ExchangeSymbol}");
#endif
            return false;
        }

        return true;
    }
}
