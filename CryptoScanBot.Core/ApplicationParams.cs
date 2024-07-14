using CommandLine;

namespace CryptoScanBot.Core;

// Define a class to receive parsed values
public class ApplicationParams
{

    [Option('f', "folder", Required = false, HelpText = "De te gebruiken folder in de APPDATA")]
    public string? AppDataFolder { get; set; }

    // Een idee? Dan hoeven we niet zo raar te switchen (soms midden in het ophalen van candles <met alle problemen van dien>)
    [Option('e', "exchange", Required = false, HelpText = "De te gebruiken exchange (Binance Spot, Binance Futures, Bybit Spot, ByBit Futures, Mexc Spot of Kucoin Spot")]
    public string? ExchangeName { get; set; }

    static public ApplicationParams? Options { get; set; }

    public static void InitApplicationOptions()
    {
        if (Options == null)
        {
            string[] args = Environment.GetCommandLineArgs();
            Options = Parser.Default.ParseArguments<ApplicationParams>(args).Value;
        }
    }

}
