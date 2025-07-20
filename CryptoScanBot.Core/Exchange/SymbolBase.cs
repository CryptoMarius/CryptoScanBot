using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Json;

using System.Text.Json;

namespace CryptoScanBot.Core.Exchange;

public class SymbolBase()
{
    internal static void SaveExchangeInfo(object exchangeInfo)
    {
        // Save for debug
        try
        {
            string filename = GlobalData.GetBaseDir();
            filename += $@"\{ExchangeBase.ExchangeOptions.ExchangeName}\";
            Directory.CreateDirectory(filename);
            filename += "symbols.json";

            string text = JsonSerializer.Serialize(exchangeInfo, JsonTools.JsonSerializerIndented);
            File.WriteAllText(filename, text);
        }
        catch
        {
            // ignore
        }

    }

}
