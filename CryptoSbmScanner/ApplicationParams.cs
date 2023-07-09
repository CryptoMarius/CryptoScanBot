using CommandLine;

namespace CryptoSbmScanner;

// Define a class to receive parsed values
class ApplicationParams
{
    static public ApplicationParams Options { get; set; }


    [Option('f', "folder", Required = false, HelpText = "De te gebruiken folder in de APPDATA")]
    public string InputFile { get; set; } = "CryptoScanner";

    // Een idee? Dan hoeven we niet zo raar te switchen (soms midden in het ophalen van candles <met alle problemen van dien>)
    [Option('e', "exchange", Required = false, HelpText = "De te gebruiken exchange")]
    public string Exchange { get; set; } = "Binance";


}
