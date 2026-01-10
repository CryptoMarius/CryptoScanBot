using CryptoExchange.Net.Interfaces;
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

    static internal bool IsSymbolAccepted(Model.CryptoExchange exchange, SymbolInfo info, IRestApiClient api, TradingMode mode, out CryptoSymbol? symbol)
    {
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
