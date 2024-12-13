using CommandLine;

namespace CryptoScanBot.Core.Intern;

// Define a class to receive parsed values
public class ApplicationParams
{

    public string? _AppDataFolder;
    [Option('f', "folder", Required = false, HelpText = "De te gebruiken folder in de APPDATA")]
    public string? AppDataFolder { get { return _AppDataFolder; } set { _AppDataFolder = value!.Trim(); } }

    // Een idee? Dan hoeven we niet zo raar te switchen (soms midden in het ophalen van candles <met alle problemen van dien>)
    private string? _ExchangeName;
    [Option('e', "exchange", Required = false, HelpText = "De te gebruiken exchange (Binance Spot, Binance Futures, Bybit Spot, ByBit Futures, Kucoin Spot of Mexc Spot)")]
    public string? ExchangeName { get { return _ExchangeName; } set { _ExchangeName = value!.Trim(); } }

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
