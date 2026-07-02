using Avalonia.Controls;

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
    [Option('e', "exchange", Required = false, HelpText = "Initialize to exchange (Binance Spot, Binance Futures, Bybit Spot, ByBit Futures, Kucoin Spot or Mexc Spot)")]
    public string? ExchangeName { get { return _ExchangeName; } set { _ExchangeName = value!.Trim(); } }

    public string? _AppLimitSymbols;
    [Option('t', "test", Required = false, HelpText = "Limit the amount of symbols for testing")]
    public string? AppLimitSymbols { get { return _AppLimitSymbols; } set { _AppLimitSymbols = value!.Trim(); } }


    public static ApplicationParams? Options { get; set; }

    public static void InitApplicationOptions()
    {
        if (Options == null)
        {
            if (Design.IsDesignMode)
            {
                Options = new()
                {
                    ExchangeName = "Binance Futures",
                    AppDataFolder = Path.Combine("CryptoScanBot", "Design"),
                };
            }
            else
            {
                string[] args = Environment.GetCommandLineArgs();
                Options = Parser.Default.ParseArguments<ApplicationParams>(args).Value;
            }
            System.Diagnostics.Debug.WriteLine($"InitApplicationOptions() {Options.ExchangeName} {Options.AppDataFolder}");
        }
    }

}
