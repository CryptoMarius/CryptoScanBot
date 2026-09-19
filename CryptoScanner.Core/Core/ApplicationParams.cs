using CommandLine;

namespace CryptoScanner.Core.Core;

///
/// A class to parse the application arguments
///
public class ApplicationParams
{

    public string? _AppDataFolder;
    [Option('f', "folder", Required = false, HelpText = "Use this folder a the datafolder for the scanner")]
    public string? AppDataFolder { get { return _AppDataFolder; } set { _AppDataFolder = value!.Trim().Trim('"'); } }

    private string? _ExchangeName;
    // No list of names here, it went stale as soon as an exchange was added or switched off
    [Option('e', "exchange", Required = false, HelpText = "Initialize to exchange, under the name it is registered with (\"Bybit Spot\", \"Binance Perpetual\", ...). Which markets are available is in CryptoDatabase.CreateExchangeList; a name that is unknown or switched off stops the scanner")]
    public string? ExchangeName { get { return _ExchangeName; } set { _ExchangeName = value!.Trim(); } }

    public string? _AppLimitSymbols;
    [Option('t', "test", Required = false, HelpText = "Limit the amount of symbols for testing")]
    public string? AppLimitSymbols { get { return _AppLimitSymbols; } set { _AppLimitSymbols = value!.Trim(); } }

    // Only the web host acts on this. It lives here because the data folder option lives here too,
    // and one scanner instance is one data folder plus one port: several instances on the same
    // machine cannot all listen on the default port.
    [Option('p', "port", Required = false, HelpText = "Port the web front end listens on (default 5000). Every instance running at the same time needs its own port")]
    public int? WebPort { get; set; }


    public static ApplicationParams? Options { get; set; }

    public static bool IsDesignMode { get; set; }

    public static void InitApplicationOptions()
    {
        if (Options == null)
        {
            if (IsDesignMode)
            {
                Options = new()
                {
                    ExchangeName = "Binance Perpetual",
                    AppDataFolder = Path.Combine("CryptoScanBot", "Design"),
                };
            }
            else
            {
                string[] args = Environment.GetCommandLineArgs();
                Options = Parser.Default.ParseArguments<ApplicationParams>(args).Value;

                // A rejected command line leaves Value null and the line below then died on a
                // NullReferenceException that named nothing. The parser has already printed what it
                // choked on, so all that is missing is a sentence saying the application stops.
                // Carrying on is not an option: without the -f value the instance would quietly
                // start on the DEFAULT data folder, which is exactly the folder it must never use.
                if (Options == null)
                    throw new ArgumentException("Could not parse the command line arguments, see the errors above");
            }
            System.Diagnostics.Debug.WriteLine($"InitApplicationOptions() {Options.ExchangeName} {Options.AppDataFolder}");
        }
    }

}
