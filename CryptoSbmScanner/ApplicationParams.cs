using CommandLine;

namespace CryptoSbmScanner;

// Define a class to receive parsed values
class ApplicationParams
{

    [Option('f', "folder", Required = false, HelpText = "De te gebruiken folder in de APPDATA")]
    public string AppDataFolder { get; set; }

    // Een idee? Dan hoeven we niet zo raar te switchen (soms midden in het ophalen van candles <met alle problemen van dien>)
    [Option('e', "exchange", Required = false, HelpText = "De te gebruiken exchange (Binance, Bybit Spot, ByBit Futures of Kucoin")]
    public string ExchangeName { get; set; }


    static public ApplicationParams Options { get; set; }

    public static void InitApplicationOptions()
    {
        if (Options == null)
        {
            string[] args = Environment.GetCommandLineArgs();
            Options = Parser.Default.ParseArguments<ApplicationParams>(args).Value;
        }
    }

}
