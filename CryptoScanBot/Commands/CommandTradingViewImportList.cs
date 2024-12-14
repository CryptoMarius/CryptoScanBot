using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using System.Diagnostics;
using System.Text;

namespace CryptoScanBot.Commands;

public class CommandTradingViewImportList
{
    /// <summary>
    /// Export all the sysmbol of the exchange to TV files
    /// </summary>
    public static void ExportList()
    {
        string folder = GlobalData.GetBaseDir() + $@"\TV-Import\";
        Directory.CreateDirectory(folder);

        StringBuilder tvImportListAll = new();
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            StringBuilder tvImportList = new();
            tvImportList.AppendLine($"###{quoteData.Name} MARKETS");

            foreach (CryptoSymbol symbol in quoteData.SymbolList)
            {
                if (symbol.Status == 1 && !symbol.IsBarometerSymbol())
                {
                    // Parse the exchange code "BINANCE" or "BYBIT" + symbol and suffix..
                    //Url = "https://www.tradingview.com/chart/?symbol=BINANCE:{BASE}{QUOTE}&interval={interval}"
                    (string url, _) = GlobalData.ExternalUrls.GetExternalRef(CryptoTradingApp.TradingView, false, symbol, GlobalData.IntervalList[0]);
                    string[] subs = url.Split(new string[] { "symbol=", "&interval=" }, StringSplitOptions.None);
                    if (subs.Length > 1)
                    {
                        tvImportList.AppendLine(subs[1]);
                        tvImportListAll.AppendLine(subs[1]);
                    }
                }
            }
            string filename = folder + $"TV import {quoteData.Name}.txt";
            File.WriteAllText(filename, tvImportList.ToString());
        }

        string filenameAll = folder + $"TV import ALL.txt";
        File.WriteAllText(filenameAll, tvImportListAll.ToString());

        GlobalData.AddTextToLogTab("Generated Tradingview import files");
        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
    }

}
